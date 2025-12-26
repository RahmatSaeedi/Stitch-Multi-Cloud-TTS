using SpeechApp.Services.Interfaces;

namespace SpeechApp.Services;

public class RetryService : IRetryService
{
    private readonly RetryOptions _options;
    private readonly Random _random = new();

    public RetryService()
    {
        _options = new RetryOptions();
    }

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        return await ExecuteWithRetryAsync(
            action,
            ex => IsTransientError(ex),
            maxRetries,
            cancellationToken);
    }

    public async Task<T> ExecuteWithRetryAsync<T>(
        Func<Task<T>> action,
        Func<Exception, bool> shouldRetry,
        int maxRetries = 3,
        CancellationToken cancellationToken = default)
    {
        int attempt = 0;
        Exception? lastException = null;

        while (attempt <= maxRetries)
        {
            try
            {
                return await action();
            }
            catch (Exception ex) when (attempt < maxRetries && shouldRetry(ex))
            {
                lastException = ex;
                attempt++;

                var delay = CalculateDelay(attempt);
                await Task.Delay(delay, cancellationToken);
            }
        }

        // If we get here, all retries failed
        throw new Exception($"Operation failed after {maxRetries} retries", lastException);
    }

    private TimeSpan CalculateDelay(int attempt)
    {
        // Exponential backoff: delay = initialDelay * (backoffMultiplier ^ attempt)
        var exponentialDelay = _options.InitialDelay.TotalMilliseconds *
                               Math.Pow(_options.BackoffMultiplier, attempt - 1);

        // Add jitter to prevent thundering herd
        if (_options.UseJitter)
        {
            var jitter = _random.NextDouble() * 0.3 * exponentialDelay; // ±30% jitter
            exponentialDelay += jitter;
        }

        // Cap at max delay
        var delayMs = Math.Min(exponentialDelay, _options.MaxDelay.TotalMilliseconds);

        return TimeSpan.FromMilliseconds(delayMs);
    }

    private bool IsTransientError(Exception ex)
    {
        // Common transient HTTP errors
        if (ex is HttpRequestException httpEx)
        {
            return true; // Network errors are typically transient
        }

        // Timeout errors
        if (ex is TaskCanceledException or TimeoutException)
        {
            return true;
        }

        // Check for specific HTTP status codes in the exception message
        var message = ex.Message.ToLower();
        if (message.Contains("429") ||  // Too Many Requests
            message.Contains("503") ||  // Service Unavailable
            message.Contains("504"))    // Gateway Timeout
        {
            return true;
        }

        return false;
    }
}
