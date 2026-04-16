#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class BulkheadPolicyTests
{
    [Fact]
    public void Constructor_WithValidName_Succeeds()
    {
        var policy = new BulkheadPolicy("test-bulkhead");

        policy.Name.Should().Be("test-bulkhead");
        policy.MaxParallelization.Should().Be(10);
        policy.MaxQueueLength.Should().Be(50);
    }

    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        Action act = () => new BulkheadPolicy("   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Policy name cannot be empty*");
    }

    [Fact]
    public void TryAcquireSlot_WhenBelowMaxParallelization_ReturnsTrue()
    {
        var policy = new BulkheadPolicy("acquire-test") { MaxParallelization = 5 };

        var result = policy.TryAcquireSlot();

        result.Should().BeTrue();
        policy.ActiveExecutions.Should().Be(1);
    }

    [Fact]
    public void TryAcquireSlot_WhenAtMaxParallelization_ReturnsfalseAndQueues()
    {
        var policy = new BulkheadPolicy("queue-test")
        {
            MaxParallelization = 2,
            MaxQueueLength = 5
        };

        policy.TryAcquireSlot();
        policy.TryAcquireSlot();

        var resultThird = policy.TryAcquireSlot();

        resultThird.Should().BeFalse();
        policy.ActiveExecutions.Should().Be(2);
        policy.QueuedRequests.Should().Be(1);
    }

    [Fact]
    public void TryAcquireSlot_WhenQueueFull_ReturnsFalseAndIncrementsRejectedCount()
    {
        var policy = new BulkheadPolicy("reject-test")
        {
            MaxParallelization = 1,
            MaxQueueLength = 1
        };

        policy.TryAcquireSlot(); // acquires
        policy.TryAcquireSlot(); // queues
        var resultRejected = policy.TryAcquireSlot(); // rejected

        resultRejected.Should().BeFalse();
        policy.RejectedCount.Should().Be(1);
    }

    [Fact]
    public void ReleaseSlot_DecreasesActiveExecutions()
    {
        var policy = new BulkheadPolicy("release-test");

        policy.TryAcquireSlot();
        policy.TryAcquireSlot();
        policy.ActiveExecutions.Should().Be(2);

        policy.ReleaseSlot();

        policy.ActiveExecutions.Should().Be(1);
    }

    [Fact]
    public void ReleaseSlot_WhenNoActiveExecutions_DoesNotGoNegative()
    {
        var policy = new BulkheadPolicy("negative-test");

        policy.ReleaseSlot();

        policy.ActiveExecutions.Should().Be(0);
    }

    [Fact]
    public void DequeueRequest_DecreasesQueuedRequests()
    {
        var policy = new BulkheadPolicy("dequeue-test")
        {
            MaxParallelization = 1,
            MaxQueueLength = 5
        };

        policy.TryAcquireSlot();
        policy.TryAcquireSlot(); // queued
        policy.QueuedRequests.Should().Be(1);

        policy.DequeueRequest();

        policy.QueuedRequests.Should().Be(0);
    }

    [Fact]
    public void RecordQueueWaitTime_WithNegativeTime_ThrowsArgumentException()
    {
        var policy = new BulkheadPolicy("negative-time");

        Action act = () => policy.RecordQueueWaitTime(-100);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Wait time cannot be negative*");
    }

    [Fact]
    public void RecordQueueWaitTime_UpdatesStatistics()
    {
        var policy = new BulkheadPolicy("stats-test");

        policy.RecordQueueWaitTime(100);
        policy.RecordQueueWaitTime(200);
        policy.RecordQueueWaitTime(300);

        policy.AverageQueueTimeMs.Should().Be(200);
        policy.LongestQueueTimeMs.Should().Be(300);
    }

    [Fact]
    public void GetUtilizationPercentage_CalculatesCorrectly()
    {
        var policy = new BulkheadPolicy("utilization-test") { MaxParallelization = 10 };

        policy.TryAcquireSlot();
        policy.TryAcquireSlot();
        policy.TryAcquireSlot();

        var utilization = policy.GetUtilizationPercentage();

        utilization.Should().Be(30);
    }

    [Fact]
    public void GetQueuedPercentage_CalculatesCorrectly()
    {
        var policy = new BulkheadPolicy("queued-pct-test")
        {
            MaxParallelization = 1,
            MaxQueueLength = 10
        };

        policy.TryAcquireSlot(); // 1 active
        policy.TryAcquireSlot(); // 1 queued
        policy.TryAcquireSlot(); // 1 more queued
        policy.RecordSuccess(); // increment TotalExecutions

        var queuedPct = policy.GetQueuedPercentage();

        queuedPct.Should().BeGreaterThan(0);
    }

    [Fact]
    public void GetRejectionPercentage_CalculatesCorrectly()
    {
        var policy = new BulkheadPolicy("rejection-pct-test")
        {
            MaxParallelization = 1,
            MaxQueueLength = 1
        };

        policy.TryAcquireSlot(); // acquires
        policy.TryAcquireSlot(); // queues
        policy.TryAcquireSlot(); // rejected
        policy.TryAcquireSlot(); // rejected
        policy.RecordSuccess(); // increment TotalExecutions

        var rejectionPct = policy.GetRejectionPercentage();

        rejectionPct.Should().BeGreaterThan(0);
    }

    [Fact]
    public void IsValidConfiguration_WithZeroMaxParallelization_ReturnsFalse()
    {
        var policy = new BulkheadPolicy("invalid-test") { MaxParallelization = 0 };

        var isValid = policy.IsValidConfiguration(out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("MaxParallelization");
    }

    [Fact]
    public void IsValidConfiguration_WithNegativeQueueLength_ReturnsFalse()
    {
        var policy = new BulkheadPolicy("negative-queue") { MaxQueueLength = -5 };

        var isValid = policy.IsValidConfiguration(out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("MaxQueueLength");
    }

    [Fact]
    public void IsValidConfiguration_WithValidSettings_ReturnsTrue()
    {
        var policy = new BulkheadPolicy("valid-test")
        {
            MaxParallelization = 10,
            MaxQueueLength = 50
        };

        var isValid = policy.IsValidConfiguration(out var error);

        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void ResetStatistics_ClearsAllMetrics()
    {
        var policy = new BulkheadPolicy("reset-test");

        policy.TryAcquireSlot();
        policy.TryAcquireSlot();
        policy.RecordQueueWaitTime(100);
        policy.RecordFailure();

        policy.ResetStatistics();

        policy.ActiveExecutions.Should().Be(0);
        policy.RejectedCount.Should().Be(0);
        policy.AverageQueueTimeMs.Should().Be(0);
        policy.LongestQueueTimeMs.Should().Be(0);
    }

    [Fact]
    public async Task ThreadSafety_ConcurrentAcquisitions_AllSucceed()
    {
        var policy = new BulkheadPolicy("concurrent-test") { MaxParallelization = 100 };
        var tasks = new Task[100];

        for (int i = 0; i < 100; i++)
        {
            tasks[i] = Task.Run(() => policy.TryAcquireSlot());
        }

        await Task.WhenAll(tasks);

        policy.ActiveExecutions.Should().Be(100);
    }
}
