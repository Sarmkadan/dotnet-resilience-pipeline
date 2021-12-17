#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Contains unit tests for the <see cref="BulkheadService"/> class.
/// Tests the bulkhead policy management functionality including slot acquisition, release, queue operations, and configuration validation.
/// </summary>
public sealed class BulkheadServiceTests
{
	/// <summary>
	/// Tests that <see cref="BulkheadService.TryAcquireSlot(BulkheadPolicy)"/> throws an <see cref="ArgumentNullException"/> when passed a null policy.
	/// </summary>
	[Fact]
	public void TryAcquireSlot_WithNullPolicy_ThrowsArgumentNullException()
	{
		var service = new BulkheadService();

		Action act = () => service.TryAcquireSlot(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("policy");
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.TryAcquireSlot(BulkheadPolicy)"/> returns true when the policy is disabled.
	/// </summary>
	[Fact]
	public void TryAcquireSlot_WithDisabledPolicy_ReturnsTrue()
	{
		var service = new BulkheadService();
		var policy = new BulkheadPolicy("disabled") { IsEnabled = false };

		var result = service.TryAcquireSlot(policy);

		result.Should().BeTrue();
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.TryAcquireSlot(BulkheadPolicy)"/> properly delegates to the policy when enabled.
	/// </summary>
	[Fact]
	public void TryAcquireSlot_WithEnabledPolicy_DelegatesToPolicy()
	{
		var service = new BulkheadService();
		var policy = new BulkheadPolicy("enabled") { IsEnabled = true, MaxParallelization = 2 };

		var result1 = service.TryAcquireSlot(policy);
		var result2 = service.TryAcquireSlot(policy);
		var result3 = service.TryAcquireSlot(policy);

		result1.Should().BeTrue();
		result2.Should().BeTrue();
		result3.Should().BeFalse();
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.ReleaseSlot(BulkheadPolicy)"/> throws an <see cref="ArgumentNullException"/> when passed a null policy.
	/// </summary>
	[Fact]
	public void ReleaseSlot_WithNullPolicy_ThrowsArgumentNullException()
	{
		var service = new BulkheadService();

		Action act = () => service.ReleaseSlot(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("policy");
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.ReleaseSlot(BulkheadPolicy)"/> properly calls the policy's ReleaseSlot method.
	/// </summary>
	[Fact]
	public void ReleaseSlot_CallsPolicyReleaseSlot()
	{
		var service = new BulkheadService();
		var policy = new BulkheadPolicy("release");

		service.TryAcquireSlot(policy);
		service.TryAcquireSlot(policy);
		policy.ActiveExecutions.Should().Be(2);

		service.ReleaseSlot(policy);

		policy.ActiveExecutions.Should().Be(1);
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.DequeueRequest(BulkheadPolicy)"/> throws an <see cref="ArgumentNullException"/> when passed a null policy.
	/// </summary>
	[Fact]
	public void DequeueRequest_WithNullPolicy_ThrowsArgumentNullException()
	{
		var service = new BulkheadService();

		Action act = () => service.DequeueRequest(null!);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("policy");
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.DequeueRequest(BulkheadPolicy)"/> properly calls the policy's DequeueRequest method.
	/// </summary>
	[Fact]
	public void DequeueRequest_CallsPolicyDequeueRequest()
	{
		var service = new BulkheadService();
		var policy = new BulkheadPolicy("dequeue") { MaxParallelization = 1, MaxQueueLength = 5 };

		service.TryAcquireSlot(policy);
		service.TryAcquireSlot(policy); // queued
		policy.QueuedRequests.Should().Be(1);

		service.DequeueRequest(policy);

		policy.QueuedRequests.Should().Be(0);
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.RecordQueueWaitTime(BulkheadPolicy, int)"/> throws an <see cref="ArgumentNullException"/> when passed a null policy.
	/// </summary>
	[Fact]
	public void RecordQueueWaitTime_WithNullPolicy_ThrowsArgumentNullException()
	{
		var service = new BulkheadService();

		Action act = () => service.RecordQueueWaitTime(null!, 100);

		act.Should().Throw<ArgumentNullException>()
			.WithParameterName("policy");
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.RecordQueueWaitTime(BulkheadPolicy, int)"/> properly calls the policy's RecordQueueWaitTime method.
	/// </summary>
	[Fact]
	public void RecordQueueWaitTime_CallsPolicyRecordQueueWaitTime()
	{
		var service = new BulkheadService();
		var policy = new BulkheadPolicy("queue-time");

		service.RecordQueueWaitTime(policy, 100);
		service.RecordQueueWaitTime(policy, 200);

		policy.AverageQueueTimeMs.Should().Be(150);
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.GetUtilizationPercentage(BulkheadPolicy)"/> returns 0 when passed a null policy.
	/// </summary>
	[Fact]
	public void GetUtilizationPercentage_WithNullPolicy_ReturnsZero()
	{
		var service = new BulkheadService();

		var utilization = service.GetUtilizationPercentage(null!);

		utilization.Should().Be(0);
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.GetUtilizationPercentage(BulkheadPolicy)"/> properly delegates to the policy.
	/// </summary>
	[Fact]
	public void GetUtilizationPercentage_DelegatesToPolicy()
	{
		var service = new BulkheadService();
		var policy = new BulkheadPolicy("util") { MaxParallelization = 10 };

		service.TryAcquireSlot(policy);
		service.TryAcquireSlot(policy);
		service.TryAcquireSlot(policy);

		var utilization = service.GetUtilizationPercentage(policy);

		utilization.Should().Be(30);
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.GetActiveExecutionCount(BulkheadPolicy)"/> returns 0 when passed a null policy.
	/// </summary>
	[Fact]
	public void GetActiveExecutionCount_WithNullPolicy_ReturnsZero()
	{
		var service = new BulkheadService();

		var count = service.GetActiveExecutionCount(null!);

		count.Should().Be(0);
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.GetActiveExecutionCount(BulkheadPolicy)"/> returns the active execution count.
	/// </summary>
	[Fact]
	public void GetActiveExecutionCount_ReturnsActiveExecutions()
	{
		var service = new BulkheadService();
		var policy = new BulkheadPolicy("active");

		service.TryAcquireSlot(policy);
		service.TryAcquireSlot(policy);

		var count = service.GetActiveExecutionCount(policy);

		count.Should().Be(2);
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.GetQueuedRequestCount(BulkheadPolicy)"/> returns 0 when passed a null policy.
	/// </summary>
	[Fact]
	public void GetQueuedRequestCount_WithNullPolicy_ReturnsZero()
	{
		var service = new BulkheadService();

		var count = service.GetQueuedRequestCount(null!);

		count.Should().Be(0);
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.GetQueuedRequestCount(BulkheadPolicy)"/> returns the queued request count.
	/// </summary>
	[Fact]
	public void GetQueuedRequestCount_ReturnsQueuedRequests()
	{
		var service = new BulkheadService();
		var policy = new BulkheadPolicy("queued") { MaxParallelization = 1, MaxQueueLength = 5 };

		service.TryAcquireSlot(policy);
		service.TryAcquireSlot(policy);
		service.TryAcquireSlot(policy);

		var count = service.GetQueuedRequestCount(policy);

		count.Should().Be(2);
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.IsValidConfiguration(BulkheadPolicy, out string)"/> returns false when passed a null policy.
	/// </summary>
	[Fact]
	public void IsValidConfiguration_WithNullPolicy_ReturnsFalse()
	{
		var service = new BulkheadService();

		var isValid = service.IsValidConfiguration(null!, out var error);

		isValid.Should().BeFalse();
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.IsValidConfiguration(BulkheadPolicy, out string)"/> properly delegates to the policy and returns false for invalid configuration.
	/// </summary>
	[Fact]
	public void IsValidConfiguration_DelegatesToPolicy()
	{
		var service = new BulkheadService();
		var invalidPolicy = new BulkheadPolicy("invalid") { MaxParallelization = 0 };

		var isValid = service.IsValidConfiguration(invalidPolicy, out var error);

		isValid.Should().BeFalse();
		error.Should().Contain("MaxParallelization");
	}

	/// <summary>
	/// Tests that <see cref="BulkheadService.IsValidConfiguration(BulkheadPolicy, out string)"/> returns true for valid configuration.
	/// </summary>
	[Fact]
	public void IsValidConfiguration_WithValidPolicy_ReturnsTrue()
	{
		var service = new BulkheadService();
		var validPolicy = new BulkheadPolicy("valid") { MaxParallelization = 10, MaxQueueLength = 50 };

		var isValid = service.IsValidConfiguration(validPolicy, out var error);

		isValid.Should().BeTrue();
		error.Should().BeNull();
	}
}
