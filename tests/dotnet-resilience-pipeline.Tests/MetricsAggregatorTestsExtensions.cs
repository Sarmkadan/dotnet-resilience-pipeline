#nullable enable

using System;
using System.Collections.Generic;
using System.Globalization;
using DotNetResiliencePipeline.Utilities;
using FluentAssertions;
using Xunit;

/// <summary>
/// Extension methods for <see cref="MetricsAggregatorTests"/> that provide additional test scenarios and helper methods.
/// </summary>
/// <remarks>
/// All extension methods validate parameters using <see cref="ArgumentNullException.ThrowIfNull"/> and <see cref="ArgumentException.ThrowIfNullOrEmpty"/>.
/// Methods use expression-bodied syntax where appropriate for one-liners.
/// </remarks>
public static class MetricsAggregatorTestsExtensions
{
    /// <summary>
    /// Creates a metrics aggregator with the specified snapshots pre-recorded.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="snapshots">The snapshots to record.</param>
    /// <returns>A configured metrics aggregator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="snapshots"/> is <see langword="null"/>.</exception>
    public static MetricsAggregator WithSnapshots(this MetricsAggregatorTests test, IEnumerable<MetricsSnapshot> snapshots)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(snapshots);

        var aggregator = new MetricsAggregator();
        foreach (var snapshot in snapshots)
        {
            aggregator.RecordSnapshot(snapshot);
        }

        return aggregator;
    }

    /// <summary>
    /// Creates a metrics aggregator with the specified number of identical snapshots.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="successRate">The success rate for all snapshots.</param>
    /// <param name="count">The number of snapshots to create.</param>
    /// <param name="avgExecutionMs">The average execution time in milliseconds (default is 50).</param>
    /// <param name="total">The total executions per snapshot (default is 100).</param>
    /// <returns>A configured metrics aggregator.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is less than 1.</exception>
    public static MetricsAggregator WithRepeatedSnapshots(
        this MetricsAggregatorTests test,
        double successRate,
        int count,
        double avgExecutionMs = 50,
        long total = 100)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentOutOfRangeException.ThrowIfLessThan(count, 1);

        var aggregator = new MetricsAggregator();
        var successfulExecutions = (long)(total * successRate / 100);
        var failedExecutions = (long)(total * (1 - successRate / 100));

        for (var i = 0; i < count; i++)
        {
            var snapshot = new MetricsSnapshot
            {
                Timestamp = DateTime.UtcNow,
                SuccessRate = successRate,
                AverageExecutionTimeMs = avgExecutionMs,
                TotalExecutions = total,
                SuccessfulExecutions = successfulExecutions,
                FailedExecutions = failedExecutions
            };
            aggregator.RecordSnapshot(snapshot);
        }

        return aggregator;
    }

    /// <summary>
    /// Asserts that the aggregated metrics have the expected values.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="aggregator">The aggregator to test.</param>
    /// <param name="expectedSnapshotCount">The expected snapshot count.</param>
    /// <param name="expectedAvgSuccessRate">The expected average success rate.</param>
    /// <param name="expectedTotalExecutions">The expected total executions.</param>
    /// <param name="expectedPeakExecutions">The expected peak executions.</param>
    /// <param name="expectedMinSuccessRate">The expected minimum success rate (optional).</param>
    /// <param name="expectedMaxSuccessRate">The expected maximum success rate (optional).</param>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="aggregator"/> is <see langword="null"/>.</exception>
    public static void ShouldHaveMetrics(
        this MetricsAggregatorTests test,
        MetricsAggregator aggregator,
        int expectedSnapshotCount,
        double expectedAvgSuccessRate,
        long expectedTotalExecutions,
        long expectedPeakExecutions,
        double? expectedMinSuccessRate = null,
        double? expectedMaxSuccessRate = null)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(aggregator);

        var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(1));

        metrics.SnapshotCount.Should().Be(expectedSnapshotCount);
        metrics.AverageSuccessRate.Should().BeApproximately(expectedAvgSuccessRate, 0.01);
        metrics.TotalExecutions.Should().Be(expectedTotalExecutions);
        metrics.PeakExecutions.Should().Be(expectedPeakExecutions);

        if (expectedMinSuccessRate is not null)
        {
            metrics.MinSuccessRate.Should().BeApproximately(expectedMinSuccessRate.Value, 0.01);
        }

        if (expectedMaxSuccessRate is not null)
        {
            metrics.MaxSuccessRate.Should().BeApproximately(expectedMaxSuccessRate.Value, 0.01);
        }
    }

    /// <summary>
    /// Asserts that the trend analysis has the expected direction and anomaly state.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="trend">The trend to analyze.</param>
    /// <param name="expectedDirection">The expected direction (Increasing, Decreasing, or Stable).</param>
    /// <param name="expectedIsAnomaly">The expected anomaly state.</param>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="trend"/> is <see langword="null"/>.</exception>
    /// <remarks>
    /// Uses pattern matching for null checks and direct property assertions.
    /// </remarks>
    public static void ShouldHaveTrend(
        this MetricsAggregatorTests test,
        MetricsTrend trend,
        string expectedDirection,
        bool expectedIsAnomaly)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(trend);

        trend.Direction.Should().Be(expectedDirection);
        trend.IsAnomaly.Should().Be(expectedIsAnomaly);
    }

    /// <summary>
    /// Creates a metrics aggregator with snapshots spaced at specific time intervals.
    /// </summary>
    /// <param name="test">The test instance.</param>
    /// <param name="successRates">The success rates for each snapshot.</param>
    /// <param name="timeIntervals">The time intervals between snapshots.</param>
    /// <param name="avgExecutionMs">The average execution time in milliseconds (default is 50).</param>
    /// <param name="total">The total executions per snapshot (default is 100).</param>
    /// <returns>A configured metrics aggregator with time-spaced snapshots.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="test"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="successRates"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentNullException"><paramref name="timeIntervals"/> is <see langword="null"/>.</exception>
    public static MetricsAggregator WithTimeSpacedSnapshots(
        this MetricsAggregatorTests test,
        IEnumerable<double> successRates,
        IEnumerable<TimeSpan> timeIntervals,
        double avgExecutionMs = 50,
        long total = 100)
    {
        ArgumentNullException.ThrowIfNull(test);
        ArgumentNullException.ThrowIfNull(successRates);
        ArgumentNullException.ThrowIfNull(timeIntervals);

        var aggregator = new MetricsAggregator();
        var currentTime = DateTime.UtcNow;

        foreach (var (rate, interval) in successRates.Zip(timeIntervals, static (r, i) => (r, i)))
        {
            currentTime += interval;
            var successfulExecutions = (long)(total * rate / 100);
            var failedExecutions = (long)(total * (1 - rate / 100));

            var snapshot = new MetricsSnapshot
            {
                Timestamp = currentTime,
                SuccessRate = rate,
                AverageExecutionTimeMs = avgExecutionMs,
                TotalExecutions = total,
                SuccessfulExecutions = successfulExecutions,
                FailedExecutions = failedExecutions
            };
            aggregator.RecordSnapshot(snapshot);
        }

        return aggregator;
    }
}