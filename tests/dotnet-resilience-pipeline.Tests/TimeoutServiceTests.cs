#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class TimeoutServiceTests
{
    [Fact]
    public async Task ExecuteAsync_WithNullPolicy_ThrowsArgumentNullException()
    {
        var service = new TimeoutService();

        Func<Task> act = () => service.ExecuteAsync<string>(
            null!,
            ct => Task.FromResult("result"));

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("policy");
    }

    [Fact]
    public async Task ExecuteAsync_WithInvalidPolicy_ThrowsInvalidPolicyConfigurationException()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("invalid") { Timeout = TimeSpan.Zero };

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            ct => Task.FromResult("result"));

        await act.Should().ThrowAsync<InvalidPolicyConfigurationException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithDisabledPolicy_BypassesTimeout()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("disabled")
        {
            IsEnabled = false,
            Timeout = TimeSpan.FromMilliseconds(10)
        };

        var result = await service.ExecuteAsync<string>(
            policy,
            async ct =>
            {
                await Task.Delay(50, ct);
                return "completed";
            });

        result.Should().Be("completed");
    }

    [Fact]
    public async Task ExecuteAsync_WithSuccessfulOperation_RecordsMetrics()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("success") { Timeout = TimeSpan.FromSeconds(5) };

        var result = await service.ExecuteAsync<string>(
            policy,
            ct => Task.FromResult("success"));

        result.Should().Be("success");
        policy.SuccessfulExecutions.Should().Be(1);
        policy.TotalExecutions.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithOperationThatTimesOut_ThrowsOperationTimeoutException()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("timeout") { Timeout = TimeSpan.FromMilliseconds(100) };

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            async ct =>
            {
                await Task.Delay(1000, ct);
                return "never-completes";
            });

        await act.Should().ThrowAsync<OperationTimeoutException>();
        policy.TimeoutCount.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_WithExternalCancellation_Rethrows()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("external-cancel") { Timeout = TimeSpan.FromSeconds(5) };
        using var cts = new CancellationTokenSource();

        var task = service.ExecuteAsync<string>(
            policy,
            async ct =>
            {
                await Task.Delay(1000, ct);
                return "result";
            },
            cts.Token);

        cts.CancelAfter(50);

        Func<Task> act = async () => await task;

        await act.Should().ThrowAsync<OperationCanceledException>();
    }

    [Fact]
    public async Task ExecuteAsync_WithOperationException_RecordsFailure()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("exception") { Timeout = TimeSpan.FromSeconds(5) };

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            ct => throw new InvalidOperationException("test error"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        policy.FailedExecutions.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_RecordsExecutionTime()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("execution-time") { Timeout = TimeSpan.FromSeconds(5) };

        await service.ExecuteAsync<string>(
            policy,
            async ct =>
            {
                await Task.Delay(50, ct);
                return "result";
            });

        policy.AverageExecutionTimeMs.Should().BeGreaterThanOrEqualTo(40);
    }

    [Fact]
    public void HasExceededTimeout_WithNullPolicy_ReturnsFalse()
    {
        var service = new TimeoutService();

        var hasExceeded = service.HasExceededTimeout(null!, 5000);

        hasExceeded.Should().BeFalse();
    }

    [Fact]
    public void HasExceededTimeout_WithTimeExceedingTimeout_ReturnsTrue()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("exceeded") { Timeout = TimeSpan.FromMilliseconds(1000) };

        var hasExceeded = service.HasExceededTimeout(policy, 1500);

        hasExceeded.Should().BeTrue();
    }

    [Fact]
    public void HasExceededTimeout_WithTimeWithinTimeout_ReturnsFalse()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("within") { Timeout = TimeSpan.FromMilliseconds(1000) };

        var hasExceeded = service.HasExceededTimeout(policy, 500);

        hasExceeded.Should().BeFalse();
    }

    [Fact]
    public void GetTimeoutMilliseconds_WithNullPolicy_ReturnsZero()
    {
        var service = new TimeoutService();

        var timeoutMs = service.GetTimeoutMilliseconds(null!);

        timeoutMs.Should().Be(0);
    }

    [Fact]
    public void GetTimeoutMilliseconds_ReturnsTimeoutInMs()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("get-timeout") { Timeout = TimeSpan.FromSeconds(5) };

        var timeoutMs = service.GetTimeoutMilliseconds(policy);

        timeoutMs.Should().Be(5000);
    }

    [Fact]
    public void GetTimeoutMilliseconds_HandlesFractionalSeconds()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("fractional") { Timeout = TimeSpan.FromMilliseconds(1500) };

        var timeoutMs = service.GetTimeoutMilliseconds(policy);

        timeoutMs.Should().Be(1500);
    }
}
