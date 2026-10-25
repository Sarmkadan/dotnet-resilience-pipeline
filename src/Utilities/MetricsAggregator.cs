#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Globalization;

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Aggregates metrics across multiple policies and provides system-wide analytics.
/// Supports time-windowed metrics and trend analysis.
/// </summary>
public sealed class MetricsAggregator
{
    private const int DefaultMaxSnapshots = 1000;
    private const double AnomalyChangePercentageThreshold = 20;
    private const double HealthySuccessRate = 95;
    private const double AcceptableSuccessRate = 85;
    private const double DegradedSuccessRate = 70;

    private readonly List<MetricsSnapshot> _snapshots = new();
    private readonly object _lockObj = new object();

    /// <summary>
    /// Gets or sets the maximum number of snapshots retained; the oldest is dropped when exceeded.
    /// </summary>
    public int MaxSnapshots { get; set; } = DefaultMaxSnapshots;

    /// <summary>
    /// Records a metrics snapshot at a point in time.
    /// </summary>
    public void RecordSnapshot(MetricsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

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
        ArgumentException.ThrowIfNullOrEmpty(metricType);

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
                trend.IsAnomaly = Math.Abs(trend.ChangePercentage) > AnomalyChangePercentageThreshold;
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
            >= HealthySuccessRate => "Healthy",
            >= AcceptableSuccessRate => "Acceptable",
            >= DegradedSuccessRate => "Degraded",
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

    /// <summary>
    /// Computes latency percentiles (P50, P90, P99) over recorded samples.
    /// Returns zeros if no samples are available.
    /// </summary>
    /// <returns>A struct containing the computed percentiles.</returns>
    public LatencyPercentiles GetLatencyPercentiles()
    {
        lock (_lockObj)
        {
            if (_snapshots.Count == 0)
            {
                return new LatencyPercentiles();
            }

            // Extract all average execution times from snapshots
            var executionTimes = _snapshots
                .Select(s => s.AverageExecutionTimeMs)
                .OrderBy(time => time)
                .ToList();

            var count = executionTimes.Count;

            return new LatencyPercentiles
            {
                P50 = GetPercentile(executionTimes, 50, count),
                P90 = GetPercentile(executionTimes, 90, count),
                P99 = GetPercentile(executionTimes, 99, count)
            };
        }
    }

    /// <summary>
    /// Helper method to compute a percentile from an ordered list.
    /// </summary>
    private static double GetPercentile(List<double> sortedValues, int percentile, int count)
    {
        if (count == 0)
        {
            return 0;
        }

        // Calculate index using nearest-rank method
        var rank = (percentile / 100.0) * count;
        var index = (int)Math.Ceiling(rank) - 1;

        if (index < 0)
        {
            index = 0;
        }

        if (index >= count)
        {
            index = count - 1;
        }

        return sortedValues[index];
    }

    /// <summary>
    /// Returns a concise string representation of the aggregator.
    /// Includes MaxSnapshots and the most recent snapshot data (if any).
    /// </summary>
    public override string ToString()
    {
        var latest = _snapshots.Count > 0 ? _snapshots[^1] : null;
        return $"MetricsAggregator {{ MaxSnapshots = {MaxSnapshots}, Timestamp = {latest?.Timestamp:u}, TotalExecutions = {latest?.TotalExecutions ?? 0}, SuccessfulExecutions = {latest?.SuccessfulExecutions ?? 0}, FailedExecutions = {latest?.FailedExecutions ?? 0}, SuccessRate = {latest?.SuccessRate ?? 0} }}";
    }
}

/// <summary>
/// Contains computed latency percentiles (P50, P90, P99).
/// </summary>
public readonly struct LatencyPercentiles
{
    /// <summary>
    /// Gets the 50th percentile (median) execution time, in milliseconds.
    /// </summary>
    public double P50 { get; init; }

    /// <summary>
    /// Gets the 90th percentile execution time, in milliseconds.
    /// </summary>
    public double P90 { get; init; }

    /// <summary>
    /// Gets the 99th percentile execution time, in milliseconds.
    /// </summary>
    public double P99 { get; init; }

    /// <summary>
    /// Returns a concise string representation of the latency percentiles.
    /// </summary>
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"LatencyPercentiles {{ P50 = {P50}ms, P90 = {P90}ms, P99 = {P99}ms }}");
    }
}

/// <summary>
/// Snapshot of metrics at a specific point in time.
/// </summary>
public sealed class MetricsSnapshot
{
    /// <summary>
    /// Gets or sets the UTC time at which the snapshot was taken.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Gets or sets the total number of executions at snapshot time.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of successful executions at snapshot time.
    /// </summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions at snapshot time.
    /// </summary>
    public long FailedExecutions { get; set; }

    /// <summary>
    /// Gets or sets the success rate, as a percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average execution time, in milliseconds.
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the number of policies active at snapshot time.
    /// </summary>
    public int ActivePolicies { get; set; }

    /// <summary>
    /// Returns a concise string representation of the snapshot.
    /// </summary>
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"MetricsSnapshot {{ Timestamp = {Timestamp:u}, TotalExecutions = {TotalExecutions}, SuccessfulExecutions = {SuccessfulExecutions}, FailedExecutions = {FailedExecutions}, SuccessRate = {SuccessRate}, AverageExecutionTimeMs = {AverageExecutionTimeMs}, ActivePolicies = {ActivePolicies} }}");
    }
}

/// <summary>
/// Aggregated metrics across a time window.
/// </summary>
public sealed class AggregatedMetrics
{
    /// <summary>
    /// Gets or sets the time window covered by the aggregation.
    /// </summary>
    public TimeSpan TimeWindow { get; set; }

