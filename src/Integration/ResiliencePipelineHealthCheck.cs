#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace DotNetResiliencePipeline.Integration;

/// <summary>
/// Health check that reports the state of the enabled policies registered on a <see cref="ResiliencyPipelineService"/>.
/// <list type="bullet">
/// <item><description><see cref="HealthStatus.Unhealthy"/> when a bulkhead is saturated: all execution slots are in use and the queue is full, so new requests are being rejected.</description></item>
/// <item><description><see cref="HealthStatus.Degraded"/> when any circuit breaker is open and failing fast.</description></item>
/// <item><description><see cref="HealthStatus.Healthy"/> otherwise.</description></item>
/// </list>
/// Unhealthy takes precedence over Degraded. The check only reads policy state; it never changes it.
/// Results carry <c>openCircuitBreakers</c> and <c>saturatedBulkheads</c> lists in <see cref="HealthReportEntry.Data"/>.
/// </summary>
public sealed class ResiliencePipelineHealthCheck : IHealthCheck
{
    private readonly ResiliencyPipelineService _pipeline;

    /// <summary>
    /// Initializes a new health check for the given pipeline.
    /// </summary>
    /// <param name="pipeline">The pipeline whose registered policies are inspected.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="pipeline"/> is null.</exception>
    public ResiliencePipelineHealthCheck(ResiliencyPipelineService pipeline)
    {
        _pipeline = pipeline ?? throw new ArgumentNullException(nameof(pipeline));
    }

    /// <inheritdoc />
    public Task<HealthCheckResult> CheckHealthAsync(
        HealthCheckContext context,
        CancellationToken cancellationToken = default)
    {
        var policies = _pipeline.GetAllPolicies();

        var saturatedBulkheads = policies
            .OfType<BulkheadPolicy>()
            .Where(b => b.IsEnabled && b.ActiveExecutions >= b.MaxParallelization && b.QueuedRequests >= b.MaxQueueLength)
            .Select(b => b.Name)
            .ToList();

        var openCircuitBreakers = policies
            .OfType<CircuitBreakerPolicy>()
            .Where(cb => cb.IsEnabled && cb.CurrentState == CircuitBreakerPolicy.CircuitState.Open)
            .Select(cb => cb.Name)
            .ToList();

        var data = new Dictionary<string, object>
        {
            { "openCircuitBreakers", openCircuitBreakers },
            { "saturatedBulkheads", saturatedBulkheads }
        };

        if (saturatedBulkheads.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Unhealthy(
                $"Bulkhead saturated: {string.Join(", ", saturatedBulkheads)}",
                data: data));
        }

        if (openCircuitBreakers.Count > 0)
        {
            return Task.FromResult(HealthCheckResult.Degraded(
                $"Circuit breaker open: {string.Join(", ", openCircuitBreakers)}",
                data: data));
        }

        return Task.FromResult(HealthCheckResult.Healthy("All resilience policies are operating normally.", data));
    }
}
