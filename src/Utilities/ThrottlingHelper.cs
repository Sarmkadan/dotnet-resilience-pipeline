#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Diagnostics;

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Helper class for throttling and rate limiting at the policy level.
/// Provides leaky bucket and sliding window algorithms for flow control.
/// </summary>
public sealed class ThrottlingHelper
{
    private readonly ConcurrentDictionary<string, Throttle> _throttles = new();

    /// <summary>
    /// Creates or gets a throttle for a policy with rate limits.
    /// </summary>
    public Throttle GetOrCreateThrottle(string policyName, int maxRequestsPerSecond, int burstSize = 0)
    {
        return _throttles.GetOrAdd(policyName, new Throttle(maxRequestsPerSecond, burstSize));
    }

    /// <summary>
    /// Checks if a request should be throttled.
    /// </summary>
    public bool ShouldThrottle(string policyName, int cost = 1)
    {
        if (!_throttles.TryGetValue(policyName, out var throttle))
            return false;

        return !throttle.IsAllowed(cost);
    }

    /// <summary>
    /// Gets throttle statistics for a policy.
    /// </summary>
    public ThrottleStatistics GetStatistics(string policyName)
    {
        if (_throttles.TryGetValue(policyName, out var throttle))
            return throttle.GetStatistics();

        return new ThrottleStatistics { PolicyName = policyName };
    }

    /// <summary>
    /// Gets statistics for all throttles.
    /// </summary>
    public Dictionary<string, ThrottleStatistics> GetAllStatistics()
    {
        return _throttles.ToDictionary(
            x => x.Key,
            x => x.Value.GetStatistics());
    }

    /// <summary>
    /// Resets throttle for a policy.
    /// </summary>
    public void ResetThrottle(string policyName)
    {
        _throttles.TryRemove(policyName, out _);
    }

    /// <summary>
    /// Clears all throttles.
    /// </summary>
    public void Clear()
    {
        _throttles.Clear();
    }
}

/// <summary>
/// Individual throttle implementation using leaky bucket algorithm.
/// </summary>
public sealed class Throttle
{
    private readonly int _maxRate;
    private readonly int _burstSize;
    private double _tokens;
    private DateTime _lastRefill;
    private long _totalRequests;
    private long _allowedRequests;
    private long _throttledRequests;
    private readonly object _lockObj = new object();

    public Throttle(int maxRequestsPerSecond, int burstSize = 0)
    {
        _maxRate = maxRequestsPerSecond;
        _burstSize = burstSize > 0 ? burstSize : maxRequestsPerSecond;
        _tokens = _burstSize;
        _lastRefill = DateTime.UtcNow;
    }

    /// <summary>
    /// Checks if a request is allowed under the current rate limit.
    /// </summary>
    public bool IsAllowed(int cost = 1)
    {
        lock (_lockObj)
        {
            RefillTokens();
            _totalRequests++;

            if (_tokens >= cost)
            {
                _tokens -= cost;
                _allowedRequests++;
                return true;
            }

            _throttledRequests++;
            return false;
        }
    }

    /// <summary>
    /// Gets current throttle statistics.
    /// </summary>
    public ThrottleStatistics GetStatistics()
    {
        lock (_lockObj)
        {
            return new ThrottleStatistics
            {
                MaxRate = _maxRate,
                TotalRequests = _totalRequests,
                AllowedRequests = _allowedRequests,
                ThrottledRequests = _throttledRequests,
                ThrottleRate = _totalRequests > 0 ? (_throttledRequests * 100.0) / _totalRequests : 0,
                AvailableTokens = (int)_tokens,
                BurstCapacity = _burstSize
            };
        }
    }

    /// <summary>
    /// Refills tokens based on time elapsed.
    /// </summary>
    private void RefillTokens()
    {
        var now = DateTime.UtcNow;
        var elapsed = (now - _lastRefill).TotalSeconds;

        if (elapsed > 0)
        {
            var tokensToAdd = _maxRate * elapsed;
            _tokens = Math.Min(_tokens + tokensToAdd, _burstSize);
            _lastRefill = now;
        }
    }
}

/// <summary>
/// Statistics for a throttle.
/// </summary>
public sealed class ThrottleStatistics
{
    public string? PolicyName { get; set; }
    public int MaxRate { get; set; }
    public long TotalRequests { get; set; }
    public long AllowedRequests { get; set; }
    public long ThrottledRequests { get; set; }
    public double ThrottleRate { get; set; }
    public int AvailableTokens { get; set; }
    public int BurstCapacity { get; set; }
    public bool IsThrottling => ThrottledRequests > 0;
}

/// <summary>
/// Extension methods for throttling operations.
/// </summary>
public static class ThrottlingExtensions
{
    /// <summary>
    /// Executes a function with throttling protection.
    /// </summary>
    public static async Task<T> ExecuteWithThrottlingAsync<T>(
        this ThrottlingHelper throttler,
        string policyName,
        Func<Task<T>> operation,
        TimeSpan? retryDelay = null)
    {
        int attempts = 0;
        const int maxAttempts = 3;
        retryDelay ??= TimeSpan.FromMilliseconds(100);

        while (attempts < maxAttempts)
        {
            if (!throttler.ShouldThrottle(policyName))
                return await operation();

            attempts++;
            if (attempts < maxAttempts)
                await Task.Delay(retryDelay.Value);
        }

        throw new InvalidOperationException($"Request throttled after {maxAttempts} attempts for policy: {policyName}");
    }

    /// <summary>
    /// Executes an action with throttling protection.
    /// </summary>
    public static async Task ExecuteWithThrottlingAsync(
        this ThrottlingHelper throttler,
        string policyName,
        Func<Task> operation,
        TimeSpan? retryDelay = null)
    {
        int attempts = 0;
        const int maxAttempts = 3;
        retryDelay ??= TimeSpan.FromMilliseconds(100);

        while (attempts < maxAttempts)
        {
            if (!throttler.ShouldThrottle(policyName))
            {
                await operation();
                return;
            }

            attempts++;
            if (attempts < maxAttempts)
                await Task.Delay(retryDelay.Value);
        }

        throw new InvalidOperationException($"Request throttled after {maxAttempts} attempts for policy: {policyName}");
    }
}
