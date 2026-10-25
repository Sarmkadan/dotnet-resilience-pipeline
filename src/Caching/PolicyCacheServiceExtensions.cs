#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Caching;

/// <summary>
/// Extension methods for <see cref="PolicyCacheService"/> and <see cref="CacheStatistics"/>.
/// </summary>
public static class PolicyCacheServiceExtensions
{
    /// <summary>
    /// Returns the cached <see cref="ResiliencyPolicy"/> for the given key, creating and caching it with
    /// <paramref name="factory"/> on a miss. Concurrent misses for the same key invoke the factory only once.
    /// </summary>
    /// <param name="service">The cache service.</param>
    /// <param name="key">The policy name used as the cache key.</param>
    /// <param name="factory">Creates the policy when no valid entry exists for <paramref name="key"/>.</param>
    /// <returns>The cached or newly created policy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/>, <paramref name="key"/>, or <paramref name="factory"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="key"/> is empty or whitespace.</exception>
    /// <exception cref="InvalidOperationException">
    /// Thrown when the factory returns null, or when the cached entry for <paramref name="key"/> does not hold a <see cref="ResiliencyPolicy"/>.
    /// </exception>
    public static ResiliencyPolicy GetOrAdd(this PolicyCacheService service, string key, Func<ResiliencyPolicy> factory)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(factory);

        if (string.IsNullOrWhiteSpace(key))
            throw new ArgumentException("Policy name cannot be empty or whitespace.", nameof(key));

        var cached = service.GetOrLoad(key, name =>
        {
            var policy = factory() ?? throw new InvalidOperationException($"Factory returned null for policy '{name}'.");
            var now = DateTime.UtcNow;

            return new CachedPolicy
            {
                PolicyName = name,
                Config = policy,
                CreatedAt = now,
                ExpiresAt = now.Add(service.DefaultTtl),
                LastAccessTime = now,
                AccessCount = 0
            };
        });

        if (cached?.Config is ResiliencyPolicy policyConfig)
            return policyConfig;

        throw new InvalidOperationException(
            $"Cached entry '{key}' does not hold a {nameof(ResiliencyPolicy)} (found {cached?.Config.GetType().Name ?? "null"}).");
    }

    /// <summary>
    /// Gets the hit rate as a fraction of 1 derived from <see cref="CacheStatistics.HitRate"/>, which is stored as a percentage.
    /// </summary>
    /// <param name="statistics">The statistics snapshot to read.</param>
    /// <returns>The hit rate as a value between 0 and 1 for a valid percentage (for example, 0.5 for 50%).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="statistics"/> is null.</exception>
    public static double GetHitRate(this CacheStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        return statistics.HitRate / 100.0;
    }

    /// <summary>
    /// Determines whether the cache has reached its <see cref="PolicyCacheService.MaxCacheSize"/> limit.
    /// Expired entries still occupy capacity until they are removed, so they count towards the limit.
    /// </summary>
    /// <param name="service">The cache service to check.</param>
    /// <returns>True if the total number of stored entries is at or above <see cref="PolicyCacheService.MaxCacheSize"/>; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="service"/> is null.</exception>
    public static bool IsFull(this PolicyCacheService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        return service.GetStatistics().TotalEntries >= service.MaxCacheSize;
    }
}
