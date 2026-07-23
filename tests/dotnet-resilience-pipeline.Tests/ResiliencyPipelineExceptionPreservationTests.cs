#nullable enable

using System;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Tests that verify the exception preservation contract: specific resiliency exceptions
/// should bubble up unchanged through the pipeline, preserving their type and inner exception chain.
/// </summary>
public sealed class ResiliencyPipelineExceptionPreservationTests
{
    /// <summary>
    /// Creates a circuit breaker policy configured to throw CircuitBreakerOpenException.
    /// </summary>
    private static CircuitBreakerPolicy CircuitBreakerAlwaysOpen(string name) =>
        new CircuitBreakerPolicy(name)
        {
            IsEnabled = true,
            FailureThreshold = 1,
            OpenDuration = TimeSpan.FromSeconds(30)
        };

    /// <summary>
    /// Creates a retry policy configured to throw MaxRetriesExceededException.
    /// </summary>
    private static RetryPolicy RetryAlwaysFail(string name, int maxRetries = 2) =>
        new RetryPolicy(name)
        {
            IsEnabled = true,
            MaxRetries = maxRetries,
            InitialDelay = TimeSpan.FromMilliseconds(1),
            Strategy = RetryPolicy.BackoffStrategy.Fixed,
            UseJitter = false
        };

    /// <summary>
    /// Creates a timeout policy configured to throw OperationTimeoutException.
    /// </summary>
    private static TimeoutPolicy TimeoutAlwaysExceed(string name) =>
        new TimeoutPolicy(name)
        {
            IsEnabled = true,
            Timeout = TimeSpan.FromMilliseconds(1)
        };

    /// <summary>
    /// Creates a bulkhead policy configured to throw BulkheadRejectedException.
    /// </summary>
    private static BulkheadPolicy BulkheadAlwaysReject(string name) =>
        new BulkheadPolicy(name)
        {
            IsEnabled = true,
            MaxParallelization = 1,
            MaxQueueLength = 0
        };

    /// <summary>
    /// Verifies that CircuitBreakerOpenException bubbles up unchanged through the pipeline.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CircuitBreakerOpenException_PreservesExceptionType()
    {
        // Arrange
        var service = new ResiliencyPipelineService();
        var circuitBreaker = CircuitBreakerAlwaysOpen("test-circuit-breaker");

        // Act
        Func<Task> act = () => service.ExecuteAsync<string>(
            async _ => await Task.FromException<string>(new InvalidOperationException("original error")),
            cancellationToken: default,
            circuitBreaker: circuitBreaker);

        // Assert
        var ex = await act.Should().ThrowAsync<CircuitBreakerOpenException>();
        ex.Which.Should().BeOfType<CircuitBreakerOpenException>();
        ex.Which.InnerException.Should().NotBeNull();
        ex.Which.InnerException.Should().BeOfType<InvalidOperationException>();
        ex.Which.InnerException.Message.Should().Be("original error");
        ex.Which.TimeUntilRetry.Should().BeGreaterThan(TimeSpan.Zero);
    }

    /// <summary>
    /// Verifies that MaxRetriesExceededException bubbles up unchanged through the pipeline.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_MaxRetriesExceededException_PreservesExceptionType()
    {
        // Arrange
        var service = new ResiliencyPipelineService();
        var retry = RetryAlwaysFail("test-retry", maxRetries: 2);

        // Act
        Func<Task> act = () => service.ExecuteAsync<string>(
            async _ => await Task.FromException<string>(new TimeoutException("transient error")),
            cancellationToken: default,
            retry: retry);

        // Assert
        var ex = await act.Should().ThrowAsync<MaxRetriesExceededException>();
        ex.Which.Should().BeOfType<MaxRetriesExceededException>();
        ex.Which.AttemptCount.Should().Be(3); // 1 initial + 2 retries
        ex.Which.AttemptExceptions.Should().NotBeEmpty();
        ex.Which.AttemptExceptions[0].Should().BeOfType<TimeoutException>();
        ex.Which.AttemptExceptions[0].Message.Should().Be("transient error");
    }

