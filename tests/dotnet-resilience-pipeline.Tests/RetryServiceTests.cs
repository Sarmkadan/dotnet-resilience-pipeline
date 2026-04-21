#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class RetryServiceTests
{
    private static RetryPolicy FastRetryPolicy(string name, int maxRetries = 3) =>
        new RetryPolicy(name)
        {
            MaxRetries = maxRetries,
            InitialDelay = TimeSpan.FromMilliseconds(1),
            Strategy = RetryPolicy.BackoffStrategy.Fixed,
            UseJitter = false
        };

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

    [Fact]
    public void CalculateRetryDelay_NullPolicy_ThrowsArgumentNullException()
    {
        var service = new RetryService();

        Action act = () => service.CalculateRetryDelay(null!, 0);

        act.Should().Throw<ArgumentNullException>();
    }

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

    [Fact]
    public void IsRetryable_NullPolicy_ReturnsFalse()
    {
        var service = new RetryService();

        service.IsRetryable(null!, new Exception()).Should().BeFalse();
    }
}
