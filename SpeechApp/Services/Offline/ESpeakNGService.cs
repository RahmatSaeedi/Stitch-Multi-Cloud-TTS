using Microsoft.JSInterop;
using SpeechApp.Models;
using SpeechApp.Services.Interfaces;

namespace SpeechApp.Services.Offline;

public class ESpeakNGService : ITTSProvider, IOfflineTTSProvider
{
    private readonly IJSRuntime _jsRuntime;
    private bool _isInitialized;

    private const string PROVIDER_ID = "espeak";
    private const int MAX_CHARACTERS = 10000;

    public ESpeakNGService(IJSRuntime jsRuntime)
    {
        _jsRuntime = jsRuntime;
    }

    public ProviderInfo GetProviderInfo()
    {
        return new ProviderInfo
        {
            Id = PROVIDER_ID,
            Name = "eSpeak-NG",
            DisplayName = "eSpeak-NG (Offline)",
            MaxCharacterLimit = MAX_CHARACTERS,
            RequiresApiKey = false,
            SupportsSSML = true,
            SetupGuideUrl = "/help/offline-setup#espeak",
            Health = ProviderHealth.Unknown
        };
    }

    public async Task<bool> IsReadyAsync()
    {
        if (_isInitialized)
            return true;

        try
        {
            _isInitialized = await _jsRuntime.InvokeAsync<bool>("espeakNG.isReady");
            return _isInitialized;
        }
        catch
        {
            return false;
        }
    }

    public async Task<List<Voice>> GetVoicesAsync(CancellationToken cancellationToken = default)
    {
        // eSpeak-NG supports 127+ languages with various variants
        // This is a subset of the most common voices
        return await Task.FromResult(new List<Voice>
        {
            // English variants
            new Voice { Id = "en", Name = "English", Language = "English", LanguageCode = "en", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "en-us", Name = "English (US)", Language = "English (US)", LanguageCode = "en-US", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "en-gb", Name = "English (UK)", Language = "English (UK)", LanguageCode = "en-GB", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "en-au", Name = "English (Australia)", Language = "English (AU)", LanguageCode = "en-AU", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },

            // Major world languages
            new Voice { Id = "es", Name = "Spanish", Language = "Spanish", LanguageCode = "es", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "fr", Name = "French", Language = "French", LanguageCode = "fr", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "de", Name = "German", Language = "German", LanguageCode = "de", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "it", Name = "Italian", Language = "Italian", LanguageCode = "it", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "pt", Name = "Portuguese", Language = "Portuguese", LanguageCode = "pt", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "ru", Name = "Russian", Language = "Russian", LanguageCode = "ru", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "zh", Name = "Chinese (Mandarin)", Language = "Chinese", LanguageCode = "zh", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "ja", Name = "Japanese", Language = "Japanese", LanguageCode = "ja", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "ko", Name = "Korean", Language = "Korean", LanguageCode = "ko", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "ar", Name = "Arabic", Language = "Arabic", LanguageCode = "ar", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "hi", Name = "Hindi", Language = "Hindi", LanguageCode = "hi", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },

            // European languages
            new Voice { Id = "nl", Name = "Dutch", Language = "Dutch", LanguageCode = "nl", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "pl", Name = "Polish", Language = "Polish", LanguageCode = "pl", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "sv", Name = "Swedish", Language = "Swedish", LanguageCode = "sv", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "da", Name = "Danish", Language = "Danish", LanguageCode = "da", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "no", Name = "Norwegian", Language = "Norwegian", LanguageCode = "no", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "fi", Name = "Finnish", Language = "Finnish", LanguageCode = "fi", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "cs", Name = "Czech", Language = "Czech", LanguageCode = "cs", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "el", Name = "Greek", Language = "Greek", LanguageCode = "el", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "tr", Name = "Turkish", Language = "Turkish", LanguageCode = "tr", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },

            // Asian languages
            new Voice { Id = "vi", Name = "Vietnamese", Language = "Vietnamese", LanguageCode = "vi", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "th", Name = "Thai", Language = "Thai", LanguageCode = "th", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
            new Voice { Id = "id", Name = "Indonesian", Language = "Indonesian", LanguageCode = "id", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },

            // African languages
            new Voice { Id = "sw", Name = "Swahili", Language = "Swahili", LanguageCode = "sw", Gender = "NEUTRAL", Quality = "Standard", ProviderId = PROVIDER_ID },
        });
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

        if (!await IsReadyAsync())
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = "eSpeak-NG is not initialized. Please reload the page."
            };
        }

