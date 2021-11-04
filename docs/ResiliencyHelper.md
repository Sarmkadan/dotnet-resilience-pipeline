# ResiliencyHelper

Provides utility methods for converting between execution records and policy results, validating pipeline configuration, generating health reports, and exposing runtime statistics for a resilience pipeline.

## API

### CreateResultFromRecord<T>(ExecutionRecord record)
**Purpose:** Transforms an `ExecutionRecord` into a `PolicyResult<T>` that captures the outcome and any associated value of type `T`.  
**Parameters:**  
- `record`: The execution record to convert.  
**Return value:** A `PolicyResult<T>` representing the pipeline execution result.  
**Exceptions:**  
- `ArgumentNullException` if `record` is `null`.  
- `InvalidOperationException` if the record does not contain a value compatible with type `T`.

### CreateRecordFromResult<T>(PolicyResult<T> result)
**Purpose:** Builds an `ExecutionRecord` from a `PolicyResult<T>` for logging or further processing.  
**Parameters:**  
- `result`: The policy result to convert.  
**Return value:** An `ExecutionRecord` encapsulating the outcome and value.  
**Exceptions:**  
- `ArgumentNullException` if `result` is `null`.  
- `InvalidOperationException` if the result's value cannot be represented as part of an execution record.

### ValidatePolicy()
**Purpose:** Checks the current pipeline policy configuration for correctness.  
**Parameters:** None.  
**Return value:** A `List<string>` containing validation error messages; an empty list indicates the configuration is valid.  
**Exceptions:**  
- `InvalidOperationException` if the helper has not been initialized with a pipeline configuration.

### GenerateHealthReport()
**Purpose:** Produces a snapshot of the pipeline's health based on accumulated execution data.  
**Parameters:** None.  
**Return value:** A `PipelineHealthReport` containing metrics such as success rates, latency, and error counts.  
**Exceptions:**  
- `InvalidOperationException` if no execution data has been recorded yet.

### DeterminePipelineHealth()
**Purpose:** Evaluates the overall health status of the pipeline using recent execution trends.  
**Parameters:** None.  
**Return value:** A `HealthStatus` enum value (`Healthy`, `Degraded`, or `Unhealthy`).  
**Exceptions:**  
- `InvalidOperationException` if insufficient data exists to make a determination.

### ExportPolicyConfig()
**Purpose:** Serializes the current policy configuration into a dictionary for inspection, debugging, or persistence.  
**Parameters:** None.  
**Return value:** A `Dictionary<string, object>` where keys are policy names and values are their respective settings.  
**Exceptions:**  
- `InvalidOperationException` if the configuration cannot be serialized (e.g., contains non‑serializable objects).

### PipelineId
**Purpose:** Unique identifier for the pipeline instance.  
**Type:** `string` (read‑only).  
**Remarks:** Set when the helper is created; never changes during the lifetime of the instance.

### ReportGeneratedAt
**Purpose:** Timestamp of the most recent health report generation.  
**Type:** `DateTime` (read‑only).  
**Remarks:** Updated each time `GenerateHealthReport` is called.

### TotalExecutions
**Purpose:** Cumulative count of pipeline executions recorded.  
**Type:** `long` (read‑only).  
**Remarks:** Incremented automatically on each pipeline run.

### SuccessRate
**Purpose:** Percentage of successful executions (0.0 to 100.0).  
**Type:** `double` (read‑only).  
**Remarks:** Derived from `TotalExecutions` and the count of successful runs; returns `0.0` when no executions have occurred.

### PolicyCount
**Purpose:** Number of policies currently configured in the pipeline.  
**Type:** `int` (read‑only).  
**Remarks:** Reflects the count of items in the `Policies` collection.

### HealthStatus
**Purpose:** Current health status as determined by `DeterminePipelineHealth`.  
**Type:** `HealthStatus` (read‑only).  
**Remarks:** Updated whenever health evaluation is performed.

### Policies
**Purpose:** Collection of snapshots for each policy in the pipeline.  
**Type:** `List<PolicySnapshot>` (read‑only).  
**Remarks:** Provides detailed per‑policy metrics such as invocation count, failure rate, and average latency.

