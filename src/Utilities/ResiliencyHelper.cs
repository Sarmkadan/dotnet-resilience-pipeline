#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Data;
using DotNetResiliencePipeline.Domain;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Helper utilities for common resilience pipeline operations.
/// </summary>
public static class ResiliencyHelper
{
    /// <summary>
    /// Creates a policy result from an execution record.
    /// </summary>
    public static PolicyResult<T> CreateResultFromRecord<T>(ExecutionRecord record, T? data = null)
    {
        if (record.IsSuccess && data is not null)
        {
            return PolicyResult<T>.Success(data, record.PolicyName, record.ExecutionTimeMs, record.AttemptCount);
        }

        var exception = new Exception(record.ErrorMessage ?? "Operation failed");
        return PolicyResult<T>.Failure(exception, record.PolicyName, record.ExecutionTimeMs, record.AttemptCount);
    }

    /// <summary>
    /// Creates an execution record from a policy result.
    /// </summary>
    public static ExecutionRecord CreateRecordFromResult<T>(
        PolicyResult<T> result,
        string policyId)
    {
        return new ExecutionRecord
        {
            ExecutionId = result.ExecutionId,
            PolicyName = result.PolicyName,
            PolicyId = policyId,
            IsSuccess = result.IsSuccess,
            ExecutionTimeMs = result.ExecutionTimeMs,
            AttemptCount = result.AttemptCount,
            ErrorMessage = result.Exception?.Message,
            ErrorType = result.Exception?.GetType().Name,
            ExecutedAt = result.ExecutedAt,
            Metadata = result.Metadata
        };
    }

    /// <summary>
    /// Validates a policy configuration and returns validation errors.
    /// </summary>
    public static List<string> ValidatePolicy(ResiliencyPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        var errors = new List<string>();

        switch (policy)
        {
            case CircuitBreakerPolicy cbPolicy:
                if (cbPolicy.FailureThreshold <= 0)
                    errors.Add("Circuit breaker failure threshold must be positive");
                if (cbPolicy.OpenDuration <= TimeSpan.Zero)
                    errors.Add("Circuit breaker open duration must be positive");
                break;

            case RetryPolicy retryPolicy:
                if (!retryPolicy.IsValidConfiguration(out var retryError))
                    errors.Add(retryError ?? "Invalid retry configuration");
                break;

            case TimeoutPolicy timeoutPolicy:
                if (!timeoutPolicy.IsValidConfiguration(out var timeoutError))
                    errors.Add(timeoutError ?? "Invalid timeout configuration");
                break;

            case BulkheadPolicy bulkheadPolicy:
                if (!bulkheadPolicy.IsValidConfiguration(out var bulkheadError))
                    errors.Add(bulkheadError ?? "Invalid bulkhead configuration");
                break;

            case FallbackPolicy fallbackPolicy:
                if (!fallbackPolicy.IsValidConfiguration(out var fallbackError))
                    errors.Add(fallbackError ?? "Invalid fallback configuration");
                break;
        }

        return errors;
    }

    /// <summary>
    /// Generates a comprehensive health report for a pipeline.
    /// </summary>
    public static PipelineHealthReport GenerateHealthReport(
        ResiliencyPipelineService pipeline,
        ExecutionHistoryRepository history)
    {
        if (pipeline is null)
            throw new ArgumentNullException(nameof(pipeline));

        var stats = pipeline.GetStatistics();
        var historyStats = new Dictionary<string, object>
        {
            { "TotalRecords", history.Count() },
            { "SuccessRate", history.GetSuccessRate() },
            { "AverageExecutionTimeMs", history.GetAverageExecutionTime() },
            { "ErrorStatistics", history.GetErrorStatistics() }
        };

        return new PipelineHealthReport
        {
            PipelineId = stats.PipelineId,
            ReportGeneratedAt = DateTime.UtcNow,
            TotalExecutions = stats.TotalExecutions,
            SuccessRate = stats.SuccessRate,
            PolicyCount = stats.PolicyCount,
            HealthStatus = DeterminePipelineHealth(stats.SuccessRate),
            Policies = stats.RegisteredPolicies,
            HistoryStatistics = historyStats
        };
    }

    /// <summary>
    /// Determines the overall health status of a pipeline.
    /// </summary>
    public static HealthStatus DeterminePipelineHealth(double successRate)
    {
        if (successRate >= 95)
            return HealthStatus.Healthy;
        if (successRate >= 80)
            return HealthStatus.Degraded;
        if (successRate >= 50)
            return HealthStatus.Unhealthy;
        return HealthStatus.Critical;
    }

    /// <summary>
    /// Exports policy configuration to a dictionary.
    /// </summary>
    public static Dictionary<string, object> ExportPolicyConfig(ResiliencyPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        var config = new Dictionary<string, object>
        {
            { "Id", policy.Id },
            { "Name", policy.Name },
            { "Type", policy.GetType().Name },
            { "IsEnabled", policy.IsEnabled },
            { "CreatedAt", policy.CreatedAt },
            { "ModifiedAt", policy.ModifiedAt },
            { "Tags", policy.Tags },
            { "Metadata", policy.Metadata }
        };

        return config;
    }
}

/// <summary>
/// Health status levels for pipeline monitoring.
/// </summary>
public enum HealthStatus
{
    Healthy,
    Degraded,
    Unhealthy,
    Critical
}

/// <summary>
/// Comprehensive health report for a resilience pipeline.
/// </summary>
public sealed class PipelineHealthReport
{
    public string PipelineId { get; set; } = string.Empty;
    public DateTime ReportGeneratedAt { get; set; }
    public long TotalExecutions { get; set; }
    public double SuccessRate { get; set; }
    public int PolicyCount { get; set; }
    public HealthStatus HealthStatus { get; set; }
    public List<PolicySnapshot> Policies { get; set; } = new();
    public Dictionary<string, object> HistoryStatistics { get; set; } = new();
}
