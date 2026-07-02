using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Benchmarks for TimeoutPolicy performance
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class TimeoutBenchmarks
{
    private TimeoutPolicy _timeoutPolicy;
    private const string PolicyName = "test-timeout";

    [GlobalSetup]
    public void Setup()
    {
        _timeoutPolicy = new TimeoutPolicy(PolicyName)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    [Benchmark]
    public void TimeoutPolicy_RecordExecutionTime()
    {
        _timeoutPolicy.RecordExecutionTime(50);
    }

    [Benchmark]
    public void TimeoutPolicy_RecordTimeout()
    {
        _timeoutPolicy.RecordTimeout(15000); // Exceeds timeout
    }

    [Benchmark]
    public bool TimeoutPolicy_IsTimedOut_Within()
    {
        return _timeoutPolicy.IsTimedOut(TimeSpan.FromMilliseconds(50));
    }

    [Benchmark]
    public bool TimeoutPolicy_IsTimedOut_Exceeds()
    {
        return _timeoutPolicy.IsTimedOut(TimeSpan.FromSeconds(15));
    }

    [Benchmark]
    public long TimeoutPolicy_GetPercentile95ExecutionTime()
    {
        // Add some execution times first
        for (int i = 0; i < 100; i++)
        {
            _timeoutPolicy.RecordExecutionTime((i % 20) * 10);
        }
        return _timeoutPolicy.GetPercentile95ExecutionTime();
    }

    [Benchmark]
    public long TimeoutPolicy_GetPercentile99ExecutionTime()
    {
        // Add some execution times first
        for (int i = 0; i < 100; i++)
        {
            _timeoutPolicy.RecordExecutionTime((i % 20) * 10);
        }
        return _timeoutPolicy.GetPercentile99ExecutionTime();
    }

    [Benchmark]
    public double TimeoutPolicy_GetTimeoutPercentage()
    {
        _timeoutPolicy.RecordTimeout(15000);
        _timeoutPolicy.RecordTimeout(15000);
        return _timeoutPolicy.GetTimeoutPercentage();
    }

    [Benchmark]
    public TimeSpan TimeoutPolicy_Get_Timeout()
    {
        return _timeoutPolicy.Timeout;
    }

    [Benchmark]
    public long TimeoutPolicy_Get_TimeoutCount()
    {
        return _timeoutPolicy.TimeoutCount;
    }
}