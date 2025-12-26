namespace SpeechApp.Services.Interfaces;

public interface IRateLimitService
{
    Task<bool> CheckRateLimitAsync(string providerId);
    Task RecordRequestAsync(string providerId);
    Task<RateLimitInfo> GetRateLimitInfoAsync(string providerId);
    Task ResetRateLimitAsync(string providerId);
}

public class RateLimitInfo
{
    public string ProviderId { get; set; } = string.Empty;
    public int RequestsRemaining { get; set; }
    public int MaxRequests { get; set; }
    public TimeSpan WindowDuration { get; set; }
    public DateTime WindowResetTime { get; set; }
    public bool IsLimited { get; set; }
}
