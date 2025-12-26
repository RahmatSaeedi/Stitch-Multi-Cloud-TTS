namespace SpeechApp.Services.Interfaces;

public interface IUsageHistoryService
{
    Task RecordUsageAsync(UsageRecord record);
    Task<List<UsageRecord>> GetUsageHistoryAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task<UsageStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null);
    Task ClearHistoryAsync();
}

public class UsageRecord
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public required string ProviderId { get; set; }
    public required string ProviderName { get; set; }
    public string? VoiceId { get; set; }
    public string? VoiceName { get; set; }
    public int CharacterCount { get; set; }
    public decimal Cost { get; set; }
    public TimeSpan Duration { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}

public class UsageStatistics
{
    public int TotalSyntheses { get; set; }
    public int SuccessfulSyntheses { get; set; }
    public int FailedSyntheses { get; set; }
    public int TotalCharacters { get; set; }
    public decimal TotalCost { get; set; }
    public TimeSpan TotalDuration { get; set; }
    public Dictionary<string, int> CharactersByProvider { get; set; } = new();
    public Dictionary<string, decimal> CostByProvider { get; set; } = new();
    public Dictionary<string, int> SynthesesByProvider { get; set; } = new();
    public string? MostUsedProvider { get; set; }
    public string? MostUsedVoice { get; set; }
}
