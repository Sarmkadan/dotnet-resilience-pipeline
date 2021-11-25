using System;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Extension methods for TimeoutBenchmarks to provide additional utility functionality.
/// </summary>
public static class TimeoutBenchmarksExtensions
{
    /// <summary>
    /// Gets the average execution time from the timeout policy.
    /// </summary>
    /// <param name="benchmarks">The TimeoutBenchmarks instance. Cannot be null.</param>
    /// <returns>The average execution time in milliseconds, or 0 if no data is available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static double TimeoutPolicy_GetAverageExecutionTime(this TimeoutBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        return benchmarks._timeoutPolicy?.AverageExecutionTimeMs ?? 0.0;
    }

    /// <summary>
    /// Gets the maximum execution time from recorded execution times.
    /// </summary>
    /// <param name="benchmarks">The TimeoutBenchmarks instance. Cannot be null.</param>
    /// <returns>The maximum execution time in milliseconds, or 0 if no data is available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static long TimeoutPolicy_GetMaxExecutionTime(this TimeoutBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        return benchmarks._timeoutPolicy?.LongestExecutionTimeMs ?? 0L;
    }

    /// <summary>
    /// Gets the minimum execution time from recorded execution times.
    /// </summary>
    /// <param name="benchmarks">The TimeoutBenchmarks instance. Cannot be null.</param>
    /// <returns>The minimum execution time in milliseconds, or 0 if no data is available.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static long TimeoutPolicy_GetMinExecutionTime(this TimeoutBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        return benchmarks._timeoutPolicy?.ShortestExecutionTimeMs ?? 0L;
    }

    /// <summary>
    /// Calculates the success rate based on timeout occurrences.
    /// </summary>
    /// <param name="benchmarks">The TimeoutBenchmarks instance. Cannot be null.</param>
    /// <returns>Success rate (1.0 = 100% success, 0.0 = 0% success).</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static double TimeoutPolicy_GetSuccessRate(this TimeoutBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var timeoutPolicy = benchmarks._timeoutPolicy;
        if (timeoutPolicy == null)
        {
            return 0.0;
        }

        var totalOperations = timeoutPolicy.TotalExecutions;
        if (totalOperations == 0)
        {
            return 0.0;
        }

        return Math.Max(0.0, 1.0 - (double)timeoutPolicy.TimeoutCount / totalOperations);
    }
}