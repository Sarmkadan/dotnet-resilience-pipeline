#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Tests for the base functionality of resiliency policies.
/// </summary>
public sealed class ResiliencyPolicyBaseTests
{
	/// <summary>
	/// Tests that the constructor throws an ArgumentException when provided with a null policy name.
	/// </summary>
	[Fact]
	public void Constructor_WithNullName_ThrowsArgumentException()
	{
		Action act = () => new CircuitBreakerPolicy(null!);

		act.Should().Throw<ArgumentException>()
			.WithMessage("*Policy name cannot be empty*");
	}

	/// <summary>
	/// Tests that the constructor throws an ArgumentException when provided with an empty policy name.
	/// </summary>
	[Fact]
	public void Constructor_WithEmptyName_ThrowsArgumentException()
	{
		Action act = () => new RetryPolicy("");

		act.Should().Throw<ArgumentException>()
			.WithMessage("*Policy name cannot be empty*");
	}

	/// <summary>
	/// Tests that RecordSuccess increments both total and successful execution counters.
	/// </summary>
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

	/// <summary>
	/// Tests that RecordFailure increments both total and failed execution counters.
	/// </summary>
	[Fact]
	public void RecordFailure_IncrementsFailureAndTotals()
	{
		var policy = new CircuitBreakerPolicy("base-failure") { FailureThreshold = 99 };

		policy.RecordFailure();

		policy.TotalExecutions.Should().Be(1);
		policy.FailedExecutions.Should().Be(1);
		policy.SuccessfulExecutions.Should().Be(0);
	}

	/// <summary>
	/// Tests that GetSuccessRate returns 0 when no executions have occurred.
	/// </summary>
	[Fact]
	public void GetSuccessRate_NoExecutions_ReturnsZero()
	{
		var policy = new RetryPolicy("rate-zero");

		policy.GetSuccessRate().Should().Be(0);
	}

	/// <summary>
	/// Tests that GetSuccessRate calculates the correct success rate with mixed execution results.
	/// </summary>
	[Fact]
	public void GetSuccessRate_MixedExecutions_CalculatesCorrectly()
	{
		var policy = new CircuitBreakerPolicy("rate-calc") { FailureThreshold = 99 };

		for (int i = 0; i < 3; i++)
			policy.RecordSuccess();
		policy.RecordFailure();

		policy.GetSuccessRate().Should().BeApproximately(75, 0.001);
	}

	/// <summary>
	/// Tests that ResetStatistics clears all execution counters.
	/// </summary>
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

	/// <summary>
	/// Tests that IsEnabled defaults to true when a policy is created.
	/// </summary>
	[Fact]
	public void IsEnabled_DefaultsToTrue()
	{
		var policy = new RetryPolicy("enabled-default");

		policy.IsEnabled.Should().BeTrue();
	}

	/// <summary>
	/// Tests that Tags collection defaults to empty when a policy is created.
	/// </summary>
	[Fact]
	public void Tags_DefaultsToEmpty()
	{
		var policy = new BulkheadPolicy("tags-default");

		policy.Tags.Should().BeEmpty();
	}

	/// <summary>
	/// Tests that Metadata property is not null when a policy is created.
	/// </summary>
	[Fact]
	public void Metadata_DefaultsToEmpty()
	{
		var policy = new TimeoutPolicy("meta-default") { Timeout = TimeSpan.FromSeconds(5) };

		policy.Metadata.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that GetSnapshot populates all base fields correctly.
	/// </summary>
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

	/// <summary>
	/// Tests that each policy instance has a unique Id.
	/// </summary>
	[Fact]
	public void Id_IsUniquePerInstance()
	{
		var p1 = new RetryPolicy("p1");
		var p2 = new RetryPolicy("p2");

		p1.Id.Should().NotBe(p2.Id);
	}

	/// <summary>
	/// Tests that ModifiedAt timestamp is updated after RecordSuccess is called.
	/// </summary>
	[Fact]
	public void ModifiedAt_UpdatesAfterRecordSuccess()
	{
		var policy = new CircuitBreakerPolicy("modified-test") { FailureThreshold = 99 };
		var before = DateTime.UtcNow;

		policy.RecordSuccess();

		policy.ModifiedAt.Should().BeOnOrAfter(before);
	}
}

/// <summary>
/// Tests for policy validation helper methods that extend the base validation functionality.
/// </summary>
public sealed class PolicyValidationHelperExtendedTests
{
	/// <summary>
	/// Tests that ValidatePolicy adds a warning when a CircuitBreakerPolicy has a high failure threshold.
	/// </summary>
	[Fact]
	public void ValidatePolicy_CircuitBreakerWithHighFailureThreshold_AddsWarning()
	{
		var policy = new CircuitBreakerPolicy("high-threshold") { FailureThreshold = 1001 };

		var report = PolicyValidationHelper.ValidatePolicy(policy);

		report.IsValid.Should().BeTrue();
		report.Warnings.Should().ContainMatch("*FailureThreshold*");
	}

	/// <summary>
	/// Tests that ValidatePolicy adds a warning when a CircuitBreakerPolicy has a very short open duration.
	/// </summary>
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

	/// <summary>
	/// Tests that ValidatePolicy adds a warning when a BulkheadPolicy has a large queue length.
	/// </summary>
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

	/// <summary>
	/// Tests that IdentifyAntiPatterns returns an anti-pattern warning for a disabled policy.
	/// </summary>
	[Fact]
	public void IdentifyAntiPatterns_DisabledPolicy_ReturnsAntiPattern()
	{
		var policy = new RetryPolicy("disabled") { IsEnabled = false };

		var antiPatterns = PolicyValidationHelper.IdentifyAntiPatterns(policy);

		antiPatterns.Should().ContainMatch("*disabled*");
	}

