#nullable enable
using DotNetResiliencePipeline.Domain;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class FallbackServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WithNullPolicy_ThrowsArgumentNullException()
    {
        var service = new FallbackService();

        Func<Task> act = () => service.ExecuteAsync<string>(
            null!,
            new Exception("test"),
            100,
            CancellationToken.None);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("policy");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPolicy_ThrowsInvalidPolicyConfigurationException()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("invalid")
        {
            FallbackOnAnyException = false,
            FallbackTriggerExceptions = new List<Type>()
        };

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            new Exception("test"),
            100,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidPolicyConfigurationException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithDisabledPolicy_ReturnsPrimaryFailure()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("disabled") { IsEnabled = false };
        var primaryException = new InvalidOperationException("primary failure");

        var result = await service.ExecuteAsync<string>(
            policy,
            primaryException,
            100,
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Exception.Should().Be(primaryException);
    }

    [Fact]
    public async Task ExecuteAsync_WhenFallbackNotTriggered_ReturnsPrimaryFailure()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("no-trigger")
        {
            FallbackOnAnyException = false,
            FallbackTriggerExceptions = new List<Type> { typeof(TimeoutException) }
        };
        var primaryException = new InvalidOperationException("not timeout");

        var result = await service.ExecuteAsync<string>(
            policy,
            primaryException,
            100,
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulFallback_ReturnsFallbackResult()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("fallback-success");
        policy.SetFallbackAction<string>(async (ct) => "fallback-result");

        var result = await service.ExecuteAsync<string>(
            policy,
            new InvalidOperationException("primary failure"),
            100,
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("fallback-result");
    }

    [Fact]
    public async Task ExecuteAsync_WithFailedFallback_ThrowsFallbackFailedException()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("fallback-fails");
        policy.SetFallbackAction<string>(async (ct) =>
        {
            throw new InvalidOperationException("fallback error");
        });

        var primaryException = new TimeoutException("primary failure");

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            primaryException,
            100,
            CancellationToken.None);

        await act.Should().ThrowAsync<FallbackFailedException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithoutFallbackActionSet_RethrowsPrimaryException()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("no-action");
        var primaryException = new InvalidOperationException("primary failure");

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            primaryException,
            100,
            CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_RecordsSuccessfulFallbackMetrics()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("metrics");
        policy.SetFallbackAction(async (ct) => "result");

        await service.ExecuteAsync<string>(
            policy,
            new InvalidOperationException("primary"),
            100,
            CancellationToken.None);

        policy.FallbackInvocationCount.Should().Be(1);
        policy.SuccessfulFallbackCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsFailedFallbackMetrics()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("failed-metrics");
        policy.SetFallbackAction<string>(async (ct) =>
        {
            throw new Exception("fallback error");
        });

        try
        {
            await service.ExecuteAsync<string>(
                policy,
                new InvalidOperationException("primary"),
                100,
                CancellationToken.None);
        }
        catch { }

        policy.FallbackInvocationCount.Should().Be(1);
        policy.FailedFallbackCount.Should().Be(1);
    }

    [Fact]
    public void ShouldTriggerFallback_WithNullPolicy_ReturnsFalse()
    {
        var service = new FallbackService();

        var result = service.ShouldTriggerFallback(null!, new InvalidOperationException("test"));

        result.Should().BeFalse();
    }

    [Fact]
    public void ShouldTriggerFallback_DelegatesToPolicy()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("delegate-trigger") { FallbackOnAnyException = true };

        var result = service.ShouldTriggerFallback(policy, new Exception("test"));

        result.Should().BeTrue();
    }

    [Fact]
    public void GetFallbackSuccessRate_WithNullPolicy_ReturnsZero()
    {
        var service = new FallbackService();

        var rate = service.GetFallbackSuccessRate(null!);

        rate.Should().Be(0);
    }

    [Fact]
    public void GetFallbackSuccessRate_DelegatesToPolicy()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("success-rate");

        policy.RecordSuccessfulFallback(100);
        policy.RecordSuccessfulFallback(100);
        policy.RecordFailedFallback(new Exception("test"), 150);

        var rate = service.GetFallbackSuccessRate(policy);

        rate.Should().BeApproximately(66.67, 1.0);
    }

    [Fact]
    public void AddFallbackTrigger_DelegatesToPolicy()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("add-trigger") { FallbackOnAnyException = false };

        service.AddFallbackTrigger(policy, typeof(TimeoutException));

        policy.FallbackTriggerExceptions.Should().Contain(typeof(TimeoutException));
    }

    [Fact]
    public void RemoveFallbackTrigger_DelegatesToPolicy()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("remove-trigger") { FallbackOnAnyException = false };

        policy.AddFallbackTrigger(typeof(TimeoutException));
        service.RemoveFallbackTrigger(policy, typeof(TimeoutException));

        policy.FallbackTriggerExceptions.Should().NotContain(typeof(TimeoutException));
    }

    [Fact]
    public async Task ExecuteAsync_WithFallbackTimeout_ThrowsOnTimeout()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("timeout-test")
        {
            FallbackTimeout = TimeSpan.FromMilliseconds(50),
            FallbackOnAnyException = true
        };

        policy.SetFallbackAction<string>(async (ct) =>
        {
            await Task.Delay(5000, ct);
            return "result";
        });

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            new InvalidOperationException("primary"),
            100,
            CancellationToken.None);

        await act.Should().ThrowAsync<FallbackFailedException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithTypedFallback_ReturnsCorrectType()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("typed-fallback");

        policy.SetFallbackAction<int>(async (ct) => 42);

        var result = await service.ExecuteAsync<int>(
            policy,
            new InvalidOperationException("primary"),
            100,
            CancellationToken.None);

        result.Data.Should().Be(42);
    }
}
