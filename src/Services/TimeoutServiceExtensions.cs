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
/// Extension methods for <see cref="TimeoutService"/> that run common timeout scenarios
/// without the caller having to build a <see cref="TimeoutPolicy"/> by hand.
/// </summary>
/// <remarks>
/// Each call that takes a raw duration creates a short-lived <see cref="TimeoutPolicy"/>,
/// so statistics are tracked per call and are not shared between calls.
/// </remarks>
public static class TimeoutServiceExtensions
{
    /// <summary>
    /// Executes an operation with a timeout expressed in milliseconds.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="service">The timeout service that enforces the limit.</param>
    /// <param name="timeoutMilliseconds">The maximum execution time in milliseconds. Must be greater than zero.</param>
    /// <param name="operation">The operation to execute. Receives the timeout-linked cancellation token.</param>
    /// <param name="cancellationToken">Cancels the operation. Caller cancellation is not reported as a timeout.</param>
    /// <returns>The result of <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeoutMilliseconds"/> is not positive.</exception>
    /// <exception cref="OperationTimeoutException">Thrown if the operation exceeds the timeout.</exception>
    public static Task<T> ExecuteWithTimeoutAsync<T>(
        this TimeoutService service,
        int timeoutMilliseconds,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        if (timeoutMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), timeoutMilliseconds, "Timeout in milliseconds must be greater than zero.");

        var policy = new TimeoutPolicy(nameof(ExecuteWithTimeoutAsync))
            .WithTimeoutMilliseconds(timeoutMilliseconds);

        return service.ExecuteAsync(policy, operation, cancellationToken);
    }

    /// <summary>
    /// Executes an operation with a timeout expressed as a <see cref="TimeSpan"/>.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="service">The timeout service that enforces the limit.</param>
    /// <param name="timeout">The maximum execution time. Must be greater than <see cref="TimeSpan.Zero"/>.</param>
    /// <param name="operation">The operation to execute. Receives the timeout-linked cancellation token.</param>
    /// <param name="cancellationToken">Cancels the operation. Caller cancellation is not reported as a timeout.</param>
    /// <returns>The result of <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeout"/> is not positive.</exception>
    /// <exception cref="OperationTimeoutException">Thrown if the operation exceeds the timeout.</exception>
    public static Task<T> ExecuteWithTimeoutAsync<T>(
        this TimeoutService service,
        TimeSpan timeout,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        if (timeout <= TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be greater than zero.");

        var policy = new TimeoutPolicy(nameof(ExecuteWithTimeoutAsync))
        {
            Timeout = timeout
        };

        return service.ExecuteAsync(policy, operation, cancellationToken);
    }

    /// <summary>
    /// Executes an operation under the given timeout strategy and returns <paramref name="fallbackValue"/>
    /// if the timeout is exceeded.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="service">The timeout service that enforces the limit.</param>
    /// <param name="timeoutStrategy">The timeout strategy that defines the limit.</param>
    /// <param name="operation">The operation to execute. Receives the timeout-linked cancellation token.</param>
    /// <param name="fallbackValue">The value returned when the operation times out.</param>
    /// <param name="cancellationToken">
    /// Cancels the operation. Caller cancellation is not treated as a timeout and is propagated.
    /// </param>
    /// <returns>The result of <paramref name="operation"/>, or <paramref name="fallbackValue"/> on timeout.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/>, <paramref name="timeoutStrategy"/> or <paramref name="operation"/> is null.</exception>
    /// <exception cref="InvalidPolicyConfigurationException">Thrown if <paramref name="timeoutStrategy"/> has an invalid configuration.</exception>
    /// <remarks>
    /// Only <see cref="OperationTimeoutException"/> is suppressed. Any other exception thrown by
    /// <paramref name="operation"/> is propagated unchanged.
    /// </remarks>
    public static async Task<T> ExecuteOrDefaultAsync<T>(
        this TimeoutService service,
        ITimeoutStrategy timeoutStrategy,
        Func<CancellationToken, Task<T>> operation,
        T fallbackValue,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (timeoutStrategy is null)
            throw new ArgumentNullException(nameof(timeoutStrategy));

        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        try
        {
            return await service.ExecuteAsync(timeoutStrategy, operation, cancellationToken);
        }
        catch (OperationTimeoutException)
        {
            return fallbackValue;
        }
    }

    /// <summary>
    /// Executes an operation with a timeout in milliseconds and returns <paramref name="fallbackValue"/>
    /// if the timeout is exceeded.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="service">The timeout service that enforces the limit.</param>
    /// <param name="timeoutMilliseconds">The maximum execution time in milliseconds. Must be greater than zero.</param>
    /// <param name="operation">The operation to execute. Receives the timeout-linked cancellation token.</param>
    /// <param name="fallbackValue">The value returned when the operation times out.</param>
    /// <param name="cancellationToken">
    /// Cancels the operation. Caller cancellation is not treated as a timeout and is propagated.
    /// </param>
    /// <returns>The result of <paramref name="operation"/>, or <paramref name="fallbackValue"/> on timeout.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeoutMilliseconds"/> is not positive.</exception>
    public static async Task<T> ExecuteOrDefaultAsync<T>(
        this TimeoutService service,
        int timeoutMilliseconds,
        Func<CancellationToken, Task<T>> operation,
        T fallbackValue,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        if (timeoutMilliseconds <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutMilliseconds), timeoutMilliseconds, "Timeout in milliseconds must be greater than zero.");

        var policy = new TimeoutPolicy(nameof(ExecuteOrDefaultAsync))
            .WithTimeoutMilliseconds(timeoutMilliseconds);

        return await service.ExecuteOrDefaultAsync(policy, operation, fallbackValue, cancellationToken);
    }
}
