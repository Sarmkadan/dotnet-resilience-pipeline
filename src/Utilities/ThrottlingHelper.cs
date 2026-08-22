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
    private readonly LinkedList<string> _lruList = new();
    private readonly object _evictionLock = new object();

    /// <summary>
    /// Gets or sets the maximum number of throttles that can be tracked.
    /// When exceeded, least recently used throttles are evicted.
    /// </summary>
    public int MaxThrottles { get; set; } = 1000;

    /// <summary>
    /// Creates or gets a throttle for a policy with rate limits.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the maximum number of throttles is exceeded and eviction fails.</exception>
    public Throttle GetOrCreateThrottle(string policyName, int maxRequestsPerSecond, int burstSize = 0)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        // Try to get existing throttle first
        if (_throttles.TryGetValue(policyName, out var existingThrottle))
        {
            UpdateLru(policyName);
            return existingThrottle;
        }

        // Create new throttle
        var newThrottle = new Throttle(maxRequestsPerSecond, burstSize);

        // Add to dictionary
        var added = _throttles.TryAdd(policyName, newThrottle);
        if (!added)
        {
            // Concurrent add failed, try to get again
            if (_throttles.TryGetValue(policyName, out existingThrottle))
            {
                UpdateLru(policyName);
                return existingThrottle;
            }
            throw new InvalidOperationException("Failed to add throttle for policy: " + policyName);
        }

        UpdateLru(policyName);

        // Enforce size limit
        EnforceSizeLimit();

        return newThrottle;
    }

    /// <summary>
    /// Checks if a request should be throttled.
    /// </summary>
    public bool ShouldThrottle(string policyName, int cost = 1)
    {
        ArgumentException.ThrowIfNullOrEmpty(policyName);

        if (!_throttles.TryGetValue(policyName, out var throttle))
            return false;

        return !throttle.IsAllowed(cost);
    }

    /// <summary>
    /// Gets throttle statistics for a policy.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is null or whitespace.</exception>
    public ThrottleStatistics GetStatistics(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        if (_throttles.TryGetValue(policyName, out var throttle))
        {
            UpdateLru(policyName);
            return throttle.GetStatistics();
        }

        return new ThrottleStatistics { PolicyName = policyName };
    }

    /// <summary>
    /// Gets statistics for all throttles.
    /// </summary>
    public Dictionary<string, ThrottleStatistics> GetAllStatistics()
    {
        // Update LRU for all entries
        foreach (var key in _throttles.Keys.ToList())
        {
            UpdateLru(key);
        }

        return _throttles.ToDictionary(
            x => x.Key,
            x => x.Value.GetStatistics());
    }

    /// <summary>
    /// Resets throttle for a policy.
    /// </summary>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is null or whitespace.</exception>
    public void ResetThrottle(string policyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        _throttles.TryRemove(policyName, out _);
        RemoveFromLru(policyName);
    }

    /// <summary>
    /// Clears all throttles.
    /// </summary>
    public void Clear()
    {
        lock (_evictionLock)
        {
            _throttles.Clear();
            _lruList.Clear();
        }
    }

    /// <summary>
    /// Removes expired throttles based on LRU tracking.
    /// </summary>
    /// <returns>Number of throttles removed.</returns>
    public int CleanupExpired()
    {
        // Note: Throttle doesn't track expiration, so we can't clean up based on that
        // This method is kept for API consistency with similar services
        return 0;
    }

    private void UpdateLru(string policyName)
    {
        lock (_evictionLock)
        {
            // Remove from current position if exists
            _lruList.Remove(policyName);
            // Add to end (most recently used)
            _lruList.AddLast(policyName);
        }
    }

    private void RemoveFromLru(string policyName)
    {
        lock (_evictionLock)
        {
            _lruList.Remove(policyName);
        }
    }

    private void EnforceSizeLimit()
    {
        lock (_evictionLock)
        {
            if (_throttles.Count <= MaxThrottles)
                return;

            // Evict least recently used throttles until we're under the limit
            while (_throttles.Count > MaxThrottles && _lruList.Count > 0)
            {
                var lruKey = _lruList.First?.Value;
                if (lruKey != null && _throttles.TryRemove(lruKey, out _))
                {
                    _lruList.RemoveFirst();
                }
                else if (lruKey != null)
                {
                    // Key not found in dictionary, remove from LRU list
                    _lruList.RemoveFirst();
                }
                else
                {
                    // Empty list, break to avoid infinite loop
                    break;
                }
            }
        }
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
    /// <summary>Gets the policy name.</summary>
    public string? PolicyName { get; set; }

    /// <summary>Gets the maximum allowed requests per second.</summary>
    public int MaxRate { get; set; }

    /// <summary>Gets the total number of requests processed.</summary>
    public long TotalRequests { get; set; }

    /// <summary>Gets the number of requests that were allowed.</summary>
    public long AllowedRequests { get; set; }

    /// <summary>Gets the number of requests that were throttled.</summary>
    public long ThrottledRequests { get; set; }

    /// <summary>Gets the throttle rate as a percentage (0-100).</summary>
    public double ThrottleRate { get; set; }

    /// <summary>Gets the number of available tokens in the bucket.</summary>
    public int AvailableTokens { get; set; }

    /// <summary>Gets the burst capacity of the throttle.</summary>
    public int BurstCapacity { get; set; }

    /// <summary>Gets whether throttling is currently active.</summary>
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
        ArgumentNullException.ThrowIfNull(throttler);
        ArgumentException.ThrowIfNullOrEmpty(policyName);
        ArgumentNullException.ThrowIfNull(operation);

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
        ArgumentNullException.ThrowIfNull(throttler);
        ArgumentException.ThrowIfNullOrEmpty(policyName);
        ArgumentNullException.ThrowIfNull(operation);

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
