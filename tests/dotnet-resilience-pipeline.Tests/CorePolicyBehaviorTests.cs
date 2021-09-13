#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class ResiliencyPolicyBaseTests
{
    [Fact]
    public void Constructor_WithNullName_ThrowsArgumentException()
    {
        Action act = () => new CircuitBreakerPolicy(null!);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Policy name cannot be empty*");
    }

    [Fact]
    public void Constructor_WithEmptyName_ThrowsArgumentException()
    {
        Action act = () => new RetryPolicy("");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Policy name cannot be empty*");
    }

    [Fact]
    public void RecordSuccess_IncrementsBothTotals()
    {
        var policy = new CircuitBreakerPolicy("base-success") { FailureThreshold = 99 };

        policy.RecordSuccess();
        policy.RecordSuccess();

        policy.TotalExecutions.Should().Be(2);
        policy.SuccessfulExecutions.Should().Be(2);
        policy.FailedExecutions.Should().Be(0);
    }

    [Fact]
    public void RecordFailure_IncrementsFailureAndTotals()
    {
        var policy = new CircuitBreakerPolicy("base-failure") { FailureThreshold = 99 };

        policy.RecordFailure();

        policy.TotalExecutions.Should().Be(1);
        policy.FailedExecutions.Should().Be(1);
        policy.SuccessfulExecutions.Should().Be(0);
    }

    [Fact]
    public void GetSuccessRate_NoExecutions_ReturnsZero()
    {
        var policy = new RetryPolicy("rate-zero");

        policy.GetSuccessRate().Should().Be(0);
    }

    [Fact]
    public void GetSuccessRate_MixedExecutions_CalculatesCorrectly()
    {
        var policy = new CircuitBreakerPolicy("rate-calc") { FailureThreshold = 99 };

        for (int i = 0; i < 3; i++)
            policy.RecordSuccess();
        policy.RecordFailure();

        policy.GetSuccessRate().Should().BeApproximately(75, 0.001);
    }

    [Fact]
    public void ResetStatistics_ClearsAllCounters()
    {
        var policy = new CircuitBreakerPolicy("reset-test") { FailureThreshold = 99 };
        policy.RecordSuccess();
        policy.RecordFailure();

        policy.ResetStatistics();

        policy.TotalExecutions.Should().Be(0);
        policy.SuccessfulExecutions.Should().Be(0);
        policy.FailedExecutions.Should().Be(0);
    }

    [Fact]
    public void IsEnabled_DefaultsToTrue()
    {
        var policy = new RetryPolicy("enabled-default");

        policy.IsEnabled.Should().BeTrue();
    }

    [Fact]
    public void Tags_DefaultsToEmpty()
    {
        var policy = new BulkheadPolicy("tags-default");

        policy.Tags.Should().BeEmpty();
    }

    [Fact]
    public void Metadata_DefaultsToEmpty()
    {
        var policy = new TimeoutPolicy("meta-default") { Timeout = TimeSpan.FromSeconds(5) };

        policy.Metadata.Should().NotBeNull();
    }

    [Fact]
    public void GetSnapshot_PopulatesAllBaseFields()
    {
        var policy = new RetryPolicy("snapshot-policy") { IsEnabled = true };
        policy.RecordSuccess();
        policy.RecordSuccess();
        policy.RecordFailure();

        var snapshot = policy.GetSnapshot();

        snapshot.PolicyName.Should().Be("snapshot-policy");
        snapshot.PolicyType.Should().Be("RetryPolicy");
        snapshot.IsEnabled.Should().BeTrue();
        snapshot.TotalExecutions.Should().Be(3);
        snapshot.SuccessfulExecutions.Should().Be(2);
        snapshot.FailedExecutions.Should().Be(1);
        snapshot.SuccessRate.Should().BeApproximately(66.67, 0.1);
        snapshot.PolicyId.Should().NotBeNullOrEmpty();
        snapshot.SnapshotTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(5));
    }

    [Fact]
    public void Id_IsUniquePerInstance()
    {
        var p1 = new RetryPolicy("p1");
        var p2 = new RetryPolicy("p2");

        p1.Id.Should().NotBe(p2.Id);
    }

    [Fact]
    public void ModifiedAt_UpdatesAfterRecordSuccess()
    {
        var policy = new CircuitBreakerPolicy("modified-test") { FailureThreshold = 99 };
        var before = DateTime.UtcNow;

        policy.RecordSuccess();

        policy.ModifiedAt.Should().BeOnOrAfter(before);
    }
}

