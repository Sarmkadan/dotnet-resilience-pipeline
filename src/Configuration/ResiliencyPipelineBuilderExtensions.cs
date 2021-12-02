#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Configuration;

/// <summary>
/// Extension methods for <see cref="ResiliencyPipelineBuilder"/> to provide fluent configuration helpers.
/// </summary>
public static class ResiliencyPipelineBuilderExtensions
{
    /// <summary>
    /// Adds a circuit breaker policy with common defaults for transient faults.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="failureThreshold">The number of failures before opening the circuit.</param>
    /// <param name="openDuration">The duration the circuit stays open.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ResiliencyPipelineBuilder WithDefaultCircuitBreaker(
        this ResiliencyPipelineBuilder builder,
        int failureThreshold = 5,
        TimeSpan? openDuration = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var duration = openDuration ?? TimeSpan.FromSeconds(30);

        return builder.WithCircuitBreaker("default-circuit-breaker", policy =>
        {
            policy.FailureThreshold = failureThreshold;
            policy.OpenDuration = duration;
        });
    }

    /// <summary>
    /// Adds an exponential backoff retry policy with sensible defaults.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="maxRetryAttempts">Maximum number of retry attempts.</param>
    /// <param name="initialDelay">Initial delay between retries.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ResiliencyPipelineBuilder WithExponentialBackoffRetry(
        this ResiliencyPipelineBuilder builder,
        int maxRetryAttempts = 3,
        TimeSpan? initialDelay = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var delay = initialDelay ?? TimeSpan.FromMilliseconds(200);

        return builder.WithRetry("exponential-backoff-retry", policy =>
        {
            policy.MaxRetries = maxRetryAttempts;
            policy.InitialDelay = delay;
            policy.Strategy = RetryPolicy.BackoffStrategy.Exponential;
        });
    }

    /// <summary>
    /// Adds a bulkhead isolation policy with common production defaults.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="maxParallelization">Maximum concurrent executions.</param>
    /// <param name="maxQueueLength">Maximum queue size for waiting executions.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ResiliencyPipelineBuilder WithIsolatedBulkhead(
        this ResiliencyPipelineBuilder builder,
        int maxParallelization = 10,
        int maxQueueLength = 100)
    {
        ArgumentNullException.ThrowIfNull(builder);

        return builder.WithBulkhead("isolated-bulkhead", maxParallelization, maxQueueLength);
    }

    /// <summary>
    /// Adds a timeout policy with a default 5-second timeout.
    /// </summary>
    /// <param name="builder">The pipeline builder.</param>
    /// <param name="timeout">Optional custom timeout. Defaults to 5 seconds.</param>
    /// <returns>The builder instance for fluent chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="builder"/> is <see langword="null"/>.</exception>
    public static ResiliencyPipelineBuilder WithDefaultTimeout(
        this ResiliencyPipelineBuilder builder,
        TimeSpan? timeout = null)
    {
        ArgumentNullException.ThrowIfNull(builder);

        var timeoutValue = timeout ?? TimeSpan.FromSeconds(5);

        return builder.WithTimeout("default-timeout", timeoutValue);
    }
}