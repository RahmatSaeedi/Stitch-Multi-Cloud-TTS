using Blazored.LocalStorage;
using SpeechApp.Services.Interfaces;
using System.Text.Json;

namespace SpeechApp.Services;

public class StorageService : IStorageService
{
    private readonly ILocalStorageService _localStorage;
    private readonly IEncryptionService _encryptionService;

    private const string API_KEY_PREFIX = "apikey_";
    private const string PREFERENCE_PREFIX = "pref_";

    public StorageService(ILocalStorageService localStorage, IEncryptionService encryptionService)
    {
        _localStorage = localStorage;
        _encryptionService = encryptionService;
    }

    public async Task SetApiKeyAsync(string providerId, string apiKey)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider ID cannot be empty", nameof(providerId));
        }

        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new ArgumentException("API key cannot be empty", nameof(apiKey));
        }

        if (!_encryptionService.IsInitialized)
        {
            throw new InvalidOperationException("Encryption service must be initialized before storing API keys");
        }

        var encryptedKey = await _encryptionService.EncryptAsync(apiKey);
        var key = $"{API_KEY_PREFIX}{providerId}";
        await _localStorage.SetItemAsStringAsync(key, encryptedKey);
    }

    public async Task<string?> GetApiKeyAsync(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider ID cannot be empty", nameof(providerId));
        }

        var key = $"{API_KEY_PREFIX}{providerId}";
        var encryptedKey = await _localStorage.GetItemAsStringAsync(key);

        if (string.IsNullOrEmpty(encryptedKey))
        {
            return null;
        }

        if (!_encryptionService.IsInitialized)
        {
            throw new InvalidOperationException("Encryption service must be initialized before retrieving API keys");
        }

        try
        {
            return await _encryptionService.DecryptAsync(encryptedKey);
        }
        catch
        {
            // If decryption fails, return null (corrupted data or wrong password)
            return null;
        }
    }

    public async Task RemoveApiKeyAsync(string providerId)
    {
        if (string.IsNullOrWhiteSpace(providerId))
        {
            throw new ArgumentException("Provider ID cannot be empty", nameof(providerId));
        }

        var key = $"{API_KEY_PREFIX}{providerId}";
        await _localStorage.RemoveItemAsync(key);
    }

    public async Task SetPreferenceAsync<T>(string key, T value)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be empty", nameof(key));
        }

        var prefKey = $"{PREFERENCE_PREFIX}{key}";
        var json = JsonSerializer.Serialize(value);
        await _localStorage.SetItemAsStringAsync(prefKey, json);
    }

    public async Task<T?> GetPreferenceAsync<T>(string key)
    {
        if (string.IsNullOrWhiteSpace(key))
        {
            throw new ArgumentException("Key cannot be empty", nameof(key));
        }

        var prefKey = $"{PREFERENCE_PREFIX}{key}";
        var json = await _localStorage.GetItemAsStringAsync(prefKey);

        if (string.IsNullOrEmpty(json))
        {
            return default;
        }

        try
        {
            return JsonSerializer.Deserialize<T>(json);
        }
        catch
        {
            return default;
        }
    }

    public async Task ClearAllAsync()
    {
        await _localStorage.ClearAsync();
    }
}
