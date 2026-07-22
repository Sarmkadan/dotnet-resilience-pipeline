using DotNetResiliencePipeline.Exceptions;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Unit tests for ResiliencyException and its derived exception classes.
/// </summary>
public class ResiliencyExceptionTests
{
    [Fact]
    public void ResiliencyException_WithNullMessage_SetsDefaultValues()
    {
        // Arrange
        string? message = null;
        string? policyName = null;
        string? policyType = null;

        // Act
        var exception = new ResiliencyException(message, policyName, policyType);

        // Assert
        exception.PolicyName.Should().BeNull();
        exception.PolicyType.Should().BeNull();
        exception.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        exception.Message.Should().NotBeNull();
    }

    [Fact]
    public void ResiliencyException_WithMessage_SetsProperties()
    {
        // Arrange
        var message = "Test error message";
        var policyName = "TestPolicy";
        var policyType = "TestType";

        // Act
        var exception = new ResiliencyException(message, policyName, policyType);

        // Assert
        exception.Message.Should().Be(message);
        exception.PolicyName.Should().Be(policyName);
        exception.PolicyType.Should().Be(policyType);
        exception.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ResiliencyException_WithNullMessage_SetsProperties()
    {
        // Arrange
        string? message = null;
        var policyName = "TestPolicy";
        var policyType = "TestType";

        // Act
        var exception = new ResiliencyException(message, policyName, policyType);

        // Assert
        exception.Message.Should().NotBeNull();
        exception.PolicyName.Should().Be(policyName);
        exception.PolicyType.Should().Be(policyType);
        exception.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void ResiliencyException_WithMessageAndInnerException_SetsProperties()
    {
        // Arrange
        var message = "Test error message";
        var innerException = new InvalidOperationException("Inner error");
        var policyName = "TestPolicy";
        var policyType = "TestType";

        // Act
        var exception = new ResiliencyException(message, innerException, policyName, policyType);

        // Assert
        exception.Message.Should().Be(message);
        exception.InnerException.Should().BeSameAs(innerException);
        exception.PolicyName.Should().Be(policyName);
        exception.PolicyType.Should().Be(policyType);
        exception.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CircuitBreakerOpenException_Constructor_SetsProperties()
    {
        // Arrange
        var policyName = "CircuitBreakerPolicy";
        var timeUntilRetry = TimeSpan.FromSeconds(30);
        var consecutiveFailures = 5;

        // Act
        var exception = new CircuitBreakerOpenException(policyName, timeUntilRetry, consecutiveFailures);

        // Assert
        exception.PolicyName.Should().Be(policyName);
        exception.PolicyType.Should().Be("CircuitBreaker");
        exception.TimeUntilRetry.Should().Be(timeUntilRetry);
        exception.ConsecutiveFailures.Should().Be(consecutiveFailures);
        exception.Message.Should().Contain(policyName);
        exception.Message.Should().Contain("30.00 seconds");
        exception.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void CircuitBreakerOpenException_ZeroRetryTime_SetsProperties()
    {
        // Arrange
        var policyName = "CircuitBreakerPolicy";
        var timeUntilRetry = TimeSpan.Zero;
        var consecutiveFailures = 0;

        // Act
        var exception = new CircuitBreakerOpenException(policyName, timeUntilRetry, consecutiveFailures);

        // Assert
        exception.TimeUntilRetry.Should().Be(TimeSpan.Zero);
        exception.ConsecutiveFailures.Should().Be(0);
        exception.Message.Should().Contain("0.00 seconds");
    }

    [Fact]
    public void CircuitBreakerOpenException_LargeRetryTime_SetsProperties()
    {
        // Arrange
        var policyName = "CircuitBreakerPolicy";
        var timeUntilRetry = TimeSpan.FromHours(24);
        var consecutiveFailures = 1000;

        // Act
        var exception = new CircuitBreakerOpenException(policyName, timeUntilRetry, consecutiveFailures);

        // Assert
        exception.TimeUntilRetry.Should().Be(timeUntilRetry);
        exception.ConsecutiveFailures.Should().Be(1000);
        exception.Message.Should().Contain("86400.00 seconds");
    }

    [Fact]
    public void BulkheadRejectedException_Constructor_SetsProperties()
    {
        // Arrange
        var policyName = "BulkheadPolicy";
        var currentExecutions = 10;
        var maxExecutions = 20;
        var queuedRequests = 5;

        // Act
        var exception = new BulkheadRejectedException(policyName, currentExecutions, maxExecutions, queuedRequests);

        // Assert
        exception.PolicyName.Should().Be(policyName);
        exception.PolicyType.Should().Be("Bulkhead");
        exception.CurrentExecutions.Should().Be(currentExecutions);
        exception.MaxExecutions.Should().Be(maxExecutions);
        exception.QueuedRequests.Should().Be(queuedRequests);
        exception.Message.Should().Contain(policyName);
        exception.Message.Should().Contain("10/20 slots in use");
        exception.Message.Should().Contain("5 queued");
        exception.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void BulkheadRejectedException_EdgeCaseValues_SetsProperties()
    {
        // Arrange
        var policyName = "BulkheadPolicy";
        var currentExecutions = 0;
        var maxExecutions = 1;
        var queuedRequests = 0;

        // Act
        var exception = new BulkheadRejectedException(policyName, currentExecutions, maxExecutions, queuedRequests);

        // Assert
        exception.CurrentExecutions.Should().Be(0);
        exception.MaxExecutions.Should().Be(1);
        exception.QueuedRequests.Should().Be(0);
        exception.Message.Should().Contain("0/1 slots in use");
    }

    [Fact]
    public void BulkheadRejectedException_MaxValues_SetsProperties()
    {
        // Arrange
        var policyName = "BulkheadPolicy";
        var currentExecutions = int.MaxValue;
        var maxExecutions = int.MaxValue;
        var queuedRequests = int.MaxValue;

        // Act
        var exception = new BulkheadRejectedException(policyName, currentExecutions, maxExecutions, queuedRequests);

        // Assert
        exception.CurrentExecutions.Should().Be(int.MaxValue);
        exception.MaxExecutions.Should().Be(int.MaxValue);
        exception.QueuedRequests.Should().Be(int.MaxValue);
    }

    [Fact]
    public void OperationTimeoutException_Constructor_SetsProperties()
    {
        // Arrange
        var policyName = "TimeoutPolicy";
        var timeout = TimeSpan.FromSeconds(5);
        var actualTimeMs = 6500L; // 6.5 seconds

        // Act
        var exception = new OperationTimeoutException(policyName, timeout, actualTimeMs);

        // Assert
        exception.PolicyName.Should().Be(policyName);
        exception.PolicyType.Should().Be("Timeout");
        exception.Timeout.Should().Be(timeout);
        exception.ActualExecutionTimeMs.Should().Be(actualTimeMs);
        exception.Message.Should().Contain("5.00 seconds");
        exception.Message.Should().Contain("6500ms");
        exception.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void OperationTimeoutException_ZeroTimeout_SetsProperties()
    {
        // Arrange
        var policyName = "TimeoutPolicy";
        var timeout = TimeSpan.Zero;
        var actualTimeMs = 0L;

        // Act
        var exception = new OperationTimeoutException(policyName, timeout, actualTimeMs);

        // Assert
        exception.Timeout.Should().Be(TimeSpan.Zero);
        exception.ActualExecutionTimeMs.Should().Be(0);
        exception.Message.Should().Contain("0.00 seconds");
        exception.Message.Should().Contain("0ms");
    }

    [Fact]
    public void OperationTimeoutException_VeryLargeTimeout_SetsProperties()
    {
        // Arrange
        var policyName = "TimeoutPolicy";
        var timeout = TimeSpan.FromHours(1);
        var actualTimeMs = long.MaxValue;

        // Act
        var exception = new OperationTimeoutException(policyName, timeout, actualTimeMs);

        // Assert
        exception.Timeout.Should().Be(timeout);
        exception.ActualExecutionTimeMs.Should().Be(long.MaxValue);
        exception.Message.Should().Contain("3600.00 seconds");
    }

    [Fact]
    public void MaxRetriesExceededException_ConstructorWithExceptions_SetsProperties()
    {
        // Arrange
        var policyName = "RetryPolicy";
        var attemptCount = 3;
        var exceptions = new List<Exception>
        {
            new InvalidOperationException("First attempt failed"),
            new ArgumentNullException("param"),
            new TimeoutException("Timeout occurred")
        };

        // Act
        var exception = new MaxRetriesExceededException(policyName, attemptCount, exceptions);

        // Assert
        exception.PolicyName.Should().Be(policyName);
        exception.PolicyType.Should().Be("Retry");
        exception.AttemptCount.Should().Be(attemptCount);
        exception.AttemptExceptions.Should().BeEquivalentTo(exceptions);
        exception.Message.Should().Contain("3 retry attempts");
        exception.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void MaxRetriesExceededException_ConstructorWithNullExceptions_SetsProperties()
    {
        // Arrange
        var policyName = "RetryPolicy";
        var attemptCount = 0;
        List<Exception>? exceptions = null;

        // Act
        var exception = new MaxRetriesExceededException(policyName, attemptCount, exceptions);

        // Assert
        exception.PolicyName.Should().Be(policyName);
        exception.AttemptCount.Should().Be(0);
        exception.AttemptExceptions.Should().BeNull();
        exception.Message.Should().Contain("0 retry attempts");
    }

    [Fact]
    public void MaxRetriesExceededException_EmptyExceptionList_SetsProperties()
    {
        // Arrange
        var policyName = "RetryPolicy";
        var attemptCount = 5;
        var exceptions = new List<Exception>();

        // Act
        var exception = new MaxRetriesExceededException(policyName, attemptCount, exceptions);

        // Assert
        exception.AttemptCount.Should().Be(5);
        exception.AttemptExceptions.Should().BeEmpty();
        exception.Message.Should().Contain("5 retry attempts");
    }

    [Fact]
    public void FallbackFailedException_ConstructorWithExceptions_SetsProperties()
    {
        // Arrange
        var policyName = "FallbackPolicy";
        var primaryEx = new InvalidOperationException("Primary operation failed");
        var fallbackEx = new TimeoutException("Fallback operation failed");

        // Act
        var exception = new FallbackFailedException(policyName, primaryEx, fallbackEx);

        // Assert
        exception.PolicyName.Should().Be(policyName);
        exception.PolicyType.Should().Be("Fallback");
        exception.PrimaryException.Should().BeSameAs(primaryEx);
        exception.FallbackException.Should().BeSameAs(fallbackEx);
        exception.Message.Should().Contain("Primary: Primary operation failed");
        exception.Message.Should().Contain("Fallback: Fallback operation failed");
        exception.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void FallbackFailedException_WithNullPrimaryException_SetsProperties()
    {
        // Arrange
        var policyName = "FallbackPolicy";
        Exception? primaryEx = null;
        var fallbackEx = new TimeoutException("Fallback operation failed");

        // Act
        var exception = new FallbackFailedException(policyName, primaryEx, fallbackEx);

        // Assert
        exception.PrimaryException.Should().BeNull();
        exception.FallbackException.Should().BeSameAs(fallbackEx);
        exception.Message.Should().Contain("Primary: ");
        exception.Message.Should().Contain("Fallback: Fallback operation failed");
    }

    [Fact]
    public void FallbackFailedException_WithNullFallbackException_SetsProperties()
    {
        // Arrange
        var policyName = "FallbackPolicy";
        var primaryEx = new InvalidOperationException("Primary operation failed");
        Exception? fallbackEx = null;

        // Act
        var exception = new FallbackFailedException(policyName, primaryEx, fallbackEx);

        // Assert
        exception.PrimaryException.Should().BeSameAs(primaryEx);
        exception.FallbackException.Should().BeNull();
        exception.Message.Should().Contain("Primary: Primary operation failed");
        exception.Message.Should().Contain("Fallback: ");
    }

    [Fact]
    public void FallbackFailedException_WithBothNullExceptions_SetsProperties()
    {
        // Arrange
        var policyName = "FallbackPolicy";
        Exception? primaryEx = null;
        Exception? fallbackEx = null;

        // Act
        var exception = new FallbackFailedException(policyName, primaryEx, fallbackEx);

        // Assert
        exception.PrimaryException.Should().BeNull();
        exception.FallbackException.Should().BeNull();
        exception.Message.Should().Contain("Primary: ");
        exception.Message.Should().Contain("Fallback: ");
    }

    [Fact]
    public void InvalidPolicyConfigurationException_ConstructorWithErrors_SetsProperties()
    {
        // Arrange
        var policyName = "ConfigPolicy";
        var message = "Invalid configuration";
        var errors = new List<string> { "Error 1", "Error 2", "Error 3" };

        // Act
        var exception = new InvalidPolicyConfigurationException(policyName, message, errors);

        // Assert
        exception.PolicyName.Should().Be(policyName);
        exception.PolicyType.Should().Be("Configuration");
        exception.Message.Should().Be(message);
        exception.ConfigurationErrors.Should().BeEquivalentTo(errors);
        exception.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void InvalidPolicyConfigurationException_ConstructorWithNullErrors_SetsProperties()
    {
        // Arrange
        var policyName = "ConfigPolicy";
        var message = "Invalid configuration";
        List<string>? errors = null;

        // Act
        var exception = new InvalidPolicyConfigurationException(policyName, message, errors);

        // Assert
        exception.ConfigurationErrors.Should().NotBeNull();
        exception.ConfigurationErrors.Should().BeEmpty();
    }

    [Fact]
    public void InvalidPolicyConfigurationException_EmptyErrorsList_SetsProperties()
    {
        // Arrange
        var policyName = "ConfigPolicy";
        var message = "Invalid configuration";
        var errors = new List<string>();

        // Act
        var exception = new InvalidPolicyConfigurationException(policyName, message, errors);

        // Assert
        exception.ConfigurationErrors.Should().BeEmpty();
    }

    [Fact]
    public void PipelineExecutionException_Constructor_SetsProperties()
    {
        // Arrange
        var message = "Pipeline execution failed";
        var executionId = "pipeline-123";
        var appliedPolicies = new List<string> { "Policy1", "Policy2", "Policy3" };

        // Act
        var exception = new PipelineExecutionException(message, executionId, appliedPolicies);

        // Assert
        exception.Message.Should().Be(message);
        exception.PolicyName.Should().BeEmpty();
        exception.PolicyType.Should().Be("Pipeline");
        exception.ExecutionId.Should().Be(executionId);
        exception.AppliedPolicies.Should().BeEquivalentTo(appliedPolicies);
        exception.OccurredAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void PipelineExecutionException_ConstructorWithNullAppliedPolicies_SetsProperties()
    {
        // Arrange
        var message = "Pipeline execution failed";
        var executionId = "pipeline-123";
        List<string>? appliedPolicies = null;

        // Act
        var exception = new PipelineExecutionException(message, executionId, appliedPolicies);

        // Assert
        exception.AppliedPolicies.Should().BeNull();
    }

    [Fact]
    public void PipelineExecutionException_EmptyAppliedPolicies_SetsProperties()
    {
        // Arrange
        var message = "Pipeline execution failed";
        var executionId = "pipeline-123";
        var appliedPolicies = new List<string>();

        // Act
        var exception = new PipelineExecutionException(message, executionId, appliedPolicies);

        // Assert
        exception.AppliedPolicies.Should().BeEmpty();
    }

    [Fact]
    public void ResiliencyException_InheritsFromException()
    {
        // Arrange & Act
        var exception = new ResiliencyException("Test message");

        // Assert
        exception.Should().BeAssignableTo<Exception>();
    }

    [Fact]
    public void CircuitBreakerOpenException_InheritsFromResiliencyException()
    {
        // Arrange & Act
        var exception = new CircuitBreakerOpenException("policy", TimeSpan.FromSeconds(10), 5);

        // Assert
        exception.Should().BeAssignableTo<ResiliencyException>();
    }

    [Fact]
    public void BulkheadRejectedException_InheritsFromResiliencyException()
    {
        // Arrange & Act
        var exception = new BulkheadRejectedException("policy", 5, 10, 2);

        // Assert
        exception.Should().BeAssignableTo<ResiliencyException>();
    }

    [Fact]
    public void OperationTimeoutException_InheritsFromResiliencyException()
    {
        // Arrange & Act
        var exception = new OperationTimeoutException("policy", TimeSpan.FromSeconds(5), 5000);

        // Assert
        exception.Should().BeAssignableTo<ResiliencyException>();
    }

    [Fact]
    public void MaxRetriesExceededException_InheritsFromResiliencyException()
    {
        // Arrange & Act
        var exception = new MaxRetriesExceededException("policy", 3, null);

        // Assert
        exception.Should().BeAssignableTo<ResiliencyException>();
    }

    [Fact]
    public void FallbackFailedException_InheritsFromResiliencyException()
    {
        // Arrange & Act
        var exception = new FallbackFailedException("policy", new Exception(), null);

        // Assert
        exception.Should().BeAssignableTo<ResiliencyException>();
    }

    [Fact]
    public void InvalidPolicyConfigurationException_InheritsFromResiliencyException()
    {
        // Arrange & Act
        var exception = new InvalidPolicyConfigurationException("policy", "message");

        // Assert
        exception.Should().BeAssignableTo<ResiliencyException>();
    }

    [Fact]
    public void PipelineExecutionException_InheritsFromResiliencyException()
    {
        // Arrange & Act
        var exception = new PipelineExecutionException("message", "id", null);

        // Assert
        exception.Should().BeAssignableTo<ResiliencyException>();
    }
}