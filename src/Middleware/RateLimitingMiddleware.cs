#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text.Json.Serialization;
using System.Threading;

namespace DotNetResiliencePipeline.Middleware;

/// <summary>
/// Middleware for rate limiting API requests and policy executions.
/// Implements token bucket algorithm with configurable limits per client/policy.
/// </summary>
public sealed class RateLimitingMiddleware
{
    /// <summary>
    /// Default number of requests allowed per second for each client.
    /// </summary>
    public const int DefaultRequestsPerSecond = 100;

    /// <summary>
    /// Default number of requests allowed per minute for each client.
    /// </summary>
    public const int DefaultRequestsPerMinute = 5000;

    [JsonInclude]
    private readonly ConcurrentDictionary<string, RateLimiter> _limiters = new();

    [JsonInclude]
    private readonly object _limitersLock = new object();

    [JsonInclude]
    private int _defaultRequestsPerSecond = DefaultRequestsPerSecond;

    [JsonInclude]
    private int _defaultRequestsPerMinute = DefaultRequestsPerMinute;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitingMiddleware"/> class with default rate limits.
    /// </summary>
    [JsonConstructor]
    public RateLimitingMiddleware()
    {
    }

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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clientId"/> is <c>null</c>.</exception>
    public bool IsRequestAllowed(string clientId, int tokensRequired = 1)
    {
        ArgumentNullException.ThrowIfNull(clientId);
        lock (_limitersLock)
        {
            var limiter = _limiters.GetOrAdd(clientId,
                new RateLimiter(_defaultRequestsPerSecond, _defaultRequestsPerMinute));

            return limiter.TryConsumeTokens(tokensRequired);
        }
    }

    /// <summary>
    /// Gets the current rate limit status for a client.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clientId"/> is <c>null</c>.</exception>
    public RateLimitStatus GetStatus(string clientId)
    {
        ArgumentNullException.ThrowIfNull(clientId);
        lock (_limitersLock)
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
                NextResetSecond = DateTime.UtcNow.AddSeconds(RateLimiter.RefillIntervalSeconds),
                NextResetMinute = DateTime.UtcNow.AddMinutes(RateLimiter.RefillIntervalMinutes)
            };
        }
    }

    /// <summary>
    /// Gets rate limit status for all clients.
    /// </summary>
    public Dictionary<string, RateLimitStatus> GetAllStatus()
    {
        lock (_limitersLock)
        {
            return _limiters.ToDictionary(
                x => x.Key,
                x => x.Value.GetStatus());
        }
    }

    /// <summary>
    /// Resets rate limit for a specific client.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clientId"/> is <c>null</c>.</exception>
    public void ResetClient(string clientId)
    {
        ArgumentNullException.ThrowIfNull(clientId);
        lock (_limitersLock)
        {
            _limiters.TryRemove(clientId, out _);
        }
    }

    /// <summary>
    /// Clears all rate limiters.
    /// </summary>
    public void ClearAll()
    {
        lock (_limitersLock)
        {
            _limiters.Clear();
        }
    }

    /// <summary>
    /// Returns a concise, informative representation of the middleware state.
    /// </summary>
    public override string ToString() =>
        $"RateLimitingMiddleware {{ DefaultRequestsPerSecond = {_defaultRequestsPerSecond}, DefaultRequestsPerMinute = {_defaultRequestsPerMinute}, ActiveClients = {_limiters.Count} }}";
}

/// <summary>
/// Individual rate limiter using token bucket algorithm.
/// </summary>
public sealed class RateLimiter
{
    /// <summary>
    /// Length of the per-second token refill window, in seconds.
    /// </summary>
    public const int RefillIntervalSeconds = 1;

    /// <summary>
    /// Length of the per-minute token refill window, in minutes.
    /// </summary>
    public const int RefillIntervalMinutes = 1;

    [JsonInclude]
    private readonly int _tokensPerSecond;

    [JsonInclude]
    private readonly int _tokensPerMinute;

    [JsonInclude]
    private long _tokensSecond;

    [JsonInclude]
    private long _tokensMinute;

    [JsonInclude]
    private DateTime _lastRefillSecond;

    [JsonInclude]
    private DateTime _lastRefillMinute;