    /// <summary>
    /// Verifies that OperationTimeoutException bubbles up unchanged through the pipeline.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_OperationTimeoutException_PreservesExceptionType()
    {
        // Arrange
        var service = new ResiliencyPipelineService();
        var timeout = TimeoutAlwaysExceed("test-timeout");

        // Act
        Func<Task> act = () => service.ExecuteAsync<string>(
            async _ =>
            {
                await Task.Delay(100); // Ensure we exceed the 1ms timeout
                return "should not reach here";
            },
            timeout: timeout);

        // Assert
        var ex = await act.Should().ThrowAsync<OperationTimeoutException>();
        ex.Which.Should().BeOfType<OperationTimeoutException>();
        ex.Which.InnerException.Should().BeNull(); // Timeout exception doesn't have inner exception
        ex.Which.Timeout.Should().Be(TimeSpan.FromMilliseconds(1));
    }

    /// <summary>
    /// Verifies that BulkheadRejectedException bubbles up unchanged through the pipeline.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_BulkheadRejectedException_PreservesExceptionType()
    {
        // Arrange
        var service = new ResiliencyPipelineService();
        var bulkhead = BulkheadAlwaysReject("test-bulkhead");

        // Act
        Func<Task> act = () => service.ExecuteAsync<string>(
            async _ => await Task.FromResult("success"),
            bulkhead: bulkhead);

        // Assert
        var ex = await act.Should().ThrowAsync<BulkheadRejectedException>();
        ex.Which.Should().BeOfType<BulkheadRejectedException>();
        ex.Which.CurrentExecutions.Should().Be(1);
        ex.Which.MaxExecutions.Should().Be(1);
        ex.Which.QueuedRequests.Should().Be(0);
    }

    /// <summary>
    /// Verifies that nested resiliency exceptions preserve their inner exception chain.
    /// For example: CircuitBreakerOpenException wrapping a TimeoutException.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NestedResiliencyExceptions_PreservesFullChain()
    {
        // Arrange
        var service = new ResiliencyPipelineService();
        var circuitBreaker = CircuitBreakerAlwaysOpen("test-circuit-breaker");

        // Create an operation that throws TimeoutException, which gets wrapped by CircuitBreakerOpenException
        Func<CancellationToken, Task<string>> operation = async _ =>
        {
            await Task.Delay(10); // Ensure circuit breaker is open
            throw new TimeoutException("original timeout error");
        };

        // Act
        Func<Task> act = () => service.ExecuteAsync(operation, circuitBreaker: circuitBreaker);

        // Assert
        var ex = await act.Should().ThrowAsync<CircuitBreakerOpenException>();
        ex.Which.Should().BeOfType<CircuitBreakerOpenException>();
        ex.Which.InnerException.Should().NotBeNull();
        ex.Which.InnerException.Should().BeOfType<TimeoutException>();
        ex.Which.InnerException.Message.Should().Be("original timeout error");
    }

