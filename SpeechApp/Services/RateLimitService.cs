using SpeechApp.Services.Interfaces;
using System.Collections.Concurrent;

namespace SpeechApp.Services;

public class RateLimitService : IRateLimitService
{
    private readonly ConcurrentDictionary<string, TokenBucket> _buckets = new();

    // Default rate limits per provider (requests per minute)
    private readonly Dictionary<string, int> _providerLimits = new()
    {
        ["google"] = 60,      // 60 requests per minute
        ["elevenlabs"] = 30,  // 30 requests per minute
        ["deepgram"] = 120,   // 120 requests per minute
        ["azure"] = 60,       // 60 requests per minute
        ["polly"] = 100       // 100 requests per minute
    };

    public Task<bool> CheckRateLimitAsync(string providerId)
    {
        var bucket = GetOrCreateBucket(providerId);
        return Task.FromResult(bucket.TryConsume());
    }

    public Task RecordRequestAsync(string providerId)
    {
        var bucket = GetOrCreateBucket(providerId);
        bucket.TryConsume();
        return Task.CompletedTask;
    }

    public Task<RateLimitInfo> GetRateLimitInfoAsync(string providerId)
    {
        var bucket = GetOrCreateBucket(providerId);
        var maxRequests = _providerLimits.GetValueOrDefault(providerId, 60);

        var info = new RateLimitInfo
        {
            ProviderId = providerId,
            RequestsRemaining = bucket.GetAvailableTokens(),
            MaxRequests = maxRequests,
            WindowDuration = TimeSpan.FromMinutes(1),
            WindowResetTime = bucket.GetResetTime(),
            IsLimited = !bucket.CanConsume()
        };

        return Task.FromResult(info);
    }

    public Task ResetRateLimitAsync(string providerId)
    {
        _buckets.TryRemove(providerId, out _);
        return Task.CompletedTask;
    }

    private TokenBucket GetOrCreateBucket(string providerId)
    {
        return _buckets.GetOrAdd(providerId, pid =>
        {
            var limit = _providerLimits.GetValueOrDefault(pid, 60);
            return new TokenBucket(limit, TimeSpan.FromMinutes(1));
        });
    }

    // Token Bucket implementation
    private class TokenBucket
    {
        private readonly int _capacity;
        private readonly TimeSpan _refillInterval;
        private int _tokens;
        private DateTime _lastRefillTime;
        private readonly object _lock = new();

        public TokenBucket(int capacity, TimeSpan refillInterval)
        {
            _capacity = capacity;
            _refillInterval = refillInterval;
            _tokens = capacity;
            _lastRefillTime = DateTime.UtcNow;
        }

        public bool TryConsume()
        {
            lock (_lock)
            {
                Refill();
                if (_tokens > 0)
                {
                    _tokens--;
                    return true;
                }
                return false;
            }
        }

        public bool CanConsume()
        {
            lock (_lock)
            {
                Refill();
                return _tokens > 0;
            }
        }

        public int GetAvailableTokens()
        {
            lock (_lock)
            {
                Refill();
                return _tokens;
            }
        }

        public DateTime GetResetTime()
        {
            lock (_lock)
            {
                return _lastRefillTime.Add(_refillInterval);
            }
        }

        private void Refill()
        {
            var now = DateTime.UtcNow;
            var timeSinceLastRefill = now - _lastRefillTime;

            if (timeSinceLastRefill >= _refillInterval)
            {
                var intervalsElapsed = (int)(timeSinceLastRefill / _refillInterval);
                var tokensToAdd = intervalsElapsed * _capacity;
                _tokens = Math.Min(_capacity, _tokens + tokensToAdd);
                _lastRefillTime = _lastRefillTime.Add(_refillInterval * intervalsElapsed);
            }
        }
    }
}
