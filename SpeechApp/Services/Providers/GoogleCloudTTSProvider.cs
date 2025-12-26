using System.Net.Http.Json;
using System.Text.Json;
using SpeechApp.Models;
using SpeechApp.Models.Providers;
using SpeechApp.Services.Interfaces;

namespace SpeechApp.Services.Providers;

public class GoogleCloudTTSProvider : ITTSProvider
{
    private readonly HttpClient _httpClient;
    private readonly IStorageService _storageService;
    private string? _apiKey;
    private List<Voice>? _cachedVoices;
    private DateTime? _cacheExpiry;

    private const string PROVIDER_ID = "google";
    private const string BASE_URL = "https://texttospeech.googleapis.com/v1";
    private const string BASE_URL_BETA = "https://texttospeech.googleapis.com/v1beta1";
    private const int MAX_CHARACTERS = 5000;
    private const decimal COST_PER_CHAR_NEURAL = 0.000016m;
    private const decimal COST_PER_CHAR_STANDARD = 0.000004m;
    private const int CACHE_DURATION_DAYS = 14;

    public GoogleCloudTTSProvider(HttpClient httpClient, IStorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;
    }

    public ProviderInfo GetProviderInfo()
    {
        return new ProviderInfo
        {
            Id = PROVIDER_ID,
            Name = "Google Cloud TTS",
            DisplayName = "Google Cloud Text-to-Speech",
            MaxCharacterLimit = MAX_CHARACTERS,
            RequiresApiKey = true,
            SupportsSSML = true,
            SetupGuideUrl = "/help#google",
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
            _apiKey = await _storageService.GetApiKeyAsync(PROVIDER_ID);
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = "API key not configured. Please add your Google Cloud API key in settings."
                };
            }
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Parse voice ID to get language code and voice name
            // Format: "languageCode/voiceName" e.g., "en-US/en-US-Neural2-A"
            var voiceParts = config.VoiceId.Split('/');
            var languageCode = voiceParts.Length > 0 ? voiceParts[0] : "en-US";
            var voiceName = voiceParts.Length > 1 ? voiceParts[1] : config.VoiceId;

            // Use v1beta1 API for all voices
            // The voice name itself contains all necessary information
            string apiBaseUrl = BASE_URL_BETA;

            var request = new GoogleSynthesisRequest
            {
                Input = new GoogleSynthesisInput
                {
                    Text = text
                },
                Voice = new GoogleVoiceSelection
                {
                    LanguageCode = languageCode,
                    Name = voiceName
                    // No model field - voice name contains all info
                },
                AudioConfig = new GoogleAudioConfig
                {
                    AudioEncoding = config.OutputFormat?.ToUpper() == "WAV" ? "LINEAR16" : "MP3",
                    SpeakingRate = config.Speed,
                    Pitch = config.Pitch,
                    VolumeGainDb = (config.Volume - 1.0) * 10 // Convert 0-2 range to dB
                }
            };

