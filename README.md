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
