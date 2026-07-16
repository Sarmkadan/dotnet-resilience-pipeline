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

## MetricsAggregatorTests

The `MetricsAggregatorTests` class provides unit tests for the `MetricsAggregator` class, verifying its functionality for recording metrics snapshots, aggregating metrics over time windows, analyzing trends, and managing snapshot history.



Here's an example usage:

```csharp
// Create a metrics aggregator
var aggregator = new MetricsAggregator();

// Record metrics snapshots
aggregator.RecordSnapshot(new MetricsSnapshot
{
    Timestamp = DateTime.UtcNow,
    SuccessRate = 95.0,
    AverageExecutionTimeMs = 45.5,
    TotalExecutions = 1000,
    SuccessfulExecutions = 950,
    FailedExecutions = 50
});

aggregator.RecordSnapshot(new MetricsSnapshot
{
    Timestamp = DateTime.UtcNow.AddMinutes(-5),
    SuccessRate = 92.0,
    AverageExecutionTimeMs = 50.2,
    TotalExecutions = 800,
    SuccessfulExecutions = 736,
    FailedExecutions = 64
});

// Get aggregated metrics for the last hour
var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromHours(1));

Console.WriteLine($"Snapshot Count: {metrics.SnapshotCount}");
Console.WriteLine($"Average Success Rate: {metrics.AverageSuccessRate:P}");
Console.WriteLine($"Min Success Rate: {metrics.MinSuccessRate:P}");
Console.WriteLine($"Max Success Rate: {metrics.MaxSuccessRate:P}");
Console.WriteLine($"Total Executions: {metrics.TotalExecutions}");
Console.WriteLine($"Peak Executions: {metrics.PeakExecutions}");
Console.WriteLine($"Average Execution Time: {metrics.AverageExecutionTimeMs}ms");

// Analyze trend
var trend = aggregator.AnalyzeTrend(TimeSpan.FromHours(1), "SuccessRate");
Console.WriteLine($"Trend Direction: {trend.Direction}");
Console.WriteLine($"Is Anomaly: {trend.IsAnomaly}");
Console.WriteLine($"Change Percentage: {trend.ChangePercentage:P}");

// Clear all snapshots
aggregator.Clear();
```

## TimeoutPolicyTests

The `TimeoutPolicyTests` class provides unit tests for the `TimeoutPolicy` class, verifying its functionality for timeout validation, execution time tracking, timeout detection, configuration validation, and metrics recording.


Here's an example usage:

```csharp
// Create a timeout policy with 5 second timeout
var policy = new TimeoutPolicy("api-timeout-policy")
{
    Timeout = TimeSpan.FromSeconds(5),
    IsEnabled = true
};

// Record execution times to track performance
policy.RecordExecutionTime(150);  // 150ms execution
policy.RecordExecutionTime(220);  // 220ms execution
policy.RecordExecutionTime(95);   // 95ms execution

// Check if operations would timeout
hasTimeout = policy.IsTimedOut(TimeSpan.FromSeconds(3));
    // Returns false - 3 seconds < 5 second timeout

hasTimeout = policy.IsTimedOut(TimeSpan.FromSeconds(10));
    // Returns true - 10 seconds > 5 second timeout

// Check timeout in milliseconds
hasTimeoutMs = policy.IsTimedOutMs(6000);
    // Returns true - 6000ms > 5000ms timeout

// Get performance metrics
averageTime = policy.AverageExecutionTimeMs;
    // Returns 155 (average of 150, 220, 95)
longestTime = policy.LongestExecutionTimeMs;
    // Returns 220
shortestTime = policy.ShortestExecutionTimeMs;
    // Returns 95

p95Time = policy.GetPercentile95ExecutionTime();
    // Returns 220 (95th percentile of [95, 150, 220])
p99Time = policy.GetPercentile99ExecutionTime();
    // Returns 220 (99th percentile of [95, 150, 220])

// Record a timeout event
policy.RecordTimeout(5500);
    // Increments timeout counter and records failure

// Check timeout statistics
timeoutCount = policy.TimeoutCount;
    // Returns 1
timeoutPercentage = policy.GetTimeoutPercentage();
    // Returns 25 (1 timeout out of 4 total executions)

// Validate configuration
isValid = policy.IsValidConfiguration(out var error);
    // Returns true, error is null

// Reset statistics for new measurement period
policy.ResetStatistics();
    // Clears all metrics and counters
```

## FallbackServiceTests

The `FallbackServiceTests` class provides unit tests for the `FallbackService` class, verifying its functionality for executing fallback operations with various policy configurations, handling exceptions, timeout scenarios, and metrics recording.





Here's an example usage:

