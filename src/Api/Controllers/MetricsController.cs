#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetResiliencePipeline.Data;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Workers;

namespace DotNetResiliencePipeline.Api.Controllers;

/// <summary>
/// REST API controller for metrics and monitoring.
/// Provides endpoints for retrieving execution statistics and health metrics.
/// </summary>
public sealed class MetricsController
{
    private const double HealthySuccessRateThreshold = 95;
    private const double DegradedSuccessRateThreshold = 80;
    /// <summary>
    /// The default number of execution records returned when no limit is specified.
    /// </summary>
    public const int DefaultHistoryLimit = 100;

    private readonly ResiliencyPipelineService _pipelineService;
    private readonly ExecutionHistoryRepository _historyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="MetricsController"/> class.
    /// </summary>
    /// <param name="pipelineService">The pipeline service whose statistics are reported.</param>
    /// <param name="historyRepository">The repository that stores execution history.</param>
    public MetricsController(ResiliencyPipelineService pipelineService, ExecutionHistoryRepository historyRepository)
    {
        _pipelineService = pipelineService;
        _historyRepository = historyRepository;
    }

    /// <summary>
    /// Returns a string representation of the controller, including current pipeline statistics.
    /// </summary>
    /// <returns>A summary string of the controller and its pipeline statistics.</returns>
    public override string ToString()
    {
        var stats = _pipelineService.GetStatistics();
        return $"MetricsController {{ PipelineId = {stats.PipelineId}, CreatedAt = {stats.CreatedAt}, TotalExecutions = {stats.TotalExecutions}, SuccessfulExecutions = {stats.SuccessfulExecutions}, FailedExecutions = {stats.FailedExecutions}, SuccessRate = {stats.SuccessRate} }}";
    }