    /// <summary>
    /// Gets or sets the number of snapshots included in the aggregation.
    /// </summary>
    public int SnapshotCount { get; set; }

    /// <summary>
    /// Gets or sets the average success rate, as a percentage.
    /// </summary>
    public double AverageSuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the average execution time, in milliseconds.
    /// </summary>
    public double AverageExecutionTimeMs { get; set; }

    /// <summary>
    /// Gets or sets the total number of executions in the window.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the highest execution count observed in a single snapshot.
    /// </summary>
    public long PeakExecutions { get; set; }

    /// <summary>
    /// Gets or sets the lowest success rate observed, as a percentage.
    /// </summary>
    public double MinSuccessRate { get; set; }

    /// <summary>
    /// Gets or sets the highest success rate observed, as a percentage.
    /// </summary>
    public double MaxSuccessRate { get; set; }

    /// <summary>
    /// Returns a concise string representation of the aggregated metrics.
    /// </summary>
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"AggregatedMetrics {{ TimeWindow = {TimeWindow}, SnapshotCount = {SnapshotCount}, AverageSuccessRate = {AverageSuccessRate}, AverageExecutionTimeMs = {AverageExecutionTimeMs}, TotalExecutions = {TotalExecutions}, PeakExecutions = {PeakExecutions}, MinSuccessRate = {MinSuccessRate}, MaxSuccessRate = {MaxSuccessRate} }}");
    }
}

/// <summary>
/// Trend analysis for a specific metric.
/// </summary>
public sealed class MetricsTrend
{
    /// <summary>
    /// Gets or sets the name of the metric being analyzed.
    /// </summary>
    public string MetricType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the time window covered by the trend.
    /// </summary>
    public TimeSpan TimeWindow { get; set; }

    /// <summary>
    /// Gets or sets the number of data points used in the trend.
    /// </summary>
    public int DataPoints { get; set; }

    /// <summary>
    /// Gets or sets the direction of the trend (for example, increasing or decreasing).
    /// </summary>
    public string Direction { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the percentage change between the previous and current values.
    /// </summary>
    public double ChangePercentage { get; set; }

    /// <summary>
    /// Gets or sets the current value of the metric.
    /// </summary>
    public double Current { get; set; }

    /// <summary>
    /// Gets or sets the previous value of the metric.
    /// </summary>
    public double Previous { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the change is considered anomalous.
    /// </summary>
    public bool IsAnomaly { get; set; }

    /// <summary>
    /// Returns a concise string representation of the trend.
    /// </summary>
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"MetricsTrend {{ MetricType = {MetricType}, TimeWindow = {TimeWindow}, DataPoints = {DataPoints}, Direction = {Direction}, ChangePercentage = {ChangePercentage}, Current = {Current}, Previous = {Previous}, IsAnomaly = {IsAnomaly} }}");
    }
}

/// <summary>
/// Comparison of metrics between two time periods.
/// </summary>
public sealed class PeriodComparison
{
    /// <summary>
    /// Gets or sets the length of the first period.
    /// </summary>
    public TimeSpan Period1 { get; set; }

    /// <summary>
    /// Gets or sets the length of the second period.
    /// </summary>
    public TimeSpan Period2 { get; set; }

    /// <summary>
    /// Gets or sets the aggregated metrics for the first period.
    /// </summary>
    public AggregatedMetrics Metrics1 { get; set; } = new();

    /// <summary>
    /// Gets or sets the aggregated metrics for the second period.
    /// </summary>
    public AggregatedMetrics Metrics2 { get; set; } = new();

    /// <summary>
    /// Gets or sets the difference in success rate between the two periods.
    /// </summary>
    public double SuccessRateDifference { get; set; }

    /// <summary>
    /// Gets or sets the difference in average execution time between the two periods, in milliseconds.
    /// </summary>
    public double ExecutionTimeDifference { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether the second period shows improvement over the first.
    /// </summary>
    public bool IsImproving { get; set; }

    /// <summary>
    /// Returns a concise string representation of the period comparison.
    /// </summary>
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"PeriodComparison {{ Period1 = {Period1}, Period2 = {Period2}, SuccessRateDifference = {SuccessRateDifference}, ExecutionTimeDifference = {ExecutionTimeDifference}, IsImproving = {IsImproving} }}");
    }
}

/// <summary>
/// Comprehensive performance report.
/// </summary>
public sealed class PerformanceReport
{
    /// <summary>
    /// Gets or sets the time window covered by the report.
    /// </summary>
    public TimeSpan TimeWindow { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the report was generated.
    /// </summary>
    public DateTime GeneratedAt { get; set; }

    /// <summary>
    /// Gets or sets the aggregated metrics for the report's time window.
    /// </summary>
    public AggregatedMetrics AggregatedMetrics { get; set; } = new();

    /// <summary>
    /// Gets or sets the trend analysis included in the report.
    /// </summary>
    public MetricsTrend Trend { get; set; } = new();

    /// <summary>
    /// Gets or sets the overall health status summary.
    /// </summary>
    public string HealthStatus { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the suggested actions derived from the report.
    /// </summary>
    public List<string> Recommendations { get; set; } = new();

    /// <summary>
    /// Returns a concise string representation of the performance report.
    /// </summary>
    public override string ToString()
    {
        return string.Create(CultureInfo.InvariantCulture, $"PerformanceReport {{ TimeWindow = {TimeWindow}, GeneratedAt = {GeneratedAt:u}, HealthStatus = {HealthStatus}, AverageSuccessRate = {AggregatedMetrics.AverageSuccessRate}, TrendDirection = {Trend.Direction}, Recommendations = {Recommendations.Count} }}");
    }
}