	/// <summary>
	/// Tests that IdentifyAntiPatterns returns an anti-pattern warning for a RetryPolicy with many retries using exponential backoff.
	/// </summary>
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

	/// <summary>
	/// Tests that SuggestOptimizations adds a suggestion when a BulkheadPolicy has zero queue length.
	/// </summary>
	[Fact]
	public void SuggestOptimizations_BulkheadWithZeroQueue_AddsQueueSuggestion()
	{
		var policy = new BulkheadPolicy("zero-queue") { MaxQueueLength = 0 };

		var suggestions = PolicyValidationHelper.SuggestOptimizations(policy);

		suggestions.Should().ContainMatch("*Queue length 0*");
	}

	/// <summary>
	/// Tests that SuggestOptimizations adds a suggestion when a RetryPolicy has fewer than 3 attempts.
	/// </summary>
	[Fact]
	public void SuggestOptimizations_RetryBelowThreeAttempts_AddsRetryCountSuggestion()
	{
		var policy = new RetryPolicy("few-retries") { MaxRetries = 2 };

		var suggestions = PolicyValidationHelper.SuggestOptimizations(policy);

		suggestions.Should().ContainMatch("*3 retries*");
	}

	/// <summary>
	/// Tests that SuggestOptimizations adds a suggestion when a CircuitBreakerPolicy has a single success threshold.
	/// </summary>
	[Fact]
	public void SuggestOptimizations_CircuitBreakerWithSingleSuccessThreshold_AddsThresholdSuggestion()
	{
		var policy = new CircuitBreakerPolicy("single-success") { SuccessThresholdInHalfOpen = 1 };

		var suggestions = PolicyValidationHelper.SuggestOptimizations(policy);

		suggestions.Should().ContainMatch("*SuccessThresholdInHalfOpen*");
	}

	/// <summary>
	/// Tests that ValidationReport.ToString contains the policy name and status information.
	/// </summary>
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

/// <summary>
/// Tests for throttling helper functionality that manages rate limiting across policies.
/// </summary>
public sealed class ThrottlingHelperTests
{
	/// <summary>
	/// Tests that GetOrCreateThrottle creates a new throttle instance for a given policy.
	/// </summary>
	[Fact]
	public void GetOrCreateThrottle_CreatesNewThrottle()
	{
		var helper = new ThrottlingHelper();

		var throttle = helper.GetOrCreateThrottle("svc", 10);

		throttle.Should().NotBeNull();
	}

	/// <summary>
	/// Tests that GetOrCreateThrottle returns the same instance when called with the same policy name.
	/// </summary>
	[Fact]
	public void GetOrCreateThrottle_SamePolicyName_ReturnsSameInstance()
	{
		var helper = new ThrottlingHelper();

		var t1 = helper.GetOrCreateThrottle("svc", 10);
		var t2 = helper.GetOrCreateThrottle("svc", 20);

		t1.Should().BeSameAs(t2);
	}

	/// <summary>
	/// Tests that ShouldThrottle returns false for an unregistered policy.
	/// </summary>
	[Fact]
	public void ShouldThrottle_UnregisteredPolicy_ReturnsFalse()
	{
		var helper = new ThrottlingHelper();

		helper.ShouldThrottle("unknown-policy").Should().BeFalse();
	}

	/// <summary>
	/// Tests that ShouldThrottle returns false when the request rate is below the configured limit.
	/// </summary>
	[Fact]
	public void ShouldThrottle_BelowRateLimit_ReturnsFalse()
	{
		var helper = new ThrottlingHelper();
		helper.GetOrCreateThrottle("fast-svc", maxRequestsPerSecond: 100);

		helper.ShouldThrottle("fast-svc").Should().BeFalse();
	}

	/// <summary>
	/// Tests that ShouldThrottle returns true after the burst capacity is exhausted.
	/// </summary>
	[Fact]
	public void ShouldThrottle_AfterBurstExhausted_ReturnsTrue()
	{
		var helper = new ThrottlingHelper();
		helper.GetOrCreateThrottle("slow-svc", maxRequestsPerSecond: 1, burstSize: 1);

		helper.ShouldThrottle("slow-svc");
		var throttled = helper.ShouldThrottle("slow-svc");

		throttled.Should().BeTrue();
	}

	/// <summary>
	/// Tests that GetStatistics returns statistics for an existing throttle.
	/// </summary>
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

	/// <summary>
	/// Tests that GetStatistics returns empty statistics for an unknown policy.
	/// </summary>
	[Fact]
	public void GetStatistics_UnknownPolicy_ReturnsEmptyStatistics()
	{
		var helper = new ThrottlingHelper();

		var stats = helper.GetStatistics("ghost-policy");

		stats.TotalRequests.Should().Be(0);
	}

	/// <summary>
	/// Tests that GetAllStatistics returns statistics for all registered throttles.
	/// </summary>
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

	/// <summary>
	/// Tests that ResetThrottle removes a throttle instance.
	/// </summary>
	[Fact]
	public void ResetThrottle_RemovesThrottle()
	{
		var helper = new ThrottlingHelper();
		helper.GetOrCreateThrottle("reset-svc", 10);
		helper.ResetThrottle("reset-svc");

		helper.ShouldThrottle("reset-svc").Should().BeFalse();
	}

	/// <summary>
	/// Tests that Clear removes all throttle instances.
	/// </summary>
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

	/// <summary>
	/// Tests that IsThrottling returns true when throttled requests exist.
	/// </summary>
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