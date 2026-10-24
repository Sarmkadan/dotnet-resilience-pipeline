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

    /// <summary>
    /// Gets or sets the time-to-live applied to entries stored without an explicit TTL.
    /// </summary>
    public TimeSpan DefaultTtl { get; set; } = TimeSpan.FromMinutes(5);

    /// <summary>
    /// Gets or creates a SemaphoreSlim for the given policy name.
    /// </summary>
    private SemaphoreSlim GetLockForKey(string policyName)
    {
        return _locks.GetOrAdd(policyName, _ => new SemaphoreSlim(1, 1));
    }

    /// <summary>
    /// Gets or sets the maximum number of entries kept before the least recently used entry is evicted.
    /// </summary>
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
                Store(cached);
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

        var now = DateTime.UtcNow;
        Store(new CachedPolicy
        {
            PolicyName = policyName,
            Config = policyConfig,
            CreatedAt = now,
            ExpiresAt = now.Add(ttl ?? DefaultTtl),
            AccessCount = 0
        });
    }

    /// <summary>
    /// Stores the given entry as-is, enforcing the cache size limit.
    /// </summary>
    private void Store(CachedPolicy entry)
    {
        lock (_globalLockObj)
        {
            // Enforce size limit
            if (_cache.Count >= MaxCacheSize)
                EvictLRU();

            _cache.AddOrUpdate(entry.PolicyName, entry, (k, v) => entry);
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
                HitRate = validEntries.Sum(c => c.AccessCount) > 0
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

    /// <summary>
    /// Returns a string representation of the cache service, including settings and one sample entry.
    /// </summary>
    public override string ToString()
    {
        var firstEntry = _cache.FirstOrDefault();
        if (firstEntry.Value != null)
        {
            var cached = firstEntry.Value;
            return $"PolicyCacheService {{ DefaultTtl = {DefaultTtl}, MaxCacheSize = {MaxCacheSize}, PolicyName = {cached.PolicyName}, Config = {cached.Config}, CreatedAt = {cached.CreatedAt}, ExpiresAt = {cached.ExpiresAt} }}";
        }
        else
        {
            return $"PolicyCacheService {{ DefaultTtl = {DefaultTtl}, MaxCacheSize = {MaxCacheSize}, PolicyName = (none), Config = (none), CreatedAt = (none), ExpiresAt = (none) }}";
        }
    }
}

/// <summary>
/// Cached policy configuration.
/// </summary>
public sealed class CachedPolicy
{
    /// <summary>
    /// Gets or sets the name of the cached policy, used as the cache key.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the cached policy configuration object.
    /// </summary>
    public object Config { get; set; } = new();

    /// <summary>
    /// Gets or sets the UTC time at which the entry was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC time after which the entry is considered expired.
    /// </summary>
    public DateTime ExpiresAt { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the entry was last read through the cache.
    /// </summary>
    public DateTime LastAccessTime { get; set; }

    /// <summary>
    /// Gets or sets the number of times the entry has been read through the cache.
    /// </summary>
    public long AccessCount { get; set; }

    /// <summary>
    /// Gets a value indicating whether the entry has passed its expiration time.
    /// </summary>
    public bool IsExpired => DateTime.UtcNow > ExpiresAt;

    /// <summary>
    /// Gets the time remaining until the entry expires; negative once it has expired.
    /// </summary>
    public TimeSpan RemainingTtl => ExpiresAt - DateTime.UtcNow;
}

/// <summary>
/// Cache statistics and metrics.
/// </summary>
public sealed class CacheStatistics
{
    /// <summary>
    /// Gets or sets the total number of entries in the cache, including expired ones.
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Gets or sets the number of entries that have not expired.
    /// </summary>
    public int ValidEntries { get; set; }

    /// <summary>
    /// Gets or sets the number of entries that have expired but are still stored.
    /// </summary>
    public int ExpiredEntries { get; set; }

    /// <summary>
    /// Gets or sets the hit rate, as a percentage of accesses across valid entries.
    /// </summary>
    public double HitRate { get; set; }

    /// <summary>
    /// Gets or sets the average time-to-live of valid entries.
    /// </summary>
    public TimeSpan AverageTtl { get; set; }

    /// <summary>
    /// Returns a string representation of the CacheStatistics object.
    /// </summary>
    public override string ToString()
    {
        return $"CacheStatistics {{ TotalEntries = {TotalEntries}, ValidEntries = {ValidEntries}, ExpiredEntries = {ExpiredEntries}, HitRate = {HitRate}, AverageTtl = {AverageTtl} }}";
    }
}
