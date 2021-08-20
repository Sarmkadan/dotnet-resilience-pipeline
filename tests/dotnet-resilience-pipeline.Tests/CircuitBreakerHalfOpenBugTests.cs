#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;

namespace DotNetResiliencePipeline.Tests;

public sealed class CircuitBreakerHalfOpenBugTests
{
    [Fact]
    public void RecordSuccess_InHalfOpen_ShouldOnlyAllowSuccessThresholdRequests()
    {
        // Arrange: Create a circuit breaker that opens immediately
        var policy = new CircuitBreakerPolicy("test-cb")
        {
            FailureThreshold = 1, // Opens after 1 failure
            SuccessThresholdInHalfOpen = 2, // Needs 2 successes to close
            OpenDuration = TimeSpan.Zero // Allows instant transition to HalfOpen
        };

        // Act: Open the circuit
        policy.RecordFailure(); // Opens circuit
        policy.AttemptReset(); // Transitions to HalfOpen

        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.HalfOpen);

        // Act: Record 2 successes - should close the circuit
        policy.RecordSuccess();
        policy.RecordSuccess();

        // Assert: Circuit should be closed now
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Closed);
    }

    [Fact]
    public void RecordSuccess_InHalfOpen_ShouldBlockAdditionalRequestsAfterSuccessThreshold()
    {
        // Arrange: Create a circuit breaker that opens immediately
        var policy = new CircuitBreakerPolicy("test-cb")
        {
            FailureThreshold = 1,
            SuccessThresholdInHalfOpen = 2,
            OpenDuration = TimeSpan.Zero
        };

        // Act: Open the circuit and transition to HalfOpen
        policy.RecordFailure();
        policy.AttemptReset();

        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.HalfOpen);

        // Act: Record 2 successes to close the circuit
        policy.RecordSuccess();
        policy.RecordSuccess();

        // Assert: Circuit is now closed
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Closed);

        // Act: Try to record more successes in Closed state (should reset failures)
        policy.RecordSuccess();
        policy.RecordSuccess();
        policy.RecordSuccess();

        // Assert: Circuit remains closed, no failures accumulated
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Closed);
        policy.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void RecordFailure_InHalfOpen_ShouldReopenCircuit()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy("test-cb")
        {
            FailureThreshold = 1,
            SuccessThresholdInHalfOpen = 2,
            OpenDuration = TimeSpan.Zero
        };

        // Open circuit and go to HalfOpen
        policy.RecordFailure();
        policy.AttemptReset();
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.HalfOpen);

        // Act: Record 1 success (not enough to close)
        policy.RecordSuccess();

        // Assert: Still in HalfOpen
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.HalfOpen);
        var successfulInHalfOpen = (int)policy.Metadata["SuccessfulInHalfOpen"];
        successfulInHalfOpen.Should().Be(1);

        // Act: Record a failure - should reopen
        policy.RecordFailure();

        // Assert: Circuit should be open again
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Open);
    }

    [Fact]
    public void AttemptReset_WhenOpenDurationElapsed_TransitionsToHalfOpen()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy("test-cb")
        {
            FailureThreshold = 1,
            OpenDuration = TimeSpan.FromMilliseconds(100)
        };

        // Open circuit
        policy.RecordFailure();
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Open);

        // Act: Attempt reset before duration elapsed
        policy.AttemptReset();
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Open);

        // Wait for duration to elapse
        Thread.Sleep(150);

        // Act: Attempt reset after duration elapsed
        policy.AttemptReset();

        // Assert: Should transition to HalfOpen
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.HalfOpen);
    }
}
