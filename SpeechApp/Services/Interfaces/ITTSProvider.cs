using SpeechApp.Models;

namespace SpeechApp.Services.Interfaces;

public interface ITTSProvider
{
    /// <summary>
    /// Gets the provider information
    /// </summary>
    ProviderInfo GetProviderInfo();

    /// <summary>
    /// Synthesizes speech from text
    /// </summary>
    Task<SynthesisResult> SynthesizeSpeechAsync(string text, VoiceConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the list of available voices
    /// </summary>
    Task<List<Voice>> GetVoicesAsync(bool bypassCache = false, CancellationToken cancellationToken = default);

    /// <summary>
    /// Validates the API key
    /// </summary>
    Task<bool> ValidateApiKeyAsync(string apiKey, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the maximum character limit for this provider
    /// </summary>
    int GetMaxCharacterLimit();

    /// <summary>
    /// Calculates the estimated cost for the given character count
    /// </summary>
    decimal CalculateCost(int characterCount, VoiceConfig? config = null);

    /// <summary>
    /// Sets the API key for this provider
    /// </summary>
    void SetApiKey(string apiKey);
}
