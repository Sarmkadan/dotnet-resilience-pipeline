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

    /// <summary>
    /// Initializes retry, circuit breaker, and bulkhead policies for benchmarking.
    /// </summary>
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

    /// <summary>
    /// Measures the delay in milliseconds for the first retry attempt using fixed backoff strategy.
    /// </summary>
    [Benchmark]
    public long RetryComparison_Fixed_Strategy()
    {
        return _fixedRetry.GetNextDelayMs(1);
    }

    /// <summary>
    /// Measures the delay in milliseconds for the second retry attempt using linear backoff strategy.
    /// </summary>
    [Benchmark]
    public long RetryComparison_Linear_Strategy()
    {
        return _linearRetry.GetNextDelayMs(2);
    }

    /// <summary>
    /// Measures the delay in milliseconds for the third retry attempt using exponential backoff strategy.
    /// </summary>
    [Benchmark]
    public long RetryComparison_Exponential_Strategy()
    {
        return _exponentialRetry.GetNextDelayMs(3);
    }

    /// <summary>
    /// Measures the delay in milliseconds for the fourth retry attempt using exponential backoff with jitter strategy.
    /// </summary>
    [Benchmark]
    public long RetryComparison_ExponentialWithJitter_Strategy()
    {
        return _exponentialWithJitterRetry.GetNextDelayMs(4);
    }

    /// <summary>
    /// Records a retry attempt for all retry policy strategies to measure the overhead of recording attempts.
    /// </summary>
    [Benchmark]
    public void RetryComparison_RecordRetryAttempt_All_Strategies()
    {
        _fixedRetry.RecordRetryAttempt();
        _linearRetry.RecordRetryAttempt();
        _exponentialRetry.RecordRetryAttempt();
        _exponentialWithJitterRetry.RecordRetryAttempt();
    }

    /// <summary>
    /// Gets the total number of retry attempts recorded across all retry policy strategies.
    /// </summary>
    [Benchmark]
    public long RetryComparison_GetTotalRetryAttempts()
    {
        return _fixedRetry.TotalRetryAttempts + _linearRetry.TotalRetryAttempts +
               _exponentialRetry.TotalRetryAttempts + _exponentialWithJitterRetry.TotalRetryAttempts;
    }

    #endregion

    #region Circuit Breaker Comparisons

    /// <summary>
    /// Measures the overhead of recording a success on a circuit breaker with a low failure threshold.
    /// </summary>
    [Benchmark]
    public void CircuitBreakerComparison_LowThreshold_RecordSuccess()
    {
        _lowThresholdCircuitBreaker.RecordSuccess();
    }

    /// <summary>
    /// Measures the overhead of recording a success on a circuit breaker with a high failure threshold.
    /// </summary>
    [Benchmark]
    public void CircuitBreakerComparison_HighThreshold_RecordSuccess()
    {
        _highThresholdCircuitBreaker.RecordSuccess();
    }

    /// <summary>
    /// Measures the overhead of recording a failure on a circuit breaker with a short open duration.
    /// </summary>
    [Benchmark]
    public void CircuitBreakerComparison_ShortDuration_RecordFailure()
    {
        _shortDurationCircuitBreaker.RecordFailure();
    }

    /// <summary>
    /// Measures the overhead of attempting to reset a circuit breaker with a long open duration.
    /// </summary>
    [Benchmark]
    public void CircuitBreakerComparison_LongDuration_AttemptReset()
    {
        _longDurationCircuitBreaker.AttemptReset();
    }

    /// <summary>
    /// Gets the current state of the low threshold circuit breaker to measure state retrieval overhead.
    /// </summary>
    [Benchmark]
    public CircuitBreakerPolicy.CircuitState CircuitBreakerComparison_GetState_All()
    {
        return _lowThresholdCircuitBreaker.CurrentState;
    }

    /// <summary>
    /// Measures the overhead of retrieving the number of circuit breaker trips for a high-threshold policy.
    /// </summary>
    [Benchmark]
    public long CircuitBreakerComparison_GetTrips_All()
    {
        return _highThresholdCircuitBreaker.CircuitBreakerTrips;
    }

    #endregion

    #region Bulkhead Comparisons

    /// <summary>
    /// Measures the overhead of attempting to acquire a slot in a small bulkhead policy.
    /// </summary>
    [Benchmark]
    public bool BulkheadComparison_Small_TryAcquireSlot()
    {
        return _smallBulkhead.TryAcquireSlot();
    }

    /// <summary>
    /// Measures the overhead of attempting to acquire a slot in a medium bulkhead policy.
    /// </summary>
    [Benchmark]
    public bool BulkheadComparison_Medium_TryAcquireSlot()
    {
        return _mediumBulkhead.TryAcquireSlot();
    }

    /// <summary>
    /// Measures the overhead of attempting to acquire a slot in a large bulkhead policy.
    /// </summary>
    [Benchmark]
    public bool BulkheadComparison_Large_TryAcquireSlot()
    {
        return _largeBulkhead.TryAcquireSlot();
    }

    /// <summary>
    /// Measures the overhead of recording queue wait times across small, medium, and large bulkhead policies.
    /// </summary>
    [Benchmark]
    public void BulkheadComparison_RecordQueueWaitTime_All()
    {
        _smallBulkhead.RecordQueueWaitTime(50);
        _mediumBulkhead.RecordQueueWaitTime(100);
        _largeBulkhead.RecordQueueWaitTime(150);
    }

    /// <summary>
    /// Measures the overhead of calculating utilization percentage after filling a small bulkhead to capacity.
    /// </summary>
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

    /// <summary>
    /// Measures the overhead of transitioning a circuit breaker from closed to open state by simulating threshold failures.
    /// </summary>
    [Benchmark]
    public void CircuitBreakerComparison_Transition_Closed_To_Open()
    {
        // Simulate failure threshold being reached
        for (int i = 0; i < 5; i++)
        {
            _lowThresholdCircuitBreaker.RecordFailure();
        }
    }

    /// <summary>
    /// Measures the overhead of recording multiple retry attempts on an exponential retry policy.
    /// </summary>
    [Benchmark]
    public void RetryComparison_Multiple_Retry_Attempts()
    {
        for (int i = 0; i < 5; i++)
        {
            _exponentialRetry.RecordRetryAttempt();
        }
    }

    /// <summary>
    /// Measures the overhead of filling a bulkhead's slots and queue, then attempting to exceed capacity to verify rejection logic.
    /// </summary>
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
