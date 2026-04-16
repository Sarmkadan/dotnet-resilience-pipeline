#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class FallbackPolicyTests
{
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

    [Fact]
    public void ShouldTriggerFallback_WithFallbackOnAnyException_ReturnsTrue()
    {
        var policy = new FallbackPolicy("trigger-any") { FallbackOnAnyException = true };

        var shouldTrigger = policy.ShouldTriggerFallback(new Exception("test"));

        shouldTrigger.Should().BeTrue();
    }

    [Fact]
    public void ShouldTriggerFallback_WithNullException_ReturnsFalse()
    {
        var policy = new FallbackPolicy("trigger-null") { FallbackOnAnyException = true };

        var shouldTrigger = policy.ShouldTriggerFallback(null!);

        shouldTrigger.Should().BeFalse();
    }

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

    [Fact]
    public void RecordSuccessfulFallback_WithNegativeTime_ThrowsArgumentException()
    {
        var policy = new FallbackPolicy("negative-time");

        Action act = () => policy.RecordSuccessfulFallback(-100);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Execution time cannot be negative*");
    }

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

    [Fact]
    public void RecordFailedFallback_WithNegativeTime_ThrowsArgumentException()
    {
        var policy = new FallbackPolicy("negative-fail");

        Action act = () => policy.RecordFailedFallback(new Exception("test"), -100);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Execution time cannot be negative*");
    }

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

    [Fact]
    public void GetFallbackSuccessRate_WithNoInvocations_ReturnsZero()
    {
        var policy = new FallbackPolicy("no-invocations");

        var successRate = policy.GetFallbackSuccessRate();

        successRate.Should().Be(0);
    }

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

    [Fact]
    public void AddFallbackTrigger_WithNullType_ThrowsArgumentNullException()
    {
        var policy = new FallbackPolicy("null-type");

        Action act = () => policy.AddFallbackTrigger(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("exceptionType");
    }

    [Fact]
    public void AddFallbackTrigger_WithNonExceptionType_ThrowsArgumentException()
    {
        var policy = new FallbackPolicy("non-exception");

        Action act = () => policy.AddFallbackTrigger(typeof(string));

        act.Should().Throw<ArgumentException>()
            .WithMessage("*is not an Exception type*");
    }

    [Fact]
    public void AddFallbackTrigger_WithValidException_Succeeds()
    {
        var policy = new FallbackPolicy("add-trigger") { FallbackOnAnyException = false };

        policy.AddFallbackTrigger(typeof(TimeoutException));

        policy.FallbackTriggerExceptions.Should().Contain(typeof(TimeoutException));
    }

    [Fact]
    public void AddFallbackTrigger_WithDuplicateType_DoesNotAddTwice()
    {
        var policy = new FallbackPolicy("duplicate-trigger") { FallbackOnAnyException = false };

        policy.AddFallbackTrigger(typeof(TimeoutException));
        policy.AddFallbackTrigger(typeof(TimeoutException));

        policy.FallbackTriggerExceptions.Count(t => t == typeof(TimeoutException)).Should().Be(1);
    }

    [Fact]
    public void RemoveFallbackTrigger_RemovesExceptionType()
    {
        var policy = new FallbackPolicy("remove-trigger") { FallbackOnAnyException = false };

        policy.AddFallbackTrigger(typeof(TimeoutException));
        policy.RemoveFallbackTrigger(typeof(TimeoutException));

        policy.FallbackTriggerExceptions.Should().NotContain(typeof(TimeoutException));
    }

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
