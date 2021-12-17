#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Contains unit tests for the <see cref="FallbackPolicy"/> class to verify fallback behavior and configuration.
/// </summary>
public sealed class FallbackPolicyTests
{
    /// <summary>
    /// Tests that the constructor successfully creates a fallback policy with a valid name.
    /// </summary>
    [Fact]
    public void Constructor_WithValidName_Succeeds()
    {
        var policy = new FallbackPolicy("test-fallback");

        policy.Name.Should().Be("test-fallback");
        policy.FallbackOnAnyException.Should().BeTrue();
        policy.FallbackTimeout.Should().Be(TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        Action act = () => new FallbackPolicy("   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Policy name cannot be empty*");
    }

    /// <summary>
    /// Tests that SetFallbackAction successfully stores a valid fallback function.
    /// </summary>
    [Fact]
    public void SetFallbackAction_WithValidFunc_StoresAction()
    {
        var policy = new FallbackPolicy("set-action");

        Func<CancellationToken, Task<string>> fallbackFunc = async (ct) =>
        {
            return await Task.FromResult("fallback-value");
        };

        policy.SetFallbackAction(fallbackFunc);

        policy.Should().NotBeNull();
    }

    /// <summary>
    /// Tests that ShouldTriggerFallback returns true when FallbackOnAnyException is enabled.
    /// </summary>
    [Fact]
    public void ShouldTriggerFallback_WithFallbackOnAnyException_ReturnsTrue()
    {
        var policy = new FallbackPolicy("trigger-any") { FallbackOnAnyException = true };

        var shouldTrigger = policy.ShouldTriggerFallback(new Exception("test"));

        shouldTrigger.Should().BeTrue();
    }

    /// <summary>
    /// Tests that ShouldTriggerFallback returns false when passed a null exception.
    /// </summary>
    [Fact]
    public void ShouldTriggerFallback_WithNullException_ReturnsFalse()
    {
        var policy = new FallbackPolicy("trigger-null") { FallbackOnAnyException = true };

        var shouldTrigger = policy.ShouldTriggerFallback(null!);

        shouldTrigger.Should().BeFalse();
    }

    /// <summary>
    /// Tests that ShouldTriggerFallback returns true when the exception matches a configured trigger type.
    /// </summary>
    [Fact]
    public void ShouldTriggerFallback_WithSpecificExceptionAndMatch_ReturnsTrue()
    {
        var policy = new FallbackPolicy("trigger-specific")
        {
            FallbackOnAnyException = false,
            FallbackTriggerExceptions = new List<Type> { typeof(InvalidOperationException) }
        };

        var shouldTrigger = policy.ShouldTriggerFallback(new InvalidOperationException("test"));

        shouldTrigger.Should().BeTrue();
    }

    /// <summary>
    /// Tests that ShouldTriggerFallback returns false when the exception doesn't match configured trigger types.
    /// </summary>
    [Fact]
    public void ShouldTriggerFallback_WithSpecificExceptionNoMatch_ReturnsFalse()
    {
        var policy = new FallbackPolicy("trigger-no-match")
        {
            FallbackOnAnyException = false,
            FallbackTriggerExceptions = new List<Type> { typeof(InvalidOperationException) }
        };

        var shouldTrigger = policy.ShouldTriggerFallback(new ArgumentException("test"));

        shouldTrigger.Should().BeFalse();
    }

    /// <summary>
    /// Tests that RecordSuccessfulFallback throws ArgumentException when provided with negative execution time.
    /// </summary>
    [Fact]
    public void RecordSuccessfulFallback_WithNegativeTime_ThrowsArgumentException()
    {
        var policy = new FallbackPolicy("negative-time");

        Action act = () => policy.RecordSuccessfulFallback(-100);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Execution time cannot be negative*");
    }

    /// <summary>
    /// Tests that RecordSuccessfulFallback correctly increments fallback invocation counters and calculates average execution time.
    /// </summary>
    [Fact]
    public void RecordSuccessfulFallback_IncrementCounters()
    {
        var policy = new FallbackPolicy("record-success");

        policy.RecordSuccessfulFallback(100);
        policy.RecordSuccessfulFallback(200);

        policy.FallbackInvocationCount.Should().Be(2);
        policy.SuccessfulFallbackCount.Should().Be(2);
        policy.AverageFallbackExecutionTimeMs.Should().Be(150);
    }

    /// <summary>
    /// Tests that RecordFailedFallback throws ArgumentException when provided with negative execution time.
    /// </summary>
    [Fact]
    public void RecordFailedFallback_WithNegativeTime_ThrowsArgumentException()
    {
        var policy = new FallbackPolicy("negative-fail");

        Action act = () => policy.RecordFailedFallback(new Exception("test"), -100);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Execution time cannot be negative*");
    }

    /// <summary>
    /// Tests that RecordFailedFallback correctly increments fallback failure counters.
    /// </summary>
    [Fact]
    public void RecordFailedFallback_IncrementCounters()
    {
        var policy = new FallbackPolicy("record-failure");
        var exception = new TimeoutException("test");

        policy.RecordFailedFallback(exception, 150);
        policy.RecordFailedFallback(exception, 250);

        policy.FallbackInvocationCount.Should().Be(2);
        policy.FailedFallbackCount.Should().Be(2);
        policy.FailedExecutions.Should().Be(2);
    }

    /// <summary>
    /// Tests that GetFallbackSuccessRate calculates the correct success rate with mixed successful and failed fallback invocations.
    /// </summary>
    [Fact]
    public void GetFallbackSuccessRate_WithMixedResults_CalculatesCorrectly()
    {
        var policy = new FallbackPolicy("success-rate");

        policy.RecordSuccessfulFallback(100);
        policy.RecordSuccessfulFallback(100);
        policy.RecordFailedFallback(new Exception("test"), 100);

        var successRate = policy.GetFallbackSuccessRate();

        successRate.Should().BeApproximately(66.66666666666666, 0.0001);
    }

    /// <summary>
    /// Tests that GetFallbackSuccessRate returns 0 when no fallback invocations have been recorded.
    /// </summary>
    [Fact]
    public void GetFallbackSuccessRate_WithNoInvocations_ReturnsZero()
    {
        var policy = new FallbackPolicy("no-invocations");

        var successRate = policy.GetFallbackSuccessRate();

        successRate.Should().Be(0);
    }

    /// <summary>
    /// Tests that GetFallbackInvocationPercentage calculates the correct percentage of fallback invocations.
    /// </summary>
    [Fact]
    public void GetFallbackInvocationPercentage_CalculatesCorrectly()
    {
        var policy = new FallbackPolicy("invocation-pct");

        for (int i = 0; i < 100; i++)
            policy.RecordSuccess();

        for (int i = 0; i < 25; i++)
            policy.RecordSuccessfulFallback(100);

        var invocationPct = policy.GetFallbackInvocationPercentage();

        invocationPct.Should().Be(20);
    }

    /// <summary>
    /// Tests that AddFallbackTrigger throws ArgumentNullException when provided with a null exception type.
    /// </summary>
    [Fact]
    public void AddFallbackTrigger_WithNullType_ThrowsArgumentNullException()
    {
        var policy = new FallbackPolicy("null-type");

        Action act = () => policy.AddFallbackTrigger(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("exceptionType");
    }

    /// <summary>
    /// Tests that AddFallbackTrigger throws ArgumentException when provided with a non-Exception type.
    /// </summary>
    [Fact]
    public void AddFallbackTrigger_WithNonExceptionType_ThrowsArgumentException()
    {
        var policy = new FallbackPolicy("non-exception");

        Action act = () => policy.AddFallbackTrigger(typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*is not an Exception type*");
    }

    /// <summary>
    /// Tests that AddFallbackTrigger successfully adds a valid exception type to the trigger list.
    /// </summary>
    [Fact]
    public void AddFallbackTrigger_WithValidException_Succeeds()
    {
        var policy = new FallbackPolicy("add-trigger") { FallbackOnAnyException = false };

        policy.AddFallbackTrigger(typeof(TimeoutException));

        policy.FallbackTriggerExceptions.Should().Contain(typeof(TimeoutException));
    }

    /// <summary>
    /// Tests that AddFallbackTrigger prevents adding duplicate exception types to the trigger list.
    /// </summary>
    [Fact]
    public void AddFallbackTrigger_WithDuplicateType_DoesNotAddTwice()
    {
        var policy = new FallbackPolicy("duplicate-trigger") { FallbackOnAnyException = false };

        policy.AddFallbackTrigger(typeof(TimeoutException));
        policy.AddFallbackTrigger(typeof(TimeoutException));

        policy.FallbackTriggerExceptions.Count(t => t == typeof(TimeoutException)).Should().Be(1);
    }

    /// <summary>
    /// Tests that RemoveFallbackTrigger successfully removes an exception type from the trigger list.
    /// </summary>
    [Fact]
    public void RemoveFallbackTrigger_RemovesExceptionType()
    {
        var policy = new FallbackPolicy("remove-trigger") { FallbackOnAnyException = false };

        policy.AddFallbackTrigger(typeof(TimeoutException));
        policy.RemoveFallbackTrigger(typeof(TimeoutException));

        policy.FallbackTriggerExceptions.Should().NotContain(typeof(TimeoutException));
    }

    /// <summary>
    /// Tests that IsValidConfiguration returns false when FallbackTimeout is set to TimeSpan.Zero.
    /// </summary>
    [Fact]
    public void IsValidConfiguration_WithZeroTimeout_ReturnsFalse()
    {
        var policy = new FallbackPolicy("zero-timeout")
        {
            FallbackTimeout = TimeSpan.Zero
        };

        var isValid = policy.IsValidConfiguration(out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("FallbackTimeout");
    }

    /// <summary>
    /// Tests that IsValidConfiguration returns false when FallbackOnAnyException is false and no trigger exceptions are configured.
    /// </summary>
    [Fact]
    public void IsValidConfiguration_WithSpecificExceptionsAndNone_ReturnsFalse()
    {
        var policy = new FallbackPolicy("no-triggers")
        {
            FallbackOnAnyException = false,
            FallbackTriggerExceptions = new List<Type>()
        };

        var isValid = policy.IsValidConfiguration(out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("fallback trigger exceptions");
    }

    /// <summary>
    /// Tests that IsValidConfiguration returns true when the policy has valid configuration settings.
    /// </summary>
    [Fact]
    public void IsValidConfiguration_WithValidSettings_ReturnsTrue()
    {
        var policy = new FallbackPolicy("valid-config")
        {
            FallbackOnAnyException = true,
            FallbackTimeout = TimeSpan.FromSeconds(5)
        };

        var isValid = policy.IsValidConfiguration(out var error);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    /// <summary>
    /// Tests that ResetStatistics clears all fallback-related metrics and counters.
    /// </summary>
    [Fact]
    public void ResetStatistics_ClearsAllMetrics()
    {
        var policy = new FallbackPolicy("reset-stats");

        policy.RecordSuccessfulFallback(100);
        policy.RecordFailedFallback(new Exception("test"), 200);
        policy.RecordSuccess();

        policy.ResetStatistics();

        policy.FallbackInvocationCount.Should().Be(0);
        policy.SuccessfulFallbackCount.Should().Be(0);
        policy.FailedFallbackCount.Should().Be(0);
        policy.AverageFallbackExecutionTimeMs.Should().Be(0);
    }
}
