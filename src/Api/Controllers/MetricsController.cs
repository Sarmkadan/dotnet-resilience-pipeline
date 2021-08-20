// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Data;

namespace DotNetResiliencePipeline.Api.Controllers;

/// <summary>
/// REST API controller for metrics and monitoring.
/// Provides endpoints for retrieving execution statistics and health metrics.
/// </summary>
public class MetricsController
{
    private readonly ResiliencyPipelineService _pipelineService;
    private readonly ExecutionHistoryRepository _historyRepository;

    public MetricsController(ResiliencyPipelineService pipelineService, ExecutionHistoryRepository historyRepository)
    {
        _pipelineService = pipelineService;
        _historyRepository = historyRepository;
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
                AverageExecutionTimeMs = stats.TotalExecutions > 0 ? 50 : 0 // Simplified
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
            var dtos = policies.Select(p => new PolicyMetricsDto
            {
                PolicyId = p.Id,
                PolicyName = p.Name,
                Type = p.GetType().Name,
                IsEnabled = p.IsEnabled,
                ExecutionCount = 0, // Would come from repository
                SuccessCount = 0,
                FailureCount = 0
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
                >= 95 => "Healthy",
                >= 80 => "Degraded",
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
    /// GET /api/metrics/history - Retrieves execution history.
    /// </summary>
    public async Task<ApiResponse<List<ExecutionRecordDto>>> GetExecutionHistoryAsync(int limit = 100)
    {
        try
        {
            // Simplified - would fetch from repository
            var records = new List<ExecutionRecordDto>();

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
}

/// <summary>
/// Pipeline-level metrics data transfer object.
/// </summary>
public class PipelineMetricsDto
{
    public string PipelineId { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public long TotalExecutions { get; set; }
    public long SuccessfulExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public int PolicyCount { get; set; }
    public double AverageExecutionTimeMs { get; set; }
}

/// <summary>
/// Per-policy metrics data transfer object.
/// </summary>
public class PolicyMetricsDto
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public long ExecutionCount { get; set; }
    public long SuccessCount { get; set; }
    public long FailureCount { get; set; }
    public double SuccessRate => ExecutionCount > 0 ? (SuccessCount * 100.0) / ExecutionCount : 0;
}

/// <summary>
/// Health status data transfer object.
/// </summary>
public class HealthStatusDto
{
    public string Status { get; set; } = string.Empty;
    public double SuccessRate { get; set; }
    public long TotalExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public DateTime LastCheckTime { get; set; }
    public Dictionary<string, string> Details { get; set; } = new();
}

/// <summary>
/// Execution record data transfer object.
/// </summary>
public class ExecutionRecordDto
{
    public string Id { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public long ExecutionTimeMs { get; set; }
    public DateTime ExecutedAt { get; set; }
    public string? ErrorMessage { get; set; }
}
