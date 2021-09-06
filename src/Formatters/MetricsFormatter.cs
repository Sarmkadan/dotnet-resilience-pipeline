#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Formatters;

/// <summary>
/// Formats performance metrics for human-readable console output.
/// Provides colored ASCII tables, progress bars, and summary statistics.
/// </summary>
public class MetricsFormatter
{
    private const string Horizontal = "─";
    private const string Vertical = "│";
    private const string Corner = "┼";

    /// <summary>
    /// Formats metrics as a readable ASCII table.
    /// </summary>
    public string FormatMetricsTable(List<PerformanceMetrics> metrics)
    {
        var sb = new StringBuilder();

        sb.AppendLine("╔════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                    PERFORMANCE METRICS REPORT                          ║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        foreach (var metric in metrics)
        {
            sb.AppendLine($"Policy: {metric.PolicyName}");
            sb.AppendLine($"  Executions: {metric.TotalExecutions} (✓ {metric.SuccessfulExecutions} | ✗ {metric.FailedExecutions})");
            sb.AppendLine($"  Success Rate: {metric.SuccessRate:F2}% {GetSuccessRateBar(metric.SuccessRate)}");
            sb.AppendLine($"  Avg Duration: {metric.AverageDurationMs:F2}ms");
            sb.AppendLine($"  Percentiles: P50={metric.P50}ms, P90={metric.P90}ms, P99={metric.P99}ms");
            sb.AppendLine($"  Throughput: {metric.ThroughputPerSecond:F2} req/sec");
            sb.AppendLine();
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats aggregated metrics report.
    /// </summary>
    public string FormatAggregatedMetrics(AggregatedMetrics metrics)
    {
        var sb = new StringBuilder();

        sb.AppendLine("╔════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                   AGGREGATED METRICS REPORT                           ║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        sb.AppendLine($"Time Window: {FormatTimeSpan(metrics.TimeWindow)}");
        sb.AppendLine($"Snapshots Analyzed: {metrics.SnapshotCount}");
        sb.AppendLine();

        sb.AppendLine($"Success Rate:");
        sb.AppendLine($"  Average: {metrics.AverageSuccessRate:F2}%");
        sb.AppendLine($"  Min: {metrics.MinSuccessRate:F2}%");
        sb.AppendLine($"  Max: {metrics.MaxSuccessRate:F2}%");
        sb.AppendLine();

        sb.AppendLine($"Execution Time:");
        sb.AppendLine($"  Average: {metrics.AverageExecutionTimeMs:F2}ms");
        sb.AppendLine($"  Peak: {metrics.PeakExecutions} executions");
        sb.AppendLine();

        sb.AppendLine($"Total Executions: {metrics.TotalExecutions}");

        return sb.ToString();
    }

    /// <summary>
    /// Formats trend analysis results.
    /// </summary>
    public string FormatTrend(MetricsTrend trend)
    {
        var sb = new StringBuilder();

        sb.AppendLine("╔════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine($"║                    TREND ANALYSIS - {trend.MetricType.ToUpper()}                   ║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        sb.AppendLine($"Direction: {trend.Direction}");
        sb.AppendLine($"Change: {trend.ChangePercentage:+0.00;-0.00;0.00}%");
        sb.AppendLine($"Data Points: {trend.DataPoints}");
        sb.AppendLine();

        sb.AppendLine($"Values:");
        sb.AppendLine($"  Previous: {trend.Previous:F2}");
        sb.AppendLine($"  Current: {trend.Current:F2}");
        sb.AppendLine();

        if (trend.IsAnomaly)
            sb.AppendLine("⚠ ANOMALY DETECTED - Significant change detected!");
        else
            sb.AppendLine("✓ Normal trend observed");

        return sb.ToString();
    }

    /// <summary>
    /// Formats health status.
    /// </summary>
    public string FormatHealthStatus(string status, double successRate, long totalExecutions)
    {
        var sb = new StringBuilder();
        var statusIcon = status switch
        {
            "Healthy" => "✓",
            "Degraded" => "⚠",
            "Critical" => "✗",
            _ => "?"
        };

        sb.AppendLine("╔════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                      HEALTH STATUS REPORT                              ║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        sb.AppendLine($"{statusIcon} Status: {status}");
        sb.AppendLine($"  Success Rate: {successRate:F2}% {GetSuccessRateBar(successRate)}");
        sb.AppendLine($"  Total Executions: {totalExecutions}");
        sb.AppendLine();

        if (status == "Healthy")
            sb.AppendLine("All systems operating normally.");
        else if (status == "Degraded")
            sb.AppendLine("Performance degradation detected. Monitor for potential issues.");
        else
            sb.AppendLine("Critical issues detected. Immediate attention required.");

        return sb.ToString();
    }

    /// <summary>
    /// Formats a comparison report.
    /// </summary>
    public string FormatComparison(PeriodComparison comparison)
    {
        var sb = new StringBuilder();

        sb.AppendLine("╔════════════════════════════════════════════════════════════════════════╗");
        sb.AppendLine("║                    PERIOD COMPARISON REPORT                            ║");
        sb.AppendLine("╚════════════════════════════════════════════════════════════════════════╝");
        sb.AppendLine();

        sb.AppendLine($"Period 1: {FormatTimeSpan(comparison.Period1)}");
        sb.AppendLine($"  Success Rate: {comparison.Metrics1.AverageSuccessRate:F2}%");
        sb.AppendLine($"  Avg Duration: {comparison.Metrics1.AverageExecutionTimeMs:F2}ms");
        sb.AppendLine();

        sb.AppendLine($"Period 2: {FormatTimeSpan(comparison.Period2)}");
        sb.AppendLine($"  Success Rate: {comparison.Metrics2.AverageSuccessRate:F2}%");
        sb.AppendLine($"  Avg Duration: {comparison.Metrics2.AverageExecutionTimeMs:F2}ms");
        sb.AppendLine();

        sb.AppendLine($"Changes:");
        var srDiff = comparison.SuccessRateDifference;
        var srSymbol = srDiff > 0 ? "↑" : srDiff < 0 ? "↓" : "→";
        sb.AppendLine($"  Success Rate: {srSymbol} {Math.Abs(srDiff):+0.00;-0.00;0.00}%");

        var etDiff = comparison.ExecutionTimeDifference;
        var etSymbol = etDiff > 0 ? "↑" : etDiff < 0 ? "↓" : "→";
        sb.AppendLine($"  Execution Time: {etSymbol} {Math.Abs(etDiff):+0.00;-0.00;0.00}ms");
        sb.AppendLine();

        sb.AppendLine(comparison.IsImproving ? "✓ Overall improvement detected" : "✗ Performance degradation detected");

        return sb.ToString();
    }

    private string GetSuccessRateBar(double rate, int width = 20)
    {
        var filled = (int)((rate / 100) * width);
        var bar = new string('█', filled) + new string('░', width - filled);
        return $"[{bar}]";
    }

    private string FormatTimeSpan(TimeSpan ts)
    {
        if (ts.TotalSeconds < 60)
            return $"{ts.TotalSeconds:F1}s";
        if (ts.TotalMinutes < 60)
            return $"{ts.TotalMinutes:F1}m";
        return $"{ts.TotalHours:F1}h";
    }
}