    [JsonIgnore]
    private object _lockObj = new object();

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimiter"/> class from serialized state.
    /// </summary>
    /// <param name="tokensPerSecond">Maximum tokens allowed per second.</param>
    /// <param name="tokensPerMinute">Maximum tokens allowed per minute.</param>
    /// <param name="tokensSecond">Current remaining tokens in the per-second bucket.</param>
    /// <param name="tokensMinute">Current remaining tokens in the per-minute bucket.</param>
    /// <param name="lastRefillSecond">Timestamp of the last per-second token refill.</param>
    /// <param name="lastRefillMinute">Timestamp of the last per-minute token refill.</param>
    [JsonConstructor]
    public RateLimiter(int tokensPerSecond, int tokensPerMinute, long tokensSecond, long tokensMinute, DateTime lastRefillSecond, DateTime lastRefillMinute)
    {
        _tokensPerSecond = tokensPerSecond;
        _tokensPerMinute = tokensPerMinute;
        _tokensSecond = tokensSecond;
        _tokensMinute = tokensMinute;
        _lastRefillSecond = lastRefillSecond;
        _lastRefillMinute = lastRefillMinute;
        _lockObj = new object();
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimiter"/> class with the specified token limits and full buckets.
    /// </summary>
    /// <param name="tokensPerSecond">Maximum tokens allowed per second.</param>
    /// <param name="tokensPerMinute">Maximum tokens allowed per minute.</param>
    public RateLimiter(int tokensPerSecond, int tokensPerMinute)
    {
        _tokensPerSecond = tokensPerSecond;
        _tokensPerMinute = tokensPerMinute;
        _tokensSecond = tokensPerSecond;
        _tokensMinute = tokensPerMinute;
        _lastRefillSecond = DateTime.UtcNow;
        _lastRefillMinute = DateTime.UtcNow;
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
                NextResetSecond = _lastRefillSecond.AddSeconds(RefillIntervalSeconds),
                NextResetMinute = _lastRefillMinute.AddMinutes(RefillIntervalMinutes)
            };
        }
    }

    /// <summary>
    /// Returns a concise, informative representation of the limiter's current token state.
    /// </summary>
    public override string ToString()
    {
        lock (_lockObj)
        {
            return $"RateLimiter {{ RequestsPerSecond = {_tokensPerSecond}, RequestsPerMinute = {_tokensPerMinute}, RemainingTokensPerSecond = {_tokensSecond}, RemainingTokensPerMinute = {_tokensMinute}, LastRefillSecond = {_lastRefillSecond:O}, LastRefillMinute = {_lastRefillMinute:O} }}";
        }
    }

    private void RefillTokens()
    {
        var now = DateTime.UtcNow;

        // Refill per-second bucket
        if ((now - _lastRefillSecond).TotalSeconds >= RefillIntervalSeconds)
        {
            var intervalsPassed = (int)(now - _lastRefillSecond).TotalSeconds;
            _tokensSecond = Math.Min(_tokensSecond + (_tokensPerSecond * intervalsPassed), _tokensPerSecond);
            _lastRefillSecond = now;
        }

        // Refill per-minute bucket
        if ((now - _lastRefillMinute).TotalMinutes >= RefillIntervalMinutes)
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
public sealed class RateLimitStatus
{
    /// <summary>
    /// Identifier of the client the status applies to.
    /// </summary>
    public string? ClientId { get; set; }

    /// <summary>
    /// Configured requests allowed per second.
    /// </summary>
    public int RequestsPerSecond { get; set; }

    /// <summary>
    /// Configured requests allowed per minute.
    /// </summary>
    public int RequestsPerMinute { get; set; }

    /// <summary>
    /// Tokens still available in the current one-second window.
    /// </summary>
    public int RemainingTokensPerSecond { get; set; }

    /// <summary>
    /// Tokens still available in the current one-minute window.
    /// </summary>
    public int RemainingTokensPerMinute { get; set; }

    /// <summary>
    /// Time at which the one-second window resets.
    /// </summary>
    public DateTime NextResetSecond { get; set; }

    /// <summary>
    /// Time at which the one-minute window resets.
    /// </summary>
    public DateTime NextResetMinute { get; set; }

    /// <summary>
    /// Whether the client is currently rate limited (either window has no tokens left).
    /// </summary>
    [JsonIgnore]
    public bool IsLimited => RemainingTokensPerSecond <= 0 || RemainingTokensPerMinute <= 0;

    /// <summary>
    /// Initializes a new instance of the <see cref="RateLimitStatus"/> class with default values.
    /// </summary>
    [JsonConstructor]
    public RateLimitStatus()
    {
    }

    /// <summary>
    /// Returns a concise, informative representation of the rate limit status.
    /// </summary>
    public override string ToString() =>
        $"RateLimitStatus {{ ClientId = {ClientId}, RequestsPerSecond = {RequestsPerSecond}, RequestsPerMinute = {RequestsPerMinute}, RemainingTokensPerSecond = {RemainingTokensPerSecond}, RemainingTokensPerMinute = {RemainingTokensPerMinute}, NextResetSecond = {NextResetSecond:O}, NextResetMinute = {NextResetMinute:O}, IsLimited = {IsLimited} }}";
}
