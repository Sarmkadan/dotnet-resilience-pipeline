# BulkheadService

The `BulkheadService` is a core component within the `dotnet-resilience-pipeline` project designed to implement the bulkhead isolation pattern. It manages a fixed pool of execution slots to prevent cascading failures by limiting the number of concurrent operations, while also maintaining a queue for requests that exceed the immediate concurrency limit. This service provides mechanisms to acquire and release execution slots, track queue metrics, and validate configuration states, ensuring system stability under high load.

## API

### `TryAcquireSlot`
Attempts to acquire an available execution slot immediately.
- **Purpose**: Determines if a new request can proceed without queuing based on current concurrency limits.
- **Parameters**: None.
- **Return Value**: Returns `true` if a slot was successfully acquired; otherwise, `false`.
- **Exceptions**: Does not throw exceptions under normal operation.

### `ReleaseSlot`
Releases a previously acquired execution slot, making it available for other waiting requests.
- **Purpose**: Signals the completion of an operation and decrements the active execution count.
- **Parameters**: None.
- **Return Value**: `void`.
- **Exceptions**: May throw if called without a corresponding prior acquisition or if the internal state is corrupted.

### `DequeueRequest`
Processes the next request waiting in the internal queue.
- **Purpose**: Moves a queued request into an active state when a slot becomes available.
- **Parameters**: None.
- **Return Value**: `void`.
- **Exceptions**: Throws if the queue is empty or if the service is in an invalid state for dequeuing.

### `RecordQueueWaitTime`
Records the duration a request spent waiting in the queue before execution.
- **Purpose**: Captures latency metrics for queued requests to support monitoring and tuning.
- **Parameters**: None (assumes context or specific timing data is handled internally or via overload not listed).
- **Return Value**: `void`.
- **Exceptions**: Does not throw under normal operation.

### `GetUtilizationPercentage`
Calculates the current percentage of utilized execution slots relative to the total capacity.
- **Purpose**: Provides a real-time metric for load monitoring.
- **Parameters**: None.
- **Return Value**: Returns a `double` representing the utilization percentage (0.0 to 100.0).
- **Exceptions**: Does not throw.

### `GetActiveExecutionCount`
Retrieves the number of currently active executions holding a slot.
- **Purpose**: Inspects the current concurrency level.
- **Parameters**: None.
- **Return Value**: Returns an `int` representing the count of active slots.
- **Exceptions**: Does not throw.

### `GetQueuedRequestCount`
Retrieves the number of requests currently waiting in the queue.
- **Purpose**: Inspects the current backlog depth.
- **Parameters**: None.
- **Return Value**: Returns an `int` representing the number of queued requests.
- **Exceptions**: Does not throw.

### `IsValidConfiguration`
Validates the current configuration settings of the bulkhead service.
- **Purpose**: Ensures that limits and queue sizes are set to logical values before or during runtime.
- **Parameters**: None.
- **Return Value**: Returns `true` if the configuration is valid; otherwise, `false`.
- **Exceptions**: Does not throw.

## Usage

### Example 1: Manual Slot Management
This example demonstrates acquiring a slot, executing logic, and ensuring the slot is released even if an exception occurs.

```csharp
var bulkhead = new BulkheadService(maxConcurrency: 10, maxQueueSize: 50);

if (bulkhead.TryAcquireSlot())
{
    try
    {
        // Execute the protected operation
        await PerformDatabaseOperationAsync();
    }
    finally
    {
        // Always release the slot to prevent deadlock
        bulkhead.ReleaseSlot();
    }
}
else
{
    // Handle rejection or enqueue logic depending on strategy
    Console.WriteLine("Request rejected: Bulkhead full.");
}
```

### Example 2: Monitoring and Metrics
This example illustrates how to inspect the state of the bulkhead to make scaling or alerting decisions.

```csharp
var bulkhead = new BulkheadService(maxConcurrency: 20, maxQueueSize: 100);

// Simulate load...

if (!bulkhead.IsValidConfiguration)
{
    throw new InvalidOperationException("Bulkhead configuration is invalid.");
}

double utilization = bulkhead.GetUtilizationPercentage();
int active = bulkhead.GetActiveExecutionCount();
int queued = bulkhead.GetQueuedRequestCount();

Console.WriteLine($"Status: {utilization:F2}% utilized");
Console.WriteLine($"Active: {active}, Queued: {queued}");

if (queued > 80)
{
    // Trigger alert or scale up logic
    LogWarning("High queue depth detected.");
}
```

## Notes

- **Thread Safety**: Given the nature of concurrency control, all public members of `BulkheadService` are expected to be thread-safe. Multiple threads may call `TryAcquireSlot`, `ReleaseSlot`, and metric getters simultaneously without external locking.
- **State Consistency**: Callers must ensure that every successful `TryAcquireSlot` call is paired with exactly one `ReleaseSlot` call. Failure to release slots will result in resource exhaustion, causing `TryAcquireSlot` to permanently return `false` once the limit is reached.
- **Queue Behavior**: The `DequeueRequest` method implies an internal queuing mechanism. If the queue is empty when this method is invoked, it may throw an exception or result in a no-op depending on the internal implementation strategy; callers should verify `GetQueuedRequestCount` before invoking if uncertain.
- **Configuration Validation**: `IsValidConfiguration` should be checked during application startup. A return value of `false` indicates logical errors in setup (e.g., negative concurrency limits) that will prevent correct operation.
- **Metric Precision**: `GetUtilizationPercentage` returns a `double`; consumers should handle floating-point precision appropriately when comparing against thresholds.
