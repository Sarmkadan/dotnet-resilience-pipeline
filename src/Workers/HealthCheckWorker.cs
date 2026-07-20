#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Events;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Workers;

/// <summary>
/// Background worker that periodically checks pipeline health.
/// Monitors policy health, detects degradation, and publishes health events.
/// </summary>
public sealed class HealthCheckWorker
{
    private readonly ResiliencyPipelineService _pipelineService;
    private readonly ResiliencyEventPublisher _eventPublisher;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _workerTask;

    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(30);
    public double HealthyThreshold { get; set; } = 0.95; // 95% success rate
    public double DegradedThreshold { get; set; } = 0.80; // 80% success rate
    public bool IsRunning { get; private set; }

    public HealthCheckWorker(ResiliencyPipelineService pipelineService, ResiliencyEventPublisher eventPublisher)
    {
        _pipelineService = pipelineService;
        _eventPublisher = eventPublisher;
    }

    /// <summary>
    /// Starts the health check worker.
    /// </summary>
    public void Start()
    {
        if (IsRunning)
            return;

        IsRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _workerTask = RunHealthChecksAsync(_cancellationTokenSource.Token);
    }

    /// <summary>
    /// Stops the health check worker.
    /// </summary>
    public async Task StopAsync()
    {
        if (!IsRunning)
            return;

        IsRunning = false;
        _cancellationTokenSource?.Cancel();

        if (_workerTask is not null)
            await _workerTask;

        _cancellationTokenSource?.Dispose();
    }

    private async Task RunHealthChecksAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                await PerformHealthCheckAsync();
                await Task.Delay(CheckInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in health check: {ex.Message}");
            }
        }
    }

    private async Task PerformHealthCheckAsync()
    {
        var stats = _pipelineService.GetStatistics();
        var policies = _pipelineService.GetAllPolicies();

        foreach (var policy in policies)
        {
            var healthStatus = DetermineHealth(stats.SuccessRate);

            // Publish health changed event if status changed
            await _eventPublisher.PublishAsync(new PolicyHealthChangedEvent
            {
                PolicyName = policy.Name,
                NewHealth = healthStatus,
                SuccessRate = stats.SuccessRate,
                SourcePolicy = policy.Name
            });
        }
    }

    private string DetermineHealth(double successRate)
    {
        if (successRate >= HealthyThreshold * 100)
            return "Healthy";
        if (successRate >= DegradedThreshold * 100)
            return "Degraded";
        return "Unhealthy";
    }

    /// <summary>
    /// Gets current health check status.
    /// </summary>
    public HealthCheckStatus GetStatus()
    {
        var stats = _pipelineService.GetStatistics();

        return new HealthCheckStatus
        {
            IsRunning = IsRunning,
            LastCheckTime = DateTime.UtcNow,
            PipelineSuccessRate = stats.SuccessRate,
            OverallHealth = DetermineHealth(stats.SuccessRate),
            TotalPolicies = stats.PolicyCount,
            TotalExecutions = stats.TotalExecutions
        };
    }

    /// <summary>
    /// Generates a comprehensive health report aggregating all policy health statuses.
    /// </summary>
    /// <returns>HealthReport containing aggregated health metrics</returns>
    public HealthReport GenerateHealthReport()
    {
        var pipelineStats = _pipelineService.GetStatistics();
        var allPolicies = _pipelineService.GetAllPolicies();

        var report = new HealthReport
        {
            IsRunning = IsRunning,
            GeneratedAt = DateTime.UtcNow,
            PipelineSuccessRate = pipelineStats.SuccessRate,
            OverallStatus = DetermineHealth(pipelineStats.SuccessRate),
            TotalExecutions = pipelineStats.TotalExecutions,
            SuccessfulExecutions = pipelineStats.SuccessfulExecutions,
            FailedExecutions = pipelineStats.FailedExecutions,
            TotalPolicies = allPolicies.Count
        };

        // Count policy health statuses
        int healthyCount = 0;
        int degradedCount = 0;
        int unhealthyCount = 0;

        foreach (var policy in allPolicies)
        {
            var policySnapshot = policy.GetSnapshot();
            var healthStatus = DetermineHealth(policySnapshot.SuccessRate);

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

        // Add additional metrics from pipeline statistics
        report.AdditionalMetrics["PipelineId"] = pipelineStats.PipelineId;
        report.AdditionalMetrics["CreatedAt"] = pipelineStats.CreatedAt;
        report.AdditionalMetrics["Thresholds"] = new Dictionary<string, double>
        {
            { "Healthy", HealthyThreshold * 100 },
            { "Degraded", DegradedThreshold * 100 }
        };

        return report;
    }
}

/// <summary>
/// Status of the health check worker.
/// </summary>
public sealed class HealthCheckStatus
{
    public bool IsRunning { get; set; }
    public DateTime LastCheckTime { get; set; }
    public double PipelineSuccessRate { get; set; }
    public string OverallHealth { get; set; } = string.Empty;
    public int TotalPolicies { get; set; }
    public long TotalExecutions { get; set; }
}

/// <summary>
/// Aggregated health report for the entire pipeline including all policies.
/// </summary>
public sealed class HealthReport
{
    public bool IsRunning { get; set; }
    public DateTime GeneratedAt { get; set; }
    public string OverallStatus { get; set; } = string.Empty;
    public double PipelineSuccessRate { get; set; }
    public long TotalExecutions { get; set; }
    public long SuccessfulExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public int TotalPolicies { get; set; }
    public int HealthyPolicies { get; set; }
    public int DegradedPolicies { get; set; }
    public int UnhealthyPolicies { get; set; }
    public List<PolicyHealthStatus> PolicyStatuses { get; set; } = new();
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Health status for an individual policy.
/// </summary>
public sealed class PolicyHealthStatus
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string PolicyType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public string HealthStatus { get; set; } = string.Empty;
    public double SuccessRate { get; set; }
    public long TotalExecutions { get; set; }
    public long SuccessfulExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public DateTime LastCheckTime { get; set; }
}