using Blazored.LocalStorage;
using SpeechApp.Services.Interfaces;

namespace SpeechApp.Services;

public class UsageHistoryService : IUsageHistoryService
{
    private readonly ILocalStorageService _localStorage;
    private const string USAGE_HISTORY_KEY = "usage_history";
    private const int MAX_RECORDS = 1000; // Keep only last 1000 records

    public UsageHistoryService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task RecordUsageAsync(UsageRecord record)
    {
        var history = await GetAllRecordsAsync();
        history.Add(record);

        // Keep only the most recent records
        if (history.Count > MAX_RECORDS)
        {
            history = history.OrderByDescending(r => r.Timestamp).Take(MAX_RECORDS).ToList();
        }

        await SaveRecordsAsync(history);
    }

    public async Task<List<UsageRecord>> GetUsageHistoryAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var history = await GetAllRecordsAsync();

        if (startDate.HasValue)
        {
            history = history.Where(r => r.Timestamp >= startDate.Value).ToList();
        }

        if (endDate.HasValue)
        {
            history = history.Where(r => r.Timestamp <= endDate.Value).ToList();
        }

        return history.OrderByDescending(r => r.Timestamp).ToList();
    }

    public async Task<UsageStatistics> GetStatisticsAsync(DateTime? startDate = null, DateTime? endDate = null)
    {
        var history = await GetUsageHistoryAsync(startDate, endDate);

        var stats = new UsageStatistics
        {
            TotalSyntheses = history.Count,
            SuccessfulSyntheses = history.Count(r => r.Success),
            FailedSyntheses = history.Count(r => !r.Success),
            TotalCharacters = history.Where(r => r.Success).Sum(r => r.CharacterCount),
            TotalCost = history.Where(r => r.Success).Sum(r => r.Cost),
            TotalDuration = TimeSpan.FromTicks(history.Where(r => r.Success).Sum(r => r.Duration.Ticks))
        };

        // Group by provider
        var successfulRecords = history.Where(r => r.Success);

        stats.CharactersByProvider = successfulRecords
            .GroupBy(r => r.ProviderName)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.CharacterCount));

        stats.CostByProvider = successfulRecords
            .GroupBy(r => r.ProviderName)
            .ToDictionary(g => g.Key, g => g.Sum(r => r.Cost));

        stats.SynthesesByProvider = successfulRecords
            .GroupBy(r => r.ProviderName)
            .ToDictionary(g => g.Key, g => g.Count());

        // Most used
        stats.MostUsedProvider = stats.SynthesesByProvider
            .OrderByDescending(kvp => kvp.Value)
            .FirstOrDefault().Key;

        stats.MostUsedVoice = successfulRecords
            .Where(r => !string.IsNullOrEmpty(r.VoiceName))
            .GroupBy(r => r.VoiceName)
            .OrderByDescending(g => g.Count())
            .FirstOrDefault()?.Key;

        return stats;
    }

    public async Task ClearHistoryAsync()
    {
        await _localStorage.RemoveItemAsync(USAGE_HISTORY_KEY);
    }

    private async Task<List<UsageRecord>> GetAllRecordsAsync()
    {
        try
        {
            return await _localStorage.GetItemAsync<List<UsageRecord>>(USAGE_HISTORY_KEY) ?? new List<UsageRecord>();
        }
        catch
        {
            return new List<UsageRecord>();
        }
    }

    private async Task SaveRecordsAsync(List<UsageRecord> records)
    {
        await _localStorage.SetItemAsync(USAGE_HISTORY_KEY, records);
    }
}
