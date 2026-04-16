#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class TimeoutPolicyTests
{
    [Fact]
    public void Constructor_WithValidName_Succeeds()
    {
        var policy = new TimeoutPolicy("test-timeout");

        policy.Name.Should().Be("test-timeout");
        policy.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        Action act = () => new TimeoutPolicy("   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Policy name cannot be empty*");
    }

    [Fact]
    public void IsTimedOut_WithExecutionTimeLessThanTimeout_ReturnsFalse()
    {
        var policy = new TimeoutPolicy("timeout-test")
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        var isTimedOut = policy.IsTimedOut(TimeSpan.FromSeconds(5));

        isTimedOut.Should().BeFalse();
    }

    [Fact]
    public void IsTimedOut_WithExecutionTimeGreaterThanTimeout_ReturnsTrue()
    {
        var policy = new TimeoutPolicy("timeout-exceed")
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        var isTimedOut = policy.IsTimedOut(TimeSpan.FromSeconds(10));

        isTimedOut.Should().BeTrue();
    }

    [Fact]
    public void IsTimedOut_WithExecutionTimeEqualToTimeout_ReturnsFalse()
    {
        var policy = new TimeoutPolicy("timeout-equal")
        {
            Timeout = TimeSpan.FromSeconds(5)
        };

        var isTimedOut = policy.IsTimedOut(TimeSpan.FromSeconds(5));

        isTimedOut.Should().BeFalse();
    }

    [Fact]
    public void IsTimedOutMs_WithTimeGreaterThanTimeout_ReturnsTrue()
    {
        var policy = new TimeoutPolicy("timeout-ms")
        {
            Timeout = TimeSpan.FromMilliseconds(1000)
        };

        var isTimedOut = policy.IsTimedOutMs(1500);

        isTimedOut.Should().BeTrue();
    }

    [Fact]
    public void IsTimedOutMs_WithTimeLessThanTimeout_ReturnsFalse()
    {
        var policy = new TimeoutPolicy("timeout-ms-under")
        {
            Timeout = TimeSpan.FromMilliseconds(1000)
        };

        var isTimedOut = policy.IsTimedOutMs(500);

        isTimedOut.Should().BeFalse();
    }

    [Fact]
    public void RecordExecutionTime_WithNegativeTime_ThrowsArgumentException()
    {
        var policy = new TimeoutPolicy("negative-time");

        Action act = () => policy.RecordExecutionTime(-100);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Execution time cannot be negative*");
    }

    [Fact]
    public void RecordExecutionTime_UpdatesStatistics()
    {
        var policy = new TimeoutPolicy("execution-stats");

        policy.RecordExecutionTime(100);
        policy.RecordExecutionTime(200);
        policy.RecordExecutionTime(300);

        policy.AverageExecutionTimeMs.Should().Be(200);
        policy.LongestExecutionTimeMs.Should().Be(300);
        policy.ShortestExecutionTimeMs.Should().Be(100);
    }

    [Fact]
    public void RecordTimeout_IncreasesTimeoutCountAndRecordsFailure()
    {
        var policy = new TimeoutPolicy("record-timeout");

        policy.RecordTimeout(5000);
        policy.RecordTimeout(5500);

        policy.TimeoutCount.Should().Be(2);
        policy.FailedExecutions.Should().Be(2);
    }

    [Fact]
    public void RecordTimeout_StoresLastTimeoutTime()
    {
        var policy = new TimeoutPolicy("last-timeout");
        var beforeTime = DateTime.UtcNow;

        policy.RecordTimeout(5000);

        var afterTime = DateTime.UtcNow;
        var lastTimeout = (DateTime)policy.Metadata["LastTimeoutAt"];
        lastTimeout.Should().BeOnOrAfter(beforeTime).And.BeOnOrBefore(afterTime);
    }

    [Fact]
    public void GetTimeoutPercentage_CalculatesCorrectly()
    {
        var policy = new TimeoutPolicy("timeout-pct");

        for (int i = 0; i < 80; i++)
            policy.RecordSuccess();

        for (int i = 0; i < 20; i++)
            policy.RecordTimeout(5000);

        var timeoutPct = policy.GetTimeoutPercentage();

        timeoutPct.Should().Be(20);
    }

    [Fact]
    public void GetTimeoutPercentage_WithNoExecutions_ReturnsZero()
    {
        var policy = new TimeoutPolicy("no-exec");

        var timeoutPct = policy.GetTimeoutPercentage();

        timeoutPct.Should().Be(0);
    }

    [Fact]
    public void GetPercentile95ExecutionTime_CalculatesCorrectly()
    {
        var policy = new TimeoutPolicy("p95-test");

        for (int i = 0; i < 100; i++)
            policy.RecordExecutionTime(i);

        var p95 = policy.GetPercentile95ExecutionTime();

        p95.Should().BeGreaterThanOrEqualTo(94);
        p95.Should().BeLessThanOrEqualTo(99);
    }

    [Fact]
    public void GetPercentile99ExecutionTime_CalculatesCorrectly()
    {
        var policy = new TimeoutPolicy("p99-test");

        for (int i = 0; i < 100; i++)
            policy.RecordExecutionTime(i);

        var p99 = policy.GetPercentile99ExecutionTime();

        p99.Should().BeGreaterThanOrEqualTo(98);
        p99.Should().BeLessThanOrEqualTo(99);
    }

    [Fact]
    public void GetPercentileExecutionTime_WithSmallSample_ReturnsSensibleValue()
    {
        var policy = new TimeoutPolicy("small-sample");

        policy.RecordExecutionTime(100);

        var p95 = policy.GetPercentile95ExecutionTime();
        var p99 = policy.GetPercentile99ExecutionTime();

        p95.Should().Be(100);
        p99.Should().Be(100);
    }

    [Fact]
    public void IsValidConfiguration_WithZeroTimeout_ReturnsFalse()
    {
        var policy = new TimeoutPolicy("zero-timeout")
        {
            Timeout = TimeSpan.Zero
        };

        var isValid = policy.IsValidConfiguration(out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("Timeout");
    }

    [Fact]
    public void IsValidConfiguration_WithNegativeTimeout_ReturnsFalse()
    {
        var policy = new TimeoutPolicy("negative-timeout")
        {
            Timeout = TimeSpan.FromSeconds(-1)
        };

        var isValid = policy.IsValidConfiguration(out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("Timeout");
    }

    [Fact]
    public void IsValidConfiguration_WithValidTimeout_ReturnsTrue()
    {
        var policy = new TimeoutPolicy("valid-timeout")
        {
            Timeout = TimeSpan.FromSeconds(10)
        };

        var isValid = policy.IsValidConfiguration(out var error);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ResetStatistics_ClearsAllMetrics()
    {
        var policy = new TimeoutPolicy("reset-stats");

        policy.RecordExecutionTime(100);
        policy.RecordExecutionTime(200);
        policy.RecordTimeout(5000);

        policy.ResetStatistics();

        policy.TimeoutCount.Should().Be(0);
        policy.AverageExecutionTimeMs.Should().Be(0);
        policy.LongestExecutionTimeMs.Should().Be(0);
        policy.ShortestExecutionTimeMs.Should().Be(long.MaxValue);
    }

    [Fact]
    public void GetSnapshot_IncludesAllMetrics()
    {
        var policy = new TimeoutPolicy("snapshot-test");

        policy.RecordExecutionTime(100);
        policy.RecordExecutionTime(200);
        policy.RecordTimeout(5000);

        var snapshot = policy.GetSnapshot();

        snapshot.Metadata.Should().ContainKeys(
            "TimeoutMs", "TimeoutCount", "TimeoutPercentage",
            "AverageExecutionTimeMs", "P95ExecutionTimeMs", "P99ExecutionTimeMs",
            "LongestExecutionTimeMs");
    }
}
