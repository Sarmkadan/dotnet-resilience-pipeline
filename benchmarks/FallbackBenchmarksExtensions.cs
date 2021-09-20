using System;

namespace YourNamespace; // Replace with actual namespace

public static class FallbackBenchmarksExtensions
{
    public static double GetAverageFallbackInvocationRate(this FallbackBenchmarks benchmarks)
    {
        var totalInvocations = benchmarks.FallbackPolicy_Get_FallbackInvocationCount;
        var fallbackInvocations = benchmarks.FallbackPolicy_Get_FallbackInvocationPercentage * totalInvocations / 100;
        var timeSpan = benchmarks.FallbackPolicy_Get_FallbackTimeout;
        return fallbackInvocations / timeSpan.TotalSeconds;
    }

    public static bool IsFallbackTriggeredFrequently(this FallbackBenchmarks benchmarks, double threshold)
    {
        return benchmarks.FallbackPolicy_GetFallbackInvocationPercentage > threshold;
    }

    public static double CalculateFallbackSuccessRatio(this FallbackBenchmarks benchmarks)
    {
        var successfulFallbacks = benchmarks.FallbackPolicy_RecordSuccessfulFallback;
        var failedFallbacks = benchmarks.FallbackPolicy_RecordFailedFallback;
        return successfulFallbacks / (successfulFallbacks + failedFallbacks);
    }
}
