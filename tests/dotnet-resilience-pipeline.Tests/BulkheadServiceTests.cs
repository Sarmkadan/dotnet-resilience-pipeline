#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class BulkheadServiceTests
{
    [Fact]
    public void TryAcquireSlot_WithNullPolicy_ThrowsArgumentNullException()
    {
        var service = new BulkheadService();

        Action act = () => service.TryAcquireSlot(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policy");
    }

    [Fact]
    public void TryAcquireSlot_WithDisabledPolicy_ReturnsTrue()
    {
        var service = new BulkheadService();
        var policy = new BulkheadPolicy("disabled") { IsEnabled = false };

        var result = service.TryAcquireSlot(policy);

        result.Should().BeTrue();
    }

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

    [Fact]
    public void ReleaseSlot_WithNullPolicy_ThrowsArgumentNullException()
    {
        var service = new BulkheadService();

        Action act = () => service.ReleaseSlot(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policy");
    }

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

    [Fact]
    public void DequeueRequest_WithNullPolicy_ThrowsArgumentNullException()
    {
        var service = new BulkheadService();

        Action act = () => service.DequeueRequest(null!);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policy");
    }

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

    [Fact]
    public void RecordQueueWaitTime_WithNullPolicy_ThrowsArgumentNullException()
    {
        var service = new BulkheadService();

        Action act = () => service.RecordQueueWaitTime(null!, 100);

        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("policy");
    }

    [Fact]
    public void RecordQueueWaitTime_CallsPolicyRecordQueueWaitTime()
    {
        var service = new BulkheadService();
        var policy = new BulkheadPolicy("queue-time");

        service.RecordQueueWaitTime(policy, 100);
        service.RecordQueueWaitTime(policy, 200);

        policy.AverageQueueTimeMs.Should().Be(150);
    }

    [Fact]
    public void GetUtilizationPercentage_WithNullPolicy_ReturnsZero()
    {
        var service = new BulkheadService();

        var utilization = service.GetUtilizationPercentage(null!);

        utilization.Should().Be(0);
    }

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

    [Fact]
    public void GetActiveExecutionCount_WithNullPolicy_ReturnsZero()
    {
        var service = new BulkheadService();

        var count = service.GetActiveExecutionCount(null!);

        count.Should().Be(0);
    }

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

    [Fact]
    public void GetQueuedRequestCount_WithNullPolicy_ReturnsZero()
    {
        var service = new BulkheadService();

        var count = service.GetQueuedRequestCount(null!);

        count.Should().Be(0);
    }

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

    [Fact]
    public void IsValidConfiguration_WithNullPolicy_ReturnsFalse()
    {
        var service = new BulkheadService();

        var isValid = service.IsValidConfiguration(null!, out var error);

        isValid.Should().BeFalse();
    }

    [Fact]
    public void IsValidConfiguration_DelegatesToPolicy()
    {
        var service = new BulkheadService();
        var invalidPolicy = new BulkheadPolicy("invalid") { MaxParallelization = 0 };

        var isValid = service.IsValidConfiguration(invalidPolicy, out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("MaxParallelization");
    }

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
