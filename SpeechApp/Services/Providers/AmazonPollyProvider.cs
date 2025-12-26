using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SpeechApp.Models;
using SpeechApp.Models.Providers;
using SpeechApp.Services.Interfaces;

namespace SpeechApp.Services.Providers;

public class AmazonPollyProvider : ITTSProvider
{
    private readonly HttpClient _httpClient;
    private readonly IStorageService _storageService;
    private string? _accessKeyId;
    private string? _secretAccessKey;
    private string? _region;
    private List<Voice>? _cachedVoices;
    private DateTime? _cacheExpiry;

    private const string PROVIDER_ID = "polly";
    private const int MAX_CHARACTERS = 3000;
    private const decimal COST_PER_CHAR_STANDARD = 0.000004m;
    private const decimal COST_PER_CHAR_NEURAL = 0.000016m;
    private const int CACHE_DURATION_DAYS = 14;
    private const string DEFAULT_REGION = "us-east-1";

    public AmazonPollyProvider(HttpClient httpClient, IStorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;
        _region = DEFAULT_REGION;
    }

    public ProviderInfo GetProviderInfo()
    {
        return new ProviderInfo
        {
            Id = PROVIDER_ID,
            Name = "Amazon Polly",
            DisplayName = "Amazon Polly TTS",
            MaxCharacterLimit = MAX_CHARACTERS,
            RequiresApiKey = true,
            SupportsSSML = true,
            SetupGuideUrl = "/help#polly",
            Health = ProviderHealth.Unknown
        };
    }

