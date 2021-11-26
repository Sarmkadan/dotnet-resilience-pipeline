using System;
using System.Collections.Generic;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Extension methods for PolicyComparisonBenchmarks providing additional utility and analysis functionality
/// </summary>
public static class PolicyComparisonBenchmarksExtensions
{
    /// <summary>
    /// Calculates the average retry delay across all retry strategies for a given retry attempt number
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance</param>
    /// <param name="attemptNumber">The retry attempt number (0-based)</param>
    /// <returns>Average delay in milliseconds across all retry strategies</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
    public static double GetAverageRetryDelayMs(this PolicyComparisonBenchmarks benchmarks, int attemptNumber)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var delays = new List<long>
        {
            benchmarks.RetryComparison_Fixed_Strategy(),
            benchmarks.RetryComparison_Linear_Strategy(),
            benchmarks.RetryComparison_Exponential_Strategy(),
            benchmarks.RetryComparison_ExponentialWithJitter_Strategy()
        };

        return delays.Average();
    }

    /// <summary>
    /// Gets the circuit breaker failure rate based on current trips and state
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance</param>
    /// <returns>Failure rate as a percentage (0-100)</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
    public static double GetCircuitBreakerFailureRate(this PolicyComparisonBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var trips = benchmarks.CircuitBreakerComparison_GetTrips_All();
        var state = benchmarks.CircuitBreakerComparison_GetState_All();

        return state switch
        {
            CircuitBreakerPolicy.CircuitState.Open => 100.0,
            _ when trips == 0 => 0.0,
            _ => Math.Min(100.0, (trips / 10.0) * 100.0)
        };
    }

    /// <summary>
    /// Gets the bulkhead utilization metrics across all bulkhead configurations
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance</param>
    /// <returns>Dictionary containing utilization percentages for each bulkhead size</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
    public static Dictionary<string, double> GetBulkheadUtilizationMetrics(this PolicyComparisonBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        return new Dictionary<string, double>
        {
            ["Small"] = benchmarks.BulkheadComparison_GetUtilization_All(),
            ["Medium"] = CalculateMediumBulkheadUtilization(benchmarks),
            ["Large"] = CalculateLargeBulkheadUtilization(benchmarks)
        };
    }

    /// <summary>
    /// Calculates the total capacity utilization across all policies
    /// </summary>
    /// <param name="benchmarks">The benchmarks instance</param>
    /// <returns>Total utilization percentage (0-100)</returns>
    /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <see langword="null"/></exception>
    public static double GetTotalPolicyUtilization(this PolicyComparisonBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var retryUtilization = benchmarks.GetRetryPolicyUtilization();
        var circuitBreakerUtilization = benchmarks.GetCircuitBreakerUtilization();
        var bulkheadUtilization = benchmarks.GetBulkheadUtilizationMetrics();

        // Normalize and average the utilizations
        var values = new List<double>
        {
            retryUtilization,
            circuitBreakerUtilization,
            bulkheadUtilization["Small"],
            bulkheadUtilization["Medium"],
            bulkheadUtilization["Large"]
        };

        return values.Average();
    }

    private static double CalculateMediumBulkheadUtilization(PolicyComparisonBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        // Medium bulkhead has MaxParallelization=20, MaxQueueLength=100
        int acquiredSlots = 0;
        for (int i = 0; i < 20; i++)
        {
            if (benchmarks.BulkheadComparison_Medium_TryAcquireSlot())
                acquiredSlots++;
        }

        return (acquiredSlots / 20.0) * 100.0;
    }

    private static double CalculateLargeBulkheadUtilization(PolicyComparisonBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        // Large bulkhead has MaxParallelization=50, MaxQueueLength=200
        int acquiredSlots = 0;
        for (int i = 0; i < 50; i++)
        {
            if (benchmarks.BulkheadComparison_Large_TryAcquireSlot())
                acquiredSlots++;
        }

        return (acquiredSlots / 50.0) * 100.0;
    }

    private static double GetRetryPolicyUtilization(this PolicyComparisonBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        // All retry policies have MaxRetries=5
        // We'll consider utilization based on how many attempts have been recorded
        // This is a simplified metric for benchmarking purposes
        var attempts = benchmarks.RetryComparison_GetTotalRetryAttempts();
        return attempts > 0 ? Math.Min(100.0, (attempts / 20.0) * 100.0) : 0.0;
    }

    private static double GetCircuitBreakerUtilization(this PolicyComparisonBenchmarks benchmarks)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);

        var state = benchmarks.CircuitBreakerComparison_GetState_All();
        var trips = benchmarks.CircuitBreakerComparison_GetTrips_All();

        // Circuit breaker utilization is based on how close it is to tripping
        return state switch
        {
            CircuitBreakerPolicy.CircuitState.Open => 100.0,
            CircuitBreakerPolicy.CircuitState.HalfOpen => 50.0,
            _ => Math.Min(100.0, (trips / 10.0) * 100.0)
        };
    }
}