#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Utilities;
using FluentAssertions;
using Moq;

namespace DotNetResiliencePipeline.Tests;

public class ResiliencyServiceTests
{
    // Local stub interface used to verify operation invocation via Moq
    private interface IAsyncOperation
    {
        Task<string> RunAsync();
    }

    [Fact]
    public async Task CircuitBreakerService_ExecuteAsync_WhenCircuitIsOpen_NeverInvokesOperation()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy("guard-cb") { FailureThreshold = 1 };
        var service = new CircuitBreakerService();
        var mockOperation = new Mock<IAsyncOperation>();

        policy.RecordFailure(); // reaches FailureThreshold = 1 → circuit opens immediately

        // Act
        Func<Task> act = () => service.ExecuteAsync<string>(
            policy, () => mockOperation.Object.RunAsync());

        // Assert
        await act.Should().ThrowAsync<CircuitBreakerOpenException>();
        mockOperation.Verify(o => o.RunAsync(), Times.Never);
    }

    [Fact]
    public void PolicyValidationHelper_ValidatePolicy_CircuitBreakerWithZeroFailureThreshold_ReturnsError()
    {
        // Arrange
        var policy = new CircuitBreakerPolicy("zero-threshold-cb") { FailureThreshold = 0 };

        // Act
        var report = PolicyValidationHelper.ValidatePolicy(policy);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainMatch("*FailureThreshold*");
    }

    [Fact]
    public void PolicyValidationHelper_ValidatePolicy_RetryWithNegativeMaxRetries_ReturnsError()
    {
        // Arrange
        var policy = new RetryPolicy("bad-retry") { MaxRetries = -1 };

        // Act
        var report = PolicyValidationHelper.ValidatePolicy(policy);

        // Assert
        report.IsValid.Should().BeFalse();
        report.Errors.Should().ContainMatch("*MaxRetries*");
    }

    [Fact]
    public void PolicyValidationHelper_SuggestOptimizations_FixedStrategyRetry_RecommendsExponentialBackoff()
    {
        // Arrange
        var policy = new RetryPolicy("fixed-retry")
        {
            Strategy = RetryPolicy.BackoffStrategy.Fixed,
            MaxRetries = 3
        };

        // Act
        var suggestions = PolicyValidationHelper.SuggestOptimizations(policy);

        // Assert
        suggestions.Should().ContainMatch("*Exponential*");
    }
}
