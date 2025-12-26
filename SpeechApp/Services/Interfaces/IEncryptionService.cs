namespace SpeechApp.Services.Interfaces;

public interface IEncryptionService
{
    /// <summary>
    /// Initializes the encryption service with a master password
    /// </summary>
    Task InitializeAsync(string masterPassword);

    /// <summary>
    /// Checks if the service is initialized
    /// </summary>
    bool IsInitialized { get; }

    /// <summary>
    /// Encrypts plaintext using AES-GCM
    /// </summary>
    Task<string> EncryptAsync(string plaintext);

    /// <summary>
    /// Decrypts ciphertext using AES-GCM
    /// </summary>
    Task<string> DecryptAsync(string ciphertext);

    /// <summary>
    /// Validates the master password
    /// </summary>
    Task<bool> ValidateMasterPasswordAsync(string password);

    /// <summary>
    /// Changes the master password
    /// </summary>
    Task ChangeMasterPasswordAsync(string oldPassword, string newPassword);
}
