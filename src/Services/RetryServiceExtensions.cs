#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Extension methods for <see cref="IRetryService"/> that add non-generic execution and
/// a default-value fallback on top of the existing retry API.
/// </summary>
public static class RetryServiceExtensions
{
    /// <summary>
    /// Executes a non-generic operation through the retry policy.
    /// </summary>
    /// <param name="service">The retry service that enforces the policy.</param>
    /// <param name="policy">The retry policy to apply.</param>
    /// <param name="operation">The operation to execute. It is re-invoked on each retry attempt.</param>
    /// <param name="cancellationToken">Cancellation token for the retry loop.</param>
    /// <returns>A task that completes when the operation succeeds.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/>, <paramref name="policy"/>, or <paramref name="operation"/> is null.</exception>
    /// <exception cref="MaxRetriesExceededException">Thrown when all retry attempts have failed.</exception>
    public static async Task ExecuteWithRetryAsync(
        this IRetryService service,
        RetryPolicy policy,
        Func<Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        await service.ExecuteAsync<bool>(
            policy,
            async () =>
            {
                await operation();
                return true;
            },
            cancellationToken);
    }

    /// <summary>
    /// Executes a non-generic operation that accepts a cancellation token through the retry policy.
    /// </summary>
    /// <param name="service">The retry service that enforces the policy.</param>
    /// <param name="policy">The retry policy to apply.</param>
    /// <param name="operation">The operation to execute. It receives the cancellation token and is re-invoked on each retry attempt.</param>
    /// <param name="cancellationToken">Cancellation token for the retry loop, passed to <paramref name="operation"/>.</param>
    /// <returns>A task that completes when the operation succeeds.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/>, <paramref name="policy"/>, or <paramref name="operation"/> is null.</exception>
    /// <exception cref="MaxRetriesExceededException">Thrown when all retry attempts have failed.</exception>
    public static async Task ExecuteWithRetryAsync(
        this IRetryService service,
        RetryPolicy policy,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        await service.ExecuteAsync<bool>(
            policy,
            async ct =>
            {
                await operation(ct);
                return true;
            },
            cancellationToken);
    }

    /// <summary>
    /// Executes an operation through the retry policy and returns <c>default</c> if all attempts fail.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="service">The retry service that enforces the policy.</param>
    /// <param name="policy">The retry policy to apply.</param>
    /// <param name="operation">The operation to execute. It receives the cancellation token and is re-invoked on each retry attempt.</param>
    /// <param name="cancellationToken">Cancellation token for the retry loop, passed to <paramref name="operation"/>.</param>
    /// <returns>The operation result, or <c>default</c> if <see cref="MaxRetriesExceededException"/> was raised.</returns>
    /// <remarks>
    /// Only <see cref="MaxRetriesExceededException"/> is suppressed. Cancellation and other exceptions propagate unchanged.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/>, <paramref name="policy"/>, or <paramref name="operation"/> is null.</exception>
    public static async Task<T?> ExecuteOrDefaultAsync<T>(
        this IRetryService service,
        RetryPolicy policy,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        try
        {
            return await service.ExecuteAsync(policy, operation, cancellationToken);
        }
        catch (MaxRetriesExceededException)
        {
            return default;
        }
    }

    /// <summary>
    /// Executes an operation through the retry policy and returns <c>default</c> if all attempts fail.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="service">The retry service that enforces the policy.</param>
    /// <param name="policy">The retry policy to apply.</param>
    /// <param name="operation">The operation to execute. It is re-invoked on each retry attempt.</param>
    /// <param name="cancellationToken">Cancellation token for the retry loop.</param>
    /// <returns>The operation result, or <c>default</c> if <see cref="MaxRetriesExceededException"/> was raised.</returns>
    /// <remarks>
    /// Only <see cref="MaxRetriesExceededException"/> is suppressed. Cancellation and other exceptions propagate unchanged.
    /// </remarks>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/>, <paramref name="policy"/>, or <paramref name="operation"/> is null.</exception>
    public static async Task<T?> ExecuteOrDefaultAsync<T>(
        this IRetryService service,
        RetryPolicy policy,
        Func<Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        try
        {
            return await service.ExecuteAsync(policy, operation, cancellationToken);
        }
        catch (MaxRetriesExceededException)
        {
            return default;
        }
    }
}
