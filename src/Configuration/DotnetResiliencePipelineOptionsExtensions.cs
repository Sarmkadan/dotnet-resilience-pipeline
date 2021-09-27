#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Configuration;

/// <summary>
/// Extension methods for <see cref="DotnetResiliencePipelineOptions"/> configuration.
/// </summary>
public static class DotnetResiliencePipelineOptionsExtensions
{
    /// <summary>
    /// Creates a <see cref="ResiliencyPipelineBuilder"/> pre-configured with all policies from the options.
    /// </summary>
    /// <param name="options">The pipeline options.</param>
    /// <param name="pipelineName">The name for the pipeline.</param>
    /// <returns>A configured pipeline builder.</returns>
    public static ResiliencyPipelineBuilder ToPipelineBuilder(this DotnetResiliencePipelineOptions options, string pipelineName)
    {
        if (options is null)
        {
            throw new ArgumentNullException(nameof(options));
        }

        var builder = new ResiliencyPipelineBuilder();

        // Add circuit breaker if configured
        if (options.CircuitBreaker is not null)
        {
            var circuitBreakerOptions = options.CircuitBreaker;
            builder.WithCircuitBreaker(pipelineName, policy =>
            {
                var configured = circuitBreakerOptions.ToPolicy(pipelineName);
                policy.FailureThreshold = configured.FailureThreshold;
                policy.OpenDuration = configured.OpenDuration;
                policy.SuccessThresholdInHalfOpen = configured.SuccessThresholdInHalfOpen;
            });
        }

        // Add retry if configured
        if (options.Retry is not null)
        {
            var retryOptions = options.Retry;
            builder.WithRetry(pipelineName, policy =>
            {
                var configured = retryOptions.ToPolicy(pipelineName);
                policy.MaxRetries = configured.MaxRetries;
                policy.InitialDelay = configured.InitialDelay;
                policy.Strategy = configured.Strategy;
                policy.MaxDelay = configured.MaxDelay;
                policy.BackoffMultiplier = configured.BackoffMultiplier;
                policy.UseJitter = configured.UseJitter;
                policy.JitterFactor = configured.JitterFactor;
            });
        }

        // Add timeout if configured
        if (options.Timeout is not null)
        {
            builder.WithTimeout(pipelineName, TimeSpan.FromSeconds(options.Timeout.TimeoutSeconds));
        }

        // Add bulkhead if configured
        if (options.Bulkhead is not null)
        {
            builder.WithBulkhead(pipelineName, options.Bulkhead.MaxParallelization, options.Bulkhead.MaxQueueLength);
        }

        // Add fallback if configured
        if (options.Fallback is not null)
        {
            var fallbackOptions = options.Fallback;
            builder.WithFallback(pipelineName, policy =>
            {
                var configured = fallbackOptions.ToPolicy(pipelineName);
                policy.FallbackOnAnyException = configured.FallbackOnAnyException;
                policy.FallbackTimeout = configured.FallbackTimeout;
            });
        }

        return builder;
    }

    /// <summary>
    /// Configures the circuit breaker with common defaults for production scenarios.
    /// </summary>
    /// <param name="options">The circuit breaker options.</param>
    /// <param name="failureThreshold">Number of failures before opening circuit.</param>
    /// <param name="openDurationSeconds">Duration in seconds to keep circuit open.</param>
    /// <param name="successThresholdInHalfOpen">Successes needed in half-open state.</param>
    /// <returns>The configured circuit breaker options.</returns>
    public static global::DotNetResiliencePipeline.Configuration.DotnetResiliencePipelineOptions.CircuitBreakerOptions ConfigureForProduction(
        this global::DotNetResiliencePipeline.Configuration.DotnetResiliencePipelineOptions.CircuitBreakerOptions options,
        int failureThreshold = 5,
        int openDurationSeconds = 30,
        int successThresholdInHalfOpen = 3)
    {
        options.FailureThreshold = failureThreshold;
        options.OpenDurationSeconds = openDurationSeconds;
        options.SuccessThresholdInHalfOpen = successThresholdInHalfOpen;
        return options;
    }

    /// <summary>
    /// Configures retry with exponential backoff for transient fault handling.
    /// </summary>
    /// <param name="options">The retry options.</param>
    /// <param name="maxRetries">Maximum retry attempts.</param>
    /// <param name="initialDelayMs">Initial delay in milliseconds.</param>
    /// <param name="maxDelayMs">Maximum delay in milliseconds.</param>
    /// <param name="backoffMultiplier">Multiplier for exponential backoff.</param>
    /// <returns>The configured retry options.</returns>
    public static global::DotNetResiliencePipeline.Configuration.DotnetResiliencePipelineOptions.RetryOptions ConfigureForTransientFaults(
        this global::DotNetResiliencePipeline.Configuration.DotnetResiliencePipelineOptions.RetryOptions options,
        int maxRetries = 3,
        int initialDelayMs = 100,
        int maxDelayMs = 30000,
        double backoffMultiplier = 2.0)
    {
        options.MaxRetries = maxRetries;
        options.InitialDelayMs = initialDelayMs;
        options.MaxDelayMs = maxDelayMs;
        options.BackoffMultiplier = backoffMultiplier;
        options.Strategy = RetryPolicy.BackoffStrategy.Exponential;
        options.UseJitter = true;
        options.JitterFactor = 0.5;
        return options;
    }

    /// <summary>
    /// Configures timeout for critical operations that must complete within a strict time limit.
    /// </summary>
    /// <param name="options">The timeout options.</param>
    /// <param name="timeoutSeconds">Timeout duration in seconds.</param>
    /// <returns>The configured timeout options.</returns>
    public static global::DotNetResiliencePipeline.Configuration.DotnetResiliencePipelineOptions.TimeoutOptions ConfigureForCriticalOperations(
        this global::DotNetResiliencePipeline.Configuration.DotnetResiliencePipelineOptions.TimeoutOptions options,
        int timeoutSeconds = 5)
    {
        options.TimeoutSeconds = timeoutSeconds;
        return options;
    }

    /// <summary>
    /// Configures bulkhead isolation for protecting critical resources.
    /// </summary>
    /// <param name="options">The bulkhead options.</param>
    /// <param name="maxParallelization">Maximum concurrent executions.</param>
    /// <param name="maxQueueLength">Maximum queue length for waiting requests.</param>
    /// <returns>The configured bulkhead options.</returns>
    public static global::DotNetResiliencePipeline.Configuration.DotnetResiliencePipelineOptions.BulkheadOptions ConfigureForIsolation(
        this global::DotNetResiliencePipeline.Configuration.DotnetResiliencePipelineOptions.BulkheadOptions options,
        int maxParallelization = 5,
        int maxQueueLength = 20)
    {
        options.MaxParallelization = maxParallelization;
        options.MaxQueueLength = maxQueueLength;
        return options;
    }
}