using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Extension methods for ConcurrencyBenchmarks to provide additional utility and analysis functionality
/// </summary>
public static class ConcurrencyBenchmarksExtensions
{
    /// <summary>
    /// Executes all benchmark methods concurrently and returns execution statistics
    /// </summary>
    /// <param name="benchmarks">The ConcurrencyBenchmarks instance</param>
    /// <returns>Dictionary with benchmark names and their execution times in milliseconds</returns>
    public static Dictionary<string, double> ExecuteAllBenchmarks(this ConcurrencyBenchmarks benchmarks)
    {
        var results = new Dictionary<string, double>(StringComparer.OrdinalIgnoreCase);
        var tasks = new List<Task>();

        // Circuit Breaker benchmarks
        tasks.Add(Task.Run(() =>
        {
            var start = DateTime.UtcNow;
            benchmarks.CircuitBreaker_Concurrent_Success_Recording();
            results["CircuitBreaker_Concurrent_Success_Recording"] = (DateTime.UtcNow - start).TotalMilliseconds;
        }));

        tasks.Add(Task.Run(() =>
        {
            var start = DateTime.UtcNow;
            benchmarks.CircuitBreaker_Concurrent_Failure_Recording();
            results["CircuitBreaker_Concurrent_Failure_Recording"] = (DateTime.UtcNow - start).TotalMilliseconds;
        }));

        tasks.Add(Task.Run(() =>
        {
            var start = DateTime.UtcNow;
            benchmarks.CircuitBreaker_Concurrent_State_Access();
            results["CircuitBreaker_Concurrent_State_Access"] = (DateTime.UtcNow - start).TotalMilliseconds;
        }));

        // Retry Policy benchmarks
        tasks.Add(Task.Run(() =>
        {
            var start = DateTime.UtcNow;
            benchmarks.RetryPolicy_Concurrent_Retry_Recording();
            results["RetryPolicy_Concurrent_Retry_Recording"] = (DateTime.UtcNow - start).TotalMilliseconds;
        }));

        tasks.Add(Task.Run(() =>
        {
            var start = DateTime.UtcNow;
            benchmarks.RetryPolicy_Concurrent_Delay_Calculation();
            results["RetryPolicy_Concurrent_Delay_Calculation"] = (DateTime.UtcNow - start).TotalMilliseconds;
        }));

        // Timeout Policy benchmarks
        tasks.Add(Task.Run(() =>
        {
            var start = DateTime.UtcNow;
            benchmarks.TimeoutPolicy_Concurrent_Execution_Recording();
            results["TimeoutPolicy_Concurrent_Execution_Recording"] = (DateTime.UtcNow - start).TotalMilliseconds;
        }));

        tasks.Add(Task.Run(() =>
        {
            var start = DateTime.UtcNow;
            benchmarks.TimeoutPolicy_Concurrent_Timeout_Recording();
            results["TimeoutPolicy_Concurrent_Timeout_Recording"] = (DateTime.UtcNow - start).TotalMilliseconds;
        }));

        // Bulkhead Policy benchmarks
        tasks.Add(Task.Run(() =>
        {
            var start = DateTime.UtcNow;
            benchmarks.BulkheadPolicy_Concurrent_Slot_Acquisition();
            results["BulkheadPolicy_Concurrent_Slot_Acquisition"] = (DateTime.UtcNow - start).TotalMilliseconds;
        }));

        tasks.Add(Task.Run(() =>
        {
            var start = DateTime.UtcNow;
            benchmarks.BulkheadPolicy_Concurrent_Queue_Wait_Recording();
            results["BulkheadPolicy_Concurrent_Queue_Wait_Recording"] = (DateTime.UtcNow - start).TotalMilliseconds;
        }));

        // Fallback Policy benchmarks
        tasks.Add(Task.Run(() =>
        {
            var start = DateTime.UtcNow;
            benchmarks.FallbackPolicy_Concurrent_Fallback_Recording();
            results["FallbackPolicy_Concurrent_Fallback_Recording"] = (DateTime.UtcNow - start).TotalMilliseconds;
        }));

        tasks.Add(Task.Run(() =>
        {
            var start = DateTime.UtcNow;
            benchmarks.FallbackPolicy_Concurrent_Fallback_Check();
            results["FallbackPolicy_Concurrent_Fallback_Check"] = (DateTime.UtcNow - start).TotalMilliseconds;
        }));

        // Mixed operations benchmark
        tasks.Add(Task.Run(() =>
        {
            var start = DateTime.UtcNow;
            benchmarks.All_Policies_Concurrent_Mixed_Operations();
            results["All_Policies_Concurrent_Mixed_Operations"] = (DateTime.UtcNow - start).TotalMilliseconds;
        }));

        Task.WaitAll(tasks.ToArray());

        return results;
    }