```csharp
// Create a fallback service
var service = new FallbackService();

// Create a fallback policy with specific trigger exceptions
var policy = new FallbackPolicy("order-processing-fallback")
{
    FallbackOnAnyException = false,
    FallbackTriggerExceptions = new List<Type> { typeof(InvalidOperationException) },
    FallbackTimeout = TimeSpan.FromSeconds(5)
};

// Set a fallback action that returns a default value
policy.SetFallbackAction<string>(async (ct) => "default-order");

// Execute with a primary operation that fails
var result = await service.ExecuteAsync<string>(
    policy,
    new InvalidOperationException("Database unavailable"),
    timeoutMilliseconds: 1000,
    CancellationToken.None
);

if (result.IsSuccess)
{
    Console.WriteLine($"Fallback succeeded: {result.Data}");
}
else
{
    Console.WriteLine($"Fallback failed: {result.Exception?.Message}");
}

// Check fallback metrics
var successRate = service.GetFallbackSuccessRate(policy);
Console.WriteLine($"Fallback success rate: {successRate:P}");

// Add a new exception type to trigger fallback
service.AddFallbackTrigger(policy, typeof(TimeoutException));

// Remove a fallback trigger
service.RemoveFallbackTrigger(policy, typeof(InvalidOperationException));
```

## TimeoutServiceTests

The `TimeoutServiceTests` class provides unit tests for the `TimeoutService` class, verifying its functionality for executing operations with timeout policies, handling timeout scenarios, validating policy configurations, and recording execution metrics.

Here's an example usage:

```csharp
// Create a timeout service
var service = new TimeoutService();

// Create a timeout policy with 2 second timeout
var policy = new TimeoutPolicy("api-timeout")
{
    Timeout = TimeSpan.FromSeconds(2),
    IsEnabled = true
};

// Example 1: Execute a successful operation within timeout
var result = await service.ExecuteAsync<string>(
    policy,
    async ct => "successful-result"
);

Console.WriteLine($"Result: {result}");
Console.WriteLine($"Successful executions: {policy.SuccessfulExecutions}");

// Example 2: Execute with disabled policy (bypasses timeout)
var disabledPolicy = new TimeoutPolicy("no-timeout")
{
    Timeout = TimeSpan.FromSeconds(1),
    IsEnabled = false
};

var disabledResult = await service.ExecuteAsync<string>(
    disabledPolicy,
    async ct => 
    {
        await Task.Delay(5000, ct); // 5 second delay
        return "completed-despite-timeout";
    }
);

Console.WriteLine($"Disabled policy result: {disabledResult}");

// Example 3: Check if timeout has been exceeded
var hasExceeded = service.HasExceededTimeout(policy, 2500); // 2.5 seconds
Console.WriteLine($"Timeout exceeded: {hasExceeded}");

// Example 4: Get timeout in milliseconds
var timeoutMs = service.GetTimeoutMilliseconds(policy);
Console.WriteLine($"Timeout in milliseconds: {timeoutMs}");

// Example 5: Handle null policy gracefully
var safeResult = await service.ExecuteAsync<string>(
    null,
    ct => Task.FromResult("safe-execution")
);
```

## CircuitBreakerPolicyTests

The `CircuitBreakerPolicyTests` class provides unit tests for the `CircuitBreakerPolicy` class, verifying its functionality for circuit breaker state transitions, failure threshold handling, success threshold validation, and manual reset operations.

Here's an example usage:

```csharp
// Create a circuit breaker policy with specific thresholds
var policy = new CircuitBreakerPolicy("payment-processing-circuit")
{
    FailureThreshold = 3,
    SuccessThresholdInHalfOpen = 2,
    OpenDuration = TimeSpan.FromSeconds(30)
};

// Test 1: Verify circuit opens at failure threshold
policy.RecordFailure(); // 1st failure
policy.RecordFailure(); // 2nd failure
policy.RecordFailure(); // 3rd failure - circuit opens
Console.WriteLine($"Circuit state after threshold: {policy.CurrentState}"); // Should be Open

// Test 2: Verify circuit remains closed below threshold
var inventoryPolicy = new CircuitBreakerPolicy("inventory-circuit")
{
    FailureThreshold = 5
};
inventoryPolicy.RecordFailure(); // 1st failure
inventoryPolicy.RecordFailure(); // 2nd failure
Console.WriteLine($"Circuit state below threshold: {inventoryPolicy.CurrentState}"); // Should be Closed

// Test 3: Verify manual reset clears statistics
policy.ManualReset();
Console.WriteLine($"After manual reset - State: {policy.CurrentState}, Failures: {policy.ConsecutiveFailures}"); // Closed, 0

// Test 4: Verify half-open to closed transition
var orderPolicy = new CircuitBreakerPolicy("order-processing-circuit")
{
    FailureThreshold = 1,
    SuccessThresholdInHalfOpen = 2,
    OpenDuration = TimeSpan.Zero // Allows instant transition to HalfOpen
};
orderPolicy.RecordFailure(); // Opens circuit
orderPolicy.AttemptReset(); // Transitions to HalfOpen
orderPolicy.RecordSuccess(); // 1st success in HalfOpen
orderPolicy.RecordSuccess(); // 2nd success - meets threshold, transitions to Closed
Console.WriteLine($"Circuit state after success threshold: {orderPolicy.CurrentState}"); // Should be Closed
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
