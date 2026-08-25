#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Workers;

/// <summary>
/// Background worker that periodically collects and aggregates metrics.
/// Maintains time-series data for trend analysis and reporting.
/// </summary>
public sealed class MetricsCollectorWorker
{
    private readonly ResiliencyPipelineService _pipelineService;
    private readonly MetricsAggregator _aggregator;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _workerTask;

    public TimeSpan CollectionInterval { get; set; } = TimeSpan.FromSeconds(10);
    public bool IsRunning { get; private set; }
    public int TotalCollections { get; private set; }

    public MetricsCollectorWorker(ResiliencyPipelineService pipelineService, MetricsAggregator aggregator)
    {
        ArgumentNullException.ThrowIfNull(pipelineService);
        ArgumentNullException.ThrowIfNull(aggregator);
        _pipelineService = pipelineService;
        _aggregator = aggregator;
    }

    /// <summary>
    /// Starts the metrics collector worker.
    /// </summary>
    public void Start()
    {
        if (IsRunning)
            return;

        IsRunning = true;
        _cancellationTokenSource = new CancellationTokenSource();
        _workerTask = RunCollectionAsync(_cancellationTokenSource.Token);
    }

    /// <summary>
    /// Stops the metrics collector worker.
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

    private async Task RunCollectionAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            try
            {
                CollectMetrics();
                TotalCollections++;
                await Task.Delay(CollectionInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Error in metrics collection: {ex.Message}");
            }
        }
    }

    private void CollectMetrics()
    {
        var stats = _pipelineService.GetStatistics();

        var snapshot = new MetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            TotalExecutions = stats.TotalExecutions,
            SuccessfulExecutions = stats.SuccessfulExecutions,
            FailedExecutions = stats.FailedExecutions,
            SuccessRate = stats.SuccessRate,
            AverageExecutionTimeMs = 0, // Would calculate from actual execution data
            ActivePolicies = stats.PolicyCount
        };

        _aggregator.RecordSnapshot(snapshot);
    }

    /// <summary>
    /// Gets current metrics collector status.
    /// </summary>
    public MetricsCollectorStatus GetStatus()
    {
        var aggregated = _aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(5));

        return new MetricsCollectorStatus
        {
            IsRunning = IsRunning,
            TotalCollections = TotalCollections,
            LastCollectionTime = DateTime.UtcNow,
            RecentMetrics = aggregated
        };
    }

    /// <summary>
    /// Gets metrics for a specific time range.
    /// </summary>
    public AggregatedMetrics GetMetricsForTimeRange(TimeSpan timeRange)
    {
        return _aggregator.GetAggregatedMetrics(timeRange);
    }

    /// <summary>
    /// Gets trend analysis.
    /// </summary>
    public MetricsTrend GetTrendAnalysis(TimeSpan timeWindow)
    {
        return _aggregator.AnalyzeTrend(timeWindow, "SuccessRate");
    }

    /// <summary>
    /// Generates a performance report.
    /// </summary>
    public PerformanceReport GenerateReport(TimeSpan timeWindow)
    {
        return _aggregator.GenerateReport(timeWindow);
    }

    /// <summary>
    /// Returns a concise, informative representation of the metrics collector worker.
    /// </summary>
    public override string ToString()
    {
        var status = GetStatus();

        return $"MetricsCollectorWorker {{ CollectionInterval = {CollectionInterval}, IsRunning = {IsRunning}, TotalCollections = {TotalCollections}, LastCollectionTime = {status.LastCollectionTime}, RecentMetrics = {status.RecentMetrics} }}";
    }
}

/// <summary>
/// Status of the metrics collector worker.
/// </summary>
public sealed class MetricsCollectorStatus
{
    public bool IsRunning { get; set; }
    public int TotalCollections { get; set; }
    public DateTime LastCollectionTime { get; set; }
    public AggregatedMetrics RecentMetrics { get; set; } = new();
}
