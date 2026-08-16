#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Interface for services handling retry policy execution.
/// </summary>
public interface IRetryService
{
    /// <summary>
    /// Executes an operation with retry logic.
    /// </summary>
    Task<T> ExecuteAsync<T>(
        RetryPolicy policy,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Executes an operation through the retry policy (without explicit cancellation token support).
    /// </summary>
    Task<T> ExecuteAsync<T>(
        RetryPolicy policy,
        Func<Task<T>> operation,
        CancellationToken cancellationToken);

    /// <summary>
    /// Calculates retry delay with configured backoff strategy.
    /// </summary>
    TimeSpan CalculateRetryDelay(RetryPolicy policy, int attemptNumber);

    /// <summary>
    /// Determines if an exception is retryable based on policy configuration.
    /// </summary>
    bool IsRetryable(RetryPolicy policy, Exception exception);

    /// <summary>
    /// Computes the next delay using the decorrelated jitter algorithm.
    /// </summary>
    TimeSpan ComputeDecorrelatedJitterDelay(RetryPolicy policy, TimeSpan previousDelay);
}
