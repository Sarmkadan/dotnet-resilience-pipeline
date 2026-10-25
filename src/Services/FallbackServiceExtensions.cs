#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Domain;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Extension methods for <see cref="FallbackService"/> that run common fallback scenarios
/// without the caller having to build a <see cref="FallbackPolicy"/> by hand.
/// </summary>
/// <remarks>
/// Each call creates a short-lived <see cref="FallbackPolicy"/> with default settings
/// (any exception triggers fallback, 5 second fallback timeout). Statistics are therefore
/// tracked per call and are not shared between calls.
/// </remarks>
public static class FallbackServiceExtensions
{
    /// <summary>
    /// Executes an operation and returns <paramref name="fallbackValue"/> if it throws.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="service">The fallback service that handles the failure.</param>
    /// <param name="operation">The primary operation. Receives the cancellation token.</param>
    /// <param name="fallbackValue">The value returned when the primary operation fails.</param>
    /// <param name="cancellationToken">
    /// Cancels the primary operation. If cancellation was requested by the caller, the failure
    /// is not replaced by the fallback value and is propagated instead.
    /// </param>
    /// <returns>The result of <paramref name="operation"/>, or <paramref name="fallbackValue"/> if it failed.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="operation"/> is null.</exception>
    /// <exception cref="FallbackFailedException">Thrown if the fallback itself cannot be executed.</exception>
    public static async Task<T> ExecuteWithValueAsync<T>(
        this FallbackService service,
        Func<CancellationToken, Task<T>> operation,
        T fallbackValue,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        var policy = new FallbackPolicy(nameof(ExecuteWithValueAsync));
        policy.SetFallbackAction<T>(_ => Task.FromResult(fallbackValue));

        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await operation(cancellationToken);
        }
        catch (Exception primaryException) when (!cancellationToken.IsCancellationRequested)
        {
            var result = await service.ExecuteAsync<T>(
                policy,
                primaryException,
                stopwatch.ElapsedMilliseconds,
                cancellationToken);

            if (!result.IsSuccess)
            {
                ExceptionDispatchInfo.Capture(primaryException).Throw();
            }

            return result.Data!;
        }
    }

    /// <summary>
    /// Runs candidate operations in order and returns the result of the first one that succeeds.
    /// </summary>
    /// <typeparam name="T">The result type of the candidates.</typeparam>
    /// <param name="service">The fallback service that handles each failure.</param>
    /// <param name="cancellationToken">Cancels the chain. Once cancelled, no further candidates are started.</param>
    /// <param name="candidates">
    /// Operations tried in the given order. The first candidate is the primary operation;
    /// each later candidate runs as a fallback for the failure before it. Must contain at least one entry.
    /// </param>
    /// <returns>The result of the first candidate that completes successfully.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="candidates"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="candidates"/> is empty or contains a null entry.</exception>
    /// <exception cref="AggregateException">Thrown when every candidate fails. Contains one exception per failed candidate.</exception>
    /// <remarks>
    /// Each fallback candidate is bounded by the default <see cref="FallbackPolicy.FallbackTimeout"/>
    /// (5 seconds). A fallback candidate that runs longer is cancelled and counted as a failure.
    /// </remarks>
    public static async Task<T> ExecuteWithChainAsync<T>(
        this FallbackService service,
        CancellationToken cancellationToken,
        params Func<CancellationToken, Task<T>>[] candidates)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (candidates is null)
            throw new ArgumentNullException(nameof(candidates));

        if (candidates.Length == 0)
            throw new ArgumentException("At least one candidate is required.", nameof(candidates));

        if (candidates.Any(candidate => candidate is null))
            throw new ArgumentException("Candidates cannot contain null entries.", nameof(candidates));

        var errors = new List<Exception>();
        var stopwatch = Stopwatch.StartNew();

        try
        {
            return await candidates[0](cancellationToken);
        }
        catch (Exception primaryException) when (!cancellationToken.IsCancellationRequested)
        {
            errors.Add(primaryException);
        }

        var lastException = errors[0];

        for (int i = 1; i < candidates.Length; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var policy = new FallbackPolicy(nameof(ExecuteWithChainAsync));
            policy.SetFallbackAction(candidates[i]);

            try
            {
                var result = await service.ExecuteAsync<T>(
                    policy,
                    lastException,
                    stopwatch.ElapsedMilliseconds,
                    cancellationToken);

                if (result.IsSuccess)
                    return result.Data!;
            }
            catch (FallbackFailedException failed)
            {
                cancellationToken.ThrowIfCancellationRequested();

                lastException = failed.FallbackException ?? failed;
                errors.Add(lastException);
            }
        }

        throw new AggregateException("All candidates failed.", errors);
    }
}
