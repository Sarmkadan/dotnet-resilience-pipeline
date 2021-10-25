# ExecutionRecord

The `ExecutionRecord` type serves as both a data model representing the outcome of a single resilience policy execution and a repository interface for managing historical execution data within the `dotnet-resilience-pipeline` project. It encapsulates critical metrics such as execution duration, attempt counts, success status, and error details, while providing built-in methods to persist new records and query existing history by various criteria including policy identity, time ranges, and success outcomes.

## API

### Properties

*   **`public string ExecutionId`**
    Gets the unique identifier assigned to this specific execution instance. This value is immutable once set and is used to correlate logs and metrics.

*   **`public string PolicyName`**
    Gets the human-readable name of the resilience policy associated with this execution.

*   **`public string PolicyId`**
    Gets the unique identifier of the resilience policy that was executed. This is used for grouping and filtering records across different instances of the same policy logic.

*   **`public bool IsSuccess`**
    Gets a value indicating whether the execution completed successfully without triggering a final failure condition.

*   **`public long ExecutionTimeMs`**
    Gets the total duration of the execution in milliseconds. This includes the time spent in all retry attempts and any delay intervals.

*   **`public int AttemptCount`**
    Gets the total number of attempts made during this execution, including the initial call and any subsequent retries.

*   **`public string? ErrorMessage`**
    Gets the error message captured if the execution failed. Returns `null` if the execution was successful.

*   **`public string? ErrorType`**
    Gets the fully qualified type name of the exception thrown if the execution failed. Returns `null` if the execution was successful.

*   **`public DateTime ExecutedAt`**
    Gets the UTC timestamp indicating when the execution started.

*   **`public Dictionary<string, object> Metadata`**
    Gets a collection of key-value pairs containing custom contextual data associated with the execution. This dictionary is mutable and can be extended by callers before recording.

*   **`public ExecutionHistoryRepository Repository`**
    Gets the underlying repository instance used by this record to persist and retrieve historical data. *Note: In the provided signature list, this appears as `ExecutionHistoryRepository`; it is treated here as a property exposing the repository context.*

### Methods

*   **`public void Record()`**
    Persists the current `ExecutionRecord` instance into the storage backend via the associated repository.
    *   **Parameters:** None.
    *   **Returns:** `void`.
    *   **Throws:** May throw storage-specific exceptions if the underlying persistence layer fails (e.g., database connectivity issues).

*   **`public List<ExecutionRecord> GetAll()`**
    Retrieves all execution records stored in the repository.
    *   **Parameters:** None.
    *   **Returns:** A list of all `ExecutionRecord` objects.
    *   **Throws:** May throw exceptions related to data retrieval failures.

*   **`public List<ExecutionRecord> GetByPolicyId(string policyId)`**
    Retrieves all execution records associated with a specific policy identifier.
    *   **Parameters:** `policyId` – The unique ID of the policy to filter by.
    *   **Returns:** A list of `ExecutionRecord` objects matching the provided ID.
    *   **Throws:** `ArgumentNullException` if `policyId` is null.

*   **`public List<ExecutionRecord> GetByTimeRange(DateTime start, DateTime end)`**
    Retrieves execution records that occurred within a specified time window.
    *   **Parameters:** `start` – The beginning of the time range (inclusive); `end` – The end of the time range (inclusive).
    *   **Returns:** A list of `ExecutionRecord` objects falling within the range.
    *   **Throws:** `ArgumentOutOfRangeException` if `end` is earlier than `start`.

*   **`public List<ExecutionRecord> GetFailedExecutions()`**
    Retrieves all records where the execution resulted in a failure.
    *   **Parameters:** None.
    *   **Returns:** A list of `ExecutionRecord` objects where `IsSuccess` is `false`.

*   **`public List<ExecutionRecord> GetSuccessfulExecutions()`**
    Retrieves all records where the execution completed successfully.
    *   **Parameters:** None.
    *   **Returns:** A list of `ExecutionRecord` objects where `IsSuccess` is `true`.

*   **`public List<ExecutionRecord> GetLatest(int count = 10)`**
    Retrieves the most recent execution records ordered by `ExecutedAt` in descending order.
    *   **Parameters:** `count` – The number of records to retrieve (default is 10).
    *   **Returns:** A list of the latest `ExecutionRecord` objects.
    *   **Throws:** `ArgumentOutOfRangeException` if `count` is less than or equal to zero.

