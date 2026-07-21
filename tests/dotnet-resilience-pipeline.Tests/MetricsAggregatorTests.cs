#nullable enable
using DotNetResiliencePipeline.Utilities;
using FluentAssertions;
using System.Threading.Tasks;
using Xunit;

/// <summary>
/// Tests for the MetricsAggregator class.
/// </summary>
public sealed class MetricsAggregatorTests
{
    /// <summary>
    /// Creates a new MetricsSnapshot with the specified success rate, average execution time, and total executions.
    /// </summary>
    /// <param name="successRate">The success rate of the snapshot.</param>
    /// <param name="avgExecutionMs">The average execution time of the snapshot in milliseconds (default is 50).</param>
    /// <param name="total">The total number of executions (default is 100).</param>
    /// <returns>A new MetricsSnapshot.</returns>
    private static MetricsSnapshot MakeSnapshot(double successRate, double avgExecutionMs = 50, long total = 100) =>
        new MetricsSnapshot
        {
            Timestamp = DateTime.UtcNow,
            SuccessRate = successRate,
            AverageExecutionTimeMs = avgExecutionMs,
            TotalExecutions = total,
            SuccessfulExecutions = (long)(total * successRate / 100),
            FailedExecutions = (long)(total * (1 - successRate / 100))
        };

    /// <summary>
    /// Verifies that recording a snapshot adds it to the history.
    /// </summary>
    [Fact]
    public void RecordSnapshot_AddsSnapshotToHistory()
    {
        var aggregator = new MetricsAggregator();
        aggregator.RecordSnapshot(MakeSnapshot(95));

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(1));