    public async Task<SynthesisResult> SynthesizeSpeechAsync(string text, VoiceConfig config, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = "Text cannot be empty"
            };
        }

        if (text.Length > MAX_CHARACTERS)
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = $"Text exceeds maximum length of {MAX_CHARACTERS} characters"
            };
        }

        await EnsureCredentialsLoadedAsync();

        if (string.IsNullOrEmpty(_accessKeyId) || string.IsNullOrEmpty(_secretAccessKey))
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = "AWS credentials not configured. Please add your Access Key ID and Secret Access Key in settings."
            };
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Determine engine and language code from voice config
            var engine = DetermineEngine(config);
            var languageCode = ExtractLanguageCode(config.VoiceId);

            var requestBody = new PollySynthesisRequest
            {
                Engine = engine,
                LanguageCode = languageCode,
                OutputFormat = "mp3",
                Text = text,
                TextType = "text",
                VoiceId = config.VoiceId
            };

            var url = $"https://polly.{_region}.amazonaws.com/v1/speech";
            var jsonPayload = JsonSerializer.Serialize(requestBody);
            var payloadBytes = Encoding.UTF8.GetBytes(jsonPayload);

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Content = new ByteArrayContent(payloadBytes);
            httpRequest.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/json");

            // Sign request with AWS Signature V4
            AwsSignatureV4.SignRequest(httpRequest, _accessKeyId, _secretAccessKey, _region ?? DEFAULT_REGION, "polly", payloadBytes);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = $"Amazon Polly API error: {response.StatusCode} - {errorContent}"
                };
            }

            var audioData = await response.Content.ReadAsByteArrayAsync(cancellationToken);
            var duration = DateTime.UtcNow - startTime;

            return new SynthesisResult
            {
                Success = true,
                AudioData = audioData,
                CharactersProcessed = text.Length,
                Cost = CalculateCost(text.Length, config),
                Duration = duration
            };
        }
        catch (HttpRequestException ex)
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = $"Network error: {ex.Message}"
            };
        }
        catch (Exception ex)
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = $"Synthesis failed: {ex.Message}"
            };
        }
    }

    public async Task<List<Voice>> GetVoicesAsync(bool bypassCache = false, CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (!bypassCache && _cachedVoices != null && _cacheExpiry.HasValue && DateTime.UtcNow < _cacheExpiry.Value)
        {
            return _cachedVoices;
        }

        await EnsureCredentialsLoadedAsync();

        if (string.IsNullOrEmpty(_accessKeyId) || string.IsNullOrEmpty(_secretAccessKey))
        {
            return new List<Voice>();
        }

        try
        {
            var url = $"https://polly.{_region}.amazonaws.com/v1/voices";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

            // Sign request with AWS Signature V4
            AwsSignatureV4.SignRequest(httpRequest, _accessKeyId, _secretAccessKey, _region ?? DEFAULT_REGION, "polly");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new List<Voice>();
            }

            var pollyResponse = await response.Content.ReadFromJsonAsync<PollyDescribeVoicesResponse>(cancellationToken: cancellationToken);

            if (pollyResponse?.Voices == null)
            {
                return new List<Voice>();
            }

            var voices = pollyResponse.Voices.Select(pv => new Voice
            {
                Id = pv.Id,
                Name = pv.Name,
                Language = pv.LanguageName,
                LanguageCode = pv.LanguageCode,
                Gender = pv.Gender?.ToUpper(),
                Quality = DetermineQuality(pv.SupportedEngines),
                ProviderId = PROVIDER_ID,
                Metadata = new Dictionary<string, object>
                {
                    ["SupportedEngines"] = pv.SupportedEngines ?? new List<string>(),
                    ["AdditionalLanguageCodes"] = pv.AdditionalLanguageCodes ?? new List<string>()
                }
            }).ToList();

            // Cache the results
            _cachedVoices = voices;
            _cacheExpiry = DateTime.UtcNow.AddDays(CACHE_DURATION_DAYS);

            return voices;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching Polly voices: {ex.Message}");
            return new List<Voice>();
        }
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        // API key format for Polly: AccessKeyId:SecretAccessKey
        apiKey = apiKey.Trim();
        var parts = apiKey.Split(':', 2);
        if (parts.Length != 2)
        {
            return false;
        }

        var accessKeyId = parts[0].Trim();
        var secretAccessKey = parts[1].Trim();

        try
        {
            var url = $"https://polly.{_region}.amazonaws.com/v1/voices";

            var httpRequest = new HttpRequestMessage(HttpMethod.Get, url);

            AwsSignatureV4.SignRequest(httpRequest, accessKeyId, secretAccessKey, _region ?? DEFAULT_REGION, "polly");

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
        }
    }

    public int GetMaxCharacterLimit()
    {
        return MAX_CHARACTERS;
    }

    public decimal CalculateCost(int characterCount, VoiceConfig? config = null)
    {
        // Determine if using neural or standard engine
        var engine = config != null ? DetermineEngine(config) : "standard";
        var costPerChar = engine == "neural" ? COST_PER_CHAR_NEURAL : COST_PER_CHAR_STANDARD;
        return characterCount * costPerChar;
    }

    public void SetApiKey(string apiKey)
    {
        // API key format: AccessKeyId:SecretAccessKey
        apiKey = apiKey?.Trim() ?? string.Empty;
        var parts = apiKey.Split(':', 2);
        if (parts.Length == 2)
        {
            _accessKeyId = parts[0].Trim();
            _secretAccessKey = parts[1].Trim();
        }
    }

    public void SetRegion(string region)
    {
        _region = region;
    }

    private async Task EnsureCredentialsLoadedAsync()
    {
        if (string.IsNullOrEmpty(_accessKeyId) || string.IsNullOrEmpty(_secretAccessKey))
        {
            var apiKey = await _storageService.GetApiKeyAsync(PROVIDER_ID);
            if (!string.IsNullOrEmpty(apiKey))
            {
                SetApiKey(apiKey);
            }
        }
    }

    private string DetermineEngine(VoiceConfig config)
    {
        // Check if voice options specify engine
        if (config.ProviderSpecificOptions != null && config.ProviderSpecificOptions.ContainsKey("Engine"))
        {
            return config.ProviderSpecificOptions["Engine"]?.ToString() ?? "standard";
        }

        // Default to neural if available, otherwise standard
        return "neural";
    }

    private string ExtractLanguageCode(string voiceId)
    {
        // Polly voice IDs often contain language hints
        // Common patterns: en-US, en-GB, es-ES, etc.
        // Default to en-US if not determinable
        return "en-US";
    }

    private string DetermineQuality(List<string>? supportedEngines)
    {
        if (supportedEngines == null || !supportedEngines.Any())
            return "Standard";

        if (supportedEngines.Contains("generative"))
            return "Generative";
        if (supportedEngines.Contains("long-form"))
            return "Long-form";
        if (supportedEngines.Contains("neural"))
            return "Neural";

        return "Standard";
    }
}
