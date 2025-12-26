using Microsoft.JSInterop;
using Blazored.LocalStorage;
using SpeechApp.Services.Interfaces;

namespace SpeechApp.Services;

public class EncryptionService : IEncryptionService
{
    private readonly IJSRuntime _jsRuntime;
    private readonly ILocalStorageService _localStorage;
    private readonly AppStateService _appState;
    private string? _salt;

    private const string SALT_KEY = "encryption_salt";
    private const string PASSWORD_HASH_KEY = "password_hash";

    public bool IsInitialized => _appState.IsEncryptionInitialized;

    public EncryptionService(IJSRuntime jsRuntime, ILocalStorageService localStorage, AppStateService appState)
    {
        _jsRuntime = jsRuntime;
        _localStorage = localStorage;
        _appState = appState;
    }

    public async Task InitializeAsync(string masterPassword)
    {
        if (string.IsNullOrWhiteSpace(masterPassword))
        {
            throw new ArgumentException("Master password cannot be empty", nameof(masterPassword));
        }

        // Check if salt exists, if not create it (first time setup)
        _salt = await _localStorage.GetItemAsStringAsync(SALT_KEY);

        if (string.IsNullOrEmpty(_salt))
        {
            // First time setup - generate new salt
            _salt = await _jsRuntime.InvokeAsync<string>("cryptoHelper.generateSalt");
            await _localStorage.SetItemAsStringAsync(SALT_KEY, _salt);

            // Hash and store password for future validation
            var passwordHash = await _jsRuntime.InvokeAsync<string>("cryptoHelper.hashPassword", masterPassword, _salt);
            await _localStorage.SetItemAsStringAsync(PASSWORD_HASH_KEY, passwordHash);
        }
        else
        {
            // Validate password
            var isValid = await ValidateMasterPasswordAsync(masterPassword);
            if (!isValid)
            {
                throw new UnauthorizedAccessException("Invalid master password");
            }
        }

        _appState.SetMasterPassword(masterPassword);
    }

    public async Task<string> EncryptAsync(string plaintext)
    {
        var masterPassword = _appState.GetMasterPassword();
        if (!_appState.IsEncryptionInitialized || string.IsNullOrEmpty(masterPassword) || string.IsNullOrEmpty(_salt))
        {
            throw new InvalidOperationException("Encryption service is not initialized. Call InitializeAsync first.");
        }

        if (string.IsNullOrWhiteSpace(plaintext))
        {
            throw new ArgumentException("Plaintext cannot be empty", nameof(plaintext));
        }

        try
        {
            // Convert salt from base64 to byte array for JS
            var saltBytes = Convert.FromBase64String(_salt);
            return await _jsRuntime.InvokeAsync<string>("cryptoHelper.encrypt", plaintext, masterPassword, saltBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Encryption failed", ex);
        }
    }

    public async Task<string> DecryptAsync(string ciphertext)
    {
        var masterPassword = _appState.GetMasterPassword();
        if (!_appState.IsEncryptionInitialized || string.IsNullOrEmpty(masterPassword) || string.IsNullOrEmpty(_salt))
        {
            throw new InvalidOperationException("Encryption service is not initialized. Call InitializeAsync first.");
        }

        if (string.IsNullOrWhiteSpace(ciphertext))
        {
            throw new ArgumentException("Ciphertext cannot be empty", nameof(ciphertext));
        }

        try
        {
            // Convert salt from base64 to byte array for JS
            var saltBytes = Convert.FromBase64String(_salt);
            return await _jsRuntime.InvokeAsync<string>("cryptoHelper.decrypt", ciphertext, masterPassword, saltBytes);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException("Decryption failed. Data may be corrupted or password is incorrect.", ex);
        }
    }

    public async Task<bool> ValidateMasterPasswordAsync(string password)
    {
        if (string.IsNullOrEmpty(_salt))
        {
            _salt = await _localStorage.GetItemAsStringAsync(SALT_KEY);
            if (string.IsNullOrEmpty(_salt))
            {
                return false; // No password set yet
            }
        }

        var storedHash = await _localStorage.GetItemAsStringAsync(PASSWORD_HASH_KEY);
        if (string.IsNullOrEmpty(storedHash))
        {
            return false;
        }

        var computedHash = await _jsRuntime.InvokeAsync<string>("cryptoHelper.hashPassword", password, _salt);
        return storedHash == computedHash;
    }

    public async Task ChangeMasterPasswordAsync(string oldPassword, string newPassword)
    {
        // Validate old password
        var isValid = await ValidateMasterPasswordAsync(oldPassword);
        if (!isValid)
        {
            throw new UnauthorizedAccessException("Invalid old password");
        }

        if (string.IsNullOrWhiteSpace(newPassword))
        {
            throw new ArgumentException("New password cannot be empty", nameof(newPassword));
        }

        // Generate new salt
        var newSalt = await _jsRuntime.InvokeAsync<string>("cryptoHelper.generateSalt");

        // Hash new password
        var newPasswordHash = await _jsRuntime.InvokeAsync<string>("cryptoHelper.hashPassword", newPassword, newSalt);

        // TODO: Re-encrypt all stored API keys with new password
        // This requires getting all encrypted keys, decrypting with old password,
        // and re-encrypting with new password

        // Update stored values
        await _localStorage.SetItemAsStringAsync(SALT_KEY, newSalt);
        await _localStorage.SetItemAsStringAsync(PASSWORD_HASH_KEY, newPasswordHash);

        // Update in-memory values
        _salt = newSalt;
        _appState.SetMasterPassword(newPassword);
    }
}
