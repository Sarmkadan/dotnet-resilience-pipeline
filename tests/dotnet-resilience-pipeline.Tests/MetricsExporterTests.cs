#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Formatters;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Provides unit tests for the <see cref="MetricsExporter"/> class to verify JSON, CSV, and Prometheus metric export functionality.
/// </summary>
public sealed class MetricsExporterTests
{
    /// <summary>
    /// Creates a test <see cref="PipelineMetricsSnapshot"/> with configurable number of policies for testing export functionality.
    /// </summary>
    /// <param name="policies">The number of policies to include in the snapshot. Defaults to 2.</param>
    /// <returns>A configured <see cref="PipelineMetricsSnapshot"/> with sample metrics data.</returns>
    private static PipelineMetricsSnapshot BuildSnapshot(int policies = 2) =>
        new PipelineMetricsSnapshot
        {
            TotalExecutions = 1000,
            SuccessfulExecutions = 950,
            FailedExecutions = 50,
            SuccessRate = 95.0,
            RetryCount = 30,
            CircuitBreakerTrips = 2,
            TimeoutCount = 5,
            PolicySnapshots = Enumerable.Range(0, policies).Select(i =>
                new PolicySnapshot
                {
                    PolicyId = $"id-{i}",
                    PolicyName = $"policy-{i}",
                    PolicyType = i == 0 ? nameof(CircuitBreakerPolicy) : nameof(RetryPolicy),
                    IsEnabled = true,
                    TotalExecutions = 500,
                    SuccessfulExecutions = 475,
                    FailedExecutions = 25,
                    SuccessRate = 95.0,
                    SnapshotTime = DateTime.UtcNow,
                    Metadata = i == 0
                        ? new Dictionary<string, object> { { "CircuitState", "Closed" } }
                        : null
                }).ToList()
        };

    // ─── JSON ─────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that exporting a valid metrics snapshot produces non-empty JSON output containing expected fields.
    /// </summary>
    [Fact]
    public void ExportJson_ValidSnapshot_ProducesValidJson()
    {
        var exporter = new MetricsExporter();
        var snapshot = BuildSnapshot();

        var json = exporter.ExportJson(snapshot);

        json.Should().NotBeNullOrWhiteSpace();
        json.Should().Contain("\"successRate\"");
        json.Should().Contain("\"totalExecutions\"");
    }

    /// <summary>
    /// Tests that JSON export includes all pipeline-level counters like retry count, circuit breaker trips, and timeout count.
    /// </summary>
    [Fact]
    public void ExportJson_IncludesAllPipelineLevelCounters()
    {
        var exporter = new MetricsExporter();
        var snapshot = BuildSnapshot();

        var json = exporter.ExportJson(snapshot);

        json.Should().Contain("\"retryCount\"");
        json.Should().Contain("\"circuitBreakerTrips\"");
        json.Should().Contain("\"timeoutCount\"");
    }

    /// <summary>
    /// Tests that passing null to JSON export method throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void ExportJson_NullSnapshot_ThrowsArgumentNullException()
    {
        var exporter = new MetricsExporter();

        Action act = () => exporter.ExportJson(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    // ─── CSV ──────────────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that CSV export for a valid snapshot includes the required header row with column names.
    /// </summary>
    [Fact]
    public void ExportCsv_ValidSnapshot_HasHeaderRow()
    {
        var exporter = new MetricsExporter();
        var snapshot = BuildSnapshot();

        var csv = exporter.ExportCsv(snapshot);

        csv.Should().StartWith("PolicyId,PolicyName,PolicyType");
    }

    /// <summary>
    /// Tests that CSV export with two policies produces three lines (1 header + 2 data rows).
    /// </summary>
    /// <param name="policies">Number of policies in the snapshot.</param>
    [Fact]
    public void ExportCsv_TwoPolicies_ProducesThreeLines()
    {
        var exporter = new MetricsExporter();
        var snapshot = BuildSnapshot(policies: 2);

        var lines = exporter.ExportCsv(snapshot)
            .Split('\n', StringSplitOptions.RemoveEmptyEntries);

        // 1 header + 2 data rows
        lines.Should().HaveCount(3);
    }

    /// <summary>
    /// Tests that CSV export includes the success rate value in the output.
    /// </summary>
    [Fact]
    public void ExportCsv_SuccessRateIncluded()
    {
        var exporter = new MetricsExporter();
        var snapshot = BuildSnapshot(policies: 1);

        var csv = exporter.ExportCsv(snapshot);

        csv.Should().Contain("95.0000");
    }

    // ─── Prometheus ───────────────────────────────────────────────────────────

    /// <summary>
    /// Tests that Prometheus export includes pipeline-level metrics like total executions, success rate, and circuit breaker trips.
    /// </summary>
    [Fact]
    public void ExportPrometheus_ContainsPipelineLevelMetrics()
    {
        var exporter = new MetricsExporter();
        var snapshot = BuildSnapshot();

        var prom = exporter.ExportPrometheus(snapshot);

        prom.Should().Contain("resilience_pipeline_executions_total");
        prom.Should().Contain("resilience_pipeline_success_rate");
        prom.Should().Contain("resilience_pipeline_circuit_breaker_trips_total");
    }

    /// <summary>
    /// Tests that Prometheus export includes per-policy metrics with policy names as labels.
    /// </summary>
    [Fact]
    public void ExportPrometheus_ContainsPerPolicyMetrics()
    {
        var exporter = new MetricsExporter();
        var snapshot = BuildSnapshot();

        var prom = exporter.ExportPrometheus(snapshot);

        prom.Should().Contain("resilience_policy_executions_total");
        prom.Should().Contain("policy_name=\"policy-0\"");
    }

    /// <summary>
    /// Tests that Prometheus export includes circuit breaker state gauge metrics.
    /// </summary>
    [Fact]
    public void ExportPrometheus_CircuitBreakerStateGaugeIncluded()
    {
        var exporter = new MetricsExporter();
        var snapshot = BuildSnapshot();

        var prom = exporter.ExportPrometheus(snapshot);

        prom.Should().Contain("resilience_circuit_breaker_state");
    }

    /// <summary>
    /// Tests that passing null to Prometheus export method throws <see cref="ArgumentNullException"/>.
    /// </summary>
    [Fact]
    public void ExportPrometheus_NullSnapshot_ThrowsArgumentNullException()
    {
        var exporter = new MetricsExporter();

        Action act = () => exporter.ExportPrometheus(null!);

        act.Should().Throw<ArgumentNullException>();
    }
}