    /// <summary>
    /// GET /api/metrics/pipeline - Retrieves pipeline-level metrics.
    /// </summary>
    public async Task<ApiResponse<PipelineMetricsDto>> GetPipelineMetricsAsync()
    {
        try
        {
            var stats = _pipelineService.GetStatistics();

            var dto = new PipelineMetricsDto
            {
                PipelineId = stats.PipelineId,
                CreatedAt = stats.CreatedAt,
                TotalExecutions = stats.TotalExecutions,
                SuccessfulExecutions = stats.SuccessfulExecutions,
                FailedExecutions = stats.FailedExecutions,
                SuccessRate = stats.SuccessRate,
                PolicyCount = stats.PolicyCount,
                AverageExecutionTimeMs = _historyRepository.GetAverageExecutionTime()
            };

            return new ApiResponse<PipelineMetricsDto> { Success = true, Data = dto };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PipelineMetricsDto> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// GET /api/metrics/policies - Retrieves per-policy metrics.
    /// </summary>
    public async Task<ApiResponse<List<PolicyMetricsDto>>> GetPoliciesMetricsAsync()
    {
        try
        {
            var policies = _pipelineService.GetAllPolicies();
            var dtos = policies.Select(p =>
            {
                var records = _historyRepository.GetByPolicyId(p.Id);
                var successCount = records.Count(r => r.IsSuccess);
                return new PolicyMetricsDto
                {
                    PolicyId = p.Id,
                    PolicyName = p.Name,
                    Type = p.GetType().Name,
                    IsEnabled = p.IsEnabled,
                    ExecutionCount = records.Count,
                    SuccessCount = successCount,
                    FailureCount = records.Count - successCount
                };
            }).ToList();

            return new ApiResponse<List<PolicyMetricsDto>> { Success = true, Data = dtos };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<PolicyMetricsDto>> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// GET /api/metrics/health - Retrieves health status of the pipeline.
    /// </summary>
    public async Task<ApiResponse<HealthStatusDto>> GetHealthStatusAsync()
    {
        try
        {
            var stats = _pipelineService.GetStatistics();

            // Determine health based on success rate
            string status = stats.SuccessRate switch
            {
                >= HealthySuccessRateThreshold => "Healthy",
                >= DegradedSuccessRateThreshold => "Degraded",
                _ => "Critical"
            };

            var dto = new HealthStatusDto
            {
                Status = status,
                SuccessRate = stats.SuccessRate,
                TotalExecutions = stats.TotalExecutions,
                FailedExecutions = stats.FailedExecutions,
                LastCheckTime = DateTime.UtcNow
            };

            return new ApiResponse<HealthStatusDto> { Success = true, Data = dto };
        }
        catch (Exception ex)
        {
            return new ApiResponse<HealthStatusDto> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// GET /api/metrics/health/report - Retrieves comprehensive JSON health report.
    /// Aggregates health status for all policies in the pipeline.
    /// </summary>
    public async Task<ApiResponse<HealthReport>> GetHealthReportAsync()
    {
        try
        {
            var stats = _pipelineService.GetStatistics();
            var allPolicies = _pipelineService.GetAllPolicies();

            var report = new HealthReport
            {
                IsRunning = true, // Health check worker is always running in background
                GeneratedAt = DateTime.UtcNow,
                PipelineSuccessRate = stats.SuccessRate,
                OverallStatus = DetermineHealthStatus(stats.SuccessRate),
                TotalExecutions = stats.TotalExecutions,
                SuccessfulExecutions = stats.SuccessfulExecutions,
                FailedExecutions = stats.FailedExecutions,
                TotalPolicies = allPolicies.Count
            };

            // Count policy health statuses
            int healthyCount = 0;
            int degradedCount = 0;
            int unhealthyCount = 0;

            foreach (var policy in allPolicies)
            {
                var policySnapshot = policy.GetSnapshot();
                var healthStatus = DetermineHealthStatus(policySnapshot.SuccessRate);

                var policyHealth = new PolicyHealthStatus
                {
                    PolicyId = policy.Id,
                    PolicyName = policy.Name,
                    PolicyType = policy.GetType().Name,
                    IsEnabled = policy.IsEnabled,
                    HealthStatus = healthStatus,
                    SuccessRate = policySnapshot.SuccessRate,
                    TotalExecutions = policySnapshot.TotalExecutions,
                    SuccessfulExecutions = policySnapshot.SuccessfulExecutions,
                    FailedExecutions = policySnapshot.FailedExecutions,
                    LastCheckTime = policySnapshot.SnapshotTime
                };

                report.PolicyStatuses.Add(policyHealth);

                // Categorize policy health
                if (healthStatus == "Healthy")
                    healthyCount++;
                else if (healthStatus == "Degraded")
                    degradedCount++;
                else
                    unhealthyCount++;
            }

            report.HealthyPolicies = healthyCount;
            report.DegradedPolicies = degradedCount;
            report.UnhealthyPolicies = unhealthyCount;

            // Add additional metrics
            report.AdditionalMetrics["PipelineId"] = stats.PipelineId;
            report.AdditionalMetrics["CreatedAt"] = stats.CreatedAt;

            return new ApiResponse<HealthReport> { Success = true, Data = report };
        }
        catch (Exception ex)
        {
            return new ApiResponse<HealthReport> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// GET /api/metrics/history - Retrieves execution history.
    /// </summary>
    public async Task<ApiResponse<List<ExecutionRecordDto>>> GetExecutionHistoryAsync(int limit = DefaultHistoryLimit)
    {
        try
        {
            if (limit <= 0)
                return new ApiResponse<List<ExecutionRecordDto>> { Success = false, Message = "Limit must be greater than 0" };

            var records = _historyRepository.GetLatest(limit).Select(r => new ExecutionRecordDto
            {
                Id = r.ExecutionId,
                PolicyName = r.PolicyName,
                IsSuccess = r.IsSuccess,
                ExecutionTimeMs = r.ExecutionTimeMs,
                ExecutedAt = r.ExecutedAt,
                ErrorMessage = r.ErrorMessage
            }).ToList();

            return new ApiResponse<List<ExecutionRecordDto>> { Success = true, Data = records };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<ExecutionRecordDto>> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// POST /api/metrics/reset - Resets all metrics.
    /// </summary>
    public async Task<ApiResponse<bool>> ResetMetricsAsync()
    {
        try
        {
            _pipelineService.ResetStatistics();
            return new ApiResponse<bool> { Success = true, Data = true };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// GET /api/metrics/percentiles - Retrieves latency percentiles (P50, P90, P99).
    /// </summary>
    public async Task<ApiResponse<LatencyPercentilesDto>> GetLatencyPercentilesAsync()
    {
        try
        {
            var percentiles = _historyRepository.GetLatencyPercentiles();

            var dto = new LatencyPercentilesDto
            {
                P50 = percentiles.P50,
                P90 = percentiles.P90,
                P99 = percentiles.P99
            };

            return new ApiResponse<LatencyPercentilesDto> { Success = true, Data = dto };
        }
        catch (Exception ex)
        {
            return new ApiResponse<LatencyPercentilesDto> { Success = false, Message = ex.Message };
        }
    }

    private string DetermineHealthStatus(double successRate)
    {
        if (successRate >= HealthySuccessRateThreshold)
            return "Healthy";
        if (successRate >= DegradedSuccessRateThreshold)
            return "Degraded";
        return "Unhealthy";
    }
}

/// <summary>
/// Pipeline-level metrics data transfer object.
/// </summary>
public sealed class PipelineMetricsDto
{
    /// <summary>
    /// Gets or sets the identifier of the pipeline.
    /// </summary>
    public string PipelineId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the UTC time at which the pipeline was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Gets or sets the total number of executions performed by the pipeline.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of successful executions.
    /// </summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions.
    /// </summary>
    public long FailedExecutions { get; set; }

    /// <summary>
    /// Gets or sets the success rate, as a percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the number of policies registered in the pipeline.
    /// </summary>
    public int PolicyCount { get; set; }

    /// <summary>
    /// Gets or sets the average execution time, in milliseconds.
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }
}

/// <summary>
/// Per-policy metrics data transfer object.
/// </summary>
public sealed class PolicyMetricsDto
{
    /// <summary>
    /// Gets or sets the identifier of the policy.
    /// </summary>
    public string PolicyId { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the policy.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy type name.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the policy is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the total number of executions of the policy.
    /// </summary>
    public long ExecutionCount { get; set; }

    /// <summary>
    /// Gets or sets the number of successful executions of the policy.
    /// </summary>
    public long SuccessCount { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions of the policy.
    /// </summary>
    public long FailureCount { get; set; }

    /// <summary>
    /// Gets the success rate of the policy, as a percentage.
    /// </summary>
    public double SuccessRate => ExecutionCount > 0 ? (SuccessCount * 100.0) / ExecutionCount : 0;
}

/// <summary>
/// Health status data transfer object.
/// </summary>
public sealed class HealthStatusDto
{
    /// <summary>
    /// Gets or sets the overall health status, such as Healthy, Degraded, or Unhealthy.
    /// </summary>
    public string Status { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the success rate, as a percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the total number of executions evaluated.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions evaluated.
    /// </summary>
    public long FailedExecutions { get; set; }

    /// <summary>
    /// Gets or sets the UTC time of the most recent health check.
    /// </summary>
    public DateTime LastCheckTime { get; set; }

    /// <summary>
    /// Gets or sets additional diagnostic details keyed by name.
    /// </summary>
    public Dictionary<string, string> Details { get; set; } = new();
}

/// <summary>
/// Execution record data transfer object.
/// </summary>
public sealed class ExecutionRecordDto
{
    /// <summary>
    /// Gets or sets the identifier of the execution record.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the policy that was executed.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the execution succeeded.
    /// </summary>
    public bool IsSuccess { get; set; }

    /// <summary>
    /// Gets or sets the execution time, in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the execution occurred.
    /// </summary>
    public DateTime ExecutedAt { get; set; }

    /// <summary>
    /// Gets or sets the error message for a failed execution, or <c>null</c> if it succeeded.
    /// </summary>
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// Latency percentiles data transfer object.
/// </summary>
public sealed class LatencyPercentilesDto
{
    /// <summary>
    /// Gets or sets the 50th percentile latency, in milliseconds.
    /// </summary>
    public double P50 { get; set; }

    /// <summary>
    /// Gets or sets the 90th percentile latency, in milliseconds.
    /// </summary>
    public double P90 { get; set; }

    /// <summary>
    /// Gets or sets the 99th percentile latency, in milliseconds.
    /// </summary>
    public double P99 { get; set; }
}
