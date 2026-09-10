using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Benchmarks for concurrent operations and thread safety
/// Measures performance under parallel load
/// </summary>
[MemoryDiagnoser]
[Orderer(BenchmarkDotNet.Order.SummaryOrderPolicy.FastestToSlowest)]
[RankColumn]
public class ConcurrencyBenchmarks
{
    private CircuitBreakerPolicy _circuitBreaker;
    private RetryPolicy _retryPolicy;
    private TimeoutPolicy _timeoutPolicy;
    private BulkheadPolicy _bulkheadPolicy;
    private FallbackPolicy _fallbackPolicy;
    private const string PolicyName = "concurrency-test";

    [GlobalSetup]
    /// <summary>
    /// Initializes the resilience policies (circuit breaker, retry, timeout, bulkhead, fallback) used in the benchmarks.
    /// </summary>
    public void Setup()
    {
        _circuitBreaker = new CircuitBreakerPolicy(PolicyName + "-cb")
        {
            FailureThreshold = 5,
            OpenDuration = TimeSpan.FromSeconds(30)
        };

        _retryPolicy = new RetryPolicy(PolicyName + "-retry")
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            Strategy = RetryPolicy.BackoffStrategy.Exponential
        };

        _timeoutPolicy = new TimeoutPolicy(PolicyName + "-timeout")
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        _bulkheadPolicy = new BulkheadPolicy(PolicyName + "-bulkhead")
        {
            MaxParallelization = 20,
            MaxQueueLength = 100
        };

        _fallbackPolicy = new FallbackPolicy(PolicyName + "-fallback")
        {
            FallbackOnAnyException = true
        };
    }

    [Benchmark]
    /// <summary>
    /// Measures concurrent success recording performance of the circuit breaker policy.
    /// </summary>
    public void CircuitBreaker_Concurrent_Success_Recording()
    {
        Parallel.For(0, 1000, i =>
        {
            _circuitBreaker.RecordSuccess();
        });
    }

    [Benchmark]
    /// <summary>
    /// Measures concurrent failure recording performance of the circuit breaker policy.
    /// </summary>
    public void CircuitBreaker_Concurrent_Failure_Recording()
    {
        Parallel.For(0, 1000, i =>
        {
            _circuitBreaker.RecordFailure();
        });
    }

    [Benchmark]
    /// <summary>
    /// Measures concurrent state access performance of the circuit breaker policy.
    /// </summary>
    public void CircuitBreaker_Concurrent_State_Access()
    {
        Parallel.For(0, 1000, i =>
        {
            var state = _circuitBreaker.CurrentState;
        });
    }

    [Benchmark]
    /// <summary>
    /// Measures concurrent retry attempt recording performance of the retry policy.
    /// </summary>
    public void RetryPolicy_Concurrent_Retry_Recording()
    {
        Parallel.For(0, 1000, i =>
        {
            _retryPolicy.RecordRetryAttempt();
        });
    }

    [Benchmark]
    /// <summary>
    /// Measures concurrent delay calculation performance of the retry policy.
    /// </summary>
    public void RetryPolicy_Concurrent_Delay_Calculation()
    {
        Parallel.For(0, 1000, i =>
        {
            _retryPolicy.GetNextDelayMs(i % 5);
        });
    }

    [Benchmark]
    /// <summary>
    /// Measures concurrent execution time recording performance of the timeout policy.
    /// </summary>
    public void TimeoutPolicy_Concurrent_Execution_Recording()
    {
        Parallel.For(0, 1000, i =>
        {
            _timeoutPolicy.RecordExecutionTime(50 + (i % 100));
        });
    }

    [Benchmark]
    /// <summary>
    /// Measures concurrent timeout recording performance of the timeout policy.
    /// </summary>
    public void TimeoutPolicy_Concurrent_Timeout_Recording()
    {
        Parallel.For(0, 1000, i =>
        {
            _timeoutPolicy.RecordTimeout(15000);
        });
    }

    [Benchmark]
    /// <summary>
    /// Measures concurrent slot acquisition and release performance of the bulkhead policy.
    /// </summary>
    public void BulkheadPolicy_Concurrent_Slot_Acquisition()
    {
        Parallel.For(0, 1000, i =>
        {
            _bulkheadPolicy.TryAcquireSlot();
            _bulkheadPolicy.ReleaseSlot();
        });
    }

    [Benchmark]
    /// <summary>
    /// Measures concurrent queue wait time recording performance of the bulkhead policy.
    /// </summary>
    public void BulkheadPolicy_Concurrent_Queue_Wait_Recording()
    {
        Parallel.For(0, 1000, i =>
        {
            _bulkheadPolicy.RecordQueueWaitTime(10 + (i % 50));
        });
    }

    [Benchmark]
    /// <summary>
    /// Measures concurrent successful fallback recording performance of the fallback policy.
    /// </summary>
    public void FallbackPolicy_Concurrent_Fallback_Recording()
    {
        Parallel.For(0, 1000, i =>
        {
            _fallbackPolicy.RecordSuccessfulFallback(50 + (i % 200));
        });
    }

    [Benchmark]
    public void FallbackPolicy_Concurrent_Fallback_Check()
    {
        Parallel.For(0, 1000, i =>
        {
            _fallbackPolicy.ShouldTriggerFallback(new InvalidOperationException("Test"));
        });
    }

    [Benchmark]
    public void All_Policies_Concurrent_Mixed_Operations()
    {
        Parallel.For(0, 500, i =>
        {
            // Mix of operations
            if (i % 5 == 0)
                _circuitBreaker.RecordSuccess();
            else if (i % 5 == 1)
                _retryPolicy.RecordRetryAttempt();
            else if (i % 5 == 2)
                _timeoutPolicy.RecordExecutionTime(50 + (i % 100));
            else if (i % 5 == 3)
            {
                _bulkheadPolicy.TryAcquireSlot();
                _bulkheadPolicy.ReleaseSlot();
            }
            else
                _fallbackPolicy.RecordSuccessfulFallback(100);
        });
    }

    [Benchmark]
    public long CircuitBreaker_Get_CircuitBreakerTrips_Concurrent()
    {
        long total = 0;
        Parallel.For(0, 1000, i =>
        {
            total += _circuitBreaker.CircuitBreakerTrips;
        });
        return total;
    }

    [Benchmark]
    public double Bulkhead_Get_Utilization_Concurrent()
    {
        // Fill bulkhead first
        for (int i = 0; i < 20; i++)
        {
            _bulkheadPolicy.TryAcquireSlot();
        }

        double total = 0;
        Parallel.For(0, 1000, i =>
        {
            total += _bulkheadPolicy.GetUtilizationPercentage();
        });
        return total;
    }
}