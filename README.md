# DotNet Resilience Pipeline

A comprehensive resilience library for .NET applications providing circuit breakers, retries, timeouts, bulkheads, and fallbacks with comprehensive metrics and health monitoring.

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

## MetricsExporter

The `MetricsExporter` class exports resilience pipeline metrics in multiple formats: JSON, CSV, and Prometheus text exposition format. It provides methods to export pipeline-level aggregated metrics as well as per-policy metrics with detailed execution statistics including total executions, successful/failed executions, success rate, retry count, circuit breaker trips, and timeout events.


Here's an example usage:

```csharp
// Create a metrics exporter
var exporter = new MetricsExporter();

// Create sample pipeline metrics snapshot
var snapshot = new PipelineMetricsSnapshot
{
    TotalExecutions = 10000,
    SuccessfulExecutions = 9500,
    FailedExecutions = 500,
    SuccessRate = 95.0,
    RetryCount = 250,
    CircuitBreakerTrips = 15,
    TimeoutCount = 8,
    PolicySnapshots = new List<PolicySnapshot>
    {
        new PolicySnapshot
        {
            PolicyId = "retry-policy-001",
            PolicyName = "Payment Processing Retry",
            PolicyType = "RetryPolicy",
            IsEnabled = true,
            TotalExecutions = 5000,
            SuccessfulExecutions = 4800,
            FailedExecutions = 200,
            SuccessRate = 96.0,
            SnapshotTime = DateTime.UtcNow
        },
        new PolicySnapshot
        {
            PolicyId = "circuit-breaker-002", 
            PolicyName = "Order Processing Circuit Breaker",
            PolicyType = "CircuitBreakerPolicy",
            IsEnabled = true,
            TotalExecutions = 3000,
            SuccessfulExecutions = 2700,
            FailedExecutions = 300,
            SuccessRate = 90.0,
            SnapshotTime = DateTime.UtcNow,
            Metadata = new Dictionary<string, object> { { "CircuitState", "Closed" } }
        },
        new PolicySnapshot
        {
            PolicyId = "timeout-policy-003",
            PolicyName = "API Timeout Policy",
            PolicyType = "TimeoutPolicy",
            IsEnabled = true,
            TotalExecutions = 2000,
            SuccessfulExecutions = 1950,
            FailedExecutions = 50,
            SuccessRate = 97.5,
            SnapshotTime = DateTime.UtcNow
        }
    }
};

// Export metrics in JSON format
string jsonMetrics = exporter.ExportJson(snapshot);
Console.WriteLine("JSON Metrics:");
Console.WriteLine(jsonMetrics);

// Export metrics in CSV format
string csvMetrics = exporter.ExportCsv(snapshot);
Console.WriteLine("\nCSV Metrics:");
Console.WriteLine(csvMetrics);

// Export metrics in Prometheus format
string prometheusMetrics = exporter.ExportPrometheus(snapshot);
Console.WriteLine("\nPrometheus Metrics:");
Console.WriteLine(prometheusMetrics);
```

## CsvReportFormatter

The `CsvReportFormatter` class formats resilience pipeline metrics, policies, execution history, performance data, logs, and errors as CSV for spreadsheet analysis and reporting. It provides methods to export various types of resilience data in a structured CSV format suitable for import into Excel, Google Sheets, or data analysis tools.




Here's an example usage:

```csharp
// Create a CSV formatter
var formatter = new CsvReportFormatter();

// Create sample pipeline statistics
var pipelineStats = new PipelineStatistics
{
    PipelineId = "order-processing-pipeline",
    CreatedAt = DateTime.UtcNow,
    TotalExecutions = 10000,
    SuccessfulExecutions = 9500,
    FailedExecutions = 500,
    SuccessRate = 95.0,
    PolicyCount = 5
};

// Format pipeline metrics as CSV
string pipelineMetricsCsv = formatter.FormatPipelineMetrics(pipelineStats);
Console.WriteLine("Pipeline Metrics CSV:");
Console.WriteLine(pipelineMetricsCsv);

// Create sample performance metrics
var performanceMetrics = new List<PerformanceMetrics>
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

// Format performance metrics as CSV
string performanceMetricsCsv = formatter.FormatPerformanceMetrics(performanceMetrics);
Console.WriteLine("\nPerformance Metrics CSV:");
Console.WriteLine(performanceMetricsCsv);

// Create sample execution records
var executionRecords = new List<ExecutionRecord>
{
    new ExecutionRecord
    {
        Timestamp = DateTime.UtcNow.AddMinutes(-5),
        PolicyName = "order-processing-circuit-breaker",
        IsSuccess = true,
        ExecutionTimeMs = 45
    },
    new ExecutionRecord
    {
        Timestamp = DateTime.UtcNow.AddMinutes(-3),
        PolicyName = "payment-processing-retry",
        IsSuccess = false,
        ExecutionTimeMs = 120
    }
};

// Format execution history as CSV
string executionHistoryCsv = formatter.FormatExecutionHistory(executionRecords);
Console.WriteLine("\nExecution History CSV:");
Console.WriteLine(executionHistoryCsv);

// Export to file
await formatter.ExportToFileAsync(pipelineMetricsCsv, "/tmp/pipeline-metrics.csv");
```

## JsonPolicySerializer

The `JsonPolicySerializer` class provides serialization and deserialization functionality for `ResiliencyPolicy` objects to and from JSON format. It supports serializing single policies, multiple policies, metrics, and file operations for importing/exporting policy configurations.


Here's an example usage:

```csharp
// Create a policy serializer
var serializer = new JsonPolicySerializer("order-processing-policies");

// Create a sample resiliency policy
var policy = new ResiliencyPolicy
{
    Id = "policy-001",
    Name = "Order Processing Circuit Breaker",
    Type = "CircuitBreaker",
    IsEnabled = true,
    CreatedAt = DateTime.UtcNow,
    FailureThreshold = 5,
    OpenDurationSeconds = 30,
    SuccessThreshold = 3,
    MaxRetries = 3,
    InitialDelayMs = 100,
    Strategy = "ExponentialBackoff",
    BackoffMultiplier = 2.0,
    TimeoutSeconds = 10
};

// Serialize a single policy to JSON
string json = serializer.Serialize(policy);
Console.WriteLine(json);

// Serialize multiple policies to JSON
var policies = new List<ResiliencyPolicy> { policy };
string multipleJson = serializer.SerializeMultiple(policies);
Console.WriteLine(multipleJson);

// Serialize metrics for a policy
string metricsJson = serializer.SerializeMetrics(policy);
Console.WriteLine(metricsJson);

// Deserialize a policy from JSON
string policyJson = @"{
    \"Id\": \"policy-001\",
    \"Name\": \"Order Processing Circuit Breaker\",
    \"Type\": \"CircuitBreaker\",
    \"IsEnabled\": true,
    \"CreatedAt\": \"2024-01-15T10:30:00Z\",
    \"FailureThreshold\": 5,
    \"OpenDurationSeconds\": 30,
    \"SuccessThreshold\": 3,
    \"MaxRetries\": 3,
    \"InitialDelayMs\": 100,
    \"Strategy\": \"ExponentialBackoff\",
    \"BackoffMultiplier\": 2.0,
    \"TimeoutSeconds\": 10
}";

ResiliencyPolicy? deserializedPolicy = serializer.Deserialize(policyJson);
if (deserializedPolicy != null)
{
    Console.WriteLine($"Deserialized policy: {deserializedPolicy.Name}");
}

// Export policies to a file
await serializer.ExportToFileAsync(policies, "/tmp/policies.json");

// Import policies from a file
var importedPolicies = await serializer.ImportFromFileAsync("/tmp/policies.json");
Console.WriteLine($"Imported {importedPolicies.Count} policies");
```
