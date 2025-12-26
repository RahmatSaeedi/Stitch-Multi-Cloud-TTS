using SpeechApp.Models;
using SpeechApp.Services.Interfaces;
using SpeechApp.Services.Providers;
using SpeechApp.Services.Offline;

namespace SpeechApp.Services;

public class TTSProviderManager : ITTSProviderManager
{
    private readonly IStorageService _storageService;
    private readonly Dictionary<string, ITTSProvider> _providers;
    private const string DEFAULT_PROVIDER_KEY = "default_provider";

    public TTSProviderManager(
        IStorageService storageService,
        GoogleCloudTTSProvider googleProvider,
        ElevenLabsProvider elevenLabsProvider,
        DeepgramProvider deepgramProvider,
        AzureTTSProvider azureProvider,
        AmazonPollyProvider pollyProvider,
        PiperTTSService piperProvider,
        ESpeakNGService espeakProvider)
    {
        _storageService = storageService;

        // Register all providers (cloud + offline)
        _providers = new Dictionary<string, ITTSProvider>
        {
            ["google"] = googleProvider,
            ["elevenlabs"] = elevenLabsProvider,
            ["deepgram"] = deepgramProvider,
            ["azure"] = azureProvider,
            ["polly"] = pollyProvider,
            ["piper"] = piperProvider,
            ["espeak"] = espeakProvider
        };
    }

    public List<ProviderInfo> GetAllProviders()
    {
        return _providers.Values
            .Select(p => p.GetProviderInfo())
            .OrderBy(p => p.DisplayName)
            .ToList();
    }

    public ITTSProvider? GetProvider(string providerId)
    {
        return _providers.TryGetValue(providerId.ToLower(), out var provider) ? provider : null;
    }

    public ITTSProvider? GetDefaultProvider()
    {
        // Try to get user's default provider
        var defaultProviderId = _storageService.GetPreferenceAsync<string>(DEFAULT_PROVIDER_KEY).Result;

        if (!string.IsNullOrEmpty(defaultProviderId) && _providers.ContainsKey(defaultProviderId))
        {
            return _providers[defaultProviderId];
        }

        // Return first provider if no default set
        return _providers.Values.FirstOrDefault();
    }

    public async Task SetDefaultProviderAsync(string providerId)
    {
        if (_providers.ContainsKey(providerId.ToLower()))
        {
            await _storageService.SetPreferenceAsync(DEFAULT_PROVIDER_KEY, providerId.ToLower());
        }
    }

    public async Task<Dictionary<string, List<Voice>>> GetAllVoicesAsync(bool bypassCache = false)
    {
        var allVoices = new Dictionary<string, List<Voice>>();

        foreach (var (providerId, provider) in _providers)
        {
            try
            {
                var voices = await provider.GetVoicesAsync(bypassCache);
                if (voices.Any())
                {
                    allVoices[providerId] = voices;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error fetching voices from {providerId}: {ex.Message}");
                allVoices[providerId] = new List<Voice>();
            }
        }

        return allVoices;
    }
}