### HistoryStatistics
**Purpose:** Additional historical metrics collected over the pipeline's lifetime.  
**Type:** `Dictionary<string, object>` (read‑only).  
**Remarks:** Keys are metric names; values are the corresponding statistical data.

## Usage

### Example 1: Converting between records and results, validating, and reporting health
```csharp
using DotNetResiliencePipeline;

// Assume `record` is an ExecutionRecord obtained from a pipeline execution.
ExecutionRecord record = GetLastExecutionRecord();

// Convert the record to a typed result.
PolicyResult<int> result = ResiliencyHelper.CreateResultFromRecord<int>(record);

// Later, convert the result back to a record for logging.
ExecutionRecord loggedRecord = ResiliencyHelper.CreateRecordFromResult<int>(result);

// Validate the pipeline configuration.
List<string> validationErrors = ResiliencyHelper.ValidatePolicy();
if (validationErrors.Count > 0)
{
    foreach (var err in validationErrors)
    {
        Console.WriteLine($"Validation error: {err}");
    }
}

// Generate and inspect a health report.
PipelineHealthReport report = ResiliencyHelper.GenerateHealthReport();
Console.WriteLine($"Success rate: {report.SuccessRate}%");
Console.WriteLine($"Average latency: {report.AverageLatencyMs} ms");
```

### Example 2: Monitoring pipeline health via instance properties
```csharp
using DotNetResiliencePipeline;

// Obtain a helper instance tied to a specific pipeline.
ResiliencyHelper helper = new ResiliencyHelper(pipelineId: "order-processing");

// Simulate some executions (internal to the pipeline).
RunPipelineSeveralTimes();

// Access runtime statistics.
Console.WriteLine($"Pipeline ID: {helper.PipelineId}");
Console.WriteLine($"Total executions: {helper.TotalExecutions}");
Console.WriteLine($"Success rate: {helper.SuccessRate:F2}%");
Console.WriteLine($"Policy count: {helper.PolicyCount}");
Console.WriteLine($"Current health: {helper.HealthStatus");

// Examine per‑policy snapshots.
foreach (var snapshot in helper.Policies)
{
    Console.WriteLine($"Policy {snapshot.Name}: {snapshot.InvocationCount} invocations, {snapshot.FailureRate:P1} failure rate");
}

// Export configuration for debugging.
var config = ResiliencyHelper.ExportPolicyConfig();
foreach (var kvp in config)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}
```

## Notes
- **Null arguments:** All static methods that accept an argument throw `ArgumentNullException` when the argument is `null`.  
- **Type mismatches:** `CreateResultFromRecord<T>` and `CreateRecordFromResult<T>` throw `InvalidOperationException` if the underlying value cannot be cast to or from the requested type `T`.  
- **Validation:** `ValidatePolicy` returns an empty list when the configuration is sound; it does not throw unless the helper is uninitialized.  
- **Health reporting:** `GenerateHealthReport` and `DeterminePipelineHealth` require at least one execution to have been recorded; otherwise they throw `InvalidOperationException`.  
- **Success rate calculation:** When `TotalExecutions` is zero, `SuccessRate` returns `0.0` to avoid division by zero.  
- **Thread safety:** The static methods are safe to call concurrently from multiple threads as they operate on immutable inputs and do not modify shared state. Instance properties are read‑only after initialization; they are updated internally by the pipeline execution logic, which should employ appropriate synchronization. Consumers may safely read the properties and enumerations returned by `Policies` and `HistoryStatistics` without additional locking, but must not modify the returned collections.  
- **Exported configuration:** The dictionary returned by `ExportPolicyConfig` may contain values of varying types; callers should perform type checks or casts as needed for their specific use case.  
- **Enumerable returns:** The `List<string>` from `ValidatePolicy`, the `List<PolicySnapshot>` from `Policies`, and the `Dictionary<string, object>` from `HistoryStatistics` return live references; altering them after retrieval can lead to undefined behavior. Treat them as immutable snapshots.
