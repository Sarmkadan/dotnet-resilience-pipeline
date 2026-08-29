#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Utilities;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Diagnostics;

namespace DotNetResiliencePipeline.Workers;

/// <summary>
/// Background worker that periodically collects and aggregates metrics.
/// Maintains time-series data for trend analysis and reporting.
/// </summary>
public sealed partial class MetricsCollectorWorker
{
    private readonly ResiliencyPipelineService _pipelineService;
    private readonly MetricsAggregator _aggregator;
    private readonly ILogger<MetricsCollectorWorker> _logger;
    private CancellationTokenSource? _cancellationTokenSource;
    private Task? _workerTask;

    public TimeSpan CollectionInterval { get; set; } = TimeSpan.FromSeconds(10);
    public bool IsRunning { get; private set; }
    public int TotalCollections { get; private set; }

    public MetricsCollectorWorker(
        ResiliencyPipelineService pipelineService,
        MetricsAggregator aggregator,
        ILogger<MetricsCollectorWorker>? logger = null)
    {
        ArgumentNullException.ThrowIfNull(pipelineService);
        ArgumentNullException.ThrowIfNull(aggregator);
        _pipelineService = pipelineService;
        _aggregator = aggregator;
        _logger = logger ?? NullLogger<MetricsCollectorWorker>.Instance;
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
        TryLog(WorkerLogs.WorkerStarted, CollectionInterval.TotalMilliseconds);
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
        TryLog(WorkerLogs.WorkerStopped, TotalCollections);
    }

    private async Task RunCollectionAsync(CancellationToken cancellationToken)
    {
        while (!cancellationToken.IsCancellationRequested)
        {
            var stopwatch = Stopwatch.StartNew();

            try
            {
                var policyCount = CollectMetrics();
                TotalCollections++;
                TryLog(WorkerLogs.CollectionCompleted, policyCount, 1, stopwatch.ElapsedMilliseconds);
                await Task.Delay(CollectionInterval, cancellationToken);
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                TryLog(WorkerLogs.CollectionFailed, "MetricsCollection", stopwatch.ElapsedMilliseconds, ex);
            }
        }
    }

    private int CollectMetrics()
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
        return stats.PolicyCount;
    }

    private void TryLog<T1>(Action<ILogger, T1, Exception?> logAction, T1 value1)
    {
        try
        {
            logAction(_logger, value1, null);
        }
        catch
        {
            // Logging must never interrupt the worker lifecycle or collection loop.
        }
    }

    private void TryLog<T1, T2, T3>(
        Action<ILogger, T1, T2, T3, Exception?> logAction,
        T1 value1,
        T2 value2,
        T3 value3,
        Exception? exception = null)
    {
        try
        {
            logAction(_logger, value1, value2, value3, exception);
        }
        catch
        {
            // Logging must never interrupt the worker lifecycle or collection loop.
        }
    }

    private void TryLog<T1, T2>(
        Action<ILogger, T1, T2, Exception?> logAction,
        T1 value1,
        T2 value2,
        Exception exception)
    {
        try
        {
            logAction(_logger, value1, value2, exception);
        }
        catch
        {
            // Logging must never interrupt the worker lifecycle or collection loop.
        }
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

    private static partial class WorkerLogs
    {
        [LoggerMessage(1, LogLevel.Information, "Metrics collector worker started with a collection interval of {CollectionIntervalMs} ms")]
        internal static partial void WorkerStarted(ILogger logger, double collectionIntervalMs, Exception? exception);

        [LoggerMessage(2, LogLevel.Information, "Metrics collector worker stopped after {TotalCollections} collections")]
        internal static partial void WorkerStopped(ILogger logger, int totalCollections, Exception? exception);

        [LoggerMessage(3, LogLevel.Debug, "Metrics collection cycle completed: {PolicyCount} policies and {MetricCount} metrics collected in {ElapsedMs} ms")]
        internal static partial void CollectionCompleted(ILogger logger, int policyCount, int metricCount, long elapsedMs, Exception? exception);

        [LoggerMessage(4, LogLevel.Error, "Metrics collection failed for {PolicyName} after {ElapsedMs} ms")]
        internal static partial void CollectionFailed(ILogger logger, string policyName, long elapsedMs, Exception? exception);
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
