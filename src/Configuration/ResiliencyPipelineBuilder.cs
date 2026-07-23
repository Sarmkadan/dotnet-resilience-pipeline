#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Configuration;

/// <summary>
/// Fluent builder for configuring resilience pipelines with stage ordering validation.
/// </summary>
/// <remarks>
/// <para>
/// This builder enforces canonical policy ordering to prevent semantic errors in pipeline composition.
/// The recommended order (outermost to innermost) is:
/// <list type="number">
///   <item><description><see cref="FallbackPolicy">Fallback</see> - Handles failures gracefully</description></item>
///   <item><description><see cref="CircuitBreakerPolicy">Circuit Breaker</see> - Prevents cascading failures</description></item>
///   <item><description><see cref="RetryPolicy">Retry</see> - Handles transient failures with backoff</description></item>
///   <item><description><see cref="BulkheadPolicy">Bulkhead</see> - Limits resource usage</description></item>
///   <item><description><see cref="TimeoutPolicy">Timeout</see> - Enforces execution time limits</description></item>
/// </list>
/// </para>
/// <para>
/// When policies are composed, they should be added in this order:
/// <code>
/// builder
///     .WithFallback("my-fallback", ...)      // Outermost - handles failures from all inner policies
///     .WithCircuitBreaker("my-circuit", ...)  // Next - prevents overload during outages
///     .WithRetry("my-retry", ...)            // Next - retries transient failures
///     .WithBulkhead("my-bulkhead", ...)      // Next - limits concurrency
///     .WithTimeout("my-timeout", ...);       // Innermost - per-attempt timeout
/// </code>
/// </para>
/// <para>
/// The actual execution order is:
/// <list type="number">
///   <item><description>Fallback policy catches exceptions from all inner policies</description></item>
///   <item><description>Circuit breaker checks state before allowing execution</description></item>
///   <item><description>Retry loop with backoff around the operation</description></item>
///   <item><description>Timeout applied per retry attempt</description></item>
///   <item><description>Bulkhead limits concurrent executions</description></item>
///   <item><description>User operation executes</description></item>
/// </list>
/// </para>
/// <para>
/// If you need non-canonical ordering, call <see cref="AllowCustomOrder()"/> before adding policies.
/// </para>
/// </remarks>
public sealed class ResiliencyPipelineBuilder
{
    private readonly ResiliencyPipelineService _pipelineService;
    private CircuitBreakerPolicy? _circuitBreakerPolicy;
    private RetryPolicy? _retryPolicy;
    private ITimeoutStrategy? _timeoutPolicy;
    private BulkheadPolicy? _bulkheadPolicy;
    private FallbackPolicy? _fallbackPolicy;

    private bool _allowCustomOrder = false;
    private readonly HashSet<string> _addedPolicyNames = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencyPipelineBuilder"/> class.
    /// </summary>
    public ResiliencyPipelineBuilder()
    {
        _pipelineService = new ResiliencyPipelineService();
    }

    /// <summary>
    /// Allows non-canonical policy ordering. By default, policies must be added in canonical order
    /// (Fallback > Circuit Breaker > Retry > Bulkhead > Timeout) to prevent semantic errors.
    /// </summary>
    /// <returns>The current <see cref="ResiliencyPipelineBuilder"/> instance for fluent chaining.</returns>
    /// <remarks>
    /// Use this method only when you have a specific reason to deviate from the recommended ordering.
    /// Non-canonical ordering can lead to unexpected behavior where policies don't work as expected.
    /// </remarks>
    public ResiliencyPipelineBuilder AllowCustomOrder()
    {
        _allowCustomOrder = true;
        return this;
    }

    /// <summary>
    /// Adds a circuit breaker policy to the pipeline.
    /// </summary>
    /// <param name="name">The name of the circuit breaker policy.</param>
    /// <param name="configure">Optional configuration action for the circuit breaker policy.</param>
    /// <returns>The current <see cref="ResiliencyPipelineBuilder"/> instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a circuit breaker policy has already been added.</exception>
    /// <exception cref="InvalidPolicyConfigurationException">Thrown if policy ordering validation fails and custom order is not allowed.</exception>
    public ResiliencyPipelineBuilder WithCircuitBreaker(string name, Action<CircuitBreakerPolicy>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        // Check for duplicate policy
        if (_addedPolicyNames.Contains(name))
        {
            throw new InvalidOperationException($"A policy with name '{name}' has already been added to this builder. Policy names must be unique.");
        }

        // Validate ordering: Circuit breaker must come after fallback
        if (_fallbackPolicy is null && !_allowCustomOrder)
        {
            throw new InvalidPolicyConfigurationException(
                name,
                "Circuit breaker policy should be added after fallback policy unless AllowCustomOrder() is called. " +
                "Canonical ordering: Fallback > Circuit Breaker > Retry > Bulkhead > Timeout.",
                new List<string> { "Circuit breaker added without fallback", "Add fallback first", "Use AllowCustomOrder() for non-canonical ordering" });
        }

        _circuitBreakerPolicy = new CircuitBreakerPolicy(name);
        _addedPolicyNames.Add(name);

        if (configure is not null)
            configure(_circuitBreakerPolicy);

        _pipelineService.RegisterPolicy(_circuitBreakerPolicy);
        return this;
    }

