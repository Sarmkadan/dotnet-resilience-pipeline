#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests cancellation semantics for the TimeoutService class.
/// </summary>
public sealed class TimeoutServiceCancellationTests
{
    /// <summary>
    /// Tests that an operation exceeding the configured timeout is recorded as a timeout.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithOperationOutlivingTimeout_ThrowsAndRecordsTimeout()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("operation-timeout")
        {
            Timeout = TimeSpan.FromMilliseconds(30)
        };
        using var safetyCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            async ct =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return "unreachable";
            },
            safetyCts.Token);

        await act.Should().ThrowAsync<OperationTimeoutException>();
        policy.TimeoutCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that a pre-cancelled caller token is propagated without recording a timeout.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithPreCancelledCallerToken_PropagatesCancellationWithoutRecordingTimeout()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("pre-cancelled")
        {
            Timeout = TimeSpan.FromMilliseconds(50)
        };
        using var cts = new CancellationTokenSource();
        cts.Cancel();

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            async ct =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return "unreachable";
            },
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        policy.TimeoutCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that mid-flight caller cancellation is propagated without recording a timeout.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithCallerTokenCancelledMidFlight_PropagatesCancellationWithoutRecordingTimeout()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("mid-flight-cancel")
        {
            Timeout = TimeSpan.FromMilliseconds(200)
        };
        using var cts = new CancellationTokenSource(TimeSpan.FromMilliseconds(30));

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            async ct =>
            {
                await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                return "unreachable";
            },
            cts.Token);

        await act.Should().ThrowAsync<OperationCanceledException>();
        policy.TimeoutCount.Should().Be(0);
    }

    /// <summary>
    /// Tests that a disabled strategy executes directly with the caller's token.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithDisabledStrategy_ExecutesDirectlyWithCallerToken()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("disabled")
        {
            IsEnabled = false,
            Timeout = TimeSpan.FromMilliseconds(20)
        };
        using var cts = new CancellationTokenSource();
        CancellationToken observedToken = default;

        var result = await service.ExecuteAsync(
            policy,
            ct =>
            {
                observedToken = ct;
                return Task.FromResult("completed");
            },
            cts.Token);

        result.Should().Be("completed");
        observedToken.Should().Be(cts.Token);
        policy.TotalExecutions.Should().Be(0);
    }

    /// <summary>
    /// Tests that the operation token is cancelled when the configured timeout fires.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WhenTimeoutFires_CancelsTokenPassedToOperation()
    {
        var service = new TimeoutService();
        var policy = new TimeoutPolicy("operation-token")
        {
            Timeout = TimeSpan.FromMilliseconds(30)
        };
        using var safetyCts = new CancellationTokenSource(TimeSpan.FromSeconds(1));
        var operationObservedCancellation = false;

        Func<Task> act = () => service.ExecuteAsync<string>(
            policy,
            async ct =>
            {
                try
                {
                    await Task.Delay(Timeout.InfiniteTimeSpan, ct);
                }
                catch (OperationCanceledException)
                {
                    operationObservedCancellation = ct.IsCancellationRequested;
                    throw;
                }

                return "unreachable";
            },
            safetyCts.Token);

        await act.Should().ThrowAsync<OperationTimeoutException>();
        operationObservedCancellation.Should().BeTrue();
    }
}