public sealed class PolicyValidationHelperExtendedTests
{
    [Fact]
    public void ValidatePolicy_CircuitBreakerWithHighFailureThreshold_AddsWarning()
    {
        var policy = new CircuitBreakerPolicy("high-threshold") { FailureThreshold = 1001 };

        var report = PolicyValidationHelper.ValidatePolicy(policy);

        report.IsValid.Should().BeTrue();
        report.Warnings.Should().ContainMatch("*FailureThreshold*");
    }

    [Fact]
    public void ValidatePolicy_CircuitBreakerWithVeryShortOpenDuration_AddsWarning()
    {
        var policy = new CircuitBreakerPolicy("short-open")
        {
            FailureThreshold = 5,
            OpenDuration = TimeSpan.FromMilliseconds(500)
        };

        var report = PolicyValidationHelper.ValidatePolicy(policy);

        report.Warnings.Should().ContainMatch("*OpenDuration*");
    }

    [Fact]
    public void ValidatePolicy_BulkheadWithLargeQueue_AddsWarning()
    {
        var policy = new BulkheadPolicy("large-queue")
        {
            MaxParallelization = 1,
            MaxQueueLength = 100
        };

        var report = PolicyValidationHelper.ValidatePolicy(policy);

        report.Warnings.Should().ContainMatch("*Queue length*");
    }

    [Fact]
    public void IdentifyAntiPatterns_DisabledPolicy_ReturnsAntiPattern()
    {
        var policy = new RetryPolicy("disabled") { IsEnabled = false };

        var antiPatterns = PolicyValidationHelper.IdentifyAntiPatterns(policy);

        antiPatterns.Should().ContainMatch("*disabled*");
    }

    [Fact]
    public void IdentifyAntiPatterns_ManyRetriesWithExponential_ReturnsAntiPattern()
    {
        var policy = new RetryPolicy("many-retries")
        {
            MaxRetries = 15,
            Strategy = RetryPolicy.BackoffStrategy.Exponential
        };

        var antiPatterns = PolicyValidationHelper.IdentifyAntiPatterns(policy);

        antiPatterns.Should().ContainMatch("*retries*exponential*");
    }

    [Fact]
    public void SuggestOptimizations_BulkheadWithZeroQueue_AddsQueueSuggestion()
    {
        var policy = new BulkheadPolicy("zero-queue") { MaxQueueLength = 0 };

        var suggestions = PolicyValidationHelper.SuggestOptimizations(policy);

        suggestions.Should().ContainMatch("*Queue length 0*");
    }

    [Fact]
    public void SuggestOptimizations_RetryBelowThreeAttempts_AddsRetryCountSuggestion()
    {
        var policy = new RetryPolicy("few-retries") { MaxRetries = 2 };

        var suggestions = PolicyValidationHelper.SuggestOptimizations(policy);

        suggestions.Should().ContainMatch("*3 retries*");
    }

    [Fact]
    public void SuggestOptimizations_CircuitBreakerWithSingleSuccessThreshold_AddsThresholdSuggestion()
    {
        var policy = new CircuitBreakerPolicy("single-success") { SuccessThresholdInHalfOpen = 1 };

        var suggestions = PolicyValidationHelper.SuggestOptimizations(policy);

        suggestions.Should().ContainMatch("*SuccessThresholdInHalfOpen*");
    }

    [Fact]
    public void ValidationReport_ToString_ContainsNameAndStatus()
    {
        var policy = new CircuitBreakerPolicy("report-cb") { FailureThreshold = 0 };

        var report = PolicyValidationHelper.ValidatePolicy(policy);
        var text = report.ToString();

        text.Should().Contain("report-cb");
        text.Should().Contain("Errors");
    }
}

