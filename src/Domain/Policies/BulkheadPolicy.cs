#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
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
[DebuggerDisplay("Bulkhead({MaxParallelization}/{MaxQueueLength}, executing={ActiveExecutions}, queued={QueuedRequests})")]
public sealed class BulkheadPolicy : ResiliencyPolicy
{
    // Constants
    public const int DefaultMaxParallelization = 10;
    public const int DefaultMaxQueueLength = 50;
    public static readonly TimeSpan DefaultMaxQueueWaitTimeout = TimeSpan.FromSeconds(30);
    private const double PercentageMultiplier = 100.0;

    // Semaphores are created lazily on first use so that the object initializer
    // (e.g. new BulkheadPolicy("x") { MaxParallelization = 5 }) takes effect before capacity is fixed.
    private SemaphoreSlim? _semaphore;
    private SemaphoreSlim? _queueSemaphore;
    private int _slotCapacity;
    private int _queueCapacity;
    private readonly object _lockObj = new object();

    /// <summary>
    /// Maximum number of concurrent executions allowed.
    /// </summary>
    public int MaxParallelization { get; set; } = DefaultMaxParallelization;

    /// <summary>
    /// Maximum number of requests to queue when bulkhead is full.
    /// </summary>
    public int MaxQueueLength { get; set; } = DefaultMaxQueueLength;

    /// <summary>
    /// Maximum time to wait in the queue before being rejected.
    /// </summary>
    public TimeSpan MaxQueueWaitTimeout { get; set; } = DefaultMaxQueueWaitTimeout;

