#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Service handling retry policy execution with exponential backoff and jitter.
/// </summary>
public sealed class RetryService : IRetryService
{
    // Use Random.Shared for thread-safe random number generation across threads
    private static readonly Random _random = Random.Shared;

    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    /// <param name="policy">The retry policy to apply.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    public async Task<T> ExecuteAsync<T>(
        RetryPolicy policy,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (!policy.IsValidConfiguration(out var error))
            throw new InvalidPolicyConfigurationException(policy.Name, error ?? "Invalid retry configuration");

        if (!policy.IsEnabled)
            return await operation(cancellationToken); // Use the provided token

        var stopwatch = Stopwatch.StartNew();
        var exceptions = new List<Exception>();
        TimeSpan previousDelay = TimeSpan.Zero; // Used for decorrelated jitter

        for (int attempt = 0; attempt <= policy.MaxRetries; attempt++)
        {
            try
            {
                var result = await operation(cancellationToken); // Use the provided token
                stopwatch.Stop();

                policy.RecordSuccess();
                return result;
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                exceptions.Add(ex);

                if (!policy.IsRetryable(ex) || attempt >= policy.MaxRetries)
                {
                    stopwatch.Stop();
                    policy.RecordFailure();

                    throw new MaxRetriesExceededException(
                        policy.Name,
                        attempt + 1,
                        exceptions);
                }

                policy.RecordRetryAttempt();

                // Calculate backoff delay
                TimeSpan delay;
                if (policy.UseDecorrelatedJitter)
                {
                    delay = ComputeDecorrelatedJitterDelay(policy, previousDelay);
                    previousDelay = delay;
                }
                else
                {
                    delay = policy.CalculateDelay(attempt);
                }

                await Task.Delay(delay, cancellationToken); // Pass token to Task.Delay
            }
        }

        throw new MaxRetriesExceededException(policy.Name, policy.MaxRetries + 1, exceptions);
    }

    /// <summary>
    /// Calculates retry delay with configured backoff strategy.
    /// </summary>
    public TimeSpan CalculateRetryDelay(RetryPolicy policy, int attemptNumber)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        return policy.CalculateDelay(attemptNumber);
    }

    /// <summary>
    /// Determines if an exception is retryable based on policy configuration.
    /// </summary>
    public bool IsRetryable(RetryPolicy policy, Exception exception)
    {
        return policy?.IsRetryable(exception) ?? false;
    }

    /// <summary>
    /// Executes an operation through the retry policy (without explicit cancellation token support).
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        RetryPolicy policy,
        Func<Task<T>> operation,
        CancellationToken cancellationToken)
    {
        return await ExecuteAsync(policy, _ => operation(), cancellationToken);
    }

    /// <summary>
    /// Computes the next delay using the decorrelated jitter algorithm:
    /// delay = min(maxDelay, random(baseDelay, previousDelay * 3))
    /// </summary>
    /// <param name="policy">The retry policy containing base and max delay settings.</param>
    /// <param name="previousDelay">The delay used in the previous retry attempt.</param>
    /// <returns>The calculated delay for the next retry attempt.</returns>
    public TimeSpan ComputeDecorrelatedJitterDelay(RetryPolicy policy, TimeSpan previousDelay)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        // Base delay is the policy's InitialDelay; max delay is MaxDelay.
        var baseDelay = policy.InitialDelay;
        var maxDelay = policy.MaxDelay;

        // Guard against non‑positive values.
        if (baseDelay <= TimeSpan.Zero)
            baseDelay = TimeSpan.FromMilliseconds(100);
        if (maxDelay <= TimeSpan.Zero)
            maxDelay = TimeSpan.FromSeconds(30);

        // Determine the upper bound for the random range.
        TimeSpan upperBound;
        if (previousDelay == TimeSpan.Zero)
        {
            upperBound = baseDelay;
        }
        else
        {
            var calculated = TimeSpan.FromMilliseconds(previousDelay.TotalMilliseconds * 3);
            upperBound = calculated.Min(maxDelay);
        }

        // Ensure the upper bound is not less than the base delay.
        upperBound = upperBound.Max(baseDelay);

        // Random value between baseDelay and upperBound.
        var randomMs = baseDelay.TotalMilliseconds + (Random.Shared.NextDouble() * (upperBound.TotalMilliseconds - baseDelay.TotalMilliseconds));
        var delay = TimeSpan.FromMilliseconds(randomMs);

        // Clamp to maxDelay just in case.
        delay = delay.Min(maxDelay);

        return delay;
    }
}