public sealed class ThrottlingHelperTests
{
    [Fact]
    public void GetOrCreateThrottle_CreatesNewThrottle()
    {
        var helper = new ThrottlingHelper();

        var throttle = helper.GetOrCreateThrottle("svc", 10);

        throttle.Should().NotBeNull();
    }

    [Fact]
    public void GetOrCreateThrottle_SamePolicyName_ReturnsSameInstance()
    {
        var helper = new ThrottlingHelper();

        var t1 = helper.GetOrCreateThrottle("svc", 10);
        var t2 = helper.GetOrCreateThrottle("svc", 20);

        t1.Should().BeSameAs(t2);
    }

    [Fact]
    public void ShouldThrottle_UnregisteredPolicy_ReturnsFalse()
    {
        var helper = new ThrottlingHelper();

        helper.ShouldThrottle("unknown-policy").Should().BeFalse();
    }

    [Fact]
    public void ShouldThrottle_BelowRateLimit_ReturnsFalse()
    {
        var helper = new ThrottlingHelper();
        helper.GetOrCreateThrottle("fast-svc", maxRequestsPerSecond: 100);

        helper.ShouldThrottle("fast-svc").Should().BeFalse();
    }

    [Fact]
    public void ShouldThrottle_AfterBurstExhausted_ReturnsTrue()
    {
        var helper = new ThrottlingHelper();
        helper.GetOrCreateThrottle("slow-svc", maxRequestsPerSecond: 1, burstSize: 1);

        helper.ShouldThrottle("slow-svc");
        var throttled = helper.ShouldThrottle("slow-svc");

        throttled.Should().BeTrue();
    }

    [Fact]
    public void GetStatistics_ExistingThrottle_ReturnsStats()
    {
        var helper = new ThrottlingHelper();
        helper.GetOrCreateThrottle("stats-svc", 10, 10);
        helper.ShouldThrottle("stats-svc");
        helper.ShouldThrottle("stats-svc");

        var stats = helper.GetStatistics("stats-svc");

        stats.TotalRequests.Should().Be(2);
        stats.MaxRate.Should().Be(10);
    }

    [Fact]
    public void GetStatistics_UnknownPolicy_ReturnsEmptyStatistics()
    {
        var helper = new ThrottlingHelper();

        var stats = helper.GetStatistics("ghost-policy");

        stats.TotalRequests.Should().Be(0);
    }

    [Fact]
    public void GetAllStatistics_ReturnsStatsForAllThrottles()
    {
        var helper = new ThrottlingHelper();
        helper.GetOrCreateThrottle("svc-a", 10);
        helper.GetOrCreateThrottle("svc-b", 20);

        var all = helper.GetAllStatistics();

        all.Should().ContainKey("svc-a");
        all.Should().ContainKey("svc-b");
    }

    [Fact]
    public void ResetThrottle_RemovesThrottle()
    {
        var helper = new ThrottlingHelper();
        helper.GetOrCreateThrottle("reset-svc", 10);
        helper.ResetThrottle("reset-svc");

        helper.ShouldThrottle("reset-svc").Should().BeFalse();
    }

    [Fact]
    public void Clear_RemovesAllThrottles()
    {
        var helper = new ThrottlingHelper();
        helper.GetOrCreateThrottle("a", 10);
        helper.GetOrCreateThrottle("b", 20);
        helper.Clear();

        var all = helper.GetAllStatistics();

        all.Should().BeEmpty();
    }

    [Fact]
    public void IsThrottling_WhenThrottledRequestsExist_ReturnsTrue()
    {
        var helper = new ThrottlingHelper();
        helper.GetOrCreateThrottle("isthrottling-svc", 1, 1);
        helper.ShouldThrottle("isthrottling-svc");
        helper.ShouldThrottle("isthrottling-svc");

        var stats = helper.GetStatistics("isthrottling-svc");
        stats.IsThrottling.Should().BeTrue();
    }
}