            var url = $"{apiBaseUrl}/text:synthesize?key={_apiKey}";
            var response = await _httpClient.PostAsJsonAsync(url, request, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = $"Google Cloud TTS API error: {response.StatusCode} - {errorContent}"
                };
            }

            var result = await response.Content.ReadFromJsonAsync<GoogleSynthesisResponse>(cancellationToken: cancellationToken);

            if (result == null || string.IsNullOrEmpty(result.AudioContent))
            {
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = "No audio data received from Google Cloud TTS"
                };
            }

            var audioData = Convert.FromBase64String(result.AudioContent);
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

        if (string.IsNullOrEmpty(_apiKey))
        {
            _apiKey = await _storageService.GetApiKeyAsync(PROVIDER_ID);
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new List<Voice>();
            }
        }

        try
        {
            var url = $"{BASE_URL}/voices?key={_apiKey}";
            var response = await _httpClient.GetFromJsonAsync<GoogleVoicesResponse>(url, cancellationToken: cancellationToken);

            if (response == null || response.Voices == null)
            {
                return new List<Voice>();
            }

            var voices = response.Voices
                .Where(gv => IsValidVoiceName(gv.Name))
                .Select(gv => new Voice
                {
                    Id = $"{gv.LanguageCodes.FirstOrDefault() ?? "en-US"}/{gv.Name}",
                    Name = gv.Name,
                    Language = GetLanguageName(gv.LanguageCodes.FirstOrDefault() ?? "en-US"),
                    LanguageCode = gv.LanguageCodes.FirstOrDefault() ?? "en-US",
                    Gender = gv.SsmlGender,
                    Quality = gv.Name.Contains("Neural2") ? "Neural2" :
                              gv.Name.Contains("Wavenet") ? "WaveNet" :
                              gv.Name.Contains("Studio") ? "Studio" :
                              gv.Name.Contains("Chirp") ? "Chirp" :
                              "Standard",
                    ProviderId = PROVIDER_ID,
                    Metadata = new Dictionary<string, object>
                    {
                        ["SampleRate"] = gv.NaturalSampleRateHertz
                    }
                }).ToList();

            // Cache the results
            _cachedVoices = voices;
            _cacheExpiry = DateTime.UtcNow.AddDays(CACHE_DURATION_DAYS);

            return voices;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching Google Cloud voices: {ex.Message}");
            return new List<Voice>();
        }
    }

    public async Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            return false;
        }

        try
        {
            var url = $"{BASE_URL}/voices?key={apiKey}";
            var response = await _httpClient.GetAsync(url, cancellationToken);
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
        // Determine if voice is Neural2 or standard
        bool isNeural = config?.VoiceId?.Contains("Neural2") == true ||
                       config?.VoiceId?.Contains("Studio") == true ||
                       config?.VoiceId?.Contains("Chirp") == true;

        var costPerChar = isNeural ? COST_PER_CHAR_NEURAL : COST_PER_CHAR_STANDARD;
        return characterCount * costPerChar;
    }

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey;
    }

    private string GetLanguageName(string languageCode)
    {
        var languageNames = new Dictionary<string, string>
        {
            ["en-US"] = "English (US)",
            ["en-GB"] = "English (UK)",
            ["en-AU"] = "English (Australia)",
            ["en-IN"] = "English (India)",
            ["es-ES"] = "Spanish (Spain)",
            ["es-US"] = "Spanish (US)",
            ["fr-FR"] = "French (France)",
            ["fr-CA"] = "French (Canada)",
            ["de-DE"] = "German",
            ["it-IT"] = "Italian",
            ["pt-BR"] = "Portuguese (Brazil)",
            ["pt-PT"] = "Portuguese (Portugal)",
            ["ja-JP"] = "Japanese",
            ["ko-KR"] = "Korean",
            ["zh-CN"] = "Chinese (Simplified)",
            ["zh-TW"] = "Chinese (Traditional)",
            ["ar-XA"] = "Arabic",
            ["hi-IN"] = "Hindi",
            ["ru-RU"] = "Russian",
            ["tr-TR"] = "Turkish"
        };

        return languageNames.TryGetValue(languageCode, out var name) ? name : languageCode;
    }

    private string? GetModelNameIfNeeded(string voiceName)
    {
        // Neural2 voices: "en-US-Neural2-A" -> model: "en-US-Neural2"
        if (voiceName.Contains("Neural2"))
        {
            var parts = voiceName.Split('-');
            if (parts.Length >= 3)
            {
                return $"{parts[0]}-{parts[1]}-{parts[2]}";
            }
        }

        // Studio voices: "en-US-Studio-O" -> model: "en-US-Studio-O"
        if (voiceName.Contains("Studio"))
        {
            return voiceName;
        }

        // Chirp voices
        if (voiceName.Contains("Chirp"))
        {
            if (voiceName.Contains("Chirp-3"))
            {
                return "chirp-3";
            }
            if (voiceName.Contains("Chirp-2"))
            {
                return "chirp-2";
            }
            return "chirp";
        }

        // Polyglot voices use full voice name as model
        if (voiceName.Contains("Polyglot"))
        {
            return voiceName;
        }

        // Journey voices: "en-US-Journey-D", "en-US-Journey-F", etc.
        if (voiceName.Contains("Journey"))
        {
            return voiceName;
        }

        // WaveNet and Standard voices don't need model specification
        // Return null to omit the field from JSON
        return null;
    }

    private bool IsValidVoiceName(string voiceName)
    {
        if (string.IsNullOrWhiteSpace(voiceName))
        {
            return false;
        }

        // Valid Google Cloud TTS voice names must follow these patterns:
        // - Standard: en-US-Standard-A, en-US-Standard-B, etc.
        // - WaveNet: en-US-Wavenet-A, en-US-Wavenet-B, etc.
        // - Neural2: en-US-Neural2-A, en-US-Neural2-B, etc.
        // - Studio: en-US-Studio-O, en-US-Studio-Q, etc.
        // - Chirp: ar-XA-Chirp-HD, en-US-Chirp2-HD, ar-XA-Chirp3-HD-Erinome, etc.
        // - Polyglot: en-US-Polyglot-1, etc.
        // - Journey: en-US-Journey-D, en-US-Journey-F, etc.

        // Voice names must contain at least one hyphen (to separate language code from voice type)
        if (!voiceName.Contains('-'))
        {
            return false;
        }

        // Must start with a valid language code pattern (e.g., en-US, ar-XA, ja-JP)
        // Language codes are typically 2-3 chars, hyphen, 2-3 chars
        var parts = voiceName.Split('-');
        if (parts.Length < 3)
        {
            return false;
        }

        // First part should be 2-3 characters (language)
        if (parts[0].Length < 2 || parts[0].Length > 3)
        {
            return false;
        }

        // Second part should be 2-3 characters (region)
        if (parts[1].Length < 2 || parts[1].Length > 3)
        {
            return false;
        }

        // Third part should be a voice type (Standard, Wavenet, Neural2, Studio, Chirp, Polyglot, Journey, etc.)
        var voiceType = parts[2];
        var validTypes = new[] { "Standard", "Wavenet", "Neural2", "Studio", "Chirp", "Chirp2", "Chirp3", "Polyglot", "Journey", "News", "Casual" };

        // Check if voice type matches any valid type (case-insensitive)
        return validTypes.Any(type => voiceType.StartsWith(type, StringComparison.OrdinalIgnoreCase));
    }
}