        var startTime = DateTime.UtcNow;

        try
        {
            // Extract voice parameters
            var speed = config.Speed;
            var pitch = config.Pitch;

            // Call JavaScript interop for eSpeak-NG WASM
            var audioBase64 = await _jsRuntime.InvokeAsync<string>(
                "espeakNG.synthesize",
                cancellationToken,
                text,
                config.VoiceId,
                (int)(speed * 175), // eSpeak speed (default 175)
                (int)(pitch * 50)   // eSpeak pitch (default 50)
            );

            if (string.IsNullOrEmpty(audioBase64))
            {
                return new SynthesisResult
                {
                    Success = false,
                    ErrorMessage = "eSpeak-NG failed to generate audio"
                };
            }

            var audioData = Convert.FromBase64String(audioBase64);
            var duration = DateTime.UtcNow - startTime;

            return new SynthesisResult
            {
                Success = true,
                AudioData = audioData,
                CharactersProcessed = text.Length,
                Cost = 0, // Offline TTS is free
                Duration = duration
            };
        }
        catch (Exception ex)
        {
            return new SynthesisResult
            {
                Success = false,
                ErrorMessage = $"eSpeak-NG synthesis failed: {ex.Message}"
            };
        }
    }

    public int GetMaxCharacterLimit() => MAX_CHARACTERS;

    // ITTSProvider implementation
    public Task<List<Voice>> GetVoicesAsync(bool bypassCache = false, CancellationToken cancellationToken = default)
    {
        // For offline providers, cache doesn't apply - always return all voices
        return GetVoicesAsync(cancellationToken);
    }

    public Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default)
    {
        // Offline providers don't require API keys
        return Task.FromResult(true);
    }

    public decimal CalculateCost(int characterCount, VoiceConfig? config = null)
    {
        // Offline TTS is free
        return 0m;
    }

    public void SetApiKey(string apiKey)
    {
        // Offline providers don't use API keys - no-op
    }

    // eSpeak-NG doesn't require model downloads - it's bundled
    public Task<bool> DownloadModelAsync(string modelId, Action<int>? progress = null, CancellationToken cancellationToken = default)
    {
        return Task.FromResult(true); // No-op for eSpeak
    }

    public Task<bool> RemoveModelAsync(string modelId)
    {
        return Task.FromResult(false); // Can't remove eSpeak models
    }

    public Task<List<OfflineVoiceModel>> GetAvailableModelsAsync()
    {
        // eSpeak is pre-bundled, no separate models to download
        return Task.FromResult(new List<OfflineVoiceModel>());
    }

    public Task<List<OfflineVoiceModel>> GetDownloadedModelsAsync()
    {
        // eSpeak is pre-bundled
        return Task.FromResult(new List<OfflineVoiceModel>
        {
            new OfflineVoiceModel
            {
                Id = "espeak-ng-core",
                Name = "eSpeak-NG Core",
                Language = "Multilingual (127+ languages)",
                LanguageCode = "mul",
                Gender = "NEUTRAL",
                Quality = "Standard",
                SizeBytes = 5 * 1024 * 1024, // ~5 MB
                Provider = PROVIDER_ID,
                IsDownloaded = true,
                DownloadedDate = DateTime.UtcNow,
                Description = "Lightweight multilingual TTS engine with 127+ language support"
            }
        });
    }

    public async Task<long> GetTotalModelSizeAsync()
    {
        var downloaded = await GetDownloadedModelsAsync();
        return downloaded.Sum(m => m.SizeBytes);
    }
}
