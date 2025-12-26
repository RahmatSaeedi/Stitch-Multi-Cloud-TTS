namespace SpeechApp.Services.Interfaces;

public interface IRetryService
{
    Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 3,
        CancellationToken cancellationToken = default);

    Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action,
        Func<Exception, bool> shouldRetry,
        int maxRetries = 3,
        CancellationToken cancellationToken = default);
}

public class RetryOptions
{
    public int MaxRetries { get; set; } = 3;
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromSeconds(1);
    public double BackoffMultiplier { get; set; } = 2.0;
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);
    public bool UseJitter { get; set; } = true;
}
