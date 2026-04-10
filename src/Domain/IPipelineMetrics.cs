#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain;

/// <summary>
/// Provides a unified view of execution counters across all policies in the pipeline.
/// </summary>
public interface IPipelineMetrics
{
    /// <summary>
    /// Returns an aggregated statistics snapshot covering all active policies.
    /// </summary>
    PipelineMetricsSnapshot GetStats();
}

/// <summary>
/// Aggregated metrics snapshot for the entire resilience pipeline.
/// </summary>
public sealed class PipelineMetricsSnapshot
{
    /// <summary>Total executions processed by the pipeline.</summary>
    public long TotalExecutions { get; init; }

    /// <summary>Total successful executions.</summary>
    public long SuccessfulExecutions { get; init; }

    /// <summary>Total failed executions.</summary>
    public long FailedExecutions { get; init; }

    /// <summary>Success rate as a percentage (0–100).</summary>
    public double SuccessRate { get; init; }

    /// <summary>
    /// Total retry attempts recorded across all retry policies.
    /// Each individual retry (not the final failure) increments this counter.
    /// </summary>
    public long RetryCount { get; init; }

    /// <summary>
    /// Total number of times any circuit breaker transitioned to the Open state.
    /// </summary>
    public long CircuitBreakerTrips { get; init; }

    /// <summary>
    /// Total number of timeout events recorded across all timeout policies.
    /// </summary>
    public long TimeoutCount { get; init; }

    /// <summary>Per-policy snapshots for detailed inspection.</summary>
    public IReadOnlyList<Policies.PolicySnapshot> PolicySnapshots { get; init; } = Array.Empty<Policies.PolicySnapshot>();
}
