#nullable enable
using DotNetResiliencePipeline.Utilities;
using FluentAssertions;
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
}
