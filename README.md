// ... (rest of the README content)

## MetricsFormatter

The `MetricsFormatter` class provides a set of methods for formatting resilience metrics into human-readable ASCII tables, reports, and summaries. It supports formatting metrics tables, aggregated metrics, trends, health status, and comparisons.

Here's an example usage:

```csharp
var formatter = new MetricsFormatter();

// Create some sample performance metrics
var metrics = new List<PerformanceMetrics>
{
    new PerformanceMetrics
    {
        PolicyName = "order-processing-circuit-breaker",
        TotalExecutions = 1000,
        SuccessfulExecutions = 950,
        FailedExecutions = 50,
        AverageDurationMs = 45.5,
        P50 = 30,
        P90 = 60,
        P99 = 100,
        ThroughputPerSecond = 10.5
    },
    new PerformanceMetrics
    {
        PolicyName = "payment-processing-retry",
        TotalExecutions = 500,
        SuccessfulExecutions = 475,
        FailedExecutions = 25,
        AverageDurationMs = 20.2,
        P50 = 15,
        P90 = 30,
        P99 = 50,
        ThroughputPerSecond = 5.2
    }
};

// Format metrics table
var metricsTable = formatter.FormatMetricsTable(metrics);
Console.WriteLine(metricsTable);

// Format aggregated metrics
var aggregatedMetrics = new AggregatedMetrics
{
    TimeWindow = TimeSpan.FromHours(1),
    SnapshotCount = 10,
    AverageSuccessRate = 0.95,
    MinSuccessRate = 0.90,
    MaxSuccessRate = 0.98,
    AverageExecutionTimeMs = 40.0,
    PeakExecutions = 150,
    TotalExecutions = 10000
};
var aggregatedMetricsReport = formatter.FormatAggregatedMetrics(aggregatedMetrics);
Console.WriteLine(aggregatedMetricsReport);

// Format trend analysis
var trend = new MetricsTrend
{
    MetricType = "SuccessRate",
    Direction = "Increasing",
    ChangePercentage = 5.0,
    DataPoints = 20,
    Previous = 0.92,
    Current = 0.97
};
var trendAnalysis = formatter.FormatTrend(trend);
Console.WriteLine(trendAnalysis);

// Format health status
var healthStatus = formatter.FormatHealthStatus("Healthy", 0.98, 10000);
Console.WriteLine(healthStatus);

// Format comparison report
var comparison = new PeriodComparison
{
    Period1 = TimeSpan.FromHours(1),
    Metrics1 = new MetricsSnapshot
    {
        AverageSuccessRate = 0.95,
        AverageExecutionTimeMs = 45.0
    },
    Period2 = TimeSpan.FromHours(2),
    Metrics2 = new MetricsSnapshot
    {
        AverageSuccessRate = 0.92,
        AverageExecutionTimeMs = 50.0
    },
    SuccessRateDifference = 0.03,
    ExecutionTimeDifference = -5.0,
    IsImproving = true
};
var comparisonReport = formatter.FormatComparison(comparison);
Console.WriteLine(comparisonReport);
```

// ... (rest of the README content)
