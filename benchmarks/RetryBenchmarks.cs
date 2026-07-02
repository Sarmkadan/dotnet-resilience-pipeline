using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using BenchmarkDotNet.Running;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Benchmarks for RetryPolicy performance
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class RetryBenchmarks
{
    private RetryPolicy _fixedRetryPolicy;
    private RetryPolicy _exponentialRetryPolicy;
    private RetryPolicy _exponentialWithJitterRetryPolicy;
    private const string PolicyName = "test-retry";

    [GlobalSetup]
    public void Setup()
    {
        _fixedRetryPolicy = new RetryPolicy(PolicyName + "-fixed")
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryPolicy.BackoffStrategy.Fixed
        };

        _exponentialRetryPolicy = new RetryPolicy(PolicyName + "-exponential")
        {
            MaxRetries = 5,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryPolicy.BackoffStrategy.Exponential,
            BackoffMultiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(30)
        };

        _exponentialWithJitterRetryPolicy = new RetryPolicy(PolicyName + "-jitter")
        {
            MaxRetries = 5,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryPolicy.BackoffStrategy.ExponentialWithJitter,
            BackoffMultiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(30),
            UseJitter = true,
            JitterFactor = 1.0
        };
    }

    [Benchmark]
    public void RetryPolicy_Fixed_Strategy()
    {
        _fixedRetryPolicy.RecordRetryAttempt();
    }

    [Benchmark]
    public void RetryPolicy_Exponential_Strategy()
    {
        _exponentialRetryPolicy.RecordRetryAttempt();
    }

    [Benchmark]
    public void RetryPolicy_ExponentialWithJitter_Strategy()
    {
        _exponentialWithJitterRetryPolicy.RecordRetryAttempt();
    }

    [Benchmark]
    public long RetryPolicy_CalculateDelay_Fixed()
    {
        return _fixedRetryPolicy.GetNextDelayMs(1);
    }

    [Benchmark]
    public long RetryPolicy_CalculateDelay_Exponential()
    {
        return _exponentialRetryPolicy.GetNextDelayMs(2);
    }

    [Benchmark]
    public long RetryPolicy_CalculateDelay_ExponentialWithJitter()
    {
        return _exponentialWithJitterRetryPolicy.GetNextDelayMs(3);
    }

    [Benchmark]
    public bool RetryPolicy_IsRetryable()
    {
        return _fixedRetryPolicy.IsRetryable(new TimeoutException());
    }

    [Benchmark]
    public RetryPolicy.BackoffStrategy RetryPolicy_Get_Strategy()
    {
        return _fixedRetryPolicy.Strategy;
    }

    [Benchmark]
    public int RetryPolicy_Get_MaxRetries()
    {
        return _fixedRetryPolicy.MaxRetries;
    }
}