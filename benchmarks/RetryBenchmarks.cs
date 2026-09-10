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

    /// <summary>
    /// Sets up the benchmark by initializing three retry policies: fixed, exponential, and exponential with jitter.
    /// </summary>
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

    /// <summary>
    /// Measures the performance of recording a retry attempt with fixed backoff strategy.
    /// </summary>
    [Benchmark]
    public void RetryPolicy_Fixed_Strategy()
    {
        _fixedRetryPolicy.RecordRetryAttempt();
    }

    /// <summary>
    /// Measures the performance of recording a retry attempt with exponential backoff strategy.
    /// </summary>
    [Benchmark]
    public void RetryPolicy_Exponential_Strategy()
    {
        _exponentialRetryPolicy.RecordRetryAttempt();
    }

    /// <summary>
    /// Measures the performance of recording a retry attempt with exponential backoff with jitter strategy.
    /// </summary>
    [Benchmark]
    public void RetryPolicy_ExponentialWithJitter_Strategy()
    {
        _exponentialWithJitterRetryPolicy.RecordRetryAttempt();
    }

    /// <summary>
    /// Measures the performance of calculating delay for fixed backoff strategy.
    /// </summary>
    [Benchmark]
    public long RetryPolicy_CalculateDelay_Fixed()
    {
        return _fixedRetryPolicy.GetNextDelayMs(1);
    }

    /// <summary>
    /// Measures the performance of calculating delay for exponential backoff strategy.
    /// </summary>
    [Benchmark]
    public long RetryPolicy_CalculateDelay_Exponential()
    {
        return _exponentialRetryPolicy.GetNextDelayMs(2);
    }

    /// <summary>
    /// Measures the performance of calculating delay for exponential backoff with jitter strategy.
    /// </summary>
    [Benchmark]
    public long RetryPolicy_CalculateDelay_ExponentialWithJitter()
    {
        return _exponentialWithJitterRetryPolicy.GetNextDelayMs(3);
    }

    /// <summary>
    /// Measures the performance of checking if an exception is retryable.
    /// </summary>
    [Benchmark]
    public bool RetryPolicy_IsRetryable()
    {
        return _fixedRetryPolicy.IsRetryable(new TimeoutException());
    }

    /// <summary>
    /// Measures the performance of getting the backoff strategy.
    /// </summary>
    [Benchmark]
    public RetryPolicy.BackoffStrategy RetryPolicy_Get_Strategy()
    {
        return _fixedRetryPolicy.Strategy;
    }

    /// <summary>
    /// Measures the performance of getting the maximum retries value.
    /// </summary>
    [Benchmark]
    public int RetryPolicy_Get_MaxRetries()
    {
        return _fixedRetryPolicy.MaxRetries;
    }
}