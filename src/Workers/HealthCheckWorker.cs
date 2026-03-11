#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Events;

namespace DotNetResiliencePipeline.Workers;

/// <summary>
/// Background worker that periodically checks pipeline health.
/// Monitors policy health, detects degradation, and publishes health events.
/// </summary>
public class HealthCheckWorker
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
        return successRate switch
        {
            >= HealthyThreshold * 100 => "Healthy",
            >= DegradedThreshold * 100 => "Degraded",
            _ => "Unhealthy"
        };
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
}

/// <summary>
/// Status of the health check worker.
/// </summary>
public class HealthCheckStatus
{
    public bool IsRunning { get; set; }
    public DateTime LastCheckTime { get; set; }
    public double PipelineSuccessRate { get; set; }
    public string OverallHealth { get; set; } = string.Empty;
    public int TotalPolicies { get; set; }
    public long TotalExecutions { get; set; }
}
