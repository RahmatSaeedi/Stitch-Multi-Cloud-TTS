using Blazored.LocalStorage;
using SpeechApp.Services.Interfaces;

namespace SpeechApp.Services;

public class ErrorLoggingService : IErrorLoggingService
{
    private readonly ILocalStorageService _localStorage;
    private const string ERROR_LOG_KEY = "error_logs";
    private const int MAX_LOGS = 100; // Keep only last 100 errors

    public ErrorLoggingService(ILocalStorageService localStorage)
    {
        _localStorage = localStorage;
    }

    public async Task LogErrorAsync(ErrorLog error)
    {
        try
        {
            var logs = await GetAllLogsAsync();
            logs.Add(error);

            // Keep only the most recent logs
            if (logs.Count > MAX_LOGS)
            {
                logs = logs.OrderByDescending(l => l.Timestamp).Take(MAX_LOGS).ToList();
            }

            await SaveLogsAsync(logs);

            // Also log to browser console
            Console.Error.WriteLine($"[{error.Severity}] {error.Timestamp}: {error.Message}");
            if (!string.IsNullOrEmpty(error.StackTrace))
            {
                Console.Error.WriteLine(error.StackTrace);
            }
        }
        catch
        {
            // Silently fail - don't want error logging to break the app
        }
    }

    public async Task<List<ErrorLog>> GetRecentErrorsAsync(int count = 50)
    {
        var logs = await GetAllLogsAsync();
        return logs.OrderByDescending(l => l.Timestamp).Take(count).ToList();
    }

    public async Task ClearErrorsAsync()
    {
        await _localStorage.RemoveItemAsync(ERROR_LOG_KEY);
    }

    private async Task<List<ErrorLog>> GetAllLogsAsync()
    {
        try
        {
            return await _localStorage.GetItemAsync<List<ErrorLog>>(ERROR_LOG_KEY) ?? new List<ErrorLog>();
        }
        catch
        {
            return new List<ErrorLog>();
        }
    }

    private async Task SaveLogsAsync(List<ErrorLog> logs)
    {
        await _localStorage.SetItemAsync(ERROR_LOG_KEY, logs);
    }
}