    /// <summary>
    /// Verifies that non-resiliency exceptions are properly wrapped in PolicyResult but the original
    /// exception is preserved in the InnerException chain.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NonResiliencyException_PreservesOriginalException()
    {
        // Arrange
        var service = new ResiliencyPipelineService();

        // Act
        var result = await service.ExecuteAsync<string>(async _ => throw new InvalidOperationException("user error"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Exception.Should().NotBeNull();
        result.Exception.Should().BeOfType<InvalidOperationException>();
        result.Exception.Message.Should().Be("user error");
    }

    /// <summary>
    /// Verifies that retry around circuit breaker preserves CircuitBreakerOpenException type.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_RetryAroundCircuitBreaker_PreservesCircuitBreakerException()
    {
        // Arrange
        var service = new ResiliencyPipelineService();
        var circuitBreaker = CircuitBreakerAlwaysOpen("test-circuit-breaker");
        var retry = RetryAlwaysFail("test-retry", maxRetries: 1);

        // Act
        Func<Task> act = () => service.ExecuteAsync<string>(
            async _ => await Task.FromException<string>(new InvalidOperationException("error")),
            circuitBreaker: circuitBreaker,
            cancellationToken: default,
            retry: retry);

        // Assert - should get CircuitBreakerOpenException, not MaxRetriesExceededException
        var ex = await act.Should().ThrowAsync<CircuitBreakerOpenException>();
        ex.Which.Should().BeOfType<CircuitBreakerOpenException>();
        ex.Which.InnerException.Should().NotBeNull();
        ex.Which.InnerException.Should().BeOfType<InvalidOperationException>();
    }

    /// <summary>
    /// Verifies that timeout around retry preserves MaxRetriesExceededException type.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_TimeoutAroundRetry_PreservesMaxRetriesException()
    {
        // Arrange
        var service = new ResiliencyPipelineService();
        var retry = RetryAlwaysFail("test-retry", maxRetries: 1);
        var timeout = TimeoutAlwaysExceed("test-timeout");

        // Act
        Func<Task> act = () => service.ExecuteAsync<string>(
            async _ => await Task.FromException<string>(new TimeoutException("error")),
            retry: retry,
            timeout: timeout);

        // Assert - should get MaxRetriesExceededException, not OperationTimeoutException
        var ex = await act.Should().ThrowAsync<MaxRetriesExceededException>();
        ex.Which.Should().BeOfType<MaxRetriesExceededException>();
        ex.Which.AttemptCount.Should().Be(2); // 1 initial + 1 retry
    }

    /// <summary>
    /// Verifies that the innermost user exception is always reachable via InnerException chain.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_DeeplyNestedExceptions_PreservesInnermostException()
    {
        // Arrange
        var service = new ResiliencyPipelineService();
        var circuitBreaker = CircuitBreakerAlwaysOpen("test-circuit-breaker");

        // Create a deeply nested exception chain
        var originalException = new ArgumentNullException("paramName", "Parameter cannot be null");
        var wrappedException = new InvalidOperationException("Operation failed", originalException);

        // Act
        Func<Task> act = () => service.ExecuteAsync<string>(
            async _ => await Task.FromException<string>(wrappedException),
            cancellationToken: default,
            circuitBreaker: circuitBreaker);

        // Assert
        var ex = await act.Should().ThrowAsync<CircuitBreakerOpenException>();
        ex.Which.Should().BeOfType<CircuitBreakerOpenException>();

        // Verify we can traverse the entire exception chain
        var current = ex.Which.InnerException;
        current.Should().NotBeNull();
        current.Should().BeOfType<InvalidOperationException>();
        current.Message.Should().Be("Operation failed");

        current = current.InnerException;
        current.Should().NotBeNull();
        current.Should().BeOfType<ArgumentNullException>();
        current.Message.Should().Be("Parameter cannot be null (Parameter 'paramName')");
    }

    /// <summary>
    /// Verifies that PipelineExecutionException can be created with proper inner exception chain.
    /// </summary>
    [Fact]
    public void PipelineExecutionException_CanBeCreatedWithInnerException()
    {
        // Arrange
        var innerException = new InvalidOperationException("original error");
        var executionId = Guid.NewGuid().ToString();
        var appliedPolicies = new List<string> { "retry", "circuit-breaker" };

        // Act
        var ex = new PipelineExecutionException(
            "Pipeline execution failed",
            innerException,
            executionId,
            appliedPolicies);

        // Assert
        ex.Should().BeOfType<PipelineExecutionException>();
        ex.Message.Should().Be("Pipeline execution failed");
        ex.InnerException.Should().BeSameAs(innerException);
        ex.ExecutionId.Should().Be(executionId);
        ex.AppliedPolicies.Should().BeEquivalentTo(appliedPolicies);
    }
}