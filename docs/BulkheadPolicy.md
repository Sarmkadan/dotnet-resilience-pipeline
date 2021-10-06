# BulkheadPolicy

The `BulkheadPolicy` class implements the bulkhead isolation pattern, a resilience strategy designed to limit the number of concurrent executions of a specific operation to prevent resource exhaustion. By enforcing constraints on parallelization and queuing, this policy ensures that a failure or overload in one part of the system does not cascade to others, maintaining overall system stability. It tracks active executions, queued requests, and rejection statistics to provide real-time visibility into resource utilization and contention levels.

## API

### Properties

*   **`public int MaxParallelization`**
    Gets the maximum number of concurrent operations allowed to execute simultaneously. This value defines the size of the "bulkhead."

*   **`public int MaxQueueLength`**
    Gets the maximum number of requests that can wait in the queue when the parallelization limit is reached. Requests exceeding this limit are immediately rejected.

*   **`public int ActiveExecutions`**
    Gets the current number of operations actively executing within the policy.

*   **`public int QueuedRequests`**
    Gets the current number of requests waiting in the queue for an execution slot to become available.

*   **`public long RejectedCount`**
    Gets the total cumulative count of requests that were rejected because both the parallelization limit and the queue limit were exceeded.

*   **`public long QueuedCount`**
    Gets the total cumulative count of requests that have been placed in the queue since the policy was instantiated or last reset.

*   **`public double AverageQueueTimeMs`**
    Gets the average time, in milliseconds, that requests have spent waiting in the queue.

*   **`public long LongestQueueTimeMs`**
    Gets the duration, in milliseconds, of the longest wait time experienced by a single request in the queue.

### Constructors

*   **`public BulkheadPolicy(string name) : base`**
    Initializes a new instance of the `BulkheadPolicy` class with the specified name.
    *   **Parameters**:
        *   `name`: A string identifier for this policy instance.
    *   **Remarks**: The base constructor is invoked to initialize common policy infrastructure.

### Methods

*   **`public bool TryAcquireSlot()`**
    Attempts to acquire an execution slot. If the number of active executions is below `MaxParallelization`, a slot is granted immediately. If the limit is reached but the queue is not full (`QueuedRequests` < `MaxQueueLength`), the request is queued. If both limits are exceeded, the method returns `false`.
    *   **Returns**: `true` if a slot was acquired (either immediately or via queuing); `false` if the request was rejected.
    *   **Throws**: No exceptions are thrown under normal operation; rejection is indicated by the return value.

*   **`public void ReleaseSlot()`**
    Releases a previously acquired execution slot, allowing a queued request to proceed or decrementing the active execution count.
    *   **Parameters**: None.
    *   **Throws**: May throw an exception if called without a corresponding successful `TryAcquireSlot` call or if the internal state is corrupted.

*   **`public void DequeueRequest()`**
    Removes a request from the waiting queue, typically invoked when a slot becomes available and a queued task is promoted to active execution.
    *   **Parameters**: None.
    *   **Throws**: May throw if the queue is empty when this method is invoked.

*   **`public void RecordQueueWaitTime(double waitTimeMs)`**
    Records the time a specific request spent waiting in the queue to update statistical metrics.
    *   **Parameters**:
        *   `waitTimeMs`: The duration the request waited, in milliseconds.
    *   **Throws**: No exceptions expected for valid numeric inputs.

*   **`public double GetUtilizationPercentage()`**
    Calculates the current utilization of the parallelization capacity.
    *   **Returns**: A double representing the percentage of `MaxParallelization` currently in use (0.0 to 100.0).
    *   **Formula**: `(ActiveExecutions / MaxParallelization) * 100`.

*   **`public double GetQueuedPercentage()`**
    Calculates the current fill level of the request queue.
    *   **Returns**: A double representing the percentage of `MaxQueueLength` currently occupied (0.0 to 100.0).
    *   **Formula**: `(QueuedRequests / MaxQueueLength) * 100`.

*   **`public double GetRejectionPercentage()`**
    Calculates the ratio of rejected requests relative to the total number of attempted requests (queued + executed + rejected).
    *   **Returns**: A double representing the rejection rate as a percentage.
    *   **Remarks**: Returns 0.0 if no requests have been attempted.

