#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using System.Globalization;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Extension methods for <see cref="CircuitBreakerHalfOpenBugTests"/> that provide
/// additional utility and verification capabilities for circuit breaker half-open state testing.
/// </summary>
public static class CircuitBreakerHalfOpenBugTestsExtensions
{
    /// <summary>
    /// Creates a circuit breaker policy configured for half-open state testing with the specified thresholds.
    /// </summary>
    /// <param name="successThreshold">The number of consecutive successes required to close the circuit from half-open state.</param>
    /// <param name="failureThreshold">The number of failures required to open the circuit.</param>
    /// <param name="openDuration">The duration the circuit stays open before transitioning to half-open.</param>
    /// <returns>A configured <see cref="CircuitBreakerPolicy"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any threshold is less than 1.</exception>
    public static CircuitBreakerPolicy CreateHalfOpenTestPolicy(
        this CircuitBreakerHalfOpenBugTests _,
        int successThreshold = 2,
        int failureThreshold = 1,
        TimeSpan? openDuration = null)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(successThreshold, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(failureThreshold, 1);

        return new CircuitBreakerPolicy("test-cb")
        {
            FailureThreshold = failureThreshold,
            SuccessThresholdInHalfOpen = successThreshold,
            OpenDuration = openDuration ?? TimeSpan.Zero
        };
    }

    /// <summary>
    /// Transitions the circuit breaker from open to half-open state and returns the policy.
    /// </summary>
    /// <param name="policy">The circuit breaker policy to transition.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    public static CircuitBreakerPolicy TransitionToHalfOpen(
        this CircuitBreakerHalfOpenBugTests _,
        CircuitBreakerPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        policy.RecordFailure(); // Open the circuit
        policy.AttemptReset();

        return policy;
    }

    /// <summary>
    /// Verifies that the circuit breaker is in the expected half-open state.
    /// </summary>
    /// <param name="policy">The circuit breaker policy to verify.</param>
    /// <param name="expectedSuccessfulInHalfOpen">The expected count of successful requests in half-open state.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    public static CircuitBreakerPolicy ShouldBeInHalfOpenState(
        this CircuitBreakerHalfOpenBugTests _,
        CircuitBreakerPolicy policy,
        int expectedSuccessfulInHalfOpen = 0)
    {
        ArgumentNullException.ThrowIfNull(policy);

        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.HalfOpen);

        if (expectedSuccessfulInHalfOpen > 0)
        {
            var successfulInHalfOpen = (int)policy.Metadata["SuccessfulInHalfOpen"];
            successfulInHalfOpen.Should().Be(expectedSuccessfulInHalfOpen);
        }

        return policy;
    }

    /// <summary>
    /// Records multiple successes in half-open state and verifies the circuit closes.
    /// </summary>
    /// <param name="policy">The circuit breaker policy.</param>
    /// <param name="successCount">The number of consecutive successes to record.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when successCount is less than 1.</exception>
    public static CircuitBreakerPolicy RecordSuccessesAndCloseCircuit(
        this CircuitBreakerHalfOpenBugTests _,
        CircuitBreakerPolicy policy,
        int successCount)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentOutOfRangeException.ThrowIfLessThan(successCount, 1);

        for (int i = 0; i < successCount; i++)
        {
            policy.RecordSuccess();
        }

        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Closed);
        return policy;
    }

    /// <summary>
    /// Records multiple failures in half-open state and verifies the circuit reopens.
    /// </summary>
    /// <param name="policy">The circuit breaker policy.</param>
    /// <param name="failureCount">The number of failures to record.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when failureCount is less than 1.</exception>
    public static CircuitBreakerPolicy RecordFailuresAndReopenCircuit(
        this CircuitBreakerHalfOpenBugTests _,
        CircuitBreakerPolicy policy,
        int failureCount)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentOutOfRangeException.ThrowIfLessThan(failureCount, 1);

        for (int i = 0; i < failureCount; i++)
        {
            policy.RecordFailure();
        }

        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Open);
        return policy;
    }

    /// <summary>
    /// Gets the current consecutive failures count from the circuit breaker policy.
    /// </summary>
    /// <param name="policy">The circuit breaker policy.</param>
    /// <returns>The consecutive failures count.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    public static int GetConsecutiveFailures(this CircuitBreakerHalfOpenBugTests _, CircuitBreakerPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return policy.ConsecutiveFailures;
    }

    /// <summary>
    /// Gets the current consecutive successes count in half-open state from the circuit breaker policy.
    /// </summary>
    /// <param name="policy">The circuit breaker policy.</param>
    /// <returns>The successful requests count in half-open state.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    public static int GetSuccessfulInHalfOpen(this CircuitBreakerHalfOpenBugTests _, CircuitBreakerPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        return (int)policy.Metadata["SuccessfulInHalfOpen"];
    }

    /// <summary>
    /// Creates a circuit breaker policy with realistic timing for half-open state testing.
    /// </summary>
    /// <param name="successThreshold">The number of consecutive successes required to close the circuit from half-open state.</param>
    /// <param name="failureThreshold">The number of failures required to open the circuit.</param>
    /// <param name="openDurationMilliseconds">The duration in milliseconds the circuit stays open before transitioning to half-open.</param>
    /// <returns>A configured <see cref="CircuitBreakerPolicy"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when any parameter is invalid.</exception>
    public static CircuitBreakerPolicy CreateRealisticHalfOpenTestPolicy(
        this CircuitBreakerHalfOpenBugTests _,
        int successThreshold = 3,
        int failureThreshold = 2,
        int openDurationMilliseconds = 500)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(successThreshold, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(failureThreshold, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(openDurationMilliseconds, 1);

        return new CircuitBreakerPolicy("test-cb")
        {
            FailureThreshold = failureThreshold,
            SuccessThresholdInHalfOpen = successThreshold,
            OpenDuration = TimeSpan.FromMilliseconds(openDurationMilliseconds)
        };
    }
}