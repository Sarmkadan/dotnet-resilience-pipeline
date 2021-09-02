using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Benchmarks comparing different configuration approaches and scenarios
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class PolicyComparisonBenchmarks
{
    // Retry policy configurations
    private RetryPolicy _fixedRetry;
    private RetryPolicy _linearRetry;
    private RetryPolicy _exponentialRetry;
    private RetryPolicy _exponentialWithJitterRetry;
    private const string RetryPolicyName = "retry-comparison";

    // Circuit breaker configurations
    private CircuitBreakerPolicy _lowThresholdCircuitBreaker;
    private CircuitBreakerPolicy _highThresholdCircuitBreaker;
    private CircuitBreakerPolicy _shortDurationCircuitBreaker;
    private CircuitBreakerPolicy _longDurationCircuitBreaker;
    private const string CircuitBreakerPolicyName = "cb-comparison";

    // Bulkhead configurations
    private BulkheadPolicy _smallBulkhead;
    private BulkheadPolicy _mediumBulkhead;
    private BulkheadPolicy _largeBulkhead;
    private const string BulkheadPolicyName = "bulkhead-comparison";

    [GlobalSetup]
    public void Setup()
    {
        // Retry configurations
        _fixedRetry = new RetryPolicy(RetryPolicyName + "-fixed")
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryPolicy.BackoffStrategy.Fixed
        };

        _linearRetry = new RetryPolicy(RetryPolicyName + "-linear")
        {
            MaxRetries = 5,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryPolicy.BackoffStrategy.Linear,
            BackoffMultiplier = 1.0
        };

        _exponentialRetry = new RetryPolicy(RetryPolicyName + "-exponential")
        {
            MaxRetries = 5,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryPolicy.BackoffStrategy.Exponential,
            BackoffMultiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(30)
        };

        _exponentialWithJitterRetry = new RetryPolicy(RetryPolicyName + "-jitter")
        {
            MaxRetries = 5,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryPolicy.BackoffStrategy.ExponentialWithJitter,
            BackoffMultiplier = 2.0,
            MaxDelay = TimeSpan.FromSeconds(30),
            UseJitter = true,
            JitterFactor = 0.5
        };

        // Circuit breaker configurations
        _lowThresholdCircuitBreaker = new CircuitBreakerPolicy(CircuitBreakerPolicyName + "-low")
        {
            FailureThreshold = 3,
            OpenDuration = TimeSpan.FromSeconds(15)
        };

        _highThresholdCircuitBreaker = new CircuitBreakerPolicy(CircuitBreakerPolicyName + "-high")
        {
            FailureThreshold = 10,
            OpenDuration = TimeSpan.FromSeconds(60)
        };

        _shortDurationCircuitBreaker = new CircuitBreakerPolicy(CircuitBreakerPolicyName + "-short")
        {
            FailureThreshold = 5,
            OpenDuration = TimeSpan.FromSeconds(5)
        };

        _longDurationCircuitBreaker = new CircuitBreakerPolicy(CircuitBreakerPolicyName + "-long")
        {
            FailureThreshold = 5,
            OpenDuration = TimeSpan.FromMinutes(5)
        };

        // Bulkhead configurations
        _smallBulkhead = new BulkheadPolicy(BulkheadPolicyName + "-small")
        {
            MaxParallelization = 5,
            MaxQueueLength = 20
        };

        _mediumBulkhead = new BulkheadPolicy(BulkheadPolicyName + "-medium")
        {
            MaxParallelization = 20,
            MaxQueueLength = 100
        };

        _largeBulkhead = new BulkheadPolicy(BulkheadPolicyName + "-large")
        {
            MaxParallelization = 50,
            MaxQueueLength = 200
        };
    }

    #region Retry Policy Comparisons

    [Benchmark]
    public long RetryComparison_Fixed_Strategy()
    {
        return _fixedRetry.GetNextDelayMs(1);
    }

    [Benchmark]
    public long RetryComparison_Linear_Strategy()
    {
        return _linearRetry.GetNextDelayMs(2);
    }

    [Benchmark]
    public long RetryComparison_Exponential_Strategy()
    {
        return _exponentialRetry.GetNextDelayMs(3);
    }

    [Benchmark]
    public long RetryComparison_ExponentialWithJitter_Strategy()
    {
        return _exponentialWithJitterRetry.GetNextDelayMs(4);
    }

    [Benchmark]
    public void RetryComparison_RecordRetryAttempt_All_Strategies()
    {
        _fixedRetry.RecordRetryAttempt();
        _linearRetry.RecordRetryAttempt();
        _exponentialRetry.RecordRetryAttempt();
        _exponentialWithJitterRetry.RecordRetryAttempt();
    }

    #endregion

    #region Circuit Breaker Comparisons

    [Benchmark]
    public void CircuitBreakerComparison_LowThreshold_RecordSuccess()
    {
        _lowThresholdCircuitBreaker.RecordSuccess();
    }

    [Benchmark]
    public void CircuitBreakerComparison_HighThreshold_RecordSuccess()
    {
        _highThresholdCircuitBreaker.RecordSuccess();
    }

    [Benchmark]
    public void CircuitBreakerComparison_ShortDuration_RecordFailure()
    {
        _shortDurationCircuitBreaker.RecordFailure();
    }

    [Benchmark]
    public void CircuitBreakerComparison_LongDuration_AttemptReset()
    {
        _longDurationCircuitBreaker.AttemptReset();
    }

    [Benchmark]
    public CircuitBreakerPolicy.CircuitState CircuitBreakerComparison_GetState_All()
    {
        return _lowThresholdCircuitBreaker.CurrentState;
    }

    [Benchmark]
    public long CircuitBreakerComparison_GetTrips_All()
    {
        return _highThresholdCircuitBreaker.CircuitBreakerTrips;
    }

    #endregion

    #region Bulkhead Comparisons

    [Benchmark]
    public bool BulkheadComparison_Small_TryAcquireSlot()
    {
        return _smallBulkhead.TryAcquireSlot();
    }

    [Benchmark]
    public bool BulkheadComparison_Medium_TryAcquireSlot()
    {
        return _mediumBulkhead.TryAcquireSlot();
    }

    [Benchmark]
    public bool BulkheadComparison_Large_TryAcquireSlot()
    {
        return _largeBulkhead.TryAcquireSlot();
    }

    [Benchmark]
    public void BulkheadComparison_RecordQueueWaitTime_All()
    {
        _smallBulkhead.RecordQueueWaitTime(50);
        _mediumBulkhead.RecordQueueWaitTime(100);
        _largeBulkhead.RecordQueueWaitTime(150);
    }

    [Benchmark]
    public double BulkheadComparison_GetUtilization_All()
    {
        // Fill small bulkhead
        for (int i = 0; i < 5; i++)
        {
            _smallBulkhead.TryAcquireSlot();
        }
        return _smallBulkhead.GetUtilizationPercentage();
    }

    #endregion

    #region Failure Scenario Benchmarks

    [Benchmark]
    public void CircuitBreakerComparison_Transition_Closed_To_Open()
    {
        // Simulate failure threshold being reached
        for (int i = 0; i < 5; i++)
        {
            _lowThresholdCircuitBreaker.RecordFailure();
        }
    }

    [Benchmark]
    public void RetryComparison_Multiple_Retry_Attempts()
    {
        for (int i = 0; i < 5; i++)
        {
            _exponentialRetry.RecordRetryAttempt();
        }
    }

    [Benchmark]
    public bool BulkheadComparison_Queue_And_Reject()
    {
        // Fill bulkhead
        for (int i = 0; i < 5; i++)
        {
            _smallBulkhead.TryAcquireSlot();
        }

        // Fill queue
        for (int i = 0; i < 20; i++)
        {
            _smallBulkhead.TryAcquireSlot();
        }

        // Try to exceed capacity - should be rejected
        return _smallBulkhead.TryAcquireSlot();
    }

    #endregion
}