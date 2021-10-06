#nullable enable

using System.Globalization;
using System.Text;
using DotNetResiliencePipeline.Domain;
using DotNetResiliencePipeline.Formatters;

namespace DotNetResiliencePipeline.Formatters;

/// <summary>
/// Extension methods for <see cref="MetricsExporter"/> that provide additional functionality
/// for working with pipeline metrics exports.
/// </summary>
public static class MetricsExporterExtensions
{
    /// <summary>
    /// Creates a summary string that provides a quick overview of the pipeline metrics.
    /// </summary>
    /// <param name="exporter">The metrics exporter instance.</param>
    /// <param name="snapshot">The pipeline metrics snapshot.</param>
    /// <returns>A formatted summary string.</returns>
    public static string CreateSummary(this MetricsExporter exporter, PipelineMetricsSnapshot snapshot)
    {
        if (exporter is null) throw new ArgumentNullException(nameof(exporter));
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

        var sb = new StringBuilder();
        sb.AppendLine("📊 Resilience Pipeline Metrics Summary");
        sb.AppendLine("=====================================");
        sb.AppendLine($"Exported at: {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();
        sb.AppendLine("📈 Pipeline Performance:");
        sb.AppendLine($"  Total executions: {snapshot.TotalExecutions:N0}");
        sb.AppendLine($"  Successful: {snapshot.SuccessfulExecutions:N0} ({snapshot.SuccessRate:P1})");
        sb.AppendLine($"  Failed: {snapshot.FailedExecutions:N0}");
        sb.AppendLine($"  Success rate: {snapshot.SuccessRate:P2}");
        sb.AppendLine();

        sb.AppendLine("🔄 Policy Statistics:");
        foreach (var policy in snapshot.PolicySnapshots.OrderByDescending(p => p.SuccessRate))
        {
            sb.AppendLine($"  {policy.PolicyType,-20} {policy.PolicyName}");
            sb.AppendLine($"    Executions: {policy.TotalExecutions:N0} | Success: {policy.SuccessfulExecutions:N0} ({policy.SuccessRate:P1})");
            sb.AppendLine($"    Failures: {policy.FailedExecutions:N0} | State: {(policy.Metadata?.TryGetValue("CircuitState", out var state) == true ? state : "N/A")}");
        }

        sb.AppendLine();
        sb.AppendLine("⚡ Additional Metrics:");
        sb.AppendLine($"  Retries: {snapshot.RetryCount:N0}");
        sb.AppendLine($"  Circuit breaker trips: {snapshot.CircuitBreakerTrips:N0}");
        sb.AppendLine($"  Timeouts: {snapshot.TimeoutCount:N0}");

        return sb.ToString();
    }

    /// <summary>
    /// Exports metrics in a tabular format suitable for console output.
    /// </summary>
    /// <param name="exporter">The metrics exporter instance.</param>
    /// <param name="snapshot">The pipeline metrics snapshot.</param>
    /// <param name="includePolicyDetails">Whether to include detailed policy breakdown.</param>
    /// <returns>A formatted table string.</returns>
    public static string ExportConsoleTable(this MetricsExporter exporter, PipelineMetricsSnapshot snapshot, bool includePolicyDetails = true)
    {
        if (exporter is null) throw new ArgumentNullException(nameof(exporter));
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

        var sb = new StringBuilder();

        // Header
        sb.AppendLine("╔════════════════════════════════════════════════════════════════╗");
        sb.AppendLine(string.Format("║ {0,-63} ║", "Resilience Pipeline Metrics"));
        sb.AppendLine("╠════════════════════════════════════════════════════════════════╣");
        sb.AppendLine(string.Format("║ {0,-20} {1,-42}║", "Exported at:", DateTime.UtcNow.ToString("yyyy-MM-dd HH:mm:ss")));
        sb.AppendLine(string.Format("║ {0,-20} {1,-25:N0}{2,18}║", "Total executions:", snapshot.TotalExecutions, ""));
        sb.AppendLine(string.Format("║ {0,-20} {1,-25:N0}{2,18}║", "Successful executions:", snapshot.SuccessfulExecutions, ""));
        sb.AppendLine(string.Format("║ {0,-20} {1,-25:N0}{2,18}║", "Failed executions:", snapshot.FailedExecutions, ""));
        sb.AppendLine(string.Format("║ {0,-20} {1,-25}{2,18}║", "Success rate:", snapshot.SuccessRate.ToString("P2"), ""));
        sb.AppendLine("╠════════════════════════════════════════════════════════════════╣");

        if (includePolicyDetails)
        {
            sb.AppendLine(string.Format("║ {0,-30} {1,-20} {2,-12} {3,-12} {4,-8}║", "Policy Name", "Type", "Executions", "Success", "Rate"));
            sb.AppendLine("╠════════════════════════════════════════════════════════════════╣");

            foreach (var policy in snapshot.PolicySnapshots.OrderByDescending(p => p.TotalExecutions))
            {
                var state = policy.Metadata?.TryGetValue("CircuitState", out var stateObj) == true
                    ? stateObj?.ToString() ?? "Unknown"
                    : "N/A";

                sb.AppendLine(string.Format("║ {0,-30} {1,-20} {2,-12:N0} {3,-12:N0} {4,-8:P1}║",
                    policy.PolicyName, policy.PolicyType, policy.TotalExecutions, policy.SuccessfulExecutions, policy.SuccessRate));
                sb.AppendLine(string.Format("║ {0,-30} {1,-20} {2,-12} {3,-12:N0} {4,-8} State: {5,-8}║",
                    " ", " ", " ", policy.FailedExecutions, " ", state));
            }

            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        }
        else
        {
            sb.AppendLine("╚════════════════════════════════════════════════════════════════╝");
        }

        return sb.ToString();
    }

    /// <summary>
    /// Exports metrics in Markdown format for documentation or reporting.
    /// </summary>
    /// <param name="exporter">The metrics exporter instance.</param>
    /// <param name="snapshot">The pipeline metrics snapshot.</param>
    /// <returns>A Markdown formatted string.</returns>
    public static string ExportMarkdown(this MetricsExporter exporter, PipelineMetricsSnapshot snapshot)
    {
        if (exporter is null) throw new ArgumentNullException(nameof(exporter));
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

        var sb = new StringBuilder();

        sb.AppendLine("# Resilience Pipeline Metrics Report");
        sb.AppendLine();
        sb.AppendLine($"**Generated at:** {DateTime.UtcNow:yyyy-MM-dd HH:mm:ss} UTC");
        sb.AppendLine();

        sb.AppendLine("## 📊 Pipeline Performance Summary");
        sb.AppendLine();
        sb.AppendLine("| Metric | Value |");
        sb.AppendLine("|--------|-------|");
        sb.AppendLine($"| Total executions | {snapshot.TotalExecutions:N0} |");
        sb.AppendLine($"| Successful executions | {snapshot.SuccessfulExecutions:N0} ({snapshot.SuccessRate:P1}) |");
        sb.AppendLine($"| Failed executions | {snapshot.FailedExecutions:N0} |");
        sb.AppendLine($"| Success rate | {snapshot.SuccessRate:P2} |");
        sb.AppendLine($"| Retries | {snapshot.RetryCount:N0} |");
        sb.AppendLine($"| Circuit breaker trips | {snapshot.CircuitBreakerTrips:N0} |");
        sb.AppendLine($"| Timeouts | {snapshot.TimeoutCount:N0} |");
        sb.AppendLine();

        sb.AppendLine("## 🔄 Policy Breakdown");
        sb.AppendLine();
        sb.AppendLine("| Policy | Type | Executions | Success | Failures | Success Rate | State |");
        sb.AppendLine("|--------|------|------------|---------|----------|--------------|-------|");

        foreach (var policy in snapshot.PolicySnapshots.OrderByDescending(p => p.SuccessRate))
        {
            var state = policy.Metadata?.TryGetValue("CircuitState", out var stateObj) == true
                ? stateObj?.ToString() ?? "Unknown"
                : "N/A";

            sb.AppendLine(string.Format("| {0} | {1} | {2:N0} | {3:N0} | {4:N0} | {5:P1} | {6} |",
                policy.PolicyName, policy.PolicyType, policy.TotalExecutions,
                policy.SuccessfulExecutions, policy.FailedExecutions, policy.SuccessRate, state));
        }

        sb.AppendLine();
        sb.AppendLine("---");
        sb.AppendLine($"*Report generated by {nameof(MetricsExporterExtensions)} at {DateTime.UtcNow:O}*");

        return sb.ToString();
    }

    /// <summary>
    /// Gets a dictionary of key performance indicators from the metrics snapshot.
    /// </summary>
    /// <param name="exporter">The metrics exporter instance.</param>
    /// <param name="snapshot">The pipeline metrics snapshot.</param>
    /// <returns>A dictionary containing KPI values.</returns>
    public static Dictionary<string, object> GetKeyPerformanceIndicators(this MetricsExporter exporter, PipelineMetricsSnapshot snapshot)
    {
        if (exporter is null) throw new ArgumentNullException(nameof(exporter));
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

        return new Dictionary<string, object>
        {
            ["TotalExecutions"] = snapshot.TotalExecutions,
            ["SuccessfulExecutions"] = snapshot.SuccessfulExecutions,
            ["FailedExecutions"] = snapshot.FailedExecutions,
            ["SuccessRate"] = snapshot.SuccessRate,
            ["SuccessRatePercentage"] = snapshot.SuccessRate * 100,
            ["RetryCount"] = snapshot.RetryCount,
            ["CircuitBreakerTrips"] = snapshot.CircuitBreakerTrips,
            ["TimeoutCount"] = snapshot.TimeoutCount,
            ["PolicyCount"] = snapshot.PolicySnapshots.Count,
            ["EnabledPolicies"] = snapshot.PolicySnapshots.Count(p => p.IsEnabled),
            ["DisabledPolicies"] = snapshot.PolicySnapshots.Count(p => !p.IsEnabled),
            ["ExportedAt"] = DateTime.UtcNow
        };
    }
}