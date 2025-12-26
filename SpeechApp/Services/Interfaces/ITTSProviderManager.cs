using SpeechApp.Models;

namespace SpeechApp.Services.Interfaces;

public interface ITTSProviderManager
{
    /// <summary>
    /// Gets all available TTS providers
    /// </summary>
    List<ProviderInfo> GetAllProviders();

    /// <summary>
    /// Gets a specific provider by ID
    /// </summary>
    ITTSProvider? GetProvider(string providerId);

    /// <summary>
    /// Gets the default provider (or first available)
    /// </summary>
    ITTSProvider? GetDefaultProvider();

    /// <summary>
    /// Sets the default provider
    /// </summary>
    Task SetDefaultProviderAsync(string providerId);

    /// <summary>
    /// Gets all voices from all configured providers
    /// </summary>
    Task<Dictionary<string, List<Voice>>> GetAllVoicesAsync(bool bypassCache = false);
}
