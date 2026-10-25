#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Extension methods for <see cref="BulkheadService"/> that add saturation checks and
/// batch execution on top of the existing slot-based API.
/// </summary>
public static class BulkheadServiceExtensions
{
    /// <summary>
    /// Determines whether every execution slot of the bulkhead is currently in use.
    /// </summary>
    /// <param name="service">The bulkhead service that owns the policy.</param>
    /// <param name="policy">The bulkhead policy to inspect.</param>
    /// <returns>
    /// <c>true</c> if the number of active executions has reached <see cref="BulkheadPolicy.MaxParallelization"/>;
    /// otherwise <c>false</c>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="policy"/> is null.</exception>
    public static bool IsSaturated(this BulkheadService service, BulkheadPolicy policy)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        return service.GetActiveExecutionCount(policy) >= policy.MaxParallelization;
    }

    /// <summary>
    /// Gets the current bulkhead utilization as a percentage of its execution slots.
    /// </summary>
    /// <param name="service">The bulkhead service that owns the policy.</param>
    /// <param name="policy">The bulkhead policy to inspect.</param>
    /// <returns>The utilization percentage, where 100 means every slot is in use.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="policy"/> is null.</exception>
    public static double GetUtilizationPercent(this BulkheadService service, BulkheadPolicy policy)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        return service.GetUtilizationPercentage(policy);
    }

    /// <summary>
    /// Runs a set of operations through the bulkhead. Each operation acquires a slot before it starts
    /// and releases it when it completes, so the bulkhead limits how many of them run at the same time.
    /// </summary>
    /// <param name="service">The bulkhead service that enforces the limit.</param>
    /// <param name="policy">The bulkhead policy that governs concurrency and queueing.</param>
    /// <param name="operations">The operations to run. They are started together and gated by the bulkhead.</param>
    /// <param name="cancellationToken">Cancels slot acquisition for operations that have not started yet.</param>
    /// <returns>A task that completes when all operations have completed.</returns>
    /// <remarks>
    /// If the policy is disabled, the operations run without acquiring slots, matching <see cref="BulkheadService"/>.
    /// If an operation fails, the returned task faults with that exception after the remaining operations finish.
    /// If a slot cannot be acquired, <see cref="BulkheadRejectedException"/> is raised for that operation.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/>, <paramref name="policy"/>, <paramref name="operations"/>, or any operation is null.</exception>
    public static Task ExecuteAllAsync(
        this BulkheadService service,
        BulkheadPolicy policy,
        IEnumerable<Func<Task>> operations,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (operations is null)
            throw new ArgumentNullException(nameof(operations));

        var operationList = operations.ToList();
        if (operationList.Any(operation => operation is null))
            throw new ArgumentNullException(nameof(operations), "Operations must not contain null entries.");

        return Task.WhenAll(operationList.Select(operation => RunThroughBulkheadAsync(service, policy, operation, cancellationToken)));
    }

    private static async Task RunThroughBulkheadAsync(
        BulkheadService service,
        BulkheadPolicy policy,
        Func<Task> operation,
        CancellationToken cancellationToken)
    {
        // A disabled policy never hands out slots, so releasing one would free a slot held by someone else
        if (!policy.IsEnabled)
        {
            await operation().ConfigureAwait(false);
            return;
        }

        await service.AcquireSlotAsync(policy, cancellationToken).ConfigureAwait(false);

        try
        {
            await operation().ConfigureAwait(false);
        }
        finally
        {
            service.ReleaseSlot(policy);
        }
    }
}
