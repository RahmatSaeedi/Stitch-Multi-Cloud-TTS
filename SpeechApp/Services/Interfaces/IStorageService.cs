namespace SpeechApp.Services.Interfaces;

public interface IStorageService
{
    /// <summary>
    /// Stores an encrypted API key for a provider
    /// </summary>
    Task SetApiKeyAsync(string providerId, string apiKey);

    /// <summary>
    /// Retrieves an encrypted API key for a provider
    /// </summary>
    Task<string?> GetApiKeyAsync(string providerId);

    /// <summary>
    /// Removes an API key for a provider
    /// </summary>
    Task RemoveApiKeyAsync(string providerId);

    /// <summary>
    /// Stores user preferences
    /// </summary>
    Task SetPreferenceAsync<T>(string key, T value);

    /// <summary>
    /// Retrieves user preferences
    /// </summary>
    Task<T?> GetPreferenceAsync<T>(string key);

    /// <summary>
    /// Clears all storage
    /// </summary>
    Task ClearAllAsync();
}
