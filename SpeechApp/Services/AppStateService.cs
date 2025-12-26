namespace SpeechApp.Services;

/// <summary>
/// Singleton service to maintain app-wide state across component lifecycles
/// </summary>
public class AppStateService
{
    private string? _masterPassword;
    private bool _isInitialized;

    public bool IsEncryptionInitialized => _isInitialized;

    public void SetMasterPassword(string password)
    {
        _masterPassword = password;
        _isInitialized = true;
    }

    public string? GetMasterPassword()
    {
        return _masterPassword;
    }

    public void ClearMasterPassword()
    {
        _masterPassword = null;
        _isInitialized = false;
    }
}
