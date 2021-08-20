// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotNetResiliencePipeline.Middleware;

/// <summary>
/// Middleware for rate limiting API requests and policy executions.
/// Implements token bucket algorithm with configurable limits per client/policy.
/// </summary>
public class RateLimitingMiddleware
{
    private readonly ConcurrentDictionary<string, RateLimiter> _limiters = new();
    private int _defaultRequestsPerSecond = 100;
    private int _defaultRequestsPerMinute = 5000;

    /// <summary>
    /// Configures default rate limits for the middleware.
    /// </summary>
    public void ConfigureLimits(int requestsPerSecond, int requestsPerMinute)
    {
        _defaultRequestsPerSecond = requestsPerSecond;
        _defaultRequestsPerMinute = requestsPerMinute;
    }

    /// <summary>
    /// Checks if a request is allowed under rate limit for a client.
    /// </summary>
    public bool IsRequestAllowed(string clientId, int tokensRequired = 1)
    {
        var limiter = _limiters.GetOrAdd(clientId,
            new RateLimiter(_defaultRequestsPerSecond, _defaultRequestsPerMinute));

        return limiter.TryConsumeTokens(tokensRequired);
    }

    /// <summary>
    /// Gets the current rate limit status for a client.
    /// </summary>
    public RateLimitStatus GetStatus(string clientId)
    {
        if (_limiters.TryGetValue(clientId, out var limiter))
            return limiter.GetStatus();

        // Return default limits for unknown client
        return new RateLimitStatus
        {
            ClientId = clientId,
            RequestsPerSecond = _defaultRequestsPerSecond,
            RequestsPerMinute = _defaultRequestsPerMinute,
            RemainingTokensPerSecond = _defaultRequestsPerSecond,
            RemainingTokensPerMinute = _defaultRequestsPerMinute,
            NextResetSecond = DateTime.UtcNow.AddSeconds(1),
            NextResetMinute = DateTime.UtcNow.AddMinutes(1)
        };
    }

    /// <summary>
    /// Gets rate limit status for all clients.
    /// </summary>
    public Dictionary<string, RateLimitStatus> GetAllStatus()
    {
        return _limiters.ToDictionary(
            x => x.Key,
            x => x.Value.GetStatus());
    }

    /// <summary>
    /// Resets rate limit for a specific client.
    /// </summary>
    public void ResetClient(string clientId)
    {
        _limiters.TryRemove(clientId, out _);
    }

    /// <summary>
    /// Clears all rate limiters.
    /// </summary>
    public void ClearAll()
    {
        _limiters.Clear();
    }
}

/// <summary>
/// Individual rate limiter using token bucket algorithm.
/// </summary>
public class RateLimiter
{
    private readonly int _tokensPerSecond;
    private readonly int _tokensPerMinute;
    private long _tokensSecond;
    private long _tokensMinute;
    private DateTime _lastRefillSecond = DateTime.UtcNow;
    private DateTime _lastRefillMinute = DateTime.UtcNow;
    private readonly object _lockObj = new object();

    public RateLimiter(int tokensPerSecond, int tokensPerMinute)
    {
        _tokensPerSecond = tokensPerSecond;
        _tokensPerMinute = tokensPerMinute;
        _tokensSecond = tokensPerSecond;
        _tokensMinute = tokensPerMinute;
    }

    /// <summary>
    /// Attempts to consume tokens. Returns true if successful.
    /// </summary>
    public bool TryConsumeTokens(int tokensRequired)
    {
        lock (_lockObj)
        {
            RefillTokens();

            // Check both limits
            if (_tokensSecond >= tokensRequired && _tokensMinute >= tokensRequired)
            {
                _tokensSecond -= tokensRequired;
                _tokensMinute -= tokensRequired;
                return true;
            }

            return false;
        }
    }

    /// <summary>
    /// Gets current rate limit status.
    /// </summary>
    public RateLimitStatus GetStatus()
    {
        lock (_lockObj)
        {
            RefillTokens();

            return new RateLimitStatus
            {
                RequestsPerSecond = _tokensPerSecond,
                RequestsPerMinute = _tokensPerMinute,
                RemainingTokensPerSecond = (int)Math.Min(_tokensSecond, _tokensPerSecond),
                RemainingTokensPerMinute = (int)Math.Min(_tokensMinute, _tokensPerMinute),
                NextResetSecond = _lastRefillSecond.AddSeconds(1),
                NextResetMinute = _lastRefillMinute.AddMinutes(1)
            };
        }
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;

        // Refill per-second bucket
        if ((now - _lastRefillSecond).TotalSeconds >= 1)
        {
            var intervalsPassed = (int)(now - _lastRefillSecond).TotalSeconds;
            _tokensSecond = Math.Min(_tokensSecond + (_tokensPerSecond * intervalsPassed), _tokensPerSecond);
            _lastRefillSecond = now;
        }

        // Refill per-minute bucket
        if ((now - _lastRefillMinute).TotalMinutes >= 1)
        {
            var intervalsPassed = (int)(now - _lastRefillMinute).TotalMinutes;
            _tokensMinute = Math.Min(_tokensMinute + (_tokensPerMinute * intervalsPassed), _tokensPerMinute);
            _lastRefillMinute = now;
        }
    }
}

/// <summary>
/// Rate limit status for a client.
/// </summary>
public class RateLimitStatus
{
    public string? ClientId { get; set; }
    public int RequestsPerSecond { get; set; }
    public int RequestsPerMinute { get; set; }
    public int RemainingTokensPerSecond { get; set; }
    public int RemainingTokensPerMinute { get; set; }
    public DateTime NextResetSecond { get; set; }
    public DateTime NextResetMinute { get; set; }
    public bool IsLimited => RemainingTokensPerSecond <= 0 || RemainingTokensPerMinute <= 0;
}
