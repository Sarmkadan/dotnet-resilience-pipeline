#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using DotNetResiliencePipeline.Domain;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Main orchestrator service that manages and coordinates all resilience policies.
/// </summary>
public sealed class ResiliencyPipelineService
{
    private readonly CircuitBreakerService _circuitBreakerService;
    private readonly RetryService _retryService;
    private readonly TimeoutService _timeoutService;
    private readonly BulkheadService _bulkheadService;
    private readonly FallbackService _fallbackService;
    private readonly Dictionary<string, ResiliencyPolicy> _policies;
    private readonly object _lockObj = new object();

    public string PipelineId { get; } = Guid.NewGuid().ToString();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
    public long TotalExecutions { get; private set; }
    public long SuccessfulExecutions { get; private set; }
    public long FailedExecutions { get; private set; }

    public ResiliencyPipelineService()
    {
        _policies = new Dictionary<string, ResiliencyPolicy>();
        _circuitBreakerService = new CircuitBreakerService();
        _retryService = new RetryService();
        _timeoutService = new TimeoutService();
        _bulkheadService = new BulkheadService();
        _fallbackService = new FallbackService();
    }

    /// <summary>
    /// Registers a new policy in the pipeline.
    /// </summary>
    public void RegisterPolicy(ResiliencyPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        lock (_lockObj)
        {
            _policies[policy.Id] = policy;
        }
    }

    /// <summary>
    /// Gets a policy by its identifier.
    /// </summary>
    public ResiliencyPolicy? GetPolicy(string policyId)
    {
        lock (_lockObj)
        {
            return _policies.TryGetValue(policyId, out var policy) ? policy : null;
        }
    }

    /// <summary>
    /// Gets a policy by name.
    /// </summary>
    public ResiliencyPolicy? GetPolicyByName(string policyName)
    {
        lock (_lockObj)
        {
            return _policies.Values.FirstOrDefault(p => p.Name == policyName);
        }
    }

    /// <summary>
    /// Lists all registered policies.
    /// </summary>
    public List<ResiliencyPolicy> GetAllPolicies()
    {
        lock (_lockObj)
        {
            return _policies.Values.ToList();
        }
    }

    /// <summary>
    /// Removes a policy from the pipeline.
    /// </summary>
    public bool RemovePolicy(string policyId)
    {
        lock (_lockObj)
        {
            return _policies.Remove(policyId);
        }
    }

