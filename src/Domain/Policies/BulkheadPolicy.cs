#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Bulkhead pattern implementation that isolates resources to prevent resource exhaustion.
/// Limits concurrent executions to protect system resources.
/// </summary>
public sealed class BulkheadPolicy : ResiliencyPolicy
{
    /// <summary>
    /// Maximum number of concurrent executions allowed.
    /// </summary>
    public int MaxParallelization { get; set; } = 10;

    /// <summary>
    /// Maximum number of requests to queue when bulkhead is full.
    /// </summary>
    public int MaxQueueLength { get; set; } = 50;

    /// <summary>
    /// Current number of active executions.
    /// </summary>
    public int ActiveExecutions { get; private set; }

    /// <summary>
    /// Current number of queued requests.
    /// </summary>
    public int QueuedRequests { get; private set; }

    /// <summary>
    /// Total number of requests rejected due to bulkhead saturation.
    /// </summary>
    public long RejectedCount { get; private set; }

    /// <summary>
    /// Total requests that waited in queue.
    /// </summary>
    public long QueuedCount { get; private set; }

    /// <summary>
    /// Average queue wait time in milliseconds.
    /// </summary>
    public double AverageQueueTimeMs { get; private set; }

    /// <summary>
    /// Longest queue wait time recorded in milliseconds.
    /// </summary>
    public long LongestQueueTimeMs { get; private set; }

    private List<long> _queueWaitTimes = new();
    private readonly object _lockObj = new object();

    public BulkheadPolicy(string name) : base(name)
    {
    }

    /// <summary>
    /// Attempts to acquire a slot for execution.
    /// Returns true if acquired, false if bulkhead is full.
    /// </summary>
    public bool TryAcquireSlot()
    {
        lock (_lockObj)
        {
            if (ActiveExecutions < MaxParallelization)
            {
                ActiveExecutions++;
                return true;
            }

            if (QueuedRequests < MaxQueueLength)
            {
                QueuedRequests++;
                QueuedCount++;
                return false; // Queued, not immediately acquired
            }

            RejectedCount++;
            RecordFailure();
            return false; // Rejected
        }
    }

    /// <summary>
    /// Releases an execution slot.
    /// </summary>
    public void ReleaseSlot()
    {
        lock (_lockObj)
        {
            if (ActiveExecutions > 0)
            {
                ActiveExecutions--;
            }
        }
    }

    /// <summary>
    /// Dequeues a request from the queue.
    /// </summary>
    public void DequeueRequest()
    {
        lock (_lockObj)
        {
            if (QueuedRequests > 0)
            {
                QueuedRequests--;
            }
        }
    }

    /// <summary>
    /// Records the time a request spent in the queue.
    /// </summary>
    public void RecordQueueWaitTime(long waitTimeMs)
    {
        if (waitTimeMs < 0)
            throw new ArgumentException("Wait time cannot be negative", nameof(waitTimeMs));

        lock (_lockObj)
        {
            _queueWaitTimes.Add(waitTimeMs);
            UpdateQueueStatistics(waitTimeMs);
        }

        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the utilization percentage of the bulkhead.
    /// </summary>
    public double GetUtilizationPercentage()
    {
        return (ActiveExecutions * 100.0) / MaxParallelization;
    }

    /// <summary>
    /// Gets the percentage of requests that were queued.
    /// </summary>
    public double GetQueuedPercentage()
    {
        long totalRequests = TotalExecutions + RejectedCount;
        if (totalRequests == 0)
            return 0;

        return (QueuedCount * 100.0) / totalRequests;
    }

    /// <summary>
    /// Gets the percentage of requests that were rejected.
    /// </summary>
    public double GetRejectionPercentage()
    {
        long totalRequests = TotalExecutions + RejectedCount;
        if (totalRequests == 0)
            return 0;

        return (RejectedCount * 100.0) / totalRequests;
    }

    /// <summary>
    /// Validates bulkhead configuration.
    /// </summary>
    public bool IsValidConfiguration(out string? error)
    {
        if (MaxParallelization <= 0)
        {
            error = "MaxParallelization must be greater than 0";
            return false;
        }

        if (MaxQueueLength < 0)
        {
            error = "MaxQueueLength cannot be negative";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Resets all statistics.
    /// </summary>
    public override void ResetStatistics()
    {
        lock (_lockObj)
        {
            base.ResetStatistics();
            ActiveExecutions = 0;
            QueuedRequests = 0;
            RejectedCount = 0;
            QueuedCount = 0;
            _queueWaitTimes.Clear();
            AverageQueueTimeMs = 0;
            LongestQueueTimeMs = 0;
        }
    }

    private void UpdateQueueStatistics(long waitTimeMs)
    {
        if (waitTimeMs > LongestQueueTimeMs)
            LongestQueueTimeMs = waitTimeMs;

        AverageQueueTimeMs = _queueWaitTimes.Average();
    }

    /// <summary>
    /// Gets detailed bulkhead policy snapshot.
    /// </summary>
    public override PolicySnapshot GetSnapshot()
    {
        var baseSnapshot = base.GetSnapshot();
        baseSnapshot.Metadata = new Dictionary<string, object>
        {
            { "MaxParallelization", MaxParallelization },
            { "MaxQueueLength", MaxQueueLength },
            { "ActiveExecutions", ActiveExecutions },
            { "QueuedRequests", QueuedRequests },
            { "UtilizationPercentage", GetUtilizationPercentage() },
            { "RejectionPercentage", GetRejectionPercentage() },
            { "AverageQueueTimeMs", AverageQueueTimeMs },
            { "LongestQueueTimeMs", LongestQueueTimeMs }
        };
        return baseSnapshot;
    }
}
