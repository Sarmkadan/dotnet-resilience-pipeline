#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Extension methods for <see cref="BulkheadPolicy"/> providing capacity helpers and fluent configuration.
/// </summary>
/// <remarks>
/// All extension methods validate their parameters using <see cref="ArgumentNullException.ThrowIfNull(object, string)"/>
/// and <see cref="ArgumentOutOfRangeException"/>. Setters follow the fluent interface pattern by returning
/// the policy instance for method chaining. The capacity limits are read by the policy when its internal
/// semaphores are first created, so changes made after the first execution do not resize them.
/// </remarks>
public static class BulkheadPolicyExtensions
{
    /// <summary>
    /// Gets the total number of requests the bulkhead can hold at one time: the executing slots plus the queue.
    /// </summary>
    /// <param name="policy">The bulkhead policy instance.</param>
    /// <returns>The sum of <see cref="BulkheadPolicy.MaxParallelization"/> and <see cref="BulkheadPolicy.MaxQueueLength"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    /// <remarks>
    /// The result is a <see cref="long"/> so that large configured limits cannot overflow the sum.
    /// </remarks>
    public static long GetTotalCapacity(this BulkheadPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return (long)policy.MaxParallelization + policy.MaxQueueLength;
    }

    /// <summary>
    /// Determines whether the bulkhead is configured with a waiting queue.
    /// </summary>
    /// <param name="policy">The bulkhead policy instance.</param>
    /// <returns>
    /// <see langword="true"/> if <see cref="BulkheadPolicy.MaxQueueLength"/> is greater than zero; otherwise, <see langword="false"/>.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    public static bool HasQueue(this BulkheadPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return policy.MaxQueueLength > 0;
    }

    /// <summary>
    /// Sets the maximum number of concurrent executions allowed by the bulkhead.
    /// </summary>
    /// <param name="policy">The bulkhead policy instance.</param>
    /// <param name="maxParallelization">The new concurrency limit. Must be greater than zero.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxParallelization"/> is not positive.</exception>
    public static BulkheadPolicy WithMaxParallelization(this BulkheadPolicy policy, int maxParallelization)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (maxParallelization <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxParallelization), maxParallelization, "MaxParallelization must be greater than zero.");
        }

        policy.MaxParallelization = maxParallelization;
        policy.ModifiedAt = DateTime.UtcNow;
        return policy;
    }

    /// <summary>
    /// Sets the maximum number of requests that may wait in the queue when the bulkhead is full.
    /// </summary>
    /// <param name="policy">The bulkhead policy instance.</param>
    /// <param name="maxQueueLength">The new queue length. Zero disables queuing. Must not be negative.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="maxQueueLength"/> is negative.</exception>
    public static BulkheadPolicy WithMaxQueueLength(this BulkheadPolicy policy, int maxQueueLength)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (maxQueueLength < 0)
        {
            throw new ArgumentOutOfRangeException(nameof(maxQueueLength), maxQueueLength, "MaxQueueLength cannot be negative.");
        }

        policy.MaxQueueLength = maxQueueLength;
        policy.ModifiedAt = DateTime.UtcNow;
        return policy;
    }
}
