using System.Net.Http.Json;
using System.Text.Json;
using SpeechApp.Models;
using SpeechApp.Models.Providers;
using SpeechApp.Services.Interfaces;

namespace SpeechApp.Services.Providers;

public class ElevenLabsProvider : ITTSProvider
{
    private readonly HttpClient _httpClient;
    private readonly IStorageService _storageService;
    private string? _apiKey;
    private List<Voice>? _cachedVoices;
    private DateTime? _cacheExpiry;

    private const string PROVIDER_ID = "elevenlabs";
    private const string BASE_URL = "https://api.elevenlabs.io/v1";
    private const int MAX_CHARACTERS = 100000;
    private const int CACHE_DURATION_DAYS = 14;

    // ElevenLabs pricing varies by plan, using approximate values
    private const decimal COST_PER_CHAR = 0.00003m; // Approximate

    public ElevenLabsProvider(HttpClient httpClient, IStorageService storageService)
    {
        _httpClient = httpClient;
        _storageService = storageService;
    }

    public ProviderInfo GetProviderInfo()
    {
        return new ProviderInfo
        {
            Id = PROVIDER_ID,
            Name = "ElevenLabs",
            DisplayName = "ElevenLabs Text-to-Speech",
            MaxCharacterLimit = MAX_CHARACTERS,
            RequiresApiKey = true,
            SupportsSSML = false,
            SetupGuideUrl = "/help#elevenlabs",
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
                    ErrorMessage = "API key not configured. Please add your ElevenLabs API key in settings."
                };
            }
        }

        var startTime = DateTime.UtcNow;

        try
        {
            var request = new ElevenLabsSynthesisRequest
            {
                Text = text,
                ModelId = "eleven_multilingual_v2",
                VoiceSettings = new ElevenLabsVoiceSettings
                {
                    Stability = 0.5,
                    SimilarityBoost = 0.75,
                    UseSpeakerBoost = true
                }
            };

            // Apply custom settings from config if provided
            if (config.ProviderSpecificOptions != null)
            {
                if (config.ProviderSpecificOptions.TryGetValue("stability", out var stability))
                {
                    request.VoiceSettings.Stability = Convert.ToDouble(stability);
                }
                if (config.ProviderSpecificOptions.TryGetValue("similarity_boost", out var similarityBoost))
                {
                    request.VoiceSettings.SimilarityBoost = Convert.ToDouble(similarityBoost);
                }
            }

            var url = $"{BASE_URL}/text-to-speech/{config.VoiceId}";

            var httpRequest = new HttpRequestMessage(HttpMethod.Post, url);
            httpRequest.Headers.Add("xi-api-key", _apiKey);
            httpRequest.Content = JsonContent.Create(request);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync(cancellationToken);
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = $"ElevenLabs API error: {response.StatusCode} - {errorContent}"
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

        if (string.IsNullOrEmpty(_apiKey))
        {
            _apiKey = (await _storageService.GetApiKeyAsync(PROVIDER_ID))?.Trim();
            if (string.IsNullOrEmpty(_apiKey))
            {
                return new List<Voice>();
            }
        }

        try
        {
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{BASE_URL}/voices");
            httpRequest.Headers.Add("xi-api-key", _apiKey);

            var response = await _httpClient.SendAsync(httpRequest, cancellationToken);

            if (!response.IsSuccessStatusCode)
            {
                return new List<Voice>();
            }

            var result = await response.Content.ReadFromJsonAsync<ElevenLabsVoicesResponse>(cancellationToken: cancellationToken);

            if (result == null || result.Voices == null)
            {
                return new List<Voice>();
            }

            var voices = result.Voices.Select(ev => new Voice
            {
                Id = ev.VoiceId,
                Name = ev.Name,
                Language = "Multilingual", // ElevenLabs voices are multilingual
                LanguageCode = "mul",
                Gender = GetGenderFromLabels(ev.Labels),
                Quality = "Premium",
                ProviderId = PROVIDER_ID,
                Metadata = new Dictionary<string, object>
                {
                    ["Category"] = ev.Category,
                    ["Labels"] = ev.Labels
                }
            }).ToList();

            // Cache the results
            _cachedVoices = voices;
            _cacheExpiry = DateTime.UtcNow.AddDays(CACHE_DURATION_DAYS);

            return voices;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Error fetching ElevenLabs voices: {ex.Message}");
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
            // Trim whitespace from API key
            apiKey = apiKey.Trim();

            // Use /voices endpoint instead of /user as it works with all API key types
            var httpRequest = new HttpRequestMessage(HttpMethod.Get, $"{BASE_URL}/voices");
            httpRequest.Headers.Add("xi-api-key", apiKey);

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
        // ElevenLabs pricing varies by subscription plan
        // This is an approximation
        return characterCount * COST_PER_CHAR;
    }

    public void SetApiKey(string apiKey)
    {
        _apiKey = apiKey?.Trim();
    }

    private string? GetGenderFromLabels(Dictionary<string, string> labels)
    {
        if (labels == null || labels.Count == 0)
        {
            return null;
        }

        // ElevenLabs labels are key-value pairs
        // Check if there's a "gender" key or look for gender values
        if (labels.TryGetValue("gender", out var gender))
        {
            return gender.ToUpper();
        }

        // Check if any value contains gender information
        foreach (var value in labels.Values)
        {
            var lowerValue = value.ToLower();
            if (lowerValue.Contains("male") && !lowerValue.Contains("female"))
            {
                return "MALE";
            }
            if (lowerValue.Contains("female"))
            {
                return "FEMALE";
            }
        }

        return null;
    }
}