    /// <summary>
    /// Current number of active executions.
    /// </summary>
    public int ActiveExecutions
    {
        get
        {
            lock (_lockObj)
            {
                return _semaphore is null ? 0 : _slotCapacity - _semaphore.CurrentCount;
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
                return _queueSemaphore is null ? 0 : _queueCapacity - _queueSemaphore.CurrentCount;
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

    /// <summary>
    /// Initializes a new bulkhead policy.
    /// </summary>
    /// <param name="name">Optional policy name. Defaults to <c>BulkheadPolicy</c>.</param>
    public BulkheadPolicy(string? name = null) : base(name ?? nameof(BulkheadPolicy))
    {
    }

    /// <summary>
    /// Returns the execution semaphore, creating it with the current <see cref="MaxParallelization"/> on first use.
    /// Must be called while holding <c>_lockObj</c>.
    /// </summary>
    private SemaphoreSlim GetSlotSemaphoreLocked()
    {
        if (_semaphore is null)
        {
            // SemaphoreSlim requires a maximum of at least 1; the real limit is tracked by _slotCapacity
            _slotCapacity = MaxParallelization;
            _semaphore = new SemaphoreSlim(_slotCapacity, Math.Max(1, _slotCapacity));
        }

        return _semaphore;
    }

    /// <summary>
    /// Returns the queue semaphore, creating it with the current <see cref="MaxQueueLength"/> on first use.
    /// Must be called while holding <c>_lockObj</c>.
    /// </summary>
    private SemaphoreSlim GetQueueSemaphoreLocked()
    {
        if (_queueSemaphore is null)
        {
            // A zero-length queue is valid, but SemaphoreSlim requires a maximum of at least 1
            _queueCapacity = MaxQueueLength;
            _queueSemaphore = new SemaphoreSlim(_queueCapacity, Math.Max(1, _queueCapacity));
        }

        return _queueSemaphore;
    }

    /// <summary>
    /// Attempts to take an execution slot without waiting and without touching the queue.
    /// </summary>
    private bool TryTakeSlot()
    {
        lock (_lockObj)
        {
            return GetSlotSemaphoreLocked().Wait(0);
        }
    }

    /// <summary>
    /// Attempts to acquire a slot for execution without waiting.
    /// Returns true if acquired immediately. Otherwise the request is queued (if there is room)
    /// and false is returned, or it is rejected (if the queue is also full) and <see cref="RejectedCount"/> is incremented.
    /// </summary>
    public bool TryAcquireSlot()
    {
        if (TryTakeSlot())
        {
            return true;
        }

        lock (_lockObj)
        {
            if (GetQueueSemaphoreLocked().Wait(0))
            {
                QueuedCount++;
            }
            else
            {
                RejectedCount++;
                RecordFailure();
            }
        }

        return false;
    }

    /// <summary>
    /// Attempts to acquire a slot for execution, waiting up to <paramref name="timeout"/>.
    /// Returns true if acquired within timeout, false if timeout expires.
    /// </summary>
    public async Task<bool> TryAcquireSlotAsync(TimeSpan timeout)
    {
        if (TryTakeSlot())
        {
            return true;
        }

        SemaphoreSlim slots;
        lock (_lockObj)
        {
            slots = GetSlotSemaphoreLocked();
        }

        return await slots.WaitAsync(timeout).ConfigureAwait(false);
    }

    /// <summary>
    /// Acquires a slot for execution, waiting if necessary.
    /// Throws BulkheadRejectedException if queue is full or timeout expires.
    /// </summary>
    public async Task AcquireSlotAsync(CancellationToken cancellationToken = default)
    {
        // First try to get a slot immediately
        if (TryTakeSlot())
        {
            return;
        }

        SemaphoreSlim queue;
        SemaphoreSlim slots;
        lock (_lockObj)
        {
            queue = GetQueueSemaphoreLocked();
            slots = GetSlotSemaphoreLocked();
        }

        // If all slots are taken, take a queue position. A full queue rejects immediately (no wait).
        if (await queue.WaitAsync(0, cancellationToken).ConfigureAwait(false))
        {
            // Record queue entry time
            var queueEntryTime = DateTime.UtcNow;

            try
            {
                // Wait for a slot with timeout
                if (await slots.WaitAsync(MaxQueueWaitTimeout, cancellationToken).ConfigureAwait(false))
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
                queue.Release();
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
        lock (_lockObj)
        {
            if (ActiveExecutions == 0)
            {
                // Nothing is held, so there is nothing to release
                return;
            }

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
    }

    /// <summary>
    /// Dequeues a request from the queue.
    /// Called when a queued request is dequeued to make room for another.
    /// </summary>
    public void DequeueRequest()
    {
        lock (_lockObj)
        {
            if (QueuedRequests == 0)
            {
                // Nothing is queued, so there is nothing to dequeue
                return;
            }

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
        return (ActiveExecutions * PercentageMultiplier) / MaxParallelization;
    }

    /// <summary>
    /// Gets the percentage of requests that were queued.
    /// </summary>
    public double GetQueuedPercentage()
    {
        long totalRequests = TotalExecutions + RejectedCount;
        if (totalRequests == 0)
            return 0;

        return (QueuedCount * PercentageMultiplier) / totalRequests;
    }

    /// <summary>
    /// Gets the percentage of requests that were rejected.
    /// </summary>
    public double GetRejectionPercentage()
    {
        long totalRequests = TotalExecutions + RejectedCount;
        if (totalRequests == 0)
            return 0;

        return (RejectedCount * PercentageMultiplier) / totalRequests;
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
            // Drop the semaphores so that slot and queue occupancy restart from zero.
            // They are recreated lazily with the current configuration on next use.
            _semaphore = null;
            _queueSemaphore = null;
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
    /// Returns a concise summary of capacity and current load, e.g. <c>Bulkhead(10/20, executing=4, queued=2)</c>.
    /// </summary>
    public override string ToString() =>
        $"Bulkhead({MaxParallelization}/{MaxQueueLength}, executing={ActiveExecutions}, queued={QueuedRequests})";

    /// <summary>
    /// Disposes the internal SemaphoreSlim instances.
    /// </summary>
    ~BulkheadPolicy()
    {
        _semaphore?.Dispose();
        _queueSemaphore?.Dispose();
    }
}
