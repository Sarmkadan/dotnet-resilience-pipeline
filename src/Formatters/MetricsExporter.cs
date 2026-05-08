#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetResiliencePipeline.Domain;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Formatters;

/// <summary>
/// Exports resilience pipeline metrics in multiple formats: JSON, CSV, and Prometheus text exposition.
/// </summary>
public sealed class MetricsExporter
{
    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.Never,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    // ─── JSON ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Exports the pipeline metrics snapshot as a JSON string.
    /// </summary>
    public string ExportJson(PipelineMetricsSnapshot snapshot)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

        var export = new MetricsExportPayload
        {
            ExportedAt = DateTime.UtcNow,
            Format = "json",
            Pipeline = new PipelineSummaryExport
            {
                TotalExecutions = snapshot.TotalExecutions,
                SuccessfulExecutions = snapshot.SuccessfulExecutions,
                FailedExecutions = snapshot.FailedExecutions,
                SuccessRate = snapshot.SuccessRate,
                RetryCount = snapshot.RetryCount,
                CircuitBreakerTrips = snapshot.CircuitBreakerTrips,
                TimeoutCount = snapshot.TimeoutCount
            },
            Policies = snapshot.PolicySnapshots.Select(MapPolicySnapshot).ToList()
        };

        return JsonSerializer.Serialize(export, _jsonOptions);
    }

    // ─── CSV ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Exports per-policy metrics as a CSV string with a header row.
    /// </summary>
    public string ExportCsv(PipelineMetricsSnapshot snapshot)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

        var sb = new StringBuilder();
        sb.AppendLine("PolicyId,PolicyName,PolicyType,IsEnabled,TotalExecutions,SuccessfulExecutions,FailedExecutions,SuccessRate");

        foreach (var p in snapshot.PolicySnapshots)
        {
            sb.AppendLine(string.Join(",",
                EscapeCsv(p.PolicyId),
                EscapeCsv(p.PolicyName),
                EscapeCsv(p.PolicyType),
                p.IsEnabled,
                p.TotalExecutions,
                p.SuccessfulExecutions,
                p.FailedExecutions,
                $"{p.SuccessRate:F4}"));
        }

        return sb.ToString();
    }

    // ─── Prometheus ───────────────────────────────────────────────────────────

    /// <summary>
    /// Exports metrics in Prometheus text exposition format (version 0.0.4).
    /// Each metric is exported with a <c>pipeline</c> label and, where applicable, a <c>policy</c> label.
    /// </summary>
    public string ExportPrometheus(PipelineMetricsSnapshot snapshot)
    {
        if (snapshot is null) throw new ArgumentNullException(nameof(snapshot));

        var sb = new StringBuilder();
        var ts = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

        // Pipeline-level counters
        WritePrometheusMetric(sb, "resilience_pipeline_executions_total",
            "Total executions processed by the resilience pipeline", "counter",
            snapshot.TotalExecutions, ts);

        WritePrometheusMetric(sb, "resilience_pipeline_executions_success_total",
            "Total successful executions", "counter",
            snapshot.SuccessfulExecutions, ts);

        WritePrometheusMetric(sb, "resilience_pipeline_executions_failure_total",
            "Total failed executions", "counter",
            snapshot.FailedExecutions, ts);

        WritePrometheusMetric(sb, "resilience_pipeline_success_rate",
            "Pipeline success rate (0-100)", "gauge",
            snapshot.SuccessRate, ts);

        WritePrometheusMetric(sb, "resilience_pipeline_retry_total",
            "Total retry attempts across all retry policies", "counter",
            snapshot.RetryCount, ts);

        WritePrometheusMetric(sb, "resilience_pipeline_circuit_breaker_trips_total",
            "Total circuit breaker trips across all circuit breaker policies", "counter",
            snapshot.CircuitBreakerTrips, ts);

        WritePrometheusMetric(sb, "resilience_pipeline_timeout_total",
            "Total timeout events across all timeout policies", "counter",
            snapshot.TimeoutCount, ts);

        // Per-policy gauges
        sb.AppendLine("# HELP resilience_policy_executions_total Total executions per policy");
        sb.AppendLine("# TYPE resilience_policy_executions_total counter");
        foreach (var p in snapshot.PolicySnapshots)
        {
            var labels = BuildPolicyLabels(p);
            sb.AppendLine($"resilience_policy_executions_total{{{labels}}} {p.TotalExecutions} {ts}");
        }

        sb.AppendLine("# HELP resilience_policy_success_rate Success rate per policy (0-100)");
        sb.AppendLine("# TYPE resilience_policy_success_rate gauge");
        foreach (var p in snapshot.PolicySnapshots)
        {
            var labels = BuildPolicyLabels(p);
            sb.AppendLine($"resilience_policy_success_rate{{{labels}}} {p.SuccessRate:F4} {ts}");
        }

        // Circuit-breaker-specific state gauge (0=Closed, 1=Open, 2=HalfOpen)
        var cbPolicies = snapshot.PolicySnapshots
            .Where(p => p.PolicyType == nameof(CircuitBreakerPolicy))
            .ToList();

        if (cbPolicies.Count > 0)
        {
            sb.AppendLine("# HELP resilience_circuit_breaker_state Current circuit breaker state (0=Closed, 1=Open, 2=HalfOpen)");
            sb.AppendLine("# TYPE resilience_circuit_breaker_state gauge");
            foreach (var p in cbPolicies)
            {
                var stateValue = p.Metadata?.TryGetValue("CircuitState", out var stateObj) == true
                    ? stateObj?.ToString() switch
                    {
                        "Closed"   => 0,
                        "Open"     => 1,
                        "HalfOpen" => 2,
                        _ => -1
                    }
                    : -1;

                var labels = BuildPolicyLabels(p);
                sb.AppendLine($"resilience_circuit_breaker_state{{{labels}}} {stateValue} {ts}");
            }
        }

        return sb.ToString();
    }

    // ─── helpers ──────────────────────────────────────────────────────────────

    private static void WritePrometheusMetric(
        StringBuilder sb, string name, string help, string type, double value, long timestamp)
    {
        sb.AppendLine($"# HELP {name} {help}");
        sb.AppendLine($"# TYPE {name} {type}");
        sb.AppendLine($"{name} {value:G} {timestamp}");
    }

    private static string BuildPolicyLabels(PolicySnapshot p)
        => $"policy_id=\"{p.PolicyId}\",policy_name=\"{EscapePrometheusLabel(p.PolicyName)}\",policy_type=\"{p.PolicyType}\"";

    private static PolicyExport MapPolicySnapshot(PolicySnapshot p) => new()
    {
        PolicyId = p.PolicyId,
        PolicyName = p.PolicyName,
        PolicyType = p.PolicyType,
        IsEnabled = p.IsEnabled,
        TotalExecutions = p.TotalExecutions,
        SuccessfulExecutions = p.SuccessfulExecutions,
        FailedExecutions = p.FailedExecutions,
        SuccessRate = p.SuccessRate,
        SnapshotTime = p.SnapshotTime,
        Metadata = p.Metadata
    };

    private static string EscapeCsv(string? value)
    {
        if (value is null) return string.Empty;
        if (value.Contains(',') || value.Contains('"') || value.Contains('\n'))
            return $"\"{value.Replace("\"", "\"\"")}\"";
        return value;
    }

    private static string EscapePrometheusLabel(string value)
        => value.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n");
}

/// <summary>Top-level export payload.</summary>
public sealed class MetricsExportPayload
{
    /// <summary>Timestamp of export generation.</summary>
    public DateTime ExportedAt { get; set; }

    /// <summary>Export format identifier.</summary>
    public string Format { get; set; } = string.Empty;

    /// <summary>Pipeline-level aggregated metrics.</summary>
    public PipelineSummaryExport Pipeline { get; set; } = new();

    /// <summary>Per-policy metrics.</summary>
    public List<PolicyExport> Policies { get; set; } = new();
}

/// <summary>Pipeline-level aggregated metrics for export.</summary>
public sealed class PipelineSummaryExport
{
    /// <summary>Total executions.</summary>
    public long TotalExecutions { get; set; }

    /// <summary>Successful executions.</summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>Failed executions.</summary>
    public long FailedExecutions { get; set; }

    /// <summary>Success rate (0–100).</summary>
    public double SuccessRate { get; set; }

    /// <summary>Total retry attempts.</summary>
    public long RetryCount { get; set; }

    /// <summary>Total circuit breaker trips.</summary>
    public long CircuitBreakerTrips { get; set; }

    /// <summary>Total timeout events.</summary>
    public long TimeoutCount { get; set; }
}

/// <summary>Per-policy metrics for export.</summary>
public sealed class PolicyExport
{
    /// <summary>Policy unique identifier.</summary>
    public string PolicyId { get; set; } = string.Empty;

    /// <summary>Policy name.</summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>Policy type name.</summary>
    public string PolicyType { get; set; } = string.Empty;

    /// <summary>Whether the policy is enabled.</summary>
    public bool IsEnabled { get; set; }

    /// <summary>Total executions.</summary>
    public long TotalExecutions { get; set; }

    /// <summary>Successful executions.</summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>Failed executions.</summary>
    public long FailedExecutions { get; set; }

    /// <summary>Success rate (0–100).</summary>
    public double SuccessRate { get; set; }

    /// <summary>Time this snapshot was taken.</summary>
    public DateTime SnapshotTime { get; set; }

    /// <summary>Additional policy-specific metadata.</summary>
    public Dictionary<string, object>? Metadata { get; set; }
}
