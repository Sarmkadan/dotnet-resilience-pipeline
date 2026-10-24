#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Events;
using DotNetResiliencePipeline.Services;

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

    private const int DefaultCheckIntervalSeconds = 30;
    private const double DefaultHealthyThreshold = 0.95;
    private const double DefaultDegradedThreshold = 0.80;
    private const int PercentageMultiplier = 100;

    /// <summary>
    /// Interval between health checks.
    /// </summary>
    public TimeSpan CheckInterval { get; set; } = TimeSpan.FromSeconds(DefaultCheckIntervalSeconds);

    /// <summary>
    /// Success rate (0-1) at or above which the pipeline is considered healthy.
    /// </summary>
    public double HealthyThreshold { get; set; } = DefaultHealthyThreshold;

    /// <summary>
    /// Success rate (0-1) at or above which the pipeline is considered degraded rather than unhealthy.
    /// </summary>
    public double DegradedThreshold { get; set; } = DefaultDegradedThreshold;

    /// <summary>
    /// Whether the health check loop is currently running.
    /// </summary>
    public bool IsRunning { get; private set; }

    /// <summary>
    /// Initializes a new health check worker.
    /// </summary>
    /// <param name="pipelineService">The pipeline service to monitor.</param>
    /// <param name="eventPublisher">The publisher used to emit health events.</param>
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
        if (successRate >= DefaultHealthyThreshold * PercentageMultiplier)
            return "Healthy";
        if (successRate >= DefaultDegradedThreshold * PercentageMultiplier)
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
    /// Returns a concise string representation of the health check worker state.
    /// </summary>
    public override string ToString()
    {
        var status = GetStatus();
        return $"HealthCheckWorker {{ CheckInterval = {CheckInterval}, HealthyThreshold = {HealthyThreshold}, DegradedThreshold = {DegradedThreshold}, IsRunning = {IsRunning}, LastCheckTime = {status.LastCheckTime}, PipelineSuccessRate = {status.PipelineSuccessRate} }}";
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
            { "Healthy", DefaultHealthyThreshold * PercentageMultiplier },
            { "Degraded", DefaultDegradedThreshold * PercentageMultiplier }
        };

        return report;
    }
}

/// <summary>
/// Status of the health check worker.
/// </summary>
public sealed class HealthCheckStatus
{
    /// <summary>
    /// Whether the health check worker is running.
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// UTC time of the most recent health check.
    /// </summary>
    public DateTime LastCheckTime { get; set; }

    /// <summary>
    /// Pipeline success rate as a percentage (0-100).
    /// </summary>
    public double PipelineSuccessRate { get; set; }

    /// <summary>
    /// Overall health label: Healthy, Degraded or Unhealthy.
    /// </summary>
    public string OverallHealth { get; set; } = string.Empty;

    /// <summary>
    /// Number of policies registered with the pipeline.
    /// </summary>
    public int TotalPolicies { get; set; }

    /// <summary>
    /// Total executions performed by the pipeline.
    /// </summary>
    public long TotalExecutions { get; set; }
}

/// <summary>
/// Aggregated health report for the entire pipeline including all policies.
/// </summary>
public sealed class HealthReport
{
    /// <summary>
    /// Whether the health check worker is running.
    /// </summary>
    public bool IsRunning { get; set; }

    /// <summary>
    /// UTC time at which the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Overall health label: Healthy, Degraded or Unhealthy.
    /// </summary>
    public string OverallStatus { get; set; } = string.Empty;

    /// <summary>
    /// Pipeline success rate as a percentage (0-100).
    /// </summary>
    public double PipelineSuccessRate { get; set; }

    /// <summary>
    /// Total executions performed by the pipeline.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Successful executions performed by the pipeline.
    /// </summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>
    /// Failed executions performed by the pipeline.
    /// </summary>
    public long FailedExecutions { get; set; }

    /// <summary>
    /// Number of policies registered with the pipeline.
    /// </summary>
    public int TotalPolicies { get; set; }

    /// <summary>
    /// Number of policies classified as healthy.
    /// </summary>
    public int HealthyPolicies { get; set; }

    /// <summary>
    /// Number of policies classified as degraded.
    /// </summary>
    public int DegradedPolicies { get; set; }

    /// <summary>
    /// Number of policies classified as unhealthy.
    /// </summary>
    public int UnhealthyPolicies { get; set; }

    /// <summary>
    /// Per-policy health status entries.
    /// </summary>
    public List<PolicyHealthStatus> PolicyStatuses { get; set; } = new();

    /// <summary>
    /// Additional metrics attached to the report, keyed by name.
    /// </summary>
    public Dictionary<string, object> AdditionalMetrics { get; set; } = new();
}

/// <summary>
/// Health status for an individual policy.
/// </summary>
public sealed class PolicyHealthStatus
{
    /// <summary>
    /// Identifier of the policy.
    /// </summary>
    public string PolicyId { get; set; } = string.Empty;

    /// <summary>
    /// Friendly name of the policy.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the policy's concrete type.
    /// </summary>
    public string PolicyType { get; set; } = string.Empty;

    /// <summary>
    /// Whether the policy is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Health label for the policy: Healthy, Degraded or Unhealthy.
    /// </summary>
    public string HealthStatus { get; set; } = string.Empty;

    /// <summary>
    /// Success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Total executions tracked by the policy.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Successful executions tracked by the policy.
    /// </summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>
    /// Failed executions tracked by the policy.
    /// </summary>
    public long FailedExecutions { get; set; }

    /// <summary>
    /// UTC time of the health check that produced this status.
    /// </summary>
    public DateTime LastCheckTime { get; set; }
}
