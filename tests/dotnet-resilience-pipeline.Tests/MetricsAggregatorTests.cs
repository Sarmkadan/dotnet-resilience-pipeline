// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Utilities;
using FluentAssertions;

namespace DotNetResiliencePipeline.Tests;

public class MetricsAggregatorTests
{
    [Fact]
    public void GetAggregatedMetrics_NoSnapshots_ReturnsEmptyMetrics()
    {
        var aggregator = new MetricsAggregator();
        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(5));
        metrics.SnapshotCount.Should().Be(0);
    }

    [Fact]
    public void RecordSnapshot_SingleSnapshot_IncrementsCount()
    {
        var aggregator = new MetricsAggregator();
        aggregator.RecordSnapshot(new MetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            SuccessRate = 99.5,
            AverageExecutionTimeMs = 50,
            TotalExecutions = 100
        });

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(5));
        metrics.SnapshotCount.Should().Be(1);
        metrics.AverageSuccessRate.Should().Be(99.5);
    }

    [Fact]
    public void RecordSnapshot_MultipleSnapshots_AggregatesCorrectly()
    {
        var aggregator = new MetricsAggregator();
        aggregator.RecordSnapshot(new MetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            SuccessRate = 90.0,
            AverageExecutionTimeMs = 100,
            TotalExecutions = 50
        });
        aggregator.RecordSnapshot(new MetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            SuccessRate = 100.0,
            AverageExecutionTimeMs = 200,
            TotalExecutions = 150
        });

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(5));
        metrics.SnapshotCount.Should().Be(2);
        metrics.AverageSuccessRate.Should().Be(95.0);
        metrics.TotalExecutions.Should().Be(200);
        metrics.PeakExecutions.Should().Be(150);
    }

    [Fact]
    public void GetAggregatedMetrics_WindowExcludesOldSnapshots()
    {
        var aggregator = new MetricsAggregator();
        // Old snapshot - outside window
        aggregator.RecordSnapshot(new MetricsSnapshot
        {
            Timestamp = DateTime.UtcNow.AddMinutes(-10),
            SuccessRate = 50.0,
            TotalExecutions = 1000
        });
        // Recent snapshot - inside window
        aggregator.RecordSnapshot(new MetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            SuccessRate = 99.0,
            TotalExecutions = 100
        });

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(5));
        metrics.SnapshotCount.Should().Be(1);
        metrics.AverageSuccessRate.Should().Be(99.0);
    }

    [Fact]
    public void MaxSnapshots_ExceedingLimit_RemovesOldest()
    {
        var aggregator = new MetricsAggregator { MaxSnapshots = 3 };

        for (int i = 0; i < 5; i++)
        {
            aggregator.RecordSnapshot(new MetricsSnapshot
            {
                Timestamp = DateTime.UtcNow,
                SuccessRate = i * 10.0,
                TotalExecutions = i
            });
        }

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(5));
        metrics.SnapshotCount.Should().BeLessThanOrEqualTo(3);
    }

    [Fact]
    public void AnalyzeTrend_InsufficientData_ReturnsDefaultTrend()
    {
        var aggregator = new MetricsAggregator();
        var trend = aggregator.AnalyzeTrend(TimeSpan.FromMinutes(5));
        trend.DataPoints.Should().Be(0);
    }
}
