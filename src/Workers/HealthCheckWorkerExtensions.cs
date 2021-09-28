#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Events;

namespace DotNetResiliencePipeline.Workers;

/// <summary>
/// Extension methods for <see cref="HealthCheckWorker"/> providing additional functionality
/// for health monitoring, logging, and integration scenarios.
/// </summary>
public static class HealthCheckWorkerExtensions
{
    /// <summary>
    /// Creates a new health check worker with the specified configuration.
    /// </summary>
    /// <param name="pipelineService">The pipeline service to monitor</param>
    /// <param name="eventPublisher">The event publisher for health events</param>
    /// <param name="checkInterval">Interval between health checks</param>
    /// <param name="healthyThreshold">Success rate threshold for healthy status (0-1)</param>
    /// <param name="degradedThreshold">Success rate threshold for degraded status (0-1)</param>
    /// <returns>A configured health check worker instance</returns>
    public static HealthCheckWorker CreateConfigured(
        this ResiliencyPipelineService pipelineService,
        ResiliencyEventPublisher eventPublisher,
        TimeSpan checkInterval,
        double healthyThreshold = 0.95,
        double degradedThreshold = 0.80)
    {
        var worker = new HealthCheckWorker(pipelineService, eventPublisher)
        {
            CheckInterval = checkInterval,
            HealthyThreshold = healthyThreshold,
            DegradedThreshold = degradedThreshold
        };

        return worker;
    }

    /// <summary>
    /// Checks if the pipeline is currently in a healthy state (success rate >= HealthyThreshold).
    /// </summary>
    /// <param name="worker">The health check worker</param>
    /// <returns>True if the pipeline is healthy, false otherwise</returns>
    public static bool IsHealthy(this HealthCheckWorker worker)
    {
        var status = worker.GetStatus();
        return status.OverallHealth.Equals("Healthy", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the pipeline is in a degraded state (DegradedThreshold <= success rate < HealthyThreshold).
    /// </summary>
    /// <param name="worker">The health check worker</param>
    /// <returns>True if the pipeline is degraded, false otherwise</returns>
    public static bool IsDegraded(this HealthCheckWorker worker)
    {
        var status = worker.GetStatus();
        return status.OverallHealth.Equals("Degraded", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Checks if the pipeline is in an unhealthy state (success rate < DegradedThreshold).
    /// </summary>
    /// <param name="worker">The health check worker</param>
    /// <returns>True if the pipeline is unhealthy, false otherwise</returns>
    public static bool IsUnhealthy(this HealthCheckWorker worker)
    {
        var status = worker.GetStatus();
        return status.OverallHealth.Equals("Unhealthy", StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Gets the health status as a formatted string for logging or display purposes.
    /// </summary>
    /// <param name="worker">The health check worker</param>
    /// <returns>Formatted health status string</returns>
    public static string GetHealthStatusString(this HealthCheckWorker worker)
    {
        var status = worker.GetStatus();
        return $"Health Status: {status.OverallHealth} | Success Rate: {status.PipelineSuccessRate:P1} | " +
               $"Policies: {status.TotalPolicies} | Executions: {status.TotalExecutions:N0} | " +
               $"Last Check: {status.LastCheckTime:yyyy-MM-dd HH:mm:ss}";
    }

    /// <summary>
    /// Waits for the health check worker to reach a stable state (either Healthy or Unhealthy).
    /// </summary>
    /// <param name="worker">The health check worker</param>
    /// <param name="timeout">Maximum time to wait for stable state</param>
    /// <param name="stableCheckInterval">Interval between stability checks</param>
    /// <returns>True if stable state was reached within timeout, false otherwise</returns>
    public static async Task<bool> WaitForStableStateAsync(
        this HealthCheckWorker worker,
        TimeSpan timeout,
        TimeSpan? stableCheckInterval = null)
    {
        var checkInterval = stableCheckInterval ?? TimeSpan.FromSeconds(1);
        var endTime = DateTime.UtcNow.Add(timeout);

        while (DateTime.UtcNow < endTime)
        {
            var status = worker.GetStatus();
            var isStable = status.OverallHealth is "Healthy" or "Unhealthy";

            if (isStable)
                return true;

            await Task.Delay(checkInterval);
        }

        return false;
    }

    /// <summary>
    /// Gets the health check statistics as a formatted string.
    /// </summary>
    /// <param name="worker">The health check worker</param>
    /// <returns>Formatted statistics string</returns>
    public static string GetStatisticsString(this HealthCheckWorker worker)
    {
        var status = worker.GetStatus();
        return $"Pipeline Statistics:\n" +
               $"- Success Rate: {status.PipelineSuccessRate:P2}\n" +
               $"- Total Executions: {status.TotalExecutions:N0}\n" +
               $"- Total Policies: {status.TotalPolicies}\n" +
               $"- Health Status: {status.OverallHealth}\n" +
               $"- Check Time: {status.LastCheckTime:yyyy-MM-dd HH:mm:ss}";
    }
}