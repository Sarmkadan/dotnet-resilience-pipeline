using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Utilities;
using DotNetResiliencePipeline.Workers;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class MetricsCollectorWorkerTests
{
    private readonly ResiliencyPipelineService _pipelineService;
    private readonly MetricsAggregator _aggregator;
    private readonly MetricsCollectorWorker _worker;

    public MetricsCollectorWorkerTests()
    {
        _pipelineService = new ResiliencyPipelineService();
        _aggregator = new MetricsAggregator();
        _worker = new MetricsCollectorWorker(_pipelineService, _aggregator);
    }

    [Fact]
    public void Constructor_SetsDefaultValues()
    {
        _worker.CollectionInterval.Should().Be(TimeSpan.FromSeconds(10));
        _worker.IsRunning.Should().BeFalse();
        _worker.TotalCollections.Should().Be(0);
    }

    [Fact]
    public async Task Start_SetsIsRunningToTrue()
    {
        _worker.Start();
        _worker.IsRunning.Should().BeTrue();
        
        // Cleanup
        await _worker.StopAsync();
    }

    [Fact]
    public async Task StopAsync_SetsIsRunningToFalse()
    {
        _worker.Start();
        await _worker.StopAsync();
        _worker.IsRunning.Should().BeFalse();
    }

    [Fact]
    public void GetStatus_ReturnsCorrectStatus()
    {
        var status = _worker.GetStatus();
        status.IsRunning.Should().BeFalse();
        status.TotalCollections.Should().Be(0);
        status.RecentMetrics.Should().NotBeNull();
    }

    [Fact]
    public void GetMetricsForTimeRange_ReturnsAggregatedMetrics()
    {
        // Act
        var metrics = _worker.GetMetricsForTimeRange(TimeSpan.FromMinutes(5));

        // Assert
        metrics.Should().NotBeNull();
    }

    [Fact]
    public void GetTrendAnalysis_ReturnsMetricsTrend()
    {
        // Act
        var trend = _worker.GetTrendAnalysis(TimeSpan.FromMinutes(5));

        // Assert
        trend.Should().NotBeNull();
        trend.MetricType.Should().Be("SuccessRate");
    }

    [Fact]
    public void GenerateReport_ReturnsPerformanceReport()
    {
        // Act
        var report = _worker.GenerateReport(TimeSpan.FromMinutes(5));

        // Assert
        report.Should().NotBeNull();
        report.AggregatedMetrics.Should().NotBeNull();
    }
}
