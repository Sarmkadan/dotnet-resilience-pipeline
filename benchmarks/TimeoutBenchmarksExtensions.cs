using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Extension methods for TimeoutBenchmarks to provide additional utility functionality
/// </summary>
public static class TimeoutBenchmarksExtensions
{
    /// <summary>
    /// Calculates the average execution time from recorded execution times
    /// </summary>
    /// <param name="benchmarks">The TimeoutBenchmarks instance</param>
    /// <returns>The average execution time in milliseconds</returns>
    public static double TimeoutPolicy_GetAverageExecutionTime(this TimeoutBenchmarks benchmarks)
    {
        if (benchmarks == null)
            throw new ArgumentNullException(nameof(benchmarks));

        // Use reflection to get the private _timeoutPolicy field
        var timeoutPolicyField = typeof(TimeoutBenchmarks).GetField(
            "_timeoutPolicy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (timeoutPolicyField == null)
            return 0;

        var timeoutPolicy = timeoutPolicyField.GetValue(benchmarks) as TimeoutPolicy;

        if (timeoutPolicy == null)
            return 0;

        // Access the execution times through reflection
        var executionTimesField = typeof(TimeoutPolicy).GetField(
            "_executionTimes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (executionTimesField == null)
            return 0;

        var executionTimes = executionTimesField.GetValue(timeoutPolicy) as List<long>;

        if (executionTimes == null || executionTimes.Count == 0)
            return 0;

        return executionTimes.Average();
    }

    /// <summary>
    /// Gets the maximum execution time from recorded execution times
    /// </summary>
    /// <param name="benchmarks">The TimeoutBenchmarks instance</param>
    /// <returns>The maximum execution time in milliseconds</returns>
    public static long TimeoutPolicy_GetMaxExecutionTime(this TimeoutBenchmarks benchmarks)
    {
        if (benchmarks == null)
            throw new ArgumentNullException(nameof(benchmarks));

        var timeoutPolicyField = typeof(TimeoutBenchmarks).GetField(
            "_timeoutPolicy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (timeoutPolicyField == null)
            return 0;

        var timeoutPolicy = timeoutPolicyField.GetValue(benchmarks) as TimeoutPolicy;

        if (timeoutPolicy == null)
            return 0;

        var executionTimesField = typeof(TimeoutPolicy).GetField(
            "_executionTimes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (executionTimesField == null)
            return 0;

        var executionTimes = executionTimesField.GetValue(timeoutPolicy) as List<long>;

        return executionTimes?.Max() ?? 0;
    }

    /// <summary>
    /// Gets the minimum execution time from recorded execution times
    /// </summary>
    /// <param name="benchmarks">The TimeoutBenchmarks instance</param>
    /// <returns>The minimum execution time in milliseconds</returns>
    public static long TimeoutPolicy_GetMinExecutionTime(this TimeoutBenchmarks benchmarks)
    {
        if (benchmarks == null)
            throw new ArgumentNullException(nameof(benchmarks));

        var timeoutPolicyField = typeof(TimeoutBenchmarks).GetField(
            "_timeoutPolicy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (timeoutPolicyField == null)
            return 0;

        var timeoutPolicy = timeoutPolicyField.GetValue(benchmarks) as TimeoutPolicy;

        if (timeoutPolicy == null)
            return 0;

        var executionTimesField = typeof(TimeoutPolicy).GetField(
            "_executionTimes",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (executionTimesField == null)
            return 0;

        var executionTimes = executionTimesField.GetValue(timeoutPolicy) as List<long>;

        return executionTimes?.Min() ?? 0;
    }

    /// <summary>
    /// Calculates the success rate based on timeout occurrences
    /// </summary>
    /// <param name="benchmarks">The TimeoutBenchmarks instance</param>
    /// <returns>Success rate (1.0 = 100% success, 0.0 = 0% success)</returns>
    public static double TimeoutPolicy_GetSuccessRate(this TimeoutBenchmarks benchmarks)
    {
        if (benchmarks == null)
            throw new ArgumentNullException(nameof(benchmarks));

        var timeoutCount = benchmarks.TimeoutPolicy_Get_TimeoutCount();
        var totalOperations = benchmarks.TimeoutPolicy_GetPercentile99ExecutionTime() > 0 ?
            Math.Max(1, benchmarks.TimeoutPolicy_GetPercentile99ExecutionTime() / 100) : 1;

        return Math.Max(0, 1.0 - (double)timeoutCount / totalOperations);
    }
}