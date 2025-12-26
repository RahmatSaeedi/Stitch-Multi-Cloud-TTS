using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using SpeechApp.Models;
using SpeechApp.Models.Providers;
using SpeechApp.Services.Interfaces;

namespace SpeechApp.Services.Providers;

public class DeepgramProvider : ITTSProvider
{
    private readonly HttpClient _httpClient;
    private readonly IStorageService _storageService;
    private string? _apiKey;
    private List<Voice>? _cachedVoices;
    private DateTime? _cacheExpiry;

    private const string PROVIDER_ID = "deepgram";
    private const string BASE_URL = "https://api.deepgram.com/v1";
    private const int MAX_CHARACTERS = 2000;
    private const decimal COST_PER_CHAR = 0.000015m;
    private const int CACHE_DURATION_DAYS = 14;

    public DeepgramProvider(HttpClient httpClient, IStorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;
    }

    public ProviderInfo GetProviderInfo()
    {
        return new ProviderInfo
        {
            Id = PROVIDER_ID,
            Name = "Deepgram",
            DisplayName = "Deepgram Text-to-Speech",
            MaxCharacterLimit = MAX_CHARACTERS,
            RequiresApiKey = true,
            SupportsSSML = false,
            SetupGuideUrl = "/help#deepgram",
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

        if (string.IsNullOrEmpty(_apiKey))
        {
            _apiKey = (await _storageService.GetApiKeyAsync(PROVIDER_ID))?.Trim();
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = "API key not configured. Please add your Deepgram API key in settings."
                };
            }
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Deepgram uses query parameters for configuration
            var model = config.VoiceId ?? "aura-asteria-en";
            var url = $"{BASE_URL}/speak?model={model}&encoding=mp3";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("Authorization", $"Token {_apiKey}");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(new { text = text }),
                Encoding.UTF8,
                "application/json"
            );

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = $"Deepgram API error: {response.StatusCode} - {errorContent}"
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

    public Task<List<Voice>> GetVoicesAsync(bool bypassCache = false, CancellationToken cancellationToken = default)
    {
        // Check cache first
        if (!bypassCache && _cachedVoices != null && _cacheExpiry.HasValue && DateTime.UtcNow < _cacheExpiry.Value)
        {
            return Task.FromResult(_cachedVoices);
        }

        // Deepgram Aura voices (as of 2024)
        var voices = new List<Voice>
        {
            new Voice { Id = "aura-asteria-en", Name = "Asteria", Language = "English", LanguageCode = "en", Gender = "FEMALE", Quality = "Premium", ProviderId = PROVIDER_ID },
            new Voice { Id = "aura-luna-en", Name = "Luna", Language = "English", LanguageCode = "en", Gender = "FEMALE", Quality = "Premium", ProviderId = PROVIDER_ID },
            new Voice { Id = "aura-stella-en", Name = "Stella", Language = "English", LanguageCode = "en", Gender = "FEMALE", Quality = "Premium", ProviderId = PROVIDER_ID },
            new Voice { Id = "aura-athena-en", Name = "Athena", Language = "English", LanguageCode = "en", Gender = "FEMALE", Quality = "Premium", ProviderId = PROVIDER_ID },
            new Voice { Id = "aura-hera-en", Name = "Hera", Language = "English", LanguageCode = "en", Gender = "FEMALE", Quality = "Premium", ProviderId = PROVIDER_ID },
            new Voice { Id = "aura-orion-en", Name = "Orion", Language = "English", LanguageCode = "en", Gender = "MALE", Quality = "Premium", ProviderId = PROVIDER_ID },
            new Voice { Id = "aura-arcas-en", Name = "Arcas", Language = "English", LanguageCode = "en", Gender = "MALE", Quality = "Premium", ProviderId = PROVIDER_ID },
            new Voice { Id = "aura-perseus-en", Name = "Perseus", Language = "English", LanguageCode = "en", Gender = "MALE", Quality = "Premium", ProviderId = PROVIDER_ID },
            new Voice { Id = "aura-angus-en", Name = "Angus", Language = "English", LanguageCode = "en", Gender = "MALE", Quality = "Premium", ProviderId = PROVIDER_ID },
            new Voice { Id = "aura-orpheus-en", Name = "Orpheus", Language = "English", LanguageCode = "en", Gender = "MALE", Quality = "Premium", ProviderId = PROVIDER_ID },
            new Voice { Id = "aura-helios-en", Name = "Helios", Language = "English", LanguageCode = "en", Gender = "MALE", Quality = "Premium", ProviderId = PROVIDER_ID },
            new Voice { Id = "aura-zeus-en", Name = "Zeus", Language = "English", LanguageCode = "en", Gender = "MALE", Quality = "Premium", ProviderId = PROVIDER_ID }
        };

        // Cache the results
        _cachedVoices = voices;
        _cacheExpiry = DateTime.UtcNow.AddDays(CACHE_DURATION_DAYS);

        return Task.FromResult(voices);
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        try
        {
            // Trim whitespace from API key
            apiKey = apiKey.Trim();

            // Try a small synthesis request to validate
            var httpRequest = new HttpRequestMessage(HttpMethod.Post, $"{BASE_URL}/speak?model=aura-asteria-en");
            httpRequest.Headers.Add("Authorization", $"Token {apiKey}");
            httpRequest.Content = new StringContent(
                JsonSerializer.Serialize(new { text = "test" }),
                Encoding.UTF8,
                "application/json"
            );

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
        return characterCount * COST_PER_CHAR;
    }

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey?.Trim();
    }
}