    /// <summary>
    /// Gets aggregated statistics from all circuit breaker related benchmarks
    /// </summary>
    /// <param name="benchmarks">The ConcurrencyBenchmarks instance</param>
    /// <returns>Tuple containing success rate, failure rate, and state access count</returns>
    public static (double SuccessRate, double FailureRate, long StateAccessCount) GetCircuitBreakerStatistics(this ConcurrencyBenchmarks benchmarks)
    {
        var successRate = benchmarks.CircuitBreaker_Get_CircuitBreakerTrips_Concurrent() / 1000.0;
        var failureRate = 1.0 - successRate;
        var stateAccessCount = 1000; // Known from benchmark implementation

        return (successRate, failureRate, stateAccessCount);
    }

    /// <summary>
    /// Gets bulkhead utilization statistics across multiple concurrent measurements
    /// </summary>
    /// <param name="benchmarks">The ConcurrencyBenchmarks instance</param>
    /// <param name="measurementCount">Number of measurements to take</param>
    /// <returns>Average utilization percentage</returns>
    public static double GetAverageBulkheadUtilization(this ConcurrencyBenchmarks benchmarks, int measurementCount = 10)
    {
        if (measurementCount <= 0)
            throw new ArgumentOutOfRangeException(nameof(measurementCount), "Must be greater than 0");

        double totalUtilization = 0;

        for (int i = 0; i < measurementCount; i++)
        {
            totalUtilization += benchmarks.Bulkhead_Get_Utilization_Concurrent();
        }

        return totalUtilization / measurementCount;
    }

    /// <summary>
    /// Executes a stress test combining all policies with configurable parallelism
    /// </summary>
    /// <param name="benchmarks">The ConcurrencyBenchmarks instance</param>
    /// <param name="parallelism">Number of parallel operations</param>
    /// <returns>Tuple with success count, failure count, and total execution time</returns>
    public static (int SuccessCount, int FailureCount, TimeSpan ExecutionTime) RunStressTest(
        this ConcurrencyBenchmarks benchmarks,
        int parallelism = 1000)
    {
        var startTime = DateTime.UtcNow;
        int successCount = 0;
        int failureCount = 0;

        Parallel.For(0, parallelism, i =>
        {
            try
            {
                if (i % 7 == 0)
                {
                    benchmarks._circuitBreaker.RecordSuccess();
                    Interlocked.Increment(ref successCount);
                }
                else if (i % 7 == 1)
                {
                    benchmarks._retryPolicy.RecordRetryAttempt();
                }
                else if (i % 7 == 2)
                {
                    benchmarks._timeoutPolicy.RecordExecutionTime(50 + (i % 100));
                }
                else if (i % 7 == 3)
                {
                    if (benchmarks._bulkheadPolicy.TryAcquireSlot())
                    {
                        benchmarks._bulkheadPolicy.ReleaseSlot();
                    }
                }
                else if (i % 7 == 4)
                {
                    benchmarks._fallbackPolicy.RecordSuccessfulFallback(100);
                }
                else if (i % 7 == 5)
                {
                    var state = benchmarks._circuitBreaker.CurrentState;
                }
                else
                {
                    benchmarks._retryPolicy.GetNextDelayMs(i % 5);
                }
            }
            catch
            {
                Interlocked.Increment(ref failureCount);
            }
        });

        return (successCount, failureCount, DateTime.UtcNow - startTime);
    }
}