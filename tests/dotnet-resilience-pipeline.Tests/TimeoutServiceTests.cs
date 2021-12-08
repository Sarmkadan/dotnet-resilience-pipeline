#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for the TimeoutService class.
/// </summary>
public sealed class TimeoutServiceTests
{
    /// <summary>
    /// Tests that ExecuteAsync throws an ArgumentNullException when the policy is null.
    /// </summary>
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

    /// <summary>
    /// Tests that ExecuteAsync throws an InvalidPolicyConfigurationException when the policy is invalid.
    /// </summary>
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

    /// <summary>
    /// Tests that ExecuteAsync bypasses the timeout when the policy is disabled.
    /// </summary>
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

    /// <summary>
    /// Tests that ExecuteAsync records metrics when the operation is successful.
    /// </summary>
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

    /// <summary>
    /// Tests that ExecuteAsync throws an OperationTimeoutException when the operation times out.
    /// </summary>
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

    /// <summary>
    /// Tests that ExecuteAsync rethrows an OperationCanceledException when the operation is externally canceled.
    /// </summary>
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

    /// <summary>
    /// Tests that ExecuteAsync records a failure when the operation throws an exception.
    /// </summary>
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

    /// <summary>
    /// Tests that ExecuteAsync records the execution time.
    /// </summary>
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

    /// <summary>
    /// Tests that HasExceededTimeout returns false when the policy is null.
    /// </summary>
    [Fact]
    public void HasExceededTimeout_WithNullPolicy_ReturnsFalse()
    {
        var service = new TimeoutService();

        var hasExceeded = service.HasExceededTimeout(null!, 5000);

        hasExceeded.Should().BeFalse();
    }

    /// <summary>
    /// Tests that HasExceededTimeout returns true when the time exceeds the timeout.
    /// </summary>
    [Fact]
    public void HasExceededTimeout_WithTimeExceedingTimeout_ReturnsTrue()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("exceeded") { Timeout = TimeSpan.FromMilliseconds(1000) };

        var hasExceeded = service.HasExceededTimeout(policy, 1500);

        hasExceeded.Should().BeTrue();
    }

    /// <summary>
    /// Tests that HasExceededTimeout returns false when the time is within the timeout.
    /// </summary>
    [Fact]
    public void HasExceededTimeout_WithTimeWithinTimeout_ReturnsFalse()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("within") { Timeout = TimeSpan.FromMilliseconds(1000) };

        var hasExceeded = service.HasExceededTimeout(policy, 500);

        hasExceeded.Should().BeFalse();
    }

    /// <summary>
    /// Tests that GetTimeoutMilliseconds returns zero when the policy is null.
    /// </summary>
    [Fact]
    public void GetTimeoutMilliseconds_WithNullPolicy_ReturnsZero()
    {
        var service = new TimeoutService();

        var timeoutMs = service.GetTimeoutMilliseconds(null!);

        timeoutMs.Should().Be(0);
    }

    /// <summary>
    /// Tests that GetTimeoutMilliseconds returns the timeout in milliseconds.
    /// </summary>
    [Fact]
    public void GetTimeoutMilliseconds_ReturnsTimeoutInMs()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("get-timeout") { Timeout = TimeSpan.FromSeconds(5) };

        var timeoutMs = service.GetTimeoutMilliseconds(policy);

        timeoutMs.Should().Be(5000);
    }

    /// <summary>
    /// Tests that GetTimeoutMilliseconds handles fractional seconds.
    /// </summary>
    [Fact]
    public void GetTimeoutMilliseconds_HandlesFractionalSeconds()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("fractional") { Timeout = TimeSpan.FromMilliseconds(1500) };

        var timeoutMs = service.GetTimeoutMilliseconds(policy);

        timeoutMs.Should().Be(1500);
    }
}
