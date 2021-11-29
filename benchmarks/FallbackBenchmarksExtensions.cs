using System;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Extension methods for analyzing fallback policy benchmark results.
/// </summary>
public static class FallbackBenchmarksExtensions
{
    /// <summary>
    /// Calculates the average fallback invocation rate (fallbacks per second).
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing fallback metrics.</param>
    /// <returns>The average fallback invocation rate in fallbacks per second.</returns>
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
    /// Determines whether fallback was triggered frequently based on the provided threshold.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing fallback metrics.</param>
    /// <param name="threshold">The threshold percentage above which fallback is considered frequent.</param>
    /// <returns>True if fallback invocation percentage exceeds the threshold; otherwise false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="benchmarks"/> is null.</exception>
    public static bool IsFallbackTriggeredFrequently(this FallbackBenchmarks benchmarks, double threshold)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        return benchmarks.FallbackPolicy_GetFallbackInvocationPercentage > threshold;
    }

    /// <summary>
    /// Calculates the success ratio of fallback operations.
    /// </summary>
    /// <param name="benchmarks">The benchmark instance containing fallback metrics.</param>
    /// <returns>The ratio of successful fallbacks to total fallbacks. Returns 0 if no fallbacks occurred.</returns>
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
