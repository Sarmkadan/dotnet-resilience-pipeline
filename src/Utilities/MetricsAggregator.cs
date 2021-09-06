#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Aggregates metrics across multiple policies and provides system-wide analytics.
/// Supports time-windowed metrics and trend analysis.
/// </summary>
public class MetricsAggregator
{
    private readonly List<MetricsSnapshot> _snapshots = new();
    private readonly object _lockObj = new object();
    public int MaxSnapshots { get; set; } = 1000;

    /// <summary>
    /// Records a metrics snapshot at a point in time.
    /// </summary>
    public void RecordSnapshot(MetricsSnapshot snapshot)
    {
        lock (_lockObj)
        {
            _snapshots.Add(snapshot);

            // Keep bounded history
            if (_snapshots.Count > MaxSnapshots)
                _snapshots.RemoveAt(0);
        }
    }

    /// <summary>
    /// Gets aggregated metrics for a time window.
    /// </summary>
    public AggregatedMetrics GetAggregatedMetrics(TimeSpan timeWindow)
    {
        lock (_lockObj)
        {
            var cutoff = DateTime.UtcNow - timeWindow;
            var relevantSnapshots = _snapshots.Where(s => s.Timestamp >= cutoff).ToList();

            if (relevantSnapshots.Count == 0)
                return new AggregatedMetrics();

            var aggregated = new AggregatedMetrics
            {
                TimeWindow = timeWindow,
                SnapshotCount = relevantSnapshots.Count,
                AverageSuccessRate = relevantSnapshots.Average(s => s.SuccessRate),
                AverageExecutionTimeMs = relevantSnapshots.Average(s => s.AverageExecutionTimeMs),
                TotalExecutions = relevantSnapshots.Sum(s => s.TotalExecutions),
                PeakExecutions = relevantSnapshots.Max(s => s.TotalExecutions),
                MinSuccessRate = relevantSnapshots.Min(s => s.SuccessRate),
                MaxSuccessRate = relevantSnapshots.Max(s => s.SuccessRate)
            };

            return aggregated;
        }
    }

    /// <summary>
    /// Analyzes trends in metrics over time.
    /// </summary>
    public MetricsTrend AnalyzeTrend(TimeSpan timeWindow, string metricType = "SuccessRate")
    {
        lock (_lockObj)
        {
            var cutoff = DateTime.UtcNow - timeWindow;
            var relevantSnapshots = _snapshots.Where(s => s.Timestamp >= cutoff).OrderBy(s => s.Timestamp).ToList();

            var trend = new MetricsTrend
            {
                MetricType = metricType,
                TimeWindow = timeWindow,
                DataPoints = relevantSnapshots.Count
            };

            if (relevantSnapshots.Count < 2)
                return trend;

            // Calculate trend direction
            var values = metricType switch
            {
                "SuccessRate" => relevantSnapshots.Select(s => s.SuccessRate).ToList(),
                "ExecutionTime" => relevantSnapshots.Select(s => s.AverageExecutionTimeMs).ToList(),
                _ => new List<double>()
            };

            if (values.Count > 1)
            {
                var firstHalf = values.Take(values.Count / 2).Average();
                var secondHalf = values.Skip(values.Count / 2).Average();

                trend.Direction = secondHalf > firstHalf ? "Increasing" : "Decreasing";
                trend.ChangePercentage = ((secondHalf - firstHalf) / firstHalf) * 100;
                trend.Current = values.Last();
                trend.Previous = values.First();
                trend.IsAnomaly = Math.Abs(trend.ChangePercentage) > 20;
            }

            return trend;
        }
    }

    /// <summary>
    /// Compares performance between different time periods.
    /// </summary>
    public PeriodComparison ComparePeriods(TimeSpan period1, TimeSpan period2)
    {
        var metrics1 = GetAggregatedMetrics(period1);
        var metrics2 = GetAggregatedMetrics(period2);

        return new PeriodComparison
        {
            Period1 = period1,
            Period2 = period2,
            Metrics1 = metrics1,
            Metrics2 = metrics2,
            SuccessRateDifference = metrics2.AverageSuccessRate - metrics1.AverageSuccessRate,
            ExecutionTimeDifference = metrics2.AverageExecutionTimeMs - metrics1.AverageExecutionTimeMs,
            IsImproving = (metrics2.AverageSuccessRate - metrics1.AverageSuccessRate) > 0
        };
    }

    /// <summary>
    /// Gets performance report for a metric range.
    /// </summary>
    public PerformanceReport GenerateReport(TimeSpan timeWindow)
    {
        var aggregated = GetAggregatedMetrics(timeWindow);
        var trend = AnalyzeTrend(timeWindow);

        var report = new PerformanceReport
        {
            TimeWindow = timeWindow,
            GeneratedAt = DateTime.UtcNow,
            AggregatedMetrics = aggregated,
            Trend = trend
        };

        // Add health assessment
        report.HealthStatus = aggregated.AverageSuccessRate switch
        {
            >= 95 => "Healthy",
            >= 85 => "Acceptable",
            >= 70 => "Degraded",
            _ => "Critical"
        };

        return report;
    }

    /// <summary>
    /// Clears all recorded snapshots.
    /// </summary>
    public void Clear()
    {
        lock (_lockObj)
        {
            _snapshots.Clear();
        }
    }
}

/// <summary>
/// Snapshot of metrics at a specific point in time.
/// </summary>
public class MetricsSnapshot
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public long TotalExecutions { get; set; }
    public long SuccessfulExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public int ActivePolicies { get; set; }
}

/// <summary>
/// Aggregated metrics across a time window.
/// </summary>
public class AggregatedMetrics
{
    public TimeSpan TimeWindow { get; set; }
    public int SnapshotCount { get; set; }
    public double AverageSuccessRate { get; set; }
    public double AverageExecutionTimeMs { get; set; }
    public long TotalExecutions { get; set; }
    public long PeakExecutions { get; set; }
    public double MinSuccessRate { get; set; }
    public double MaxSuccessRate { get; set; }
}

/// <summary>
/// Trend analysis for a specific metric.
/// </summary>
public class MetricsTrend
{
    public string MetricType { get; set; } = string.Empty;
    public TimeSpan TimeWindow { get; set; }
    public int DataPoints { get; set; }
    public string Direction { get; set; } = string.Empty;
    public double ChangePercentage { get; set; }
    public double Current { get; set; }
    public double Previous { get; set; }
    public bool IsAnomaly { get; set; }
}

/// <summary>
/// Comparison of metrics between two time periods.
/// </summary>
public class PeriodComparison
{
    public TimeSpan Period1 { get; set; }
    public TimeSpan Period2 { get; set; }
    public AggregatedMetrics Metrics1 { get; set; } = new();
    public AggregatedMetrics Metrics2 { get; set; } = new();
    public double SuccessRateDifference { get; set; }
    public double ExecutionTimeDifference { get; set; }
    public bool IsImproving { get; set; }
}

/// <summary>
/// Comprehensive performance report.
/// </summary>
public class PerformanceReport
{
    public TimeSpan TimeWindow { get; set; }
    public DateTime GeneratedAt { get; set; }
    public AggregatedMetrics AggregatedMetrics { get; set; } = new();
    public MetricsTrend Trend { get; set; } = new();
    public string HealthStatus { get; set; } = string.Empty;
    public List<string> Recommendations { get; set; } = new();
}
