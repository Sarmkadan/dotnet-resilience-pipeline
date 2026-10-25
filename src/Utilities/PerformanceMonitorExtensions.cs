#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Extension methods for <see cref="PerformanceMonitor"/> and <see cref="PerformanceMetrics"/>.
/// </summary>
public static class PerformanceMonitorExtensions
{
    /// <summary>
    /// Runs <paramref name="operationFunc"/> and records its duration and outcome under <paramref name="operation"/>.
    /// A call that completes normally is recorded as successful; a call that throws is recorded as failed and the exception is rethrown.
    /// Durations are measured in whole milliseconds and rounded up to at least 1 ms, because
    /// <see cref="PerformanceMonitor.RecordExecution"/> ignores zero-duration measurements.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="monitor">The monitor that receives the measurement.</param>
    /// <param name="operation">The name under which the execution is recorded.</param>
    /// <param name="operationFunc">The asynchronous operation to measure.</param>
    /// <returns>The result produced by <paramref name="operationFunc"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="monitor"/>, <paramref name="operation"/> or <paramref name="operationFunc"/> is null.</exception>
    /// <exception cref="Exception">Any exception thrown by <paramref name="operationFunc"/> is propagated unchanged after being recorded as a failure.</exception>
    public static async Task<T> MeasureAsync<T>(this PerformanceMonitor monitor, string operation, Func<Task<T>> operationFunc)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentNullException.ThrowIfNull(operation);
        ArgumentNullException.ThrowIfNull(operationFunc);

        var stopwatch = Stopwatch.StartNew();
        try
        {
            var result = await operationFunc();
            monitor.RecordExecution(operation, ElapsedMs(stopwatch), success: true);
            return result;
        }
        catch
        {
            monitor.RecordExecution(operation, ElapsedMs(stopwatch), success: false);
            throw;
        }
    }

    /// <summary>
    /// Gets the policies with the highest average execution duration, slowest first.
    /// The result is a point-in-time snapshot; each entry is a copy that does not change when the monitor records further executions.
    /// </summary>
    /// <param name="monitor">The monitor to inspect.</param>
    /// <param name="count">The maximum number of policies to return. Zero returns an empty list.</param>
    /// <returns>Up to <paramref name="count"/> metrics, ordered by <see cref="PerformanceMetrics.AverageDurationMs"/> descending.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="monitor"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="count"/> is negative.</exception>
    public static List<PerformanceMetrics> GetSlowestOperations(this PerformanceMonitor monitor, int count)
    {
        ArgumentNullException.ThrowIfNull(monitor);
        ArgumentOutOfRangeException.ThrowIfNegative(count);

        if (count == 0)
            return new List<PerformanceMetrics>();

        return monitor.ComparePerformance()
            .Take(count)
            .Select(comparison => monitor.GetMetrics(comparison.PolicyName))
            .ToList();
    }

    /// <summary>
    /// Determines whether the policy's success rate meets the required minimum.
    /// Metrics with no recorded executions are considered healthy, because there is no evidence of failure.
    /// </summary>
    /// <param name="metrics">The metrics to evaluate.</param>
    /// <param name="minSuccessRate">The minimum acceptable success rate, as a percentage between 0 and 100 inclusive.</param>
    /// <returns><c>true</c> if there are no executions or <see cref="PerformanceMetrics.SuccessRate"/> is at least <paramref name="minSuccessRate"/>; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="minSuccessRate"/> is NaN or outside the range 0 to 100.</exception>
    public static bool IsHealthy(this PerformanceMetrics metrics, double minSuccessRate)
    {
        ArgumentNullException.ThrowIfNull(metrics);
        if (double.IsNaN(minSuccessRate) || minSuccessRate < 0 || minSuccessRate > 100)
            throw new ArgumentOutOfRangeException(nameof(minSuccessRate), minSuccessRate, "Minimum success rate must be between 0 and 100 (percentage).");

        if (metrics.TotalExecutions == 0)
            return true;

        return metrics.SuccessRate >= minSuccessRate;
    }

    private static long ElapsedMs(Stopwatch stopwatch) =>
        Math.Max(1L, stopwatch.ElapsedMilliseconds);
}
