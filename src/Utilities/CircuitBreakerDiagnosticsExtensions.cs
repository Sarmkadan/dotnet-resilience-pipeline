#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Extension methods for <see cref="CircuitBreakerDiagnosticReport"/>.
/// </summary>
public static class CircuitBreakerDiagnosticReportExtensions
{
    /// <summary>
    /// Determines whether the circuit breaker diagnostic report has any issues.
    /// </summary>
    /// <param name="report">The circuit breaker diagnostic report.</param>
    /// <returns>True if the report contains issues; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">If report is null.</exception>
    public static bool HasIssues(this CircuitBreakerDiagnosticReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return report.HasIssues;
    }

    /// <summary>
    /// Gets the critical issues from the circuit breaker diagnostic report.
    /// </summary>
    /// <param name="report">The circuit breaker diagnostic report.</param>
    /// <returns>A list of critical issue descriptions.</returns>
    /// <exception cref="ArgumentNullException">If report is null.</exception>
    public static List<string> GetCriticalIssues(this CircuitBreakerDiagnosticReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        // For now, we consider all issues as critical since we don't have a severity classification.
        // In the future, this could be enhanced to filter by severity.
        return new List<string>(report.Issues);
    }

    /// <summary>
    /// Converts the circuit breaker diagnostic report to a markdown string.
    /// </summary>
    /// <param name="report">The circuit breaker diagnostic report.</param>
    /// <returns>A markdown formatted string representing the report.</returns>
    /// <exception cref="ArgumentNullException">If report is null.</exception>
    public static string ToMarkdown(this CircuitBreakerDiagnosticReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        var sb = new StringBuilder();
        sb.AppendLine($"# Circuit Breaker Diagnostic Report: {report.PolicyName}");
        sb.AppendLine();
        sb.AppendLine($"**Generated At:** {report.GeneratedAt:u}");
        sb.AppendLine();
        sb.AppendLine($"## Policy Information");
        sb.AppendLine();
        sb.AppendLine($"| Property | Value |");
        sb.AppendLine($"|----------|-------|");
        sb.AppendLine($"| Policy ID | {report.PolicyId} |");
        sb.AppendLine($"| Policy Name | {report.PolicyName} |");
        sb.AppendLine($"| Current State | {report.CurrentState} |");
        sb.AppendLine($"| Failure Threshold | {report.FailureThreshold} |");
        sb.AppendLine($"| Open Duration | {report.OpenDuration.TotalSeconds} seconds |");
        sb.AppendLine($"| Success Threshold (Half-Open) | {report.SuccessThreshold} |");
        sb.AppendLine();
        sb.AppendLine($"## Issues Detected ({report.Issues.Count})");
        sb.AppendLine();
        if (report.Issues.Any())
        {
            sb.AppendLine("| Issue |");
            sb.AppendLine($"|-------|");
            foreach (var issue in report.Issues)
            {
                sb.AppendLine($"| {issue} |");
            }
        }
        else
        {
            sb.AppendLine("No issues detected.");
        }
        sb.AppendLine();
        sb.AppendLine($"## Recommendations ({report.Recommendations.Count})");
        sb.AppendLine();
        if (report.Recommendations.Any())
        {
            sb.AppendLine("| Recommendation |");
            sb.AppendLine($"|----------------|");
            foreach (var rec in report.Recommendations)
            {
                sb.AppendLine($"| {rec} |");
            }
        }
        else
        {
            sb.AppendLine("No recommendations.");
        }

        return sb.ToString();
    }
}