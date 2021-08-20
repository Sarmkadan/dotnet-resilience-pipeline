// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Formatters;

/// <summary>
/// Formats execution metrics and reports as CSV for spreadsheet analysis.
/// Supports metrics export, policy reports, and execution history.
/// </summary>
public class CsvReportFormatter
{
    private const char Delimiter = ',';
    private const string Quote = "\"";

    /// <summary>
    /// Formats pipeline metrics as CSV.
    /// </summary>
    public string FormatPipelineMetrics(PipelineStatistics stats)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Metric,Value");

        // Data
        sb.AppendLine($"Pipeline ID,{EscapeCsv(stats.PipelineId)}");
        sb.AppendLine($"Created At,{stats.CreatedAt:O}");
        sb.AppendLine($"Total Executions,{stats.TotalExecutions}");
        sb.AppendLine($"Successful Executions,{stats.SuccessfulExecutions}");
        sb.AppendLine($"Failed Executions,{stats.FailedExecutions}");
        sb.AppendLine($"Success Rate,{stats.SuccessRate:F2}%");
        sb.AppendLine($"Policy Count,{stats.PolicyCount}");

        return sb.ToString();
    }

    /// <summary>
    /// Formats policy details as CSV.
    /// </summary>
    public string FormatPolicies(List<Domain.Policies.ResiliencyPolicy> policies)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Policy Name,Type,ID,Enabled,Created At");

        // Data
        foreach (var policy in policies)
        {
            sb.AppendLine($"{EscapeCsv(policy.Name)},{policy.GetType().Name},{EscapeCsv(policy.Id)},{policy.IsEnabled},{DateTime.UtcNow:O}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats execution history as CSV.
    /// </summary>
    public string FormatExecutionHistory(List<ExecutionRecord> records)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Timestamp,Policy Name,Success,Duration Ms,Status");

        // Data
        foreach (var record in records)
        {
            var status = record.IsSuccess ? "Success" : "Failed";
            sb.AppendLine($"{record.Timestamp:O},{EscapeCsv(record.PolicyName)},{record.IsSuccess},{record.ExecutionTimeMs},{status}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats performance metrics as CSV.
    /// </summary>
    public string FormatPerformanceMetrics(List<PerformanceMetrics> metrics)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Policy Name,Total Executions,Successful,Failed,Success Rate %,Avg Duration Ms,P50,P90,P99,Throughput/Sec");

        // Data
        foreach (var metric in metrics)
        {
            sb.Append($"{EscapeCsv(metric.PolicyName)},");
            sb.Append($"{metric.TotalExecutions},");
            sb.Append($"{metric.SuccessfulExecutions},");
            sb.Append($"{metric.FailedExecutions},");
            sb.Append($"{metric.SuccessRate:F2},");
            sb.Append($"{metric.AverageDurationMs:F2},");
            sb.Append($"{metric.P50},");
            sb.Append($"{metric.P90},");
            sb.Append($"{metric.P99},");
            sb.AppendLine($"{metric.ThroughputPerSecond:F2}");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats logging data as CSV.
    /// </summary>
    public string FormatLogs(List<Middleware.LogEntry> logs)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Timestamp,Policy Name,Operation,Success,Duration Ms,Exception,Message");

        // Data
        foreach (var log in logs)
        {
            sb.Append($"{log.Timestamp:O},");
            sb.Append($"{EscapeCsv(log.PolicyName)},");
            sb.Append($"{EscapeCsv(log.OperationName)},");
            sb.Append($"{log.Success},");
            sb.Append($"{log.DurationMs},");
            sb.Append($"{EscapeCsv(log.Exception ?? "")},");
            sb.AppendLine(EscapeCsv(log.Message ?? ""));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Formats error contexts as CSV.
    /// </summary>
    public string FormatErrors(List<Middleware.ErrorContext> errors)
    {
        var sb = new StringBuilder();

        // Header
        sb.AppendLine("Timestamp,Policy Name,Exception Type,Is Recoverable,Message,Recommendation");

        // Data
        foreach (var error in errors)
        {
            sb.Append($"{error.Timestamp:O},");
            sb.Append($"{EscapeCsv(error.PolicyName)},");
            sb.Append($"{EscapeCsv(error.ExceptionType)},");
            sb.Append($"{error.IsRecoverable},");
            sb.Append($"{EscapeCsv(error.ExceptionMessage)},");
            sb.AppendLine(EscapeCsv(error.RecoveryRecommendation));
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports report to file.
    /// </summary>
    public async Task ExportToFileAsync(string content, string filePath)
    {
        await File.WriteAllTextAsync(filePath, content, Encoding.UTF8);
    }

    /// <summary>
    /// Escapes special characters in CSV fields.
    /// </summary>
    private string EscapeCsv(string field)
    {
        if (string.IsNullOrEmpty(field))
            return "";

        if (field.Contains(Delimiter) || field.Contains(Quote) || field.Contains("\n"))
            return Quote + field.Replace(Quote, Quote + Quote) + Quote;

        return field;
    }
}

/// <summary>
/// Execution record for CSV export.
/// </summary>
public class ExecutionRecord
{
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string PolicyName { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public long ExecutionTimeMs { get; set; }
}
