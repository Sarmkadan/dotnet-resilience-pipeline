#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotNetResiliencePipeline.Caching;

/// <summary>
/// Caching service for policy lookup and configuration caching.
/// Reduces repeated policy lookups and improves performance.
/// </summary>
public class PolicyCacheService
{
    private readonly ConcurrentDictionary<string, CachedPolicy> _cache = new();
    private readonly object _lockObj = new object();

    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(5);
    public int MaxCacheSize { get; set; } = 1000;

    /// <summary>
    /// Gets a cached policy by name.
    /// </summary>
    public CachedPolicy? Get(string policyName)
    {
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
    /// Caches a policy configuration.
    /// </summary>
    public void Set(string policyName, object policyConfig, TimeSpan? ttl = null)
    {
        lock (_lockObj)
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
    public bool Invalidate(string policyName)
    {
        return _cache.TryRemove(policyName, out _);
    }

    /// <summary>
    /// Clears all cache entries.
    /// </summary>
    public void Clear()
    {
        lock (_lockObj)
        {
            _cache.Clear();
        }
    }

    /// <summary>
    /// Gets cache statistics.
    /// </summary>
    public CacheStatistics GetStatistics()
    {
        lock (_lockObj)
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
public class CachedPolicy
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
public class CacheStatistics
{
    public int TotalEntries { get; set; }
    public int ValidEntries { get; set; }
    public int ExpiredEntries { get; set; }
    public double HitRate { get; set; }
    public TimeSpan AverageTtl { get; set; }
}
