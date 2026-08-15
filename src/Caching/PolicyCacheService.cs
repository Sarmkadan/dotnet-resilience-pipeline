#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Caching;

/// <summary>
/// Caching service for policy lookup and configuration caching.
/// Reduces repeated policy lookups and improves performance.
/// </summary>
public sealed class PolicyCacheService
{
    private readonly ConcurrentDictionary<string, CachedPolicy> _cache = new();
    private readonly ConcurrentDictionary<string, SemaphoreSlim> _locks = new();
    private readonly object _globalLockObj = new object();

    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or creates a SemaphoreSlim for the given policy name.
    /// </summary>
    private SemaphoreSlim GetLockForKey(string policyName)
    {
        return _locks.GetOrAdd(policyName, _ => new SemaphoreSlim(1, 1));
    }
    public int MaxCacheSize { get; set; } = 1000;

    /// <summary>
    /// Gets a cached policy by name.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when policyName is null.</exception>
    public CachedPolicy? Get(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
            throw new ArgumentNullException(nameof(policyName), "Policy name cannot be null or whitespace");

        if (_cache.TryGetValue(policyName, out var cached))
        {
            if (!cached.IsExpired)
            {
                cached.AccessCount++;
                cached.LastAccessTime = DateTime.UtcNow;
                return cached;
            }

            // Remove expired entry
            _cache.TryRemove(policyName, out _);
        }

        return null;
    }

    /// <summary>
    /// Gets a cached policy by name, loading it if not present using per-key locking.
    /// Ensures that concurrent misses for the same key only load once.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when policyName is null.</exception>
    public CachedPolicy? GetOrLoad(string policyName, Func<string, CachedPolicy> loadFunc)
    {
        if (string.IsNullOrWhiteSpace(policyName))
            throw new ArgumentNullException(nameof(policyName), "Policy name cannot be null or whitespace");

        if (loadFunc is null)
            throw new ArgumentNullException(nameof(loadFunc));

        // Try to get from cache first
        var cached = Get(policyName);
        if (cached is not null)
        {
            return cached;
        }

        // Use per-key locking to ensure only one thread loads the policy
        var keyLock = GetLockForKey(policyName);
        keyLock.Wait();
        try
        {
            // Double-check after acquiring lock
            cached = Get(policyName);
            if (cached is not null)
            {
                return cached;
            }

            // Load the policy
            cached = loadFunc(policyName);
            if (cached is not null)
            {
                Set(cached.PolicyName, cached.Config, cached.RemainingTtl);
            }

            return cached;
        }
        finally
        {
            keyLock.Release();
        }
    }

    /// <summary>
    /// Caches a policy configuration.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when policyName or policyConfig is null.</exception>
    /// <exception cref="ConfigurationException">Thrown when cache size limit is exceeded.</exception>
    public void Set(string policyName, object policyConfig, TimeSpan? ttl = null)
    {
        if (string.IsNullOrWhiteSpace(policyName))
            throw new ArgumentNullException(nameof(policyName), "Policy name cannot be null or whitespace");

        if (policyConfig is null)
            throw new ArgumentNullException(nameof(policyConfig));

        lock (_globalLockObj)
        {
            // Enforce size limit
            if (_cache.Count >= MaxCacheSize)
                EvictLRU();

            var cached = new CachedPolicy
            {
                PolicyName = policyName,
                Config = policyConfig,
                CreatedAt = DateTime.UtcNow,
                ExpiresAt = DateTime.UtcNow.Add(ttl ?? DefaultTtl),
                AccessCount = 1
            };

            _cache.AddOrUpdate(policyName, cached, (k, v) => cached);
        }
    }

    /// <summary>
    /// Invalidates a cached policy.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when policyName is null.</exception>
    public bool Invalidate(string policyName)
    {
        if (string.IsNullOrWhiteSpace(policyName))
            throw new ArgumentNullException(nameof(policyName), "Policy name cannot be null or whitespace");

        return _cache.TryRemove(policyName, out _);
    }

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    public void Clear()
    {
        lock (_globalLockObj)
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        lock (_globalLockObj)
        {
            var validEntries = _cache.Values.Where(c => !c.IsExpired).ToList();

            return new CacheStatistics
            {
                TotalEntries = _cache.Count,
                ValidEntries = validEntries.Count,
                ExpiredEntries = _cache.Count - validEntries.Count,
                HitRate = validEntries.Count > 0
                    ? (validEntries.Sum(c => c.AccessCount) * 100.0) / validEntries.Sum(c => c.AccessCount)
                    : 0,
                AverageTtl = validEntries.Count > 0
                    ? TimeSpan.FromMilliseconds(validEntries.Average(c => (c.ExpiresAt - c.CreatedAt).TotalMilliseconds))
                    : TimeSpan.Zero
            };
        }
    }

    /// <summary>
    /// Removes expired entries.
    /// </summary>
    /// <returns>Number of expired entries removed.</returns>
    public int CleanupExpired()
    {
        var expiredKeys = _cache.Where(x => x.Value.IsExpired).Select(x => x.Key).ToList();

        foreach (var key in expiredKeys)
            _cache.TryRemove(key, out _);

        return expiredKeys.Count;
    }

    private void EvictLRU()
    {
        // Evict least recently used entry
        var lru = _cache.Values.OrderBy(c => c.LastAccessTime).FirstOrDefault();
        if (lru is not null)
            _cache.TryRemove(lru.PolicyName, out _);
    }
}

/// <summary>
/// Cached policy configuration.
/// </summary>
public sealed class CachedPolicy
{
    public string PolicyName { get; set; } = string.Empty;
    public object Config { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
    public DateTime LastAccessTime { get; set; }
    public long AccessCount { get; set; }

    public bool IsExpired => DateTime.UtcNow > ExpiresAt;
    public TimeSpan RemainingTtl => ExpiresAt - DateTime.UtcNow;
}

/// <summary>
/// Cache statistics and metrics.
/// </summary>
public sealed class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int ValidEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public double HitRate { get; set; }
    public TimeSpan AverageTtl { get; set; }
}