*   **`public double GetAverageExecutionTime(string? policyId = null)`**
    Calculates the average execution time in milliseconds.
    *   **Parameters:** `policyId` – Optional. If provided, calculates the average only for the specified policy; otherwise, calculates across all records.
    *   **Returns:** The average time as a `double`. Returns `0.0` if no records match the criteria.
    *   **Throws:** None.

*   **`public double GetSuccessRate(string? policyId = null)`**
    Calculates the percentage of successful executions.
    *   **Parameters:** `policyId` – Optional. If provided, calculates the rate only for the specified policy; otherwise, calculates across all records.
    *   **Returns:** A value between `0.0` and `100.0` representing the success percentage. Returns `0.0` if no records match the criteria.
    *   **Throws:** None.

## Usage

### Example 1: Recording a Failed Execution with Metadata

This example demonstrates how to populate an `ExecutionRecord` with failure details and custom metadata, then persist it to the history store.

```csharp
var record = new ExecutionRecord
{
    ExecutionId = Guid.NewGuid().ToString(),
    PolicyName = "HttpRetryPolicy",
    PolicyId = "policy-123",
    IsSuccess = false,
    ExecutionTimeMs = 1540,
    AttemptCount = 3,
    ErrorMessage = "Connection timed out after 3 attempts.",
    ErrorType = "System.Net.Http.HttpRequestException",
    ExecutedAt = DateTime.UtcNow,
    Metadata = new Dictionary<string, object>
    {
        { "RequestId", "req-998877" },
        { "Endpoint", "https://api.example.com/data" }
    }
};

// Persist the record to the repository
record.Record();

Console.WriteLine($"Recorded failure for policy {record.PolicyName}");
```

### Example 2: Analyzing Recent Performance Metrics

This example retrieves recent failed executions for a specific policy and calculates the current success rate and average execution time.

```csharp
// Assume 'repository' is an initialized ExecutionHistoryRepository instance
// and we are accessing methods via a context or static helper that exposes them,
// or instantiating a record to access instance methods if designed that way.
// Based on the signature, these methods appear to be instance members acting on the repository context.

var contextRecord = new ExecutionRecord(); // Initialized to access repository methods

string targetPolicyId = "policy-123";

// Fetch failed executions for analysis
var failures = contextRecord.GetByPolicyId(targetPolicyId)
                            .Where(r => !r.IsSuccess)
                            .ToList();

// Calculate metrics specifically for this policy
double avgTime = contextRecord.GetAverageExecutionTime(targetPolicyId);
double successRate = contextRecord.GetSuccessRate(targetPolicyId);

Console.WriteLine($"Policy: {targetPolicyId}");
Console.WriteLine($"Recent Failures: {failures.Count}");
Console.WriteLine($"Average Execution Time: {avgTime:F2}ms");
Console.WriteLine($"Success Rate: {successRate:F1}%");
```

## Notes

*   **Thread Safety:** The `Metadata` dictionary is a standard `Dictionary<string, object>` and is not thread-safe. If multiple threads access and modify the same `ExecutionRecord` instance's metadata concurrently, external synchronization is required. The query methods (`GetAll`, `GetByPolicyId`, etc.) rely on the underlying `ExecutionHistoryRepository` implementation for thread safety; callers should assume read operations are safe but verify the repository's specific concurrency guarantees.
*   **Null Handling:** Properties `ErrorMessage` and `ErrorType` are explicitly nullable (`string?`). Consumers must check `IsSuccess` before accessing these properties to avoid logical errors, though they will return `null` rather than throwing if accessed on a successful record.
*   **Empty Result Sets:** The statistical methods `GetAverageExecutionTime` and `GetSuccessRate` return `0.0` when no records match the filter criteria, rather than throwing an exception or returning `NaN`. This prevents application crashes during dashboard rendering or monitoring when no data exists yet.
*   **Time Zones:** The `ExecutedAt` property should always be set and interpreted as UTC. The `GetByTimeRange` method expects `DateTime` inputs; callers are responsible for ensuring the provided `start` and `end` times are normalized to UTC to avoid mismatched query results.
*   **Repository Coupling:** The presence of data retrieval methods (`GetAll`, `GetLatest`, etc.) directly on the `ExecutionRecord` type (or its associated repository instance exposed via the type) suggests a tight coupling between the data model and the data access layer. Ensure the `ExecutionHistoryRepository` is properly initialized before invoking these methods to avoid null reference exceptions.