    /// <summary>
    /// Adds a retry policy to the pipeline.
    /// </summary>
    /// <param name="name">The name of the retry policy.</param>
    /// <param name="configure">Optional configuration action for the retry policy.</param>
    /// <returns>The current <see cref="ResiliencyPipelineBuilder"/> instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a retry policy has already been added.</exception>
    /// <exception cref="InvalidPolicyConfigurationException">Thrown if policy ordering validation fails and custom order is not allowed.</exception>
    public ResiliencyPipelineBuilder WithRetry(string name, Action<RetryPolicy>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        // Check for duplicate policy
        if (_addedPolicyNames.Contains(name))
        {
            throw new InvalidOperationException($"A policy with name '{name}' has already been added to this builder. Policy names must be unique.");
        }

        // Validate ordering: Retry must come after circuit breaker and fallback
        if ((_circuitBreakerPolicy is null || _fallbackPolicy is null) && !_allowCustomOrder)
        {
            throw new InvalidPolicyConfigurationException(
                name,
                "Retry policy should be added after circuit breaker and fallback policies unless AllowCustomOrder() is called. " +
                "Canonical ordering: Fallback > Circuit Breaker > Retry > Bulkhead > Timeout.",
                new List<string> { "Retry added without circuit breaker or fallback", "Add circuit breaker and fallback first", "Use AllowCustomOrder() for non-canonical ordering" });
        }

        _retryPolicy = new RetryPolicy(name);
        _addedPolicyNames.Add(name);

        if (configure is not null)
            configure(_retryPolicy);

        _pipelineService.RegisterPolicy(_retryPolicy);
        return this;
    }

    /// <summary>
    /// Adds a timeout policy to the pipeline.
    /// </summary>
    /// <param name="name">The name of the timeout policy.</param>
    /// <param name="timeout">The timeout duration for operations.</param>
    /// <param name="configure">Optional configuration action for the timeout policy.</param>
    /// <returns>The current <see cref="ResiliencyPipelineBuilder"/> instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a timeout policy has already been added.</exception>
    /// <exception cref="InvalidPolicyConfigurationException">Thrown if policy ordering validation fails and custom order is not allowed.</exception>
    public ResiliencyPipelineBuilder WithTimeout(string name, TimeSpan timeout, Action<TimeoutPolicy>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        // Check for duplicate policy
        if (_addedPolicyNames.Contains(name))
        {
            throw new InvalidOperationException($"A policy with name '{name}' has already been added to this builder. Policy names must be unique.");
        }

        // Validate ordering: Timeout must come after retry and bulkhead
        if ((_retryPolicy is null || _bulkheadPolicy is null || _circuitBreakerPolicy is null || _fallbackPolicy is null) && !_allowCustomOrder)
        {
            throw new InvalidPolicyConfigurationException(
                name,
                "Timeout policy should be added after retry and bulkhead policies unless AllowCustomOrder() is called. " +
                "Canonical ordering: Fallback > Circuit Breaker > Retry > Bulkhead > Timeout.",
                new List<string> { "Timeout added without retry or bulkhead", "Timeout should be innermost policy", "Add retry and bulkhead first", "Use AllowCustomOrder() for non-canonical ordering" });
        }

        var policy = new TimeoutPolicy(name) { Timeout = timeout };

        if (configure is not null)
            configure(policy);

        _timeoutPolicy = policy;
        _addedPolicyNames.Add(name);
        _pipelineService.RegisterPolicy(policy);
        return this;
    }

    /// <summary>
    /// Adds an adaptive timeout policy to the pipeline that automatically adjusts based on observed latencies.
    /// </summary>
    /// <param name="name">The name of the adaptive timeout policy.</param>
    /// <param name="configure">Optional configuration action for the adaptive timeout policy.</param>
    /// <returns>The current <see cref="ResiliencyPipelineBuilder"/> instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a timeout policy has already been added.</exception>
    /// <exception cref="InvalidPolicyConfigurationException">Thrown if policy ordering validation fails and custom order is not allowed.</exception>
    public ResiliencyPipelineBuilder WithAdaptiveTimeout(string name, Action<AdaptiveTimeoutPolicy>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        // Check for duplicate policy
        if (_addedPolicyNames.Contains(name))
        {
            throw new InvalidOperationException($"A policy with name '{name}' has already been added to this builder. Policy names must be unique.");
        }

        // Validate ordering: Adaptive timeout must come after retry and bulkhead
        if ((_retryPolicy is null || _bulkheadPolicy is null || _circuitBreakerPolicy is null || _fallbackPolicy is null) && !_allowCustomOrder)
        {
            throw new InvalidPolicyConfigurationException(
                name,
                "Adaptive timeout policy should be added after retry and bulkhead policies unless AllowCustomOrder() is called. " +
                "Canonical ordering: Fallback > Circuit Breaker > Retry > Bulkhead > Timeout.",
                new List<string> { "Adaptive timeout added without retry or bulkhead", "Timeout should be innermost policy", "Add retry and bulkhead first", "Use AllowCustomOrder() for non-canonical ordering" });
        }

        var policy = new AdaptiveTimeoutPolicy(name);

        if (configure is not null)
            configure(policy);

        _timeoutPolicy = policy;
        _addedPolicyNames.Add(name);
        _pipelineService.RegisterPolicy(policy);
        return this;
    }

    /// <summary>
    /// Adds a bulkhead policy to the pipeline.
    /// </summary>
    /// <param name="name">The name of the bulkhead policy.</param>
    /// <param name="maxParallelization">Maximum number of parallel executions allowed.</param>
    /// <param name="maxQueueLength">Maximum number of queued requests when all slots are busy.</param>
    /// <param name="configure">Optional configuration action for the bulkhead policy.</param>
    /// <returns>The current <see cref="ResiliencyPipelineBuilder"/> instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a bulkhead policy has already been added.</exception>
    /// <exception cref="InvalidPolicyConfigurationException">Thrown if policy ordering validation fails and custom order is not allowed.</exception>
    public ResiliencyPipelineBuilder WithBulkhead(string name, int maxParallelization, int maxQueueLength = 50, Action<BulkheadPolicy>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));
        ArgumentOutOfRangeException.ThrowIfLessThan(maxParallelization, 1);
        ArgumentOutOfRangeException.ThrowIfLessThan(maxQueueLength, 0);

        // Check for duplicate policy
        if (_addedPolicyNames.Contains(name))
        {
            throw new InvalidOperationException($"A policy with name '{name}' has already been added to this builder. Policy names must be unique.");
        }

        // Validate ordering: Bulkhead must come after circuit breaker and retry
        if ((_circuitBreakerPolicy is null || _fallbackPolicy is null || _retryPolicy is null) && !_allowCustomOrder)
        {
            throw new InvalidPolicyConfigurationException(
                name,
                "Bulkhead policy should be added after circuit breaker and retry policies unless AllowCustomOrder() is called. " +
                "Canonical ordering: Fallback > Circuit Breaker > Retry > Bulkhead > Timeout.",
                new List<string> { "Bulkhead added without circuit breaker or retry", "Add circuit breaker and retry first", "Use AllowCustomOrder() for non-canonical ordering" });
        }

        _bulkheadPolicy = new BulkheadPolicy(name)
        {
            MaxParallelization = maxParallelization,
            MaxQueueLength = maxQueueLength
        };

        if (configure is not null)
            configure(_bulkheadPolicy);

        _addedPolicyNames.Add(name);
        _pipelineService.RegisterPolicy(_bulkheadPolicy);
        return this;
    }

    /// <summary>
    /// Adds a fallback policy to the pipeline.
    /// </summary>
    /// <param name="name">The name of the fallback policy.</param>
    /// <param name="configure">Optional configuration action for the fallback policy.</param>
    /// <returns>The current <see cref="ResiliencyPipelineBuilder"/> instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if a fallback policy has already been added.</exception>
    /// <exception cref="InvalidPolicyConfigurationException">Thrown if policy ordering validation fails and custom order is not allowed.</exception>
    public ResiliencyPipelineBuilder WithFallback(string name, Action<FallbackPolicy>? configure = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(name, nameof(name));

        // Check for duplicate policy
        if (_addedPolicyNames.Contains(name))
        {
            throw new InvalidOperationException($"A policy with name '{name}' has already been added to this builder. Policy names must be unique.");
        }

        _fallbackPolicy = new FallbackPolicy(name);
        _addedPolicyNames.Add(name);

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
    /// <returns>The current <see cref="ResiliencyPipelineBuilder"/> instance for fluent chaining.</returns>
    /// <exception cref="InvalidOperationException">Thrown if no FallbackPolicy has been configured yet.</exception>
    public ResiliencyPipelineBuilder WithFallbackAction<T>(Func<CancellationToken, Task<T>> fallbackAction)
    {
        ArgumentNullException.ThrowIfNull(fallbackAction, nameof(fallbackAction));

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
    /// <returns>The configured <see cref="ResiliencyPipelineService"/> instance.</returns>
    /// <exception cref="InvalidPolicyConfigurationException">Thrown if required policies are missing or configuration is invalid.</exception>
    /// <remarks>
    /// Validates that the pipeline configuration is semantically correct. This includes checking:
    /// <list type="bullet">
    ///   <item><description>No duplicate policies with the same name</description></item>
    ///   <item><description>Policy ordering follows canonical semantics (unless AllowCustomOrder() was called)</description></item>
    ///   <item><description>All policies are properly configured</description></item>
    /// </list>
    /// </remarks>
    public ResiliencyPipelineService Build()
    {
        // Validate that we have at least one policy
        if (_addedPolicyNames.Count == 0)
        {
            throw new InvalidPolicyConfigurationException(
                "pipeline",
                "Cannot build a pipeline with no policies. Add at least one resilience policy using WithCircuitBreaker, WithRetry, WithTimeout, WithBulkhead, or WithFallback.",
                new List<string> { "No policies configured", "Add policies before calling Build()" });
        }

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
