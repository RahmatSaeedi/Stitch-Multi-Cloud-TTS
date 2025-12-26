using SpeechApp.Models;

namespace SpeechApp.Services.Interfaces;

public interface IOfflineTTSProvider
{
    /// <summary>
    /// Gets the provider information
    /// </summary>
    ProviderInfo GetProviderInfo();

    /// <summary>
    /// Checks if the provider is ready to synthesize (models loaded)
    /// </summary>
    Task<bool> IsReadyAsync();

    /// <summary>
    /// Gets available voices for offline synthesis
    /// </summary>
    Task<List<Voice>> GetVoicesAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Synthesizes speech from text using offline models
    /// </summary>
    Task<SynthesisResult> SynthesizeSpeechAsync(string text, VoiceConfig config, CancellationToken cancellationToken = default);

    /// <summary>
    /// Gets the maximum character limit for this provider
    /// </summary>
    int GetMaxCharacterLimit();

    /// <summary>
    /// Downloads and installs a voice model
    /// </summary>
    /// <param name="modelId">ID of the model to download</param>
    /// <param name="progress">Progress callback (0-100)</param>
    Task<bool> DownloadModelAsync(string modelId, Action<int>? progress = null, CancellationToken cancellationToken = default);

    /// <summary>
    /// Removes a downloaded voice model
    /// </summary>
    Task<bool> RemoveModelAsync(string modelId);

    /// <summary>
    /// Gets list of available models that can be downloaded
    /// </summary>
    Task<List<OfflineVoiceModel>> GetAvailableModelsAsync();

    /// <summary>
    /// Gets list of currently downloaded models
    /// </summary>
    Task<List<OfflineVoiceModel>> GetDownloadedModelsAsync();

    /// <summary>
    /// Gets total size of downloaded models in bytes
    /// </summary>
    Task<long> GetTotalModelSizeAsync();
}
