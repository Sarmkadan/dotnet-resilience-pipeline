#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;

namespace DotNetResiliencePipeline.Tests;

public sealed class CircuitBreakerPolicyTests
{
    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        // Arrange & Act
        Action act = () => new CircuitBreakerPolicy("   ");

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Policy name cannot be empty*");
    }

    [Fact]
    public void RecordFailure_AtFailureThreshold_TransitionsToOpenState()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy("payment-cb") { FailureThreshold = 3 };

        // Act
        policy.RecordFailure();
        policy.RecordFailure();
        policy.RecordFailure(); // hits threshold

        // Assert
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Open);
        policy.ConsecutiveFailures.Should().Be(3);
    }

    [Fact]
    public void RecordFailure_BelowFailureThreshold_RemainsInClosedState()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy("inventory-cb") { FailureThreshold = 5 };

        // Act
        policy.RecordFailure();
        policy.RecordFailure();

        // Assert
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Closed);
        policy.ConsecutiveFailures.Should().Be(2);
    }

    [Fact]
    public void RecordSuccess_InHalfOpenAtSuccessThreshold_TransitionsToClosedState()
    {
        // Arrange: open the circuit, then immediately allow half-open
        var policy = new CircuitBreakerPolicy("order-cb")
        {
            FailureThreshold = 1,
            SuccessThresholdInHalfOpen = 2,
            OpenDuration = TimeSpan.Zero   // allows instant transition to HalfOpen
        };

        policy.RecordFailure();  // opens circuit
        policy.AttemptReset();   // transitions to HalfOpen since OpenDuration == Zero

        // Act
        policy.RecordSuccess();
        policy.RecordSuccess(); // meets SuccessThresholdInHalfOpen = 2

        // Assert
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Closed);
        policy.ConsecutiveFailures.Should().Be(0);
    }

    [Fact]
    public void ManualReset_AfterCircuitOpens_ResetsToClosedAndClearsStatistics()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy("notification-cb") { FailureThreshold = 2 };
        policy.RecordFailure();
        policy.RecordFailure();
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Open);

        // Act
        policy.ManualReset();

        // Assert
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Closed);
        policy.ConsecutiveFailures.Should().Be(0);
        policy.TotalExecutions.Should().Be(0);
    }
}
