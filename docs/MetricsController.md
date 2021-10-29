# MetricsController
The `MetricsController` provides access to runtime metrics collected by the resilience pipeline, enabling monitoring of pipeline health, policy effectiveness, and execution history.

## API
### Properties
- **PipelineId** (`string`)  
  Gets the unique identifier of the pipeline associated with this controller.  
  Does not throw.

- **CreatedAt** (`DateTime`)  
  Gets the timestamp when the controller was instantiated.  
  Does not throw.

- **TotalExecutions** (`long`)  
  Gets the total number of pipeline executions recorded since startup.  
  Does not throw.

- **SuccessfulExecutions** (`long`)  
  Gets the count of executions that completed successfully.  
  Does not throw.

- **FailedExecutions** (`long`)  
  Gets the count of executions that resulted in a failure.  
  Does not throw.

- **SuccessRate** (`double`)  
  Gets the ratio of successful executions to total executions, expressed as a value between 0 and 1.  
  Does not throw.

- **PolicyCount** (`int`)  
  Gets the number of distinct policies configured in the pipeline.  
  Does not throw.

- **AverageExecutionTimeMs** (`double`)  
  Gets the average execution time of the pipeline in milliseconds.  
  Does not throw.

- **PolicyId** (`string`)  
  Gets the identifier of the policy whose metrics are currently being inspected (if applicable).  
  Does not throw.

- **PolicyName** (`string`)  
  Gets the display name of the policy referenced by `PolicyId`.  
  Does not throw.

- **Type** (`string`)  
  Gets the type of the policy (e.g., `Retry`, `CircuitBreaker`).  
  Does not throw.

- **IsEnabled** (`bool`)  
  Gets whether the policy referenced by `PolicyId` is currently enabled.  
  Does not throw.

- **ExecutionCount** (`long`)  
  Gets the number of times the policy referenced by `PolicyId` has been executed.  
  Does not throw.

- **SuccessCount** (`long`)  
  Gets the number of successful executions of the policy referenced by `PolicyId`.  
  Does not throw.

### Methods
- **GetPipelineMetricsAsync()** (`Task<ApiResponse<PipelineMetricsDto>>`)  
  Retrieves a snapshot of aggregate pipeline metrics.  
  *Parameters*: none.  
  *Return value*: An `ApiResponse` containing a `PipelineMetricsDto` on success, or error information if the operation fails.  
  *Throws*: May throw `InvalidOperationException` if the metrics store is not initialized, or `ObjectDisposedException` if the controller has been disposed.

- **GetPoliciesMetricsAsync()** (`Task<ApiResponse<List<PolicyMetricsDto>>>`)  
  Retrieves metrics for each policy in the pipeline.  
  *Parameters*: none.  
  *Return value*: An `ApiResponse` containing a list of `PolicyMetricsDto` instances, or error information.  
  *Throws*: May throw `InvalidOperationException` if the metrics store is unavailable, or `ObjectDisposedException` if the controller has been disposed.

- **GetHealthStatusAsync()** (`Task<ApiResponse<HealthStatusDto>>`)  
  Retrieves the current health status of the pipeline.  
  *Parameters*: none.  
  *Return value*: An `ApiResponse` containing a `HealthStatusDto` indicating health, or error information.  
  *Throws*: May throw `InvalidOperationException` when health data cannot be computed, or `ObjectDisposedException` if the controller is disposed.

- **GetExecutionHistoryAsync()** (`Task<ApiResponse<List<ExecutionRecordDto>>>`)  
  Retrieves a chronological list of recent execution records.  
  *Parameters*: none.  
  *Return value*: An `ApiResponse` containing a list of `ExecutionRecordDto`, or error information.  
  *Throws*: May throw `InvalidOperationException` if the history buffer is not accessible, or `ObjectDisposedException` if the controller has been disposed.

- **ResetMetricsAsync()** (`Task<ApiResponse<bool>>`)  
  Resets all collected metrics to their initial state.  
  *Parameters*: none.  
  *Return value*: An `ApiResponse` containing `true` if the reset succeeded, or error information.  
  *Throws*: May throw `InvalidOperationException` if reset is not permitted in the current state, or `ObjectDisposedException` if the controller has been disposed.

## Usage
```csharp
// Example 1: Retrieve pipeline metrics and display success rate
var metricsController = new MetricsController(); // Assume DI or appropriate construction
ApiResponse<PipelineMetricsDto> response = await metricsController.GetPipelineMetricsAsync();
if (response.Succeeded)
{
    Console.WriteLine($"Pipeline {metricsController.PipelineId} success rate: {response.Data.SuccessRate:P2}");
}
else
{
    Console.Error.WriteLine($"Failed to get metrics: {response.ErrorMessage}");
}
```

```csharp
// Example 2: Reset metrics and verify the operation
ApiResponse<bool> resetResponse = await metricsController.ResetMetricsAsync();
if (resetResponse.Succeeded && resetResponse.Data)
{
    Console.WriteLine("Metrics have been reset.");
}
else
{
    Console.Error.WriteLine($"Reset failed: {resetResponse.ErrorMessage}");
}
```

## Notes
- All property getters are thread‑safe and return a consistent snapshot; they never modify state and therefore do not throw under normal concurrent access.
- The asynchronous methods may mutate internal state (e.g., `ResetMetricsAsync` clears counters). Concurrent invocation of a mutating method with any read‑only method can lead to race conditions where the read observes partially updated metrics. For safe concurrent use, ensure that mutating operations are serialized or that the controller is not accessed by other threads while a reset is in progress.
- If the underlying metrics store becomes unavailable after the controller has been created, subsequent calls to the async methods will return an unsuccessful `ApiResponse` rather than throwing, unless the store is in a disposed or fundamentally broken state, in which case the exceptions documented above may be raised.
- The values returned by the properties reflect the state at the moment they are accessed; rapid successive calls may observe different results if metrics are being updated concurrently.
