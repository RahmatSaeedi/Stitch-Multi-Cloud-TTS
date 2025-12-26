namespace SpeechApp.Services.Interfaces;

public interface IErrorLoggingService
{
    Task LogErrorAsync(ErrorLog error);
    Task<List<ErrorLog>> GetRecentErrorsAsync(int count = 50);
    Task ClearErrorsAsync();
}

public class ErrorLog
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string Message { get; set; } = string.Empty;
    public string? StackTrace { get; set; }
    public ErrorSeverity Severity { get; set; } = ErrorSeverity.Error;
    public string? Component { get; set; }
    public string? ProviderId { get; set; }
    public Dictionary<string, string>? AdditionalData { get; set; }
}

public enum ErrorSeverity
{
    Info,
    Warning,
    Error,
    Critical
}
