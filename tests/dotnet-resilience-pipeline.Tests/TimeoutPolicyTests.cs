#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using Xunit;

/// <summary>
/// Contains unit tests for the <see cref="TimeoutPolicy"/> class.
/// </summary>
public sealed class TimeoutPolicyTests
{
    /// <summary>
    /// Verifies that constructing a <see cref="TimeoutPolicy"/> with a valid name succeeds
    /// and sets the <c>Name</c> and default <c>Timeout</c> values.
    /// </summary>
    [Fact]
    public void Constructor_WithValidName_Succeeds()
    {
        var policy = new TimeoutPolicy("test-timeout");

        policy.Name.Should().Be("test-timeout");
        policy.Timeout.Should().Be(TimeSpan.FromSeconds(10));
    }

    /// <summary>
    /// Ensures that constructing a <see cref="TimeoutPolicy"/> with a whitespace-only name
    /// throws an <see cref="ArgumentException"/> containing the expected message.
    /// </summary>
    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        Action act = () => new TimeoutPolicy("   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Policy name cannot be empty*");
    }

    /// <summary>
    /// Confirms that <see cref="TimeoutPolicy.IsTimedOut(TimeSpan)"/> returns <c>false</c>
    /// when the execution time is less than the configured timeout.
    /// </summary>
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

    /// <summary>
    /// Confirms that <see cref="TimeoutPolicy.IsTimedOut(TimeSpan)"/> returns <c>true</c>
    /// when the execution time exceeds the configured timeout.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="TimeoutPolicy.IsTimedOut(TimeSpan)"/> returns <c>false</c>
    /// when the execution time is exactly equal to the timeout.
    /// </summary>
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

    /// <summary>
    /// Checks that <see cref="TimeoutPolicy.IsTimedOutMs(long)"/> returns <c>true</c>
    /// when the supplied milliseconds exceed the timeout.
    /// </summary>
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

    /// <summary>
    /// Checks that <see cref="TimeoutPolicy.IsTimedOutMs(long)"/> returns <c>false</c>
    /// when the supplied milliseconds are less than the timeout.
    /// </summary>
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

    /// <summary>
    /// Validates that <see cref="TimeoutPolicy.RecordExecutionTime(long)"/> throws an
    /// <see cref="ArgumentException"/> when a negative execution time is provided.
    /// </summary>
    [Fact]
    public void RecordExecutionTime_WithNegativeTime_ThrowsArgumentException()
    {
        var policy = new TimeoutPolicy("negative-time");

        Action act = () => policy.RecordExecutionTime(-100);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Execution time cannot be negative*");
    }

    /// <summary>
    /// Ensures that <see cref="TimeoutPolicy.RecordExecutionTime(long)"/> correctly updates
    /// average, longest, and shortest execution time statistics.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="TimeoutPolicy.RecordTimeout(long)"/> increments the timeout count
    /// and records a failure for each call.
    /// </summary>
    [Fact]
    public void RecordTimeout_IncreasesTimeoutCountAndRecordsFailure()
    {
        var policy = new TimeoutPolicy("record-timeout");

        policy.RecordTimeout(5000);
        policy.RecordTimeout(5500);

        policy.TimeoutCount.Should().Be(2);
        policy.FailedExecutions.Should().Be(2);
    }

    /// <summary>
    /// Checks that <see cref="TimeoutPolicy.RecordTimeout(long)"/> stores the timestamp of the
    /// most recent timeout in the policy metadata under the key <c>LastTimeoutAt</c>.
    /// </summary>
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

    /// <summary>
    /// Confirms that <see cref="TimeoutPolicy.GetTimeoutPercentage()"/> returns the correct
    /// percentage based on recorded successes and timeouts.
    /// </summary>
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

    /// <summary>
    /// Ensures that <see cref="TimeoutPolicy.GetTimeoutPercentage()"/> returns zero when no
    /// executions have been recorded.
    /// </summary>
    [Fact]
    public void GetTimeoutPercentage_WithNoExecutions_ReturnsZero()
    {
        var policy = new TimeoutPolicy("no-exec");

        var timeoutPct = policy.GetTimeoutPercentage();

        timeoutPct.Should().Be(0);
    }

    /// <summary>
    /// Validates that <see cref="TimeoutPolicy.GetPercentile95ExecutionTime()"/> returns a value
    /// within the expected 95th percentile range for a sample of 100 execution times.
    /// </summary>
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

    /// <summary>
    /// Validates that <see cref="TimeoutPolicy.GetPercentile99ExecutionTime()"/> returns a value
    /// within the expected 99th percentile range for a sample of 100 execution times.
    /// </summary>
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

    /// <summary>
    /// Checks that percentile calculations return a sensible value when the sample size is very small.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="TimeoutPolicy.IsValidConfiguration(out string?)"/> returns
    /// <c>false</c> and an appropriate error message when the timeout is zero.
    /// </summary>
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

    /// <summary>
    /// Verifies that <see cref="TimeoutPolicy.IsValidConfiguration(out string?)"/> returns
    /// <c>false</c> and an appropriate error message when the timeout is negative.
    /// </summary>
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

    /// <summary>
    /// Confirms that a positive, non‑zero timeout yields a valid configuration with no error message.
    /// </summary>
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

    /// <summary>
    /// Ensures that <see cref="TimeoutPolicy.ResetStatistics()"/> clears all metric counters and
    /// resets execution‑time statistics to their initial values.
    /// </summary>
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

    /// <summary>
    /// Checks that <see cref="TimeoutPolicy.GetSnapshot()"/> returns a snapshot whose metadata
    /// contains the expected keys for timeout and execution‑time metrics.
    /// </summary>
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