    /// <summary>
    /// Executes an operation through the complete resilience pipeline.
    /// </summary>
    public async Task<PolicyResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CircuitBreakerPolicy? circuitBreaker = null,
        RetryPolicy? retry = null,
        TimeoutPolicy? timeout = null,
        BulkheadPolicy? bulkhead = null,
        FallbackPolicy? fallback = null)
    {
        var stopwatch = Stopwatch.StartNew();
        var executionId = Guid.NewGuid().ToString();

        try
        {
            // Circuit breaker check
            if (circuitBreaker?.IsEnabled == true)
            {
                var cbResult = await _circuitBreakerService.ExecuteAsync(
                    circuitBreaker,
                    () => _executeWithRetryTimeoutBulkhead(operation, retry, timeout, bulkhead));

                stopwatch.Stop();
                return cbResult is T result
                    ? PolicyResult<T>.Success(result, "Pipeline", stopwatch.ElapsedMilliseconds)
                    : throw new InvalidOperationException("Circuit breaker result type mismatch");
            }

            // Direct execution with retry, timeout, bulkhead
            var result = await _executeWithRetryTimeoutBulkhead(operation, retry, timeout, bulkhead);
            stopwatch.Stop();

            lock (_lockObj)
            {
                TotalExecutions++;
                SuccessfulExecutions++;
            }

            return PolicyResult<T>.Success(result, "Pipeline", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            // Try fallback
            if (fallback?.IsEnabled == true)
            {
                return await _fallbackService.ExecuteAsync<T>(fallback, operation, ex, stopwatch.ElapsedMilliseconds);
            }

            lock (_lockObj)
            {
                TotalExecutions++;
                FailedExecutions++;
            }

            return PolicyResult<T>.Failure(ex, "Pipeline", stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Executes a void operation through the pipeline.
    /// </summary>
    public async Task<PolicyResult> ExecuteAsync(
        Func<CancellationToken, Task> operation,
        CircuitBreakerPolicy? circuitBreaker = null,
        RetryPolicy? retry = null,
        TimeoutPolicy? timeout = null,
        BulkheadPolicy? bulkhead = null,
        FallbackPolicy? fallback = null)
    {
        var stopwatch = Stopwatch.StartNew();

        try
        {
            if (circuitBreaker?.IsEnabled == true)
            {
                await _circuitBreakerService.ExecuteAsync(
                    circuitBreaker,
                    async () => { await operation(CancellationToken.None); return (object)null!; });
            }
            else
            {
                await _executeWithRetryTimeoutBulkhead(
                    ct => operation(ct),
                    retry, timeout, bulkhead);
            }

            stopwatch.Stop();
            lock (_lockObj)
            {
                TotalExecutions++;
                SuccessfulExecutions++;
            }

            return PolicyResult.Success("Pipeline", stopwatch.ElapsedMilliseconds);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            lock (_lockObj)
            {
                TotalExecutions++;
                FailedExecutions++;
            }

            return PolicyResult.Failure(ex, "Pipeline", stopwatch.ElapsedMilliseconds);
        }
    }

    /// <summary>
    /// Gets execution statistics for the pipeline.
    /// </summary>
    public PipelineStatistics GetStatistics()
    {
        lock (_lockObj)
        {
            return new PipelineStatistics
            {
                PipelineId = PipelineId,
                CreatedAt = CreatedAt,
                TotalExecutions = TotalExecutions,
                SuccessfulExecutions = SuccessfulExecutions,
                FailedExecutions = FailedExecutions,
                SuccessRate = TotalExecutions == 0 ? 0 : (SuccessfulExecutions * 100.0) / TotalExecutions,
                PolicyCount = _policies.Count,
                RegisteredPolicies = _policies.Values.Select(p => p.GetSnapshot()).ToList()
            };
        }
    }

    /// <summary>
    /// Resets pipeline statistics.
    /// </summary>
    public void ResetStatistics()
    {
        lock (_lockObj)
        {
            TotalExecutions = 0;
            SuccessfulExecutions = 0;
            FailedExecutions = 0;

            foreach (var policy in _policies.Values)
            {
                policy.ResetStatistics();
            }
        }
    }

    private async Task<T> _executeWithRetryTimeoutBulkhead<T>(
        Func<CancellationToken, Task<T>> operation,
        RetryPolicy? retry = null,
        TimeoutPolicy? timeout = null,
        BulkheadPolicy? bulkhead = null)
    {
        if (bulkhead?.IsEnabled == true && !_bulkheadService.TryAcquireSlot(bulkhead))
            throw new BulkheadRejectedException(bulkhead.Name, bulkhead.ActiveExecutions, bulkhead.MaxParallelization, bulkhead.QueuedRequests);

        try
        {
            if (timeout?.IsEnabled == true)
                return await _timeoutService.ExecuteAsync(timeout, operation);

            if (retry?.IsEnabled == true)
                return await _retryService.ExecuteAsync(retry, operation);

            return await operation(CancellationToken.None);
        }
        finally
        {
            if (bulkhead?.IsEnabled == true)
                _bulkheadService.ReleaseSlot(bulkhead);
        }
    }
}

/// <summary>
/// Statistics snapshot for the entire pipeline.
/// </summary>
public sealed class PipelineStatistics
{
    public string PipelineId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long TotalExecutions { get; set; }
    public long SuccessfulExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public int PolicyCount { get; set; }
    public List<PolicySnapshot> RegisteredPolicies { get; set; } = new();
}