*   **`public bool IsValidConfiguration()`**
    Validates the current configuration of the bulkhead policy.
    *   **Returns**: `true` if `MaxParallelization` and `MaxQueueLength` are set to valid positive integers; `false` otherwise.

*   **`public override void ResetStatistics()`**
    Resets all statistical counters (`RejectedCount`, `QueuedCount`, `AverageQueueTimeMs`, `LongestQueueTimeMs`) to their initial values without affecting the current active execution state or configuration limits.
    *   **Parameters**: None.

*   **`public override PolicySnapshot GetSnapshot()`**
    Captures the current state of the policy, including configuration limits and real-time statistics, into an immutable snapshot object.
    *   **Returns**: A `PolicySnapshot` instance containing the state data at the moment of invocation.

## Usage

### Example 1: Manual Slot Management
This example demonstrates manual acquisition and release of slots for a critical database operation, ensuring no more than 5 concurrent connections are opened.

```csharp
var policy = new BulkheadPolicy("DbBulkhead");

if (!policy.IsValidConfiguration())
{
    throw new InvalidOperationException("Bulkhead policy is misconfigured.");
}

if (policy.TryAcquireSlot())
{
    try
    {
        // Simulate work occupying a slot
        await ExecuteDatabaseQueryAsync();
        
        // Record wait time if the request was queued before execution
        // (Value would be retrieved from context in a real implementation)
        policy.RecordQueueWaitTime(0.0); 
    }
    finally
    {
        // Always release the slot to prevent deadlocks
        policy.ReleaseSlot();
    }
}
else
{
    // Handle rejection (e.g., return 503 Service Unavailable)
    Console.WriteLine($"Request rejected. Active: {policy.ActiveExecutions}, Queued: {policy.QueuedRequests}");
}
```

### Example 2: Monitoring and Statistics
This example illustrates how to inspect policy metrics to monitor system health and utilization trends.

```csharp
var policy = new BulkheadPolicy("ApiGatewayBulkhead");

// ... simulate traffic ...

// Capture a point-in-time snapshot for logging
var snapshot = policy.GetSnapshot();
Console.WriteLine($"Snapshot taken at {snapshot.Timestamp}");

// Check real-time utilization
double utilization = policy.GetUtilizationPercentage();
double queueFill = policy.GetQueuedPercentage();
double rejectionRate = policy.GetRejectionPercentage();

if (utilization > 90.0)
{
    Console.WriteLine("Warning: Bulkhead near capacity.");
}

if (rejectionRate > 5.0)
{
    Console.WriteLine("Alert: High rejection rate detected. Consider scaling resources.");
}

// Reset counters for the next monitoring interval
policy.ResetStatistics();
```

## Notes

*   **Thread Safety**: The `BulkheadPolicy` is designed for concurrent access. Properties such as `ActiveExecutions` and `QueuedRequests` reflect the state at the moment of access and may change immediately after reading. Statistical counters (`RejectedCount`, `QueuedCount`) use atomic operations or locking internally to ensure accuracy under high concurrency.
*   **Slot Lifecycle**: It is critical that every successful call to `TryAcquireSlot` is paired with exactly one call to `ReleaseSlot`. Failing to release a slot will permanently reduce the available parallelization capacity, eventually leading to a deadlock where `ActiveExecutions` equals `MaxParallelization` and no new requests can proceed.
*   **Queue Behavior**: The policy distinguishes between immediate execution and queuing. `TryAcquireSlot` returns `true` for both scenarios. Consumers must rely on external mechanisms or context to determine if a request was queued immediately or executed instantly, though `RecordQueueWaitTime` implies a queue occurrence if the time is greater than zero.
*   **Configuration Immutability**: While `IsValidConfiguration` checks the setup, the properties `MaxParallelization` and `MaxQueueLength` appear to be read-only in this interface. Changing limits likely requires creating a new instance of `BulkheadPolicy`.
*   **Statistical Precision**: `AverageQueueTimeMs` is a running average. A sudden spike in wait times may take several requests to significantly shift the average. For detecting sudden latency spikes, `LongestQueueTimeMs` or a custom sliding window implementation based on snapshots may be more appropriate.
