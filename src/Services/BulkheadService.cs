#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

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
	/// Attempts to acquire a slot in the bulkhead without waiting.
	/// </summary>
	public bool TryAcquireSlot(BulkheadPolicy policy)
	{
		ArgumentNullException.ThrowIfNull(policy);

		if (!policy.IsEnabled)
			return true;

		return policy.TryAcquireSlot();
	}

	/// <summary>
	/// Attempts to acquire a slot in the bulkhead with a timeout.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="timeout"/> is negative and is not <see cref="Timeout.InfiniteTimeSpan"/>.
	/// </exception>
	public async Task<bool> TryAcquireSlotAsync(BulkheadPolicy policy, TimeSpan timeout)
	{
		ArgumentNullException.ThrowIfNull(policy);
		if (timeout < TimeSpan.Zero && timeout != Timeout.InfiniteTimeSpan)
			throw new ArgumentOutOfRangeException(nameof(timeout));

		if (!policy.IsEnabled)
			return true;

		return await policy.TryAcquireSlotAsync(timeout).ConfigureAwait(false);
	}

	/// <summary>
	/// Acquires a slot in the bulkhead, waiting if necessary.
	/// Throws BulkheadRejectedException if queue is full or timeout expires.
	/// </summary>
	public async Task AcquireSlotAsync(BulkheadPolicy policy, CancellationToken cancellationToken = default)
	{
		ArgumentNullException.ThrowIfNull(policy);

		if (!policy.IsEnabled)
			return;

		await policy.AcquireSlotAsync(cancellationToken).ConfigureAwait(false);
	}

	/// <summary>
	/// Releases a slot in the bulkhead.
	/// </summary>
	public void ReleaseSlot(BulkheadPolicy policy)
	{
		ArgumentNullException.ThrowIfNull(policy);

		policy.ReleaseSlot();
	}

	/// <summary>
	/// Dequeues a request from the bulkhead queue.
	/// </summary>
	public void DequeueRequest(BulkheadPolicy policy)
	{
		ArgumentNullException.ThrowIfNull(policy);

		policy.DequeueRequest();
	}

	/// <summary>
	/// Records queue wait time.
	/// </summary>
	/// <exception cref="ArgumentOutOfRangeException">
	/// <paramref name="waitTimeMs"/> is negative.
	/// </exception>
	public void RecordQueueWaitTime(BulkheadPolicy policy, long waitTimeMs)
	{
		ArgumentNullException.ThrowIfNull(policy);
		ArgumentOutOfRangeException.ThrowIfNegative(waitTimeMs);

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