        metrics.SnapshotCount.Should().Be(1);
    }

    /// <summary>
    /// Verifies that getting aggregated metrics with an empty history returns default aggregated metrics.
    /// </summary>
    [Fact]
    public void GetAggregatedMetrics_EmptyHistory_ReturnsDefaultAggregatedMetrics()
    {
        var aggregator = new MetricsAggregator();

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(1));

        metrics.SnapshotCount.Should().Be(0);
        metrics.AverageSuccessRate.Should().Be(0);
    }

    /// <summary>
    /// Verifies that getting aggregated metrics with multiple snapshots averages the success rate.
    /// </summary>
    [Fact]
    public void GetAggregatedMetrics_MultipleSnapshots_AveragesSuccessRate()
    {
        var aggregator = new MetricsAggregator();
        aggregator.RecordSnapshot(MakeSnapshot(90));
        aggregator.RecordSnapshot(MakeSnapshot(80));
        aggregator.RecordSnapshot(MakeSnapshot(70));

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(1));

        metrics.AverageSuccessRate.Should().BeApproximately(80, 0.01);
    }

    /// <summary>
    /// Verifies that getting aggregated metrics sums the total executions.
    /// </summary>
    [Fact]
    public void GetAggregatedMetrics_SumsTotalExecutions()
    {
        var aggregator = new MetricsAggregator();
        aggregator.RecordSnapshot(MakeSnapshot(100, total: 50));
        aggregator.RecordSnapshot(MakeSnapshot(100, total: 75));

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(1));

        metrics.TotalExecutions.Should().Be(125);
    }

    /// <summary>
    /// Verifies that getting aggregated metrics tracks the peak executions.
    /// </summary>
    [Fact]
    public void GetAggregatedMetrics_TracksPeakExecutions()
    {
        var aggregator = new MetricsAggregator();
        aggregator.RecordSnapshot(MakeSnapshot(99, total: 10));
        aggregator.RecordSnapshot(MakeSnapshot(99, total: 500));
        aggregator.RecordSnapshot(MakeSnapshot(99, total: 20));

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(1));

        metrics.PeakExecutions.Should().Be(500);
    }

    /// <summary>
    /// Verifies that getting aggregated metrics tracks the min and max success rate.
    /// </summary>
    [Fact]
    public void GetAggregatedMetrics_TracksMinAndMaxSuccessRate()
    {
        var aggregator = new MetricsAggregator();
        aggregator.RecordSnapshot(MakeSnapshot(60));
        aggregator.RecordSnapshot(MakeSnapshot(85));
        aggregator.RecordSnapshot(MakeSnapshot(98));

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(1));

        metrics.MinSuccessRate.Should().BeApproximately(60, 0.01);
        metrics.MaxSuccessRate.Should().BeApproximately(98, 0.01);
    }

    /// <summary>
    /// Verifies that when the max snapshots is exceeded, the oldest snapshot is evicted.
    /// </summary>
    [Fact]
    public void MaxSnapshots_WhenExceeded_EvictsOldestSnapshot()
    {
        var aggregator = new MetricsAggregator { MaxSnapshots = 3 };
        aggregator.RecordSnapshot(MakeSnapshot(10));
        aggregator.RecordSnapshot(MakeSnapshot(20));
        aggregator.RecordSnapshot(MakeSnapshot(30));
        aggregator.RecordSnapshot(MakeSnapshot(40));

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(1));

        metrics.SnapshotCount.Should().Be(3);
        metrics.MinSuccessRate.Should().BeApproximately(20, 0.01);
    }

    /// <summary>
    /// Verifies that analyzing the trend with insufficient data returns an empty trend.
    /// </summary>
    [Fact]
    public void AnalyzeTrend_InsufficientData_ReturnsEmptyTrend()
    {
        var aggregator = new MetricsAggregator();
        aggregator.RecordSnapshot(MakeSnapshot(90));

        var trend = aggregator.AnalyzeTrend(TimeSpan.FromMinutes(1));

        trend.DataPoints.Should().Be(1);
        trend.Direction.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that analyzing the trend with improving success rate returns an increasing direction.
    /// </summary>
    [Fact]
    public void AnalyzeTrend_ImprovingSuccessRate_ReturnsIncreasingDirection()
    {
        var aggregator = new MetricsAggregator();
        for (int i = 0; i < 4; i++)
            aggregator.RecordSnapshot(MakeSnapshot(50 + i * 10));

        var trend = aggregator.AnalyzeTrend(TimeSpan.FromMinutes(1), "SuccessRate");

        trend.Direction.Should().Be("Increasing");
        trend.DataPoints.Should().Be(4);
    }

    /// <summary>
    /// Verifies that analyzing the trend with declining success rate returns a decreasing direction.
    /// </summary>
    [Fact]
    public void AnalyzeTrend_DecliningSuccessRate_ReturnsDecreasingDirection()
    {
        var aggregator = new MetricsAggregator();
        for (int i = 0; i < 4; i++)
            aggregator.RecordSnapshot(MakeSnapshot(90 - i * 10));

        var trend = aggregator.AnalyzeTrend(TimeSpan.FromMinutes(1), "SuccessRate");

        trend.Direction.Should().Be("Decreasing");
    }

    /// <summary>
    /// Verifies that analyzing the trend with a large change percentage marks it as an anomaly.
    /// </summary>
    [Fact]
    public void AnalyzeTrend_LargeChangePercentage_MarksAsAnomaly()
    {
        var aggregator = new MetricsAggregator();
        aggregator.RecordSnapshot(MakeSnapshot(90));
        aggregator.RecordSnapshot(MakeSnapshot(90));
        aggregator.RecordSnapshot(MakeSnapshot(20));
        aggregator.RecordSnapshot(MakeSnapshot(20));

        var trend = aggregator.AnalyzeTrend(TimeSpan.FromMinutes(1), "SuccessRate");

        trend.IsAnomaly.Should().BeTrue();
    }

    /// <summary>
    /// Verifies that clearing the aggregator removes all snapshots.
    /// </summary>
    [Fact]
    public void Clear_RemovesAllSnapshots()
    {
        var aggregator = new MetricsAggregator();
        aggregator.RecordSnapshot(MakeSnapshot(99));
        aggregator.RecordSnapshot(MakeSnapshot(98));

        aggregator.Clear();

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(1));
        metrics.SnapshotCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies that recording a single snapshot works correctly.
    /// </summary>
    [Fact]
    public void RecordSnapshot_SingleSnapshot_WorksCorrectly()
    {
        var aggregator = new MetricsAggregator();
        var snapshot = MakeSnapshot(95, avgExecutionMs: 100, total: 200);

        aggregator.RecordSnapshot(snapshot);

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(1));
        metrics.SnapshotCount.Should().Be(1);
        metrics.AverageSuccessRate.Should().BeApproximately(95, 0.01);
        metrics.AverageExecutionTimeMs.Should().BeApproximately(100, 0.01);
        metrics.TotalExecutions.Should().Be(200);
        metrics.PeakExecutions.Should().Be(200);
        metrics.MinSuccessRate.Should().BeApproximately(95, 0.01);
        metrics.MaxSuccessRate.Should().BeApproximately(95, 0.01);
    }

    /// <summary>
    /// Verifies that concurrent recording from multiple threads doesn't corrupt the snapshots.
    /// </summary>
    [Fact]
    public void RecordSnapshot_ConcurrentRecording_DoesNotCorruptData()
    {
        var aggregator = new MetricsAggregator();
        var tasks = new Task[10];

        // Record snapshots concurrently from 10 threads
        for (int i = 0; i < 10; i++)
        {
            int threadId = i;
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 100; j++)
                {
                    var snapshot = MakeSnapshot(80 + threadId, avgExecutionMs: 50 + threadId, total: 1000);
                    aggregator.RecordSnapshot(snapshot);
                }
            });
        }

        Task.WaitAll(tasks);

        // Verify all snapshots were recorded
        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromHours(1));
        metrics.SnapshotCount.Should().Be(1000);
        metrics.AverageSuccessRate.Should().BeApproximately(84.5, 0.5); // Average of 80-89
    }

    /// <summary>
    /// Verifies that the Clear method properly resets the aggregator.
    /// </summary>
    [Fact]
    public void Clear_ResetsAggregator()
    {
        var aggregator = new MetricsAggregator();

        // Add some snapshots
        aggregator.RecordSnapshot(MakeSnapshot(90));
        aggregator.RecordSnapshot(MakeSnapshot(85));
        aggregator.RecordSnapshot(MakeSnapshot(95));

        // Verify snapshots exist
        var metricsBefore = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(1));
        metricsBefore.SnapshotCount.Should().Be(3);

        // Clear and verify
        aggregator.Clear();
        var metricsAfter = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(1));
        metricsAfter.SnapshotCount.Should().Be(0);
        metricsAfter.AverageSuccessRate.Should().Be(0);
        metricsAfter.TotalExecutions.Should().Be(0);
    }

    /// <summary>
    /// Verifies that GetLatencyPercentiles returns correct values for a single snapshot.
    /// </summary>
    [Fact]
    public void GetLatencyPercentiles_SingleSnapshot_ReturnsCorrectPercentiles()
    {
        var aggregator = new MetricsAggregator();
        aggregator.RecordSnapshot(MakeSnapshot(95, avgExecutionMs: 100, total: 50));

        var percentiles = aggregator.GetLatencyPercentiles();
        percentiles.P50.Should().Be(100);
        percentiles.P90.Should().Be(100);
        percentiles.P99.Should().Be(100);
    }

    /// <summary>
    /// Verifies that GetLatencyPercentiles returns correct values for multiple snapshots.
    /// </summary>
    [Fact]
    public void GetLatencyPercentiles_MultipleSnapshots_ReturnsCorrectPercentiles()
    {
        var aggregator = new MetricsAggregator();
        aggregator.RecordSnapshot(MakeSnapshot(95, avgExecutionMs: 50, total: 100));
        aggregator.RecordSnapshot(MakeSnapshot(95, avgExecutionMs: 100, total: 100));
        aggregator.RecordSnapshot(MakeSnapshot(95, avgExecutionMs: 150, total: 100));
        aggregator.RecordSnapshot(MakeSnapshot(95, avgExecutionMs: 200, total: 100));
        aggregator.RecordSnapshot(MakeSnapshot(95, avgExecutionMs: 250, total: 100));

        var percentiles = aggregator.GetLatencyPercentiles();
        percentiles.P50.Should().Be(150); // 3rd value in sorted [50, 100, 150, 200, 250]
        percentiles.P90.Should().Be(250); // 90th percentile = ceil(0.9 * 5) - 1 = 4th index = 250
        percentiles.P99.Should().Be(250); // 99th percentile = ceil(0.99 * 5) - 1 = 4th index = 250
    }

    /// <summary>
    /// Verifies that GetLatencyPercentiles returns zeros when no snapshots are recorded.
    /// </summary>
    [Fact]
    public void GetLatencyPercentiles_EmptyAggregator_ReturnsZeros()
    {
        var aggregator = new MetricsAggregator();

        var percentiles = aggregator.GetLatencyPercentiles();
        percentiles.P50.Should().Be(0);
        percentiles.P90.Should().Be(0);
        percentiles.P99.Should().Be(0);
    }

    /// <summary>
    /// Verifies that GetLatencyPercentiles handles concurrent calls correctly.
    /// </summary>
    [Fact]
    public void GetLatencyPercentiles_ConcurrentAccess_DoesNotThrow()
    {
        var aggregator = new MetricsAggregator();

        // Add some snapshots
        for (int i = 0; i < 50; i++)
        {
            aggregator.RecordSnapshot(MakeSnapshot(95, avgExecutionMs: 100 + i * 10, total: 100));
        }

        var tasks = new Task[10];
        for (int i = 0; i < 10; i++)
        {
            tasks[i] = Task.Run(() =>
            {
                for (int j = 0; j < 10; j++)
                {
                    var percentiles = aggregator.GetLatencyPercentiles();
                    percentiles.P50.Should().BeGreaterThan(0);
                }
            });
        }

        Task.WaitAll(tasks);
    }
}
