using Xunit;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Tests;

public class PerformanceMonitorTests
{
    [Fact]
    public void RecordExecution_ShouldAccumulateMetricsCorrectly()
    {
        // Arrange
        var monitor = new PerformanceMonitor();
        const string policyName = "TestPolicy";

        // Act
        monitor.RecordExecution(policyName, 100, true);
        monitor.RecordExecution(policyName, 200, false);

        // Assert
        var metrics = monitor.GetMetrics(policyName);
        Assert.Equal(2, metrics.TotalExecutions);
        Assert.Equal(300, metrics.TotalDurationMs);
        Assert.Equal(150, metrics.AverageDurationMs);
        Assert.Equal(1, metrics.SuccessfulExecutions);
        Assert.Equal(1, metrics.FailedExecutions);
        Assert.Equal(50.0, metrics.SuccessRate);
    }

    [Fact]
    public void GetAllMetrics_ShouldReturnAllRecordedPolicies()
    {
        // Arrange
        var monitor = new PerformanceMonitor();

        // Act
        monitor.RecordExecution("PolicyA", 100, true);
        monitor.RecordExecution("PolicyB", 100, true);

        // Assert
        var allMetrics = monitor.GetAllMetrics();
        Assert.Equal(2, allMetrics.Count);
        Assert.Contains(allMetrics, m => m.PolicyName == "PolicyA");
        Assert.Contains(allMetrics, m => m.PolicyName == "PolicyB");
    }

    [Fact]
    public void IdentifyPerformanceIssues_ShouldDetectSlowExecutionAndHighFailure()
    {
        // Arrange
        var monitor = new PerformanceMonitor();
        // Slow execution (avg > 1000)
        monitor.RecordExecution("SlowPolicy", 2000, true);
        // High failure rate (> 50%)
        monitor.RecordExecution("FailingPolicy", 100, false);
        monitor.RecordExecution("FailingPolicy", 100, false);

        // Act
        var issues = monitor.IdentifyPerformanceIssues(slowThresholdMs: 1000);

        // Assert
        Assert.Equal(2, issues.Count);
        var slowIssue = issues.First(i => i.PolicyName == "SlowPolicy");
        Assert.Equal("SlowExecution", slowIssue.IssueType);
        Assert.Equal("Warning", slowIssue.Severity);

        var failIssue = issues.First(i => i.PolicyName == "FailingPolicy");
        Assert.Equal("HighFailureRate", failIssue.IssueType);
    }

    [Fact]
    public void Clear_ShouldResetAllMetrics()
    {
        // Arrange
        var monitor = new PerformanceMonitor();
        monitor.RecordExecution("Policy1", 100, true);

        // Act
        monitor.Clear();

        // Assert
        Assert.Empty(monitor.GetAllMetrics());
        var metrics = monitor.GetMetrics("Policy1");
        Assert.Equal(0, metrics.TotalExecutions);
    }

    [Fact]
    public void ComparePerformance_ShouldCalculateRelativePerformance()
    {
        // Arrange
        var monitor = new PerformanceMonitor();
        monitor.RecordExecution("Fast", 100, true);
        monitor.RecordExecution("Slow", 200, true);

        // Act
        var comparisons = monitor.ComparePerformance();

        // Assert
        Assert.Equal(2, comparisons.Count);
        // Ordered by AverageDurationMs descending
        Assert.Equal("Slow", comparisons[0].PolicyName);
        Assert.Equal("Fast", comparisons[1].PolicyName);

        // Slowest is 200ms. Fast is 100ms. Fast should be 50% of slowest.
        Assert.Equal(50.0, comparisons[1].PercentageOfSlowest);
        // Slowest is 100% of itself.
        Assert.Equal(100.0, comparisons[0].PercentageOfSlowest);
    }

    [Fact]
    public void GetMetrics_UnknownPolicy_ShouldReturnEmptyMetrics()
    {
        // Arrange
        var monitor = new PerformanceMonitor();

        // Act
        var metrics = monitor.GetMetrics("NonExistent");

        // Assert
        Assert.Equal("NonExistent", metrics.PolicyName);
        Assert.Equal(0, metrics.TotalExecutions);
        Assert.Equal(0, metrics.TotalDurationMs);
    }
}
