#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Contains unit tests for the <see cref="BulkheadPolicy"/> class, which implements bulkhead isolation pattern
/// to limit concurrent execution and queue requests when all slots are occupied.
/// </summary>
public sealed class BulkheadPolicyTests
{
    /// <summary>
    /// Tests that the constructor successfully creates a bulkhead policy with valid configuration.
    /// </summary>
    [Fact]
    public void Constructor_WithValidName_Succeeds()
    {
        var policy = new BulkheadPolicy("test-bulkhead");

        policy.Name.Should().Be("test-bulkhead");
        policy.MaxParallelization.Should().Be(10);
        policy.MaxQueueLength.Should().Be(50);
    }

    /// <summary>
    /// Tests that the constructor throws an ArgumentException when provided with whitespace-only name.
    /// </summary>
    [Fact]
    public void Constructor_WithWhitespaceName_ThrowsArgumentException()
    {
        Action act = () => new BulkheadPolicy("   ");

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Policy name cannot be empty*");
    }

    /// <summary>
    /// Tests that TryAcquireSlot returns true when the number of active executions is below MaxParallelization.
    /// </summary>
    [Fact]
    public void TryAcquireSlot_WhenBelowMaxParallelization_ReturnsTrue()
    {
        var policy = new BulkheadPolicy("acquire-test") { MaxParallelization = 5 };

        var result = policy.TryAcquireSlot();

        result.Should().BeTrue();
        policy.ActiveExecutions.Should().Be(1);
    }

    /// <summary>
    /// Tests that TryAcquireSlot returns false and queues the request when MaxParallelization is reached.
    /// </summary>
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

    /// <summary>
    /// Tests that TryAcquireSlot returns false and increments RejectedCount when both MaxParallelization and MaxQueueLength are reached.
    /// </summary>
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

    /// <summary>
    /// Tests that ReleaseSlot decreases the ActiveExecutions counter.
    /// </summary>
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

    /// <summary>
    /// Tests that ReleaseSlot does not allow ActiveExecutions to go below zero.
    /// </summary>
    [Fact]
    public void ReleaseSlot_WhenNoActiveExecutions_DoesNotGoNegative()
    {
        var policy = new BulkheadPolicy("negative-test");

        policy.ReleaseSlot();

        policy.ActiveExecutions.Should().Be(0);
    }

    /// <summary>
    /// Tests that DequeueRequest decreases the QueuedRequests counter.
    /// </summary>
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

    /// <summary>
    /// Tests that RecordQueueWaitTime throws an ArgumentException when provided with negative wait time.
    /// </summary>
    [Fact]
    public void RecordQueueWaitTime_WithNegativeTime_ThrowsArgumentException()
    {
        var policy = new BulkheadPolicy("negative-time");

        Action act = () => policy.RecordQueueWaitTime(-100);

        act.Should().Throw<ArgumentException>()
            .WithMessage("*Wait time cannot be negative*");
    }

    /// <summary>
    /// Tests that RecordQueueWaitTime correctly updates the average and longest queue time statistics.
    /// </summary>
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

    /// <summary>
    /// Tests that GetUtilizationPercentage correctly calculates the percentage of active executions relative to MaxParallelization.
    /// </summary>
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

    /// <summary>
    /// Tests that GetQueuedPercentage correctly calculates the percentage of queued requests relative to MaxQueueLength.
    /// </summary>
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

    /// <summary>
    /// Tests that GetRejectionPercentage correctly calculates the percentage of rejected requests relative to total executions.
    /// </summary>
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

    /// <summary>
    /// Tests that IsValidConfiguration returns false when MaxParallelization is set to zero.
    /// </summary>
    [Fact]
    public void IsValidConfiguration_WithZeroMaxParallelization_ReturnsFalse()
    {
        var policy = new BulkheadPolicy("invalid-test") { MaxParallelization = 0 };

        var isValid = policy.IsValidConfiguration(out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("MaxParallelization");
    }

    /// <summary>
    /// Tests that IsValidConfiguration returns false when MaxQueueLength is set to a negative value.
    /// </summary>
    [Fact]
    public void IsValidConfiguration_WithNegativeQueueLength_ReturnsFalse()
    {
        var policy = new BulkheadPolicy("negative-queue") { MaxQueueLength = -5 };

        var isValid = policy.IsValidConfiguration(out var error);

        isValid.Should().BeFalse();
        error.Should().Contain("MaxQueueLength");
    }

    /// <summary>
    /// Tests that IsValidConfiguration returns true when all configuration values are valid.
    /// </summary>
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

    /// <summary>
    /// Tests that ResetStatistics clears all metrics including active executions, rejected count, and queue times.
    /// </summary>
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

    /// <summary>
    /// Tests that concurrent calls to TryAcquireSlot are thread-safe and all succeed within the configured MaxParallelization limit.
    /// </summary>
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
