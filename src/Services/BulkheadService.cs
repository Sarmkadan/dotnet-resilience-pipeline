#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Service handling bulkhead policy execution for resource isolation.
/// </summary>
public sealed class BulkheadService
{
    /// <summary>
    /// Attempts to acquire a slot in the bulkhead.
    /// </summary>
    public bool TryAcquireSlot(BulkheadPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (!policy.IsEnabled)
            return true;

        return policy.TryAcquireSlot();
    }

    /// <summary>
    /// Releases a slot in the bulkhead.
    /// </summary>
    public void ReleaseSlot(BulkheadPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        policy.ReleaseSlot();
    }

    /// <summary>
    /// Dequeues a request from the bulkhead queue.
    /// </summary>
    public void DequeueRequest(BulkheadPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        policy.DequeueRequest();
    }

    /// <summary>
    /// Records queue wait time.
    /// </summary>
    public void RecordQueueWaitTime(BulkheadPolicy policy, long waitTimeMs)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        policy.RecordQueueWaitTime(waitTimeMs);
    }

    /// <summary>
    /// Gets current bulkhead utilization percentage.
    /// </summary>
    public double GetUtilizationPercentage(BulkheadPolicy policy)
    {
        return policy?.GetUtilizationPercentage() ?? 0;
    }

    /// <summary>
    /// Gets the number of currently active executions.
    /// </summary>
    public int GetActiveExecutionCount(BulkheadPolicy policy)
    {
        return policy?.ActiveExecutions ?? 0;
    }

    /// <summary>
    /// Gets the number of queued requests.
    /// </summary>
    public int GetQueuedRequestCount(BulkheadPolicy policy)
    {
        return policy?.QueuedRequests ?? 0;
    }

    /// <summary>
    /// Validates bulkhead configuration.
    /// </summary>
    public bool IsValidConfiguration(BulkheadPolicy policy, out string? error)
    {
        if (policy is null) { error = null; return false; }
        return policy.IsValidConfiguration(out error);
    }
}
