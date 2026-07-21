#nullable enable
using DotNetResiliencePipeline.Domain;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>Provides unit tests for the <see cref="FallbackService"/> class.</summary>
public sealed class FallbackServiceTests
{
    [Fact]
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> throws <see cref="ArgumentNullException"/> when the policy is null.</summary>
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
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> throws <see cref="InvalidPolicyConfigurationException"/> when the policy configuration is invalid.</summary>
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
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> returns the primary failure when the policy is disabled.</summary>
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
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> returns the primary failure when the fallback condition is not met.</summary>
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
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> returns the successful fallback result.</summary>
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
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> throws <see cref="FallbackFailedException"/> when the fallback action fails.</summary>
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
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> rethrows the primary exception when no fallback action is defined.</summary>
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
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> correctly records metrics for a successful fallback.</summary>
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
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> correctly records metrics for a failed fallback.</summary>
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
    /// <summary>Tests that <see cref="FallbackService.ShouldTriggerFallback"/> returns false when the policy is null.</summary>
    public void ShouldTriggerFallback_WithNullPolicy_ReturnsFalse()
    {
        var service = new FallbackService();

        var result = service.ShouldTriggerFallback(null!, new InvalidOperationException("test"));

        result.Should().BeFalse();
    }

    [Fact]
    /// <summary>Tests that <see cref="FallbackService.ShouldTriggerFallback"/> correctly delegates to the policy.</summary>
    public void ShouldTriggerFallback_DelegatesToPolicy()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("delegate-trigger") { FallbackOnAnyException = true };

        var result = service.ShouldTriggerFallback(policy, new Exception("test"));

        result.Should().BeTrue();
    }

    [Fact]
    /// <summary>Tests that <see cref="FallbackService.GetFallbackSuccessRate"/> returns zero when the policy is null.</summary>
    public void GetFallbackSuccessRate_WithNullPolicy_ReturnsZero()
    {
        var service = new FallbackService();

        var rate = service.GetFallbackSuccessRate(null!);

        rate.Should().Be(0);
    }

    [Fact]
    /// <summary>Tests that <see cref="FallbackService.GetFallbackSuccessRate"/> correctly delegates to the policy.</summary>
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
    /// <summary>Tests that <see cref="FallbackService.AddFallbackTrigger"/> correctly delegates to the policy.</summary>
    public void AddFallbackTrigger_DelegatesToPolicy()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("add-trigger") { FallbackOnAnyException = false };

        service.AddFallbackTrigger(policy, typeof(TimeoutException));

        policy.FallbackTriggerExceptions.Should().Contain(typeof(TimeoutException));
    }

    [Fact]
    /// <summary>Tests that <see cref="FallbackService.RemoveFallbackTrigger"/> correctly delegates to the policy.</summary>
    public void RemoveFallbackTrigger_DelegatesToPolicy()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("remove-trigger") { FallbackOnAnyException = false };

        policy.AddFallbackTrigger(typeof(TimeoutException));
        service.RemoveFallbackTrigger(policy, typeof(TimeoutException));

        policy.FallbackTriggerExceptions.Should().NotContain(typeof(TimeoutException));
    }

    [Fact]
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> throws <see cref="FallbackFailedException"/> when the fallback action times out.</summary>
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
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> returns the correct result type for a typed fallback.</summary>
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

    [Fact]
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> returns the primary success result when fallback is not triggered.</summary>
    public async Task ExecuteAsync_PrimarySuccess_NoFallbackTriggered_ReturnsPrimaryResult()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("primary-success")
        {
            IsEnabled = true,
            FallbackOnAnyException = false,
            FallbackTriggerExceptions = new List<Type> { typeof(TimeoutException) }
        };

        var result = await service.ExecuteAsync<string>(
            policy,
            new InvalidOperationException("primary failure"), // This won't trigger fallback
            100,
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    /// <summary>Tests that <see cref="FallbackService.ExecuteAsync{TResult}"/> properly handles cancellation during fallback execution.</summary>
    public async Task ExecuteAsync_CancellationHonored_DuringFallbackExecution()
    {
        var service = new FallbackService();
        var policy = new FallbackPolicy("cancellation-test")
        {
            FallbackTimeout = TimeSpan.FromSeconds(10),
            FallbackOnAnyException = true
        };

        policy.SetFallbackAction<string>(async (ct) =>
        {
            await Task.Delay(1000, ct); // Long-running fallback that respects cancellation
            return "fallback-result";
        });

        var cts = new CancellationTokenSource();
        var fallbackTask = service.ExecuteAsync<string>(
            policy,
            new InvalidOperationException("primary failure"),
            100,
            cts.Token);

        // Cancel immediately
        cts.Cancel();

        // Cancellation should be honored and throw OperationCanceledException or wrapped in FallbackFailedException
        var exception = await Assert.ThrowsAsync<FallbackFailedException>(() => fallbackTask);
        exception.FallbackException.Should().BeAssignableTo<OperationCanceledException>();
    }
}
