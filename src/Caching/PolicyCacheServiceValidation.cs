#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotNetResiliencePipeline.Caching;

/// <summary>
/// Provides validation helpers for <see cref="PolicyCacheService"/> instances.
/// </summary>
public static class PolicyCacheServiceValidation
{
    /// <summary>
    /// Validates a <see cref="PolicyCacheService"/> instance and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <returns>An immutable list of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this PolicyCacheService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate DefaultTtl
        if (value.DefaultTtl <= TimeSpan.Zero)
        {
            problems.Add(
                $"DefaultTtl must be positive, but was {value.DefaultTtl.TotalMilliseconds}ms.");
        }

        // Validate MaxCacheSize
        if (value.MaxCacheSize <= 0)
        {
            problems.Add(
                $"MaxCacheSize must be positive, but was {value.MaxCacheSize}.");
        }

        // Validate PolicyName (if there are any cached entries)
        if (value.GetStatistics().TotalEntries > 0)
        {
            // Note: We can't easily validate individual policy names without iterating cache
            // The Get/Set methods already validate policy names on access
        }

        // Note: CreatedAt, ExpiresAt, LastAccessTime, and AccessCount are properties
        // of individual CachedPolicy entries, not the PolicyCacheService itself.
        // These are validated through GetStatistics() which aggregates them.
        // The service doesn't expose these properties directly.

        // Validate TotalEntries (should match actual cache state)
        var stats = value.GetStatistics();
        if (stats.TotalEntries < 0)
        {
            problems.Add(
                $"TotalEntries must be non-negative, but was {stats.TotalEntries}.");
        }

        // Validate ValidEntries and ExpiredEntries consistency
        if (stats.ValidEntries < 0 || stats.ExpiredEntries < 0)
        {
            problems.Add(
                $"ValidEntries and ExpiredEntries must be non-negative, but were {stats.ValidEntries} and {stats.ExpiredEntries}.");
        }

        if (stats.TotalEntries != stats.ValidEntries + stats.ExpiredEntries)
        {
            problems.Add(
                $"TotalEntries ({stats.TotalEntries}) must equal ValidEntries ({stats.ValidEntries}) + ExpiredEntries ({stats.ExpiredEntries}).");
        }

        // Validate HitRate (should be between 0 and 100)
        if (stats.HitRate < 0 || stats.HitRate > 100)
        {
            problems.Add(
                $"HitRate must be between 0 and 100, but was {stats.HitRate:F2}%.");
        }

        // Validate AverageTtl (should be non-negative)
        if (stats.AverageTtl < TimeSpan.Zero)
        {
            problems.Add(
                $"AverageTtl must be non-negative, but was {stats.AverageTtl.TotalMilliseconds}ms.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether a <see cref="PolicyCacheService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    public static bool IsValid(this PolicyCacheService? value)
    {
        return value is not null && Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="PolicyCacheService"/> instance is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this PolicyCacheService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = Validate(value);
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"PolicyCacheService is invalid. Problems:{Environment.NewLine}  - {
            string.Join($"{Environment.NewLine}  - ", problems)
            }");
    }
}