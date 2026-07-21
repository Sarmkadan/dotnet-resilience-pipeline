#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

/// <summary>
/// Tests for the AdaptiveTimeoutService class.
/// </summary>
public sealed class AdaptiveTimeoutServiceTests
{
    private readonly AdaptiveTimeoutService _service;

    public AdaptiveTimeoutServiceTests()
    {
        _service = new AdaptiveTimeoutService(NullLogger<AdaptiveTimeoutService>.Instance);
    }

    /// <summary>
    /// Tests that ExecuteAsync throws an ArgumentNullException when the policy is null.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithNullPolicy_ThrowsArgumentNullException()
    {
        Func<Task> act = () => _service.ExecuteAsync<string>(
            null!,
            ct => Task.FromResult("result"));

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("policy");
    }

    /// <summary>
    /// Tests that ExecuteAsync throws an ArgumentNullException when the operation is null.
    /// </>
    [Fact]
    public async Task ExecuteAsync_WithNullOperation_ThrowsArgumentNullException()
    {
        var policy = new AdaptiveTimeoutPolicy("test");

        Func<Task> act = () => _service.ExecuteAsync<string>(
            policy,
            null!);

        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("operation");
    }

    /// <summary>
    /// Tests that ExecuteAsync throws an InvalidPolicyConfigurationException when the policy is invalid.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithInvalidPolicy_ThrowsInvalidPolicyConfigurationException()
    {
        var service = new AdaptiveTimeoutService(NullLogger<AdaptiveTimeoutService>.Instance);
        var policy = new AdaptiveTimeoutPolicy("invalid") { MinTimeout = TimeSpan.FromSeconds(10), MaxTimeout = TimeSpan.FromSeconds(5) };

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
        var policy = new AdaptiveTimeoutPolicy("disabled")
        {
            IsEnabled = false,
            InitialTimeout = TimeSpan.FromMilliseconds(10)
        };

        var result = await _service.ExecuteAsync<string>(
            policy,
            async ct =>
            {
                await Task.Delay(50, ct);
                return "completed";
            });

        result.Should().Be("completed");
    }

    /// <summary>
    /// Tests that timeout grows after slow samples.
    /// </summary>
    [Fact]
    public void Timeout_Grows_After_Slow_Samples()
    {
        var policy = new AdaptiveTimeoutPolicy("grow-test")
        {
            InitialTimeout = TimeSpan.FromMilliseconds(100),
            MinTimeout = TimeSpan.FromMilliseconds(50),
            MaxTimeout = TimeSpan.FromSeconds(10),
            TargetPercentile = 90.0,
            HeadroomFactor = 1.5,
            WindowSize = 10,
            MinSampleSize = 5,
            AdjustmentInterval = TimeSpan.FromSeconds(1)
        };

        // Record several slow executions to trigger timeout growth
        var slowTimes = new[] { 200L, 250L, 300L, 350L, 400L };
        foreach (var time in slowTimes)
        {
            policy.RecordExecutionTime(time);
        }

        // After recording slow samples, timeout should have grown
        policy.CurrentTimeout.Should().BeGreaterThan(policy.InitialTimeout);
        policy.CurrentTimeout.Should().BeLessThanOrEqualTo(policy.MaxTimeout);
    }

    /// <summary>
    /// Tests that timeout shrinks after fast samples.
    /// </summary>
    [Fact]
    public void Timeout_Shrinks_After_Fast_Samples()
    {
        var policy = new AdaptiveTimeoutPolicy("shrink-test")
        {
            InitialTimeout = TimeSpan.FromSeconds(2),
            MinTimeout = TimeSpan.FromMilliseconds(50),
            MaxTimeout = TimeSpan.FromSeconds(10),
            TargetPercentile = 90.0,
            HeadroomFactor = 1.5,
            WindowSize = 10,
            MinSampleSize = 5,
            AdjustmentInterval = TimeSpan.FromSeconds(1)
        };

        // First, record some slow executions to establish a higher baseline
        var slowTimes = new[] { 1500L, 1600L, 1700L, 1800L, 1900L };
        foreach (var time in slowTimes)
        {
            policy.RecordExecutionTime(time);
        }

        var initialAdjustedTimeout = policy.CurrentTimeout;
        initialAdjustedTimeout.Should().BeGreaterThan(policy.InitialTimeout);

        // Now record fast executions to trigger timeout shrinkage
        var fastTimes = new[] { 50L, 60L, 70L, 80L, 90L };
        foreach (var time in fastTimes)
        {
            policy.RecordExecutionTime(time);
        }

        // After recording fast samples, timeout should have shrunk
        policy.CurrentTimeout.Should().BeLessThan(initialAdjustedTimeout);
        policy.CurrentTimeout.Should().BeGreaterThanOrEqualTo(policy.MinTimeout);
    }

    /// <summary>
    /// Tests that timeout respects configured min/max bounds.
    /// </summary>
    [Fact]
    public void Timeout_Respects_MinMax_Bounds()
    {
        var policy = new AdaptiveTimeoutPolicy("bounds-test")
        {
            InitialTimeout = TimeSpan.FromMilliseconds(500),
            MinTimeout = TimeSpan.FromMilliseconds(100),
            MaxTimeout = TimeSpan.FromSeconds(2),
            TargetPercentile = 95.0,
            HeadroomFactor = 2.0, // Aggressive growth factor
            WindowSize = 10,
            MinSampleSize = 3,
            AdjustmentInterval = TimeSpan.FromSeconds(1)
        };

        // Test that timeout doesn't go below MinTimeout
        var veryFastTimes = new[] { 10L, 20L, 30L };
        foreach (var time in veryFastTimes)
        {
            policy.RecordExecutionTime(time);
        }

        policy.CurrentTimeout.Should().BeGreaterThanOrEqualTo(policy.MinTimeout);

        // Reset and test that timeout doesn't go above MaxTimeout
        policy.ResetStatistics();
        var verySlowTimes = new[] { 5000L, 6000L, 7000L };
        foreach (var time in verySlowTimes)
        {
            policy.RecordExecutionTime(time);
        }

        policy.CurrentTimeout.Should().BeLessThanOrEqualTo(policy.MaxTimeout);
    }

    /// <summary>
    /// Tests that service behaves sanely with zero recorded samples.
    /// </summary>
    [Fact]
    public void GetCurrentTimeout_ReturnsInitialTimeout_WithZeroSamples()
    {
        var policy = new AdaptiveTimeoutPolicy("zero-samples")
        {
            InitialTimeout = TimeSpan.FromSeconds(5)
        };

        // With zero samples, current timeout should equal initial timeout
        policy.CurrentTimeout.Should().Be(policy.InitialTimeout);
    }

    /// <summary>
    /// Tests that GetCurrentTimeout returns the current effective timeout for the given policy.
    /// </summary>
    [Fact]
    public void GetCurrentTimeout_ReturnsCurrentTimeout()
    {
        var policy = new AdaptiveTimeoutPolicy("get-timeout")
        {
            InitialTimeout = TimeSpan.FromSeconds(3)
        };

        var timeout = _service.GetCurrentTimeout(policy);

        timeout.Should().Be(policy.CurrentTimeout);
    }

    /// <summary>
    /// Tests that GetCurrentTimeout throws ArgumentNullException when policy is null.
    /// </summary>
    [Fact]
    public void GetCurrentTimeout_WithNullPolicy_ThrowsArgumentNullException()
    {
        Func<TimeSpan> act = () => _service.GetCurrentTimeout(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policy");
    }

    /// <summary>
    /// Tests that GetAdaptationSummary returns a proper dictionary with adaptation metrics.
    /// </summary>
    [Fact]
    public void GetAdaptationSummary_ReturnsProperDictionary()
    {
        var policy = new AdaptiveTimeoutPolicy("summary-test")
        {
            InitialTimeout = TimeSpan.FromSeconds(1),
            MinTimeout = TimeSpan.FromMilliseconds(100),
            MaxTimeout = TimeSpan.FromSeconds(10)
        };

        // Add some sample data
        policy.RecordExecutionTime(150);
        policy.RecordExecutionTime(200);
        policy.RecordSuccess();

        var summary = _service.GetAdaptationSummary(policy);

        summary.Should().ContainKey("PolicyName").WhichValue.Should().Be("summary-test");
        summary.Should().ContainKey("CurrentTimeoutMs");
        summary.Should().ContainKey("InitialTimeoutMs");
        summary.Should().ContainKey("TargetPercentile");
        summary.Should().ContainKey("TotalAdjustments");
        summary.Should().ContainKey("LastAdjustmentAt");
        summary.Should().ContainKey("TimeoutCount");
        summary.Should().ContainKey("TimeoutPercentage");
        summary.Should().ContainKey("P95ExecutionTimeMs");
        summary.Should().ContainKey("SuccessRate");
        summary.Should().ContainKey("TotalExecutions");
    }

    /// <summary>
    /// Tests that GetAdaptationSummary throws ArgumentNullException when policy is null.
    /// </summary>
    [Fact]
    public void GetAdaptationSummary_WithNullPolicy_ThrowsArgumentNullException()
    {
        Func<Dictionary<string, object>> act = () => _service.GetAdaptationSummary(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policy");
    }

    /// <summary>
    /// Tests that ExecuteAsync records metrics when the operation is successful.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithSuccessfulOperation_RecordsMetrics()
    {
        var policy = new AdaptiveTimeoutPolicy("success-test") { InitialTimeout = TimeSpan.FromSeconds(2) };

        var result = await _service.ExecuteAsync<string>(
            policy,
            ct => Task.FromResult("success"));

        result.Should().Be("success");
        policy.SuccessfulExecutions.Should().Be(1);
        policy.TotalExecutions.Should().Be(1);
        policy._responseWindow.Count.Should().Be(1); // Execution time recorded
    }

    /// <summary>
    /// Tests that ExecuteAsync throws an OperationTimeoutException when the operation times out.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithOperationThatTimesOut_ThrowsOperationTimeoutException()
    {
        var policy = new AdaptiveTimeoutPolicy("timeout-test") { InitialTimeout = TimeSpan.FromMilliseconds(50) };

        Func<Task> act = () => _service.ExecuteAsync<string>(
            policy,
            async ct =>
            {
                await Task.Delay(200, ct); // Longer than timeout
                return "never-completes";
            });

        await act.Should().ThrowAsync<OperationTimeoutException>();
        policy.TimeoutCount.Should().Be(1);
    }

    /// <summary>
    /// Tests that ExecuteAsync records a failure when the operation throws an exception.
    /// </summary>
    [Fact]
    public async Task ExecuteAsync_WithOperationException_RecordsFailure()
    {
        var policy = new AdaptiveTimeoutPolicy("exception-test") { InitialTimeout = TimeSpan.FromSeconds(2) };

        Func<Task> act = () => _service.ExecuteAsync<string>(
            policy,
            ct => throw new InvalidOperationException("test error"));

        await act.Should().ThrowAsync<InvalidOperationException>();
        policy.FailedExecutions.Should().Be(1);
        policy.TotalExecutions.Should().Be(1);
    }
}