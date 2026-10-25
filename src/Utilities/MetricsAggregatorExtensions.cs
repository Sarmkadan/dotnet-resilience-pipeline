#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Extension methods for <see cref="AggregatedMetrics"/> and for ranking per-policy <see cref="PerformanceMetrics"/>.
/// Percentages use the 0-100 scale employed by <see cref="AggregatedMetrics.AverageSuccessRate"/>.
/// </summary>
public static class MetricsAggregatorExtensions
{
    private const double MinPercentage = 0;
    private const double MaxPercentage = 100;

    /// <summary>
    /// Gets the error rate for an aggregation window, as a percentage (100 minus the average success rate).
    /// Returns 0 when the window contains no snapshots, because no failures were observed.
    /// </summary>
    /// <param name="metrics">The aggregated metrics to evaluate.</param>
    /// <returns>The error rate in the range 0-100.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
    public static double GetErrorRate(this AggregatedMetrics metrics)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        if (metrics.SnapshotCount == 0)
        {
            return MinPercentage;
        }

        return Math.Clamp(MaxPercentage - metrics.AverageSuccessRate, MinPercentage, MaxPercentage);
    }

    /// <summary>
    /// Determines whether an aggregation window meets a service-level objective: the average success rate
    /// must be at least <paramref name="minSuccessRate"/> and the average execution time must not exceed
    /// <paramref name="maxAverageExecutionTimeMs"/>. The latency bound is checked against the average because
    /// <see cref="AggregatedMetrics"/> does not carry percentiles.
    /// Returns false when the window contains no snapshots, because the objective cannot be confirmed.
    /// </summary>
    /// <param name="metrics">The aggregated metrics to evaluate.</param>
    /// <param name="minSuccessRate">The minimum acceptable average success rate, as a percentage (0-100).</param>
    /// <param name="maxAverageExecutionTimeMs">The maximum acceptable average execution time, in milliseconds.</param>
    /// <returns>True when both bounds are met; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="minSuccessRate"/> is NaN or outside 0-100, or when <paramref name="maxAverageExecutionTimeMs"/> is negative.</exception>
    public static bool IsWithinSlo(this AggregatedMetrics metrics, double minSuccessRate, long maxAverageExecutionTimeMs)
    {
        ArgumentNullException.ThrowIfNull(metrics);

        if (double.IsNaN(minSuccessRate) || minSuccessRate < MinPercentage || minSuccessRate > MaxPercentage)
        {
            throw new ArgumentOutOfRangeException(nameof(minSuccessRate), minSuccessRate, "Minimum success rate must be between 0 and 100.");
        }

        ArgumentOutOfRangeException.ThrowIfNegative(maxAverageExecutionTimeMs);

        if (metrics.SnapshotCount == 0)
        {
            return false;
        }

        return metrics.AverageSuccessRate >= minSuccessRate
            && metrics.AverageExecutionTimeMs <= maxAverageExecutionTimeMs;
    }

    /// <summary>
    /// Returns the most active policies, ordered by total execution count (descending).
    /// Ties are broken by policy name (ordinal) so the result is deterministic.
    /// </summary>
    /// <param name="policyMetrics">The per-policy metrics to rank.</param>
    /// <param name="count">The maximum number of policies to return; 0 returns an empty list.</param>
    /// <returns>Up to <paramref name="count"/> policy metrics, most active first.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policyMetrics"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    public static IReadOnlyList<PerformanceMetrics> GetTopPolicies(this IEnumerable<PerformanceMetrics> policyMetrics, int count)
    {
        ArgumentNullException.ThrowIfNull(policyMetrics);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        return policyMetrics
            .OrderByDescending(m => m.TotalExecutions)
            .ThenBy(m => m.PolicyName, StringComparer.Ordinal)
            .Take(count)
            .ToList();
    }
}
