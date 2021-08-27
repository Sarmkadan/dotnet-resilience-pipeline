#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Service handling retry policy execution with exponential backoff and jitter.
/// </summary>
public sealed class RetryService
{
    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
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

        for (int attempt = 0; attempt <= policy.MaxRetries; attempt++)
        {
            try
            {
                var result = await operation(cancellationToken); // Use the provided token
                stopwatch.Stop();

                policy.RecordSuccess();
                return result;
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
                var delayMs = policy.CalculateDelay(attempt);
                await Task.Delay((int)delayMs.TotalMilliseconds, cancellationToken); // Pass token to Task.Delay
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
        CancellationToken cancellationToken) // Added CancellationToken
    {
        return await ExecuteAsync(policy, _ => operation(), cancellationToken);
    }
}