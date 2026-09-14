#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotNetResiliencePipeline.Workers;

/// <summary>
/// Extension methods for <see cref="MetricsCollectorWorker"/>.
/// </summary>
public static class MetricsCollectorWorkerExtensions
{
    /// <summary>
    /// Determines whether the metrics collector status is stale based on the maximum age.
    /// </summary>
    /// <param name="status">The metrics collector status.</param>
    /// <param name="maxAge">The maximum age before the status is considered stale.</param>
    /// <returns><see langword="true"/> if the status is stale; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="status"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="maxAge"/> is negative.</exception>
    public static bool IsStale(this MetricsCollectorStatus status, TimeSpan maxAge)
    {
        if (status == null)
            throw new ArgumentNullException(nameof(status));

        if (maxAge < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(maxAge), "Maximum age must be non-negative.");

        return (DateTime.UtcNow - status.LastCollectionTime) > maxAge;
    }

    /// <summary>
    /// Restarts the metrics collector worker asynchronously.
    /// </summary>
    /// <param name="worker">The metrics collector worker.</param>
    /// <returns>A task that represents the asynchronous restart operation.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="worker"/> is <see langword="null"/>.</exception>
    public static async Task RestartAsync(this MetricsCollectorWorker worker)
    {
        if (worker == null)
            throw new ArgumentNullException(nameof(worker));

        await worker.StopAsync();
        worker.Start();
    }

    /// <summary>
    /// Sets the collection interval for the metrics collector worker.
    /// </summary>
    /// <param name="worker">The metrics collector worker.</param>
    /// <param name="interval">The collection interval.</param>
    /// <returns>The same metrics collector worker instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="worker"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="interval"/> is zero or negative.</exception>
    public static MetricsCollectorWorker WithInterval(this MetricsCollectorWorker worker, TimeSpan interval)
    {
        if (worker == null)
            throw new ArgumentNullException(nameof(worker));

        if (interval <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(interval), "Collection interval must be positive.");

        worker.CollectionInterval = interval;
        return worker;
    }
}