using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Benchmarks for FallbackPolicy performance
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class FallbackBenchmarks
{
    private FallbackPolicy _fallbackPolicy;
    private const string PolicyName = "test-fallback";

    [GlobalSetup]
    public void Setup()
    {
        _fallbackPolicy = new FallbackPolicy(PolicyName)
        {
            FallbackOnAnyException = true,
            FallbackTimeout = TimeSpan.FromSeconds(5)
        };
    }

    [Benchmark]
    public void FallbackPolicy_RecordSuccessfulFallback()
    {
        _fallbackPolicy.RecordSuccessfulFallback(100);
    }

    [Benchmark]
    public void FallbackPolicy_RecordFailedFallback()
    {
        _fallbackPolicy.RecordFailedFallback(new InvalidOperationException("Test"), 100);
    }

    [Benchmark]
    public bool FallbackPolicy_ShouldTriggerFallback_Any()
    {
        return _fallbackPolicy.ShouldTriggerFallback(new TimeoutException());
    }

    [Benchmark]
    public bool FallbackPolicy_ShouldTriggerFallback_Specific()
    {
        _fallbackPolicy.FallbackOnAnyException = false;
        _fallbackPolicy.AddFallbackTrigger(typeof(TimeoutException));
        return _fallbackPolicy.ShouldTriggerFallback(new TimeoutException());
    }

    [Benchmark]
    public double FallbackPolicy_GetFallbackSuccessRate()
    {
        _fallbackPolicy.RecordSuccessfulFallback(100);
        _fallbackPolicy.RecordSuccessfulFallback(150);
        _fallbackPolicy.RecordFailedFallback(new InvalidOperationException("Test"), 200);
        return _fallbackPolicy.GetFallbackSuccessRate();
    }

    [Benchmark]
    public double FallbackPolicy_GetFallbackInvocationPercentage()
    {
        _fallbackPolicy.RecordSuccessfulFallback(100);
        _fallbackPolicy.RecordFailedFallback(new InvalidOperationException("Test"), 200);
        return _fallbackPolicy.GetFallbackInvocationPercentage();
    }

    [Benchmark]
    public TimeSpan FallbackPolicy_Get_FallbackTimeout()
    {
        return _fallbackPolicy.FallbackTimeout;
    }

    [Benchmark]
    public long FallbackPolicy_Get_FallbackInvocationCount()
    {
        return _fallbackPolicy.FallbackInvocationCount;
    }
}