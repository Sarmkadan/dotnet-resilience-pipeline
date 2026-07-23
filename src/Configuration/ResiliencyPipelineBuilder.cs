#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Configuration;

/// <summary>
/// Fluent builder for configuring resilience pipelines.
/// </summary>
public sealed class ResiliencyPipelineBuilder
{
    private readonly ResiliencyPipelineService _pipelineService;
    private CircuitBreakerPolicy? _circuitBreakerPolicy;
    private RetryPolicy? _retryPolicy;
    private ITimeoutStrategy? _timeoutPolicy;
    private BulkheadPolicy? _bulkheadPolicy;
    private FallbackPolicy? _fallbackPolicy;

    public ResiliencyPipelineBuilder()
    {
        _pipelineService = new ResiliencyPipelineService();
    }

    /// <summary>
    /// Adds a circuit breaker policy to the pipeline.
    /// </summary>
    public ResiliencyPipelineBuilder WithCircuitBreaker(string name, Action<CircuitBreakerPolicy>? configure = null)
    {
        _circuitBreakerPolicy = new CircuitBreakerPolicy(name);

        if (configure is not null)
            configure(_circuitBreakerPolicy);

        _pipelineService.RegisterPolicy(_circuitBreakerPolicy);
        return this;
    }

    /// <summary>
    /// Adds a retry policy to the pipeline.
    /// </summary>
    public ResiliencyPipelineBuilder WithRetry(string name, Action<RetryPolicy>? configure = null)
    {
        _retryPolicy = new RetryPolicy(name);

        if (configure is not null)
            configure(_retryPolicy);

        _pipelineService.RegisterPolicy(_retryPolicy);
        return this;
    }

    /// <summary>
    /// Adds a timeout policy to the pipeline.
    /// </summary>
    public ResiliencyPipelineBuilder WithTimeout(string name, TimeSpan timeout, Action<TimeoutPolicy>? configure = null)
    {
        var policy = new TimeoutPolicy(name) { Timeout = timeout };

        if (configure is not null)
            configure(policy);

        _timeoutPolicy = policy;
        _pipelineService.RegisterPolicy(policy);
        return this;
    }

    /// <summary>
    /// Adds an adaptive timeout policy to the pipeline that automatically adjusts based on observed latencies.
    /// </summary>
    public ResiliencyPipelineBuilder WithAdaptiveTimeout(string name, Action<AdaptiveTimeoutPolicy>? configure = null)
    {
        var policy = new AdaptiveTimeoutPolicy(name);

        if (configure is not null)
            configure(policy);

        _timeoutPolicy = policy;
        _pipelineService.RegisterPolicy(policy);
        return this;
    }

    /// <summary>
    /// Adds a bulkhead policy to the pipeline.
    /// </summary>
    public ResiliencyPipelineBuilder WithBulkhead(string name, int maxParallelization, int maxQueueLength = 50, Action<BulkheadPolicy>? configure = null)
    {
        _bulkheadPolicy = new BulkheadPolicy(name)
        {
            MaxParallelization = maxParallelization,
            MaxQueueLength = maxQueueLength
        };

        if (configure is not null)
            configure(_bulkheadPolicy);

        _pipelineService.RegisterPolicy(_bulkheadPolicy);
        return this;
    }

    /// <summary>
    /// Adds a fallback policy to the pipeline.
    /// </summary>
    public ResiliencyPipelineBuilder WithFallback(string name, Action<FallbackPolicy>? configure = null)
    {
        _fallbackPolicy = new FallbackPolicy(name);

        if (configure is not null)
            configure(_fallbackPolicy);

        _pipelineService.RegisterPolicy(_fallbackPolicy);
        return this;
    }

    /// <summary>
    /// Sets an asynchronous fallback action for the configured FallbackPolicy.
    /// </summary>
    /// <typeparam name="T">The return type of the operation.</typeparam>
    /// <param name="fallbackAction">The asynchronous function to execute as a fallback.</param>
    /// <returns>The current ResiliencyPipelineBuilder instance.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no FallbackPolicy has been configured yet.</exception>
    public ResiliencyPipelineBuilder WithFallbackAction<T>(Func<CancellationToken, Task<T>> fallbackAction)
    {
        if (_fallbackPolicy is null)
        {
            throw new InvalidOperationException("A FallbackPolicy must be configured before setting a fallback action. Use WithFallback first.");
        }

        _fallbackPolicy.SetFallbackAction(fallbackAction);
        return this;
    }

    /// <summary>
    /// Builds and returns the configured pipeline service.
    /// </summary>
    public ResiliencyPipelineService Build()
    {
        return _pipelineService;
    }

    /// <summary>
    /// Gets the configured circuit breaker policy.
    /// </summary>
    public CircuitBreakerPolicy? GetCircuitBreakerPolicy() => _circuitBreakerPolicy;

    /// <summary>
    /// Gets the configured retry policy.
    /// </summary>
    public RetryPolicy? GetRetryPolicy() => _retryPolicy;

    /// <summary>
    /// Gets the configured timeout policy.
    /// </summary>
    public ITimeoutStrategy? GetTimeoutPolicy() => _timeoutPolicy;

    /// <summary>
    /// Gets the configured bulkhead policy.
    /// </summary>
    public BulkheadPolicy? GetBulkheadPolicy() => _bulkheadPolicy;

    /// <summary>
    /// Gets the configured fallback policy.
    /// </summary>
    public FallbackPolicy? GetFallbackPolicy() => _fallbackPolicy;
}
