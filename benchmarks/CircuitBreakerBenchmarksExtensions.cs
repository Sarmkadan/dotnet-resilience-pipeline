using System;
using BenchmarkDotNet.Attributes;

namespace DotNetResiliencePipeline.Benchmarks
{
/// <summary>
/// Extension methods for <see cref="CircuitBreakerBenchmarks"/> that provide fluent APIs for testing circuit breaker state transitions and behavior.
/// </summary>
public static class CircuitBreakerBenchmarksExtensions
{
/// <summary>
/// Waits for the circuit breaker to reach the specified target state.
/// </summary>
/// <param name="benchmarks">The benchmark instance. Cannot be null.</param>
/// <param name="targetState">The target circuit state to wait for.</param>
/// <param name="timeout">Maximum time to wait for the state transition.</param>
/// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is null.</exception>
/// <exception cref="TimeoutException">Thrown when the target state is not reached within the timeout period.</exception>
public static void WaitForState(this CircuitBreakerBenchmarks benchmarks, CircuitBreakerPolicy.CircuitState targetState, TimeSpan timeout)
{
    ArgumentNullException.ThrowIfNull(benchmarks);

    var startTime = DateTime.UtcNow;
    var pollingInterval = TimeSpan.FromMilliseconds(100);

    while (benchmarks.CircuitBreaker_Get_CurrentState() != targetState)
    {
        if (DateTime.UtcNow - startTime > timeout)
            throw new TimeoutException($"State did not transition to {targetState} within {timeout.TotalSeconds} seconds");

        System.Threading.Thread.Sleep(pollingInterval);
    }
}

/// <summary>
/// Asserts that the circuit breaker transitions from one state to another within the specified timeout.
/// </summary>
/// <param name="benchmarks">The benchmark instance. Cannot be null.</param>
/// <param name="fromState">The expected initial state.</param>
/// <param name="toState">The expected target state after transition.</param>
/// <param name="timeout">Maximum time to wait for the state transition.</param>
/// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is null.</exception>
/// <exception cref="InvalidOperationException">Thrown when the initial state does not match the expected state.</exception>
/// <exception cref="TimeoutException">Thrown when the state transition does not occur within the timeout period.</exception>
public static void AssertStateTransition(this CircuitBreakerBenchmarks benchmarks, CircuitBreakerPolicy.CircuitState fromState, CircuitBreakerPolicy.CircuitState toState, TimeSpan timeout)
{
    ArgumentNullException.ThrowIfNull(benchmarks);

    var initialState = benchmarks.CircuitBreaker_Get_CurrentState();
    if (initialState != fromState)
        throw new InvalidOperationException($"Expected initial state {fromState}, got {initialState}");

    benchmarks.WaitForState(toState, timeout);
}

/// <summary>
/// Triggers the specified number of failures on the circuit breaker.
/// </summary>
/// <param name="benchmarks">The benchmark instance. Cannot be null.</param>
/// <param name="failureCount">Number of failures to trigger. Must be non-negative.</param>
/// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is null.</exception>
/// <exception cref="ArgumentOutOfRangeException"><paramref name="failureCount"/> is negative.</exception>
public static void TriggerFailures(this CircuitBreakerBenchmarks benchmarks, int failureCount)
{
    ArgumentNullException.ThrowIfNull(benchmarks);
    ArgumentOutOfRangeException.ThrowIfNegative(failureCount);

    for (int i = 0; i < failureCount; i++)
    {
        benchmarks.CircuitBreaker_Failure_Recording();
    }
}

/// <summary>
/// Verifies that the circuit breaker has the expected number of trips.
/// </summary>
/// <param name="benchmarks">The benchmark instance. Cannot be null.</param>
/// <param name="expectedTrips">The expected number of circuit breaker trips.</param>
/// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is null.</exception>
/// <exception cref="InvalidOperationException">Thrown when the actual trip count does not match the expected value.</exception>
public static void VerifyTripCount(this CircuitBreakerBenchmarks benchmarks, long expectedTrips)
{
    ArgumentNullException.ThrowIfNull(benchmarks);

    var actualTrips = benchmarks.CircuitBreaker_Get_CircuitBreakerTrips();
    if (actualTrips != expectedTrips)
        throw new InvalidOperationException($"Expected {expectedTrips} trips, got {actualTrips}");
}
}
}