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
    /// <summary>
    /// Initializes a new TimeoutPolicy with a 10-second timeout for each benchmark iteration.
    /// </summary>
    public void Setup()
    {
        _timeoutPolicy = new TimeoutPolicy(PolicyName)
        {
            Timeout = TimeSpan.FromSeconds(10)
        };
    }

    [Benchmark]
    /// <summary>
    /// Records an execution time of 50 milliseconds to the timeout policy.
    /// </summary>
    public void TimeoutPolicy_RecordExecutionTime()
    {
        _timeoutPolicy.RecordExecutionTime(50);
    }

    [Benchmark]
    /// <summary>
    /// Records a timeout event with a duration of 15 seconds (exceeds the 10-second timeout).
    /// </summary>
    public void TimeoutPolicy_RecordTimeout()
    {
        _timeoutPolicy.RecordTimeout(15000); // Exceeds timeout
    }

    [Benchmark]
    /// <summary>
    /// Checks if a 50-millisecond duration is considered timed out by the policy.
    /// </summary>
    public bool TimeoutPolicy_IsTimedOut_Within()
    {
        return _timeoutPolicy.IsTimedOut(TimeSpan.FromMilliseconds(50));
    }

    [Benchmark]
    /// <summary>
    /// Checks if a 15-second duration is considered timed out by the policy.
    /// </summary>
    public bool TimeoutPolicy_IsTimedOut_Exceeds()
    {
        return _timeoutPolicy.IsTimedOut(TimeSpan.FromSeconds(15));
    }

    [Benchmark]
    /// <summary>
    /// Adds 100 execution times ranging from 0 to 190 ms and returns the 95th percentile execution time.
    /// </summary>
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
    /// <summary>
    /// Adds 100 execution times ranging from 0 to 190 ms and returns the 99th percentile execution time.
    /// </summary>
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
    /// <summary>
    /// Records two timeout events and returns the percentage of timeouts.
    /// </summary>
    public double TimeoutPolicy_GetTimeoutPercentage()
    {
        _timeoutPolicy.RecordTimeout(15000);
        _timeoutPolicy.RecordTimeout(15000);
        return _timeoutPolicy.GetTimeoutPercentage();
    }

    [Benchmark]
    /// <summary>
    /// Returns the configured timeout TimeSpan.
    /// </summary>
    public TimeSpan TimeoutPolicy_Get_Timeout()
    {
        return _timeoutPolicy.Timeout;
    }

    [Benchmark]
    /// <summary>
    /// Returns the total number of timeout events recorded.
    /// </summary>
    public long TimeoutPolicy_Get_TimeoutCount()
    {
        return _timeoutPolicy.TimeoutCount;
    }
}
