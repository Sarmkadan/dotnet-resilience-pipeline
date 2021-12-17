#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for the RetryService class.
/// </summary>
public sealed class RetryServiceTests
{
    /// <summary>
    /// Creates a new RetryPolicy instance with the specified name and maximum retries.
    /// </summary>
    /// <param name="name">The name of the policy.</param>
    /// <param name="maxRetries">The maximum number of retries.</param>
    /// <returns>A new RetryPolicy instance.</returns>
    private static RetryPolicy FastRetryPolicy(string name, int maxRetries = 3) =>
        new RetryPolicy(name)
        {
            MaxRetries = maxRetries,
            InitialDelay = TimeSpan.FromMilliseconds(1),
            Strategy = RetryPolicy.BackoffStrategy.Fixed,
            UseJitter = false
        };

    /// <summary>
    /// Tests that the ExecuteAsync method returns the value without retrying when the operation succeeds.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_OperationSucceeds_ReturnsValueWithoutRetrying()
    {
        var service = new RetryService();
        var policy = FastRetryPolicy("success-retry");
        int callCount = 0;

        var result = await service.ExecuteAsync<string>(
            policy,
            _ => { callCount++; return Task.FromResult("ok"); },
            CancellationToken.None);

        result.Should().Be("ok");
        callCount.Should().Be(1);
        policy.SuccessfulExecutions.Should().Be(1);
        policy.TotalRetryAttempts.Should().Be(0);
    }

    /// <summary>
    /// Tests that the ExecuteAsync method retries and returns the value when the operation fails transiently.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_TransientFailureThenSuccess_RetriesAndReturnsValue()
    {
        var service = new RetryService();
        var policy = FastRetryPolicy("transient-retry", maxRetries: 3);
        int callCount = 0;

        var result = await service.ExecuteAsync<string>(
            policy,
            _ =>
            {
                callCount++;
                if (callCount < 3)
                    throw new TimeoutException("transient");
                return Task.FromResult("recovered");
            },
            CancellationToken.None);

        result.Should().Be("recovered");
        callCount.Should().Be(3);
        policy.TotalRetryAttempts.Should().Be(2);
        policy.SuccessfulExecutions.Should().Be(1);
    }

    /// <summary>
    /// Tests that the ExecuteAsync method throws a MaxRetriesExceededException when all attempts are exhausted.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_AllAttemptsExhausted_ThrowsMaxRetriesExceededException()
    {
        var service = new RetryService();
        var policy = FastRetryPolicy("exhaust-retry", maxRetries: 2);

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            _ => throw new TimeoutException("always fails"),
            CancellationToken.None);

        var ex = await act.Should().ThrowAsync<MaxRetriesExceededException>();
        ex.Which.AttemptCount.Should().BeGreaterThan(0);
        ex.Which.AttemptExceptions.Should().NotBeEmpty();
        policy.FailedExecutions.Should().Be(1);
    }

    /// <summary>
    /// Tests that the ExecuteAsync method throws immediately without retrying when a non-retryable exception is thrown.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NonRetryableException_ThrowsImmediatelyWithoutRetrying()
    {
        var service = new RetryService();
        var policy = new RetryPolicy("non-retryable")
        {
            MaxRetries = 5,
            InitialDelay = TimeSpan.FromMilliseconds(1),
            RetryableExceptions = new List<Type> { typeof(TimeoutException) }
        };
        int callCount = 0;

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            _ => { callCount++; throw new InvalidOperationException("not retryable"); },
            CancellationToken.None);

        await act.Should().ThrowAsync<MaxRetriesExceededException>();
        callCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that the ExecuteAsync method throws an ArgumentNullException when the policy is null.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_NullPolicy_ThrowsArgumentNullException()
    {
        var service = new RetryService();

        Func<Task> act = () => service.ExecuteAsync<string>(
            null!,
            _ => Task.FromResult("x"),
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("policy");
    }

    /// <summary>
    /// Tests that the ExecuteAsync method throws an InvalidPolicyConfigurationException when the policy configuration is invalid.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_InvalidPolicyConfiguration_ThrowsInvalidPolicyConfigurationException()
    {
        var service = new RetryService();
        var badPolicy = new RetryPolicy("bad-config") { MaxRetries = -1 };

        Func<Task> act = () => service.ExecuteAsync<string>(
            badPolicy,
            _ => Task.FromResult("x"),
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidPolicyConfigurationException>();
    }

    /// <summary>
    /// Tests that the ExecuteAsync method stops retrying when a cancellation is requested.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_CancellationRequested_StopsRetrying()
    {
        var service = new RetryService();
        var policy = FastRetryPolicy("cancel-retry", maxRetries: 10);
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            _ => throw new TimeoutException("fail"),
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    /// <summary>
    /// Tests that the ExecuteAsync method executes once when the policy is disabled.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_DisabledPolicy_ExecutesOnce()
    {
        var service = new RetryService();
        var policy = FastRetryPolicy("disabled-retry");
        policy.IsEnabled = false;
        int callCount = 0;

        var result = await service.ExecuteAsync<string>(
            policy,
            _ => { callCount++; return Task.FromResult("direct"); },
            CancellationToken.None);

        result.Should().Be("direct");
        callCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that the CalculateRetryDelay method delegates to the policy.
    /// </summary>
    [Fact]
    public void CalculateRetryDelay_DelegatesToPolicy()
    {
        var service = new RetryService();
        var policy = new RetryPolicy("delay-test")
        {
            Strategy = RetryPolicy.BackoffStrategy.Fixed,
            InitialDelay = TimeSpan.FromMilliseconds(150),
            MaxRetries = 3,
            UseJitter = false
        };

        var delay = service.CalculateRetryDelay(policy, 0);

        delay.Should().Be(TimeSpan.FromMilliseconds(150));
    }

    /// <summary>
    /// Tests that the CalculateRetryDelay method throws an ArgumentNullException when the policy is null.
    /// </summary>
    [Fact]
    public void CalculateRetryDelay_NullPolicy_ThrowsArgumentNullException()
    {
        var service = new RetryService();

        Action act = () => service.CalculateRetryDelay(null!, 0);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that the IsRetryable method returns true when the exception is retryable.
    /// </summary>
    [Fact]
    public void IsRetryable_WithMatchingException_ReturnsTrue()
    {
        var service = new RetryService();
        var policy = new RetryPolicy("retryable-check")
        {
            RetryableExceptions = new List<Type> { typeof(TimeoutException) }
        };

        service.IsRetryable(policy, new TimeoutException()).Should().BeTrue();
    }

    /// <summary>
    /// Tests that the IsRetryable method returns false when the exception is not retryable.
    /// </summary>
    [Fact]
    public void IsRetryable_WithNonMatchingException_ReturnsFalse()
    {
        var service = new RetryService();
        var policy = new RetryPolicy("non-retryable-check")
        {
            RetryableExceptions = new List<Type> { typeof(TimeoutException) }
        };

        service.IsRetryable(policy, new ArgumentException()).Should().BeFalse();
    }

    /// <summary>
    /// Tests that the IsRetryable method returns false when the policy is null.
    /// </summary>
    [Fact]
    public void IsRetryable_NullPolicy_ReturnsFalse()
    {
        var service = new RetryService();

        service.IsRetryable(null!, new Exception()).Should().BeFalse();
    }
}
