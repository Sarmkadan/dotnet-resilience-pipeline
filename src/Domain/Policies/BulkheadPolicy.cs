#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Bulkhead pattern implementation that isolates resources to prevent resource exhaustion.
/// Limits concurrent executions to protect system resources.
/// </summary>
/// <summary>
/// The <see cref="BulkheadPolicy"/> class implements the bulkhead pattern to isolate resources and prevent resource exhaustion.
/// It limits concurrent executions to protect system resources and manages request queuing when the bulkhead is full.
/// </summary>
/// <seealso cref="ResiliencyPolicy"/>
public sealed class BulkheadPolicy : ResiliencyPolicy
{
    private readonly SemaphoreSlim _semaphore;
    private readonly SemaphoreSlim _queueSemaphore;
    private readonly object _lockObj = new object();

    /// <summary>
    /// Maximum number of concurrent executions allowed.
    /// </summary>
    public int MaxParallelization { get; set; } = 10;

    /// <summary>
    /// Maximum number of requests to queue when bulkhead is full.
    /// </summary>
    public int MaxQueueLength { get; set; } = 50;

    /// <summary>
    /// Maximum time to wait in the queue before being rejected.
    /// </summary>
    public TimeSpan MaxQueueWaitTimeout { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Current number of active executions.
    /// </summary>
    public int ActiveExecutions
    {
        get
        {
            lock (_lockObj)
            {
                return MaxParallelization - _semaphore.CurrentCount;
            }
        }
    }

    /// <summary>
    /// Current number of queued requests.
    /// </summary>
    public int QueuedRequests
    {
        get
        {
            lock (_lockObj)
            {
                return MaxQueueLength - _queueSemaphore.CurrentCount;
            }
        }
    }

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

    public BulkheadPolicy(string name) : base(name)
    {
        _semaphore = new SemaphoreSlim(MaxParallelization, MaxParallelization);
        _queueSemaphore = new SemaphoreSlim(MaxQueueLength, MaxQueueLength);
    }

    /// <summary>
    /// Attempts to acquire a slot for execution without waiting.
    /// Returns true if acquired immediately, false if bulkhead is full.
    /// </summary>
    public bool TryAcquireSlot()
    {
        if (_semaphore.CurrentCount > 0)
        {
            lock (_lockObj)
            {
                if (_semaphore.Wait(0))
                {
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to acquire a slot for execution with a timeout.
    /// Returns true if acquired within timeout, false if timeout expires.
    /// </summary>
    public async Task<bool> TryAcquireSlotAsync(TimeSpan timeout)
    {
        if (_semaphore.CurrentCount > 0)
        {
            if (await _semaphore.WaitAsync(0).ConfigureAwait(false))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Acquires a slot for execution, waiting if necessary.
    /// Throws BulkheadRejectedException if queue is full or timeout expires.
    /// </summary>
    public async Task AcquireSlotAsync(CancellationToken cancellationToken = default)
    {
        // First try to get a slot immediately
        if (TryAcquireSlot())
        {
            return;
        }

        // If all slots are taken, try to queue
        if (await _queueSemaphore.WaitAsync(MaxQueueWaitTimeout, cancellationToken).ConfigureAwait(false))
        {
            // Record queue entry time
            var queueEntryTime = DateTime.UtcNow;

            try
            {
                // Wait for a slot with timeout
                if (await _semaphore.WaitAsync(MaxQueueWaitTimeout, cancellationToken).ConfigureAwait(false))
                {
                    // Successfully acquired a slot
                    lock (_lockObj)
                    {
                        QueuedCount++;
                        var waitTimeMs = (long)(DateTime.UtcNow - queueEntryTime).TotalMilliseconds;
                        RecordQueueWaitTime(waitTimeMs);
                    }
                    return;
                }

                // Timeout waiting for a slot
                lock (_lockObj)
                {
                    RejectedCount++;
                    RecordFailure();
                }
                throw new BulkheadRejectedException(
                    Name,
                    ActiveExecutions,
                    MaxParallelization,
                    QueuedRequests);
            }
            finally
            {
                // Release the queue slot regardless of success or failure
                _queueSemaphore.Release();
            }
        }

        // Queue is full or timeout expired
        lock (_lockObj)
        {
            RejectedCount++;
            RecordFailure();
        }

        throw new BulkheadRejectedException(
            Name,
            ActiveExecutions,
            MaxParallelization,
            QueuedRequests);
    }

    /// <summary>
    /// Releases a slot back to the bulkhead.
    /// Must be called in a finally block to ensure SemaphoreSlim is always released.
    /// </summary>
    public void ReleaseSlot()
    {
        ArgumentNullException.ThrowIfNull(_semaphore, nameof(_semaphore));

        try
        {
            _semaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Semaphore was already at max capacity, ignore
        }
        catch (ObjectDisposedException)
        {
            // Semaphore was disposed, ignore
        }
    }

    /// <summary>
    /// Dequeues a request from the queue.
    /// Called when a queued request is dequeued to make room for another.
    /// </summary>
    public void DequeueRequest()
    {
        ArgumentNullException.ThrowIfNull(_queueSemaphore, nameof(_queueSemaphore));

        try
        {
            _queueSemaphore.Release();
        }
        catch (SemaphoreFullException)
        {
            // Queue semaphore was already at max capacity, ignore
        }
        catch (ObjectDisposedException)
        {
            // Queue semaphore was disposed, ignore
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
            _queueWaitTimes.Clear();
            AverageQueueTimeMs = 0;
            LongestQueueTimeMs = 0;
            RejectedCount = 0;
            QueuedCount = 0;
        }
    }

    private void UpdateQueueStatistics(long waitTimeMs)
    {
        if (waitTimeMs > LongestQueueTimeMs)
            LongestQueueTimeMs = waitTimeMs;

        AverageQueueTimeMs = _queueWaitTimes.Any() ? _queueWaitTimes.Average() : 0;
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
            { "MaxQueueWaitTimeout", MaxQueueWaitTimeout.TotalMilliseconds },
            { "ActiveExecutions", ActiveExecutions },
            { "QueuedRequests", QueuedRequests },
            { "UtilizationPercentage", GetUtilizationPercentage() },
            { "RejectionPercentage", GetRejectionPercentage() },
            { "AverageQueueTimeMs", AverageQueueTimeMs },
            { "LongestQueueTimeMs", LongestQueueTimeMs }
        };
        return baseSnapshot;
    }

    /// <summary>
    /// Disposes the internal SemaphoreSlim instances.
    /// </summary>
    ~BulkheadPolicy()
    {
        _semaphore?.Dispose();
        _queueSemaphore?.Dispose();
    }
}