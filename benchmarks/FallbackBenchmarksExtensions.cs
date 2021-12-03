using System;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Provides extension methods for analyzing fallback policy benchmark results, including calculating fallback invocation rates, 
/// determining frequency of fallback triggers, and computing success ratios of fallback operations.
/// </summary>
public static class FallbackBenchmarksExtensions
{
    /// <summary>
    /// Calculates the average fallback invocation rate (fallbacks per second) based on total fallback invocations, 
    /// fallback invocation percentage, and the fallback timeout duration.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing fallback metrics.</param>
    /// <returns>
    /// The average fallback invocation rate in fallbacks per second. Returns 0 if the timeout duration is zero or negative.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static double GetAverageFallbackInvocationRate(this FallbackBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var totalInvocations = benchmarks.FallbackPolicy_Get_FallbackInvocationCount;
        var fallbackInvocations = benchmarks.FallbackPolicy_GetFallbackInvocationPercentage * totalInvocations / 100;
        var timeSpan = benchmarks.FallbackPolicy_Get_FallbackTimeout;

        return timeSpan.TotalSeconds > 0
            ? fallbackInvocations / timeSpan.TotalSeconds
            : 0;
    }

    /// <summary>
    /// Determines whether the fallback policy was triggered frequently by comparing the fallback invocation percentage 
    /// against a specified threshold percentage.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing fallback metrics.</param>
    /// <param name="threshold">The threshold percentage (e.g., 50 for 50%) above which fallback is considered frequent.</param>
    /// <returns>
    /// <c>true</c> if the fallback invocation percentage exceeds the threshold; otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static bool IsFallbackTriggeredFrequently(this FallbackBenchmarks benchmarks, double threshold)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        return benchmarks.FallbackPolicy_GetFallbackInvocationPercentage > threshold;
    }

    /// <summary>
    /// Calculates the success ratio of fallback operations by comparing the number of successful fallbacks to the total 
    /// number of fallback invocations. The success rate is derived from the fallback success percentage and total invocations.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing fallback metrics.</param>
    /// <returns>
    /// The ratio of successful fallbacks to total fallbacks. Returns 0 if no fallbacks occurred.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static double CalculateFallbackSuccessRatio(this FallbackBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var successfulFallbacks = benchmarks.FallbackPolicy_Get_FallbackSuccessRate * benchmarks.FallbackPolicy_Get_FallbackInvocationCount / 100;
        var failedFallbacks = benchmarks.FallbackPolicy_Get_FallbackInvocationCount - successfulFallbacks;

        return successfulFallbacks + failedFallbacks > 0
            ? successfulFallbacks / (successfulFallbacks + failedFallbacks)
            : 0;
    }
}
