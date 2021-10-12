# ResiliencyLoggingMiddleware

`ResiliencyLoggingMiddleware` is a diagnostic component within the `dotnet-resilience-pipeline` library that records execution outcomes of resilience policies. It captures metadata such as policy name, operation name, success status, duration, and exception details for each execution attempt. The collected log entries can be queried, filtered, and summarized to support observability, debugging, and performance analysis of resilience pipelines.

## API

### Constructors

**`public ResiliencyLoggingMiddleware`**

Initializes a new instance of the middleware with an empty log store and a default maximum log capacity. The internal collection is ready to accept execution records immediately after construction.

### Properties

**`public int MaxLogEntries`**

Gets or sets the maximum number of log entries the middleware retains. When the limit is reached, the oldest entries are evicted to make room for new ones. The default value is implementation-defined. Setting this to a non-positive value may result in unbounded growth or immediate eviction, depending on internal handling.

### Methods

**`public void LogExecution(string policyName, string operationName, bool success, long durationMs, string? exception, string? message)`**

Records a single execution attempt. All parameters are stored directly in a new `LogEntry`. If the number of stored entries exceeds `MaxLogEntries`, the oldest entry is removed. This method does not return a value and does not throw exceptions under normal operation.

| Parameter       | Type      | Purpose                                                                 |
|-----------------|-----------|-------------------------------------------------------------------------|
| `policyName`    | `string`  | Identifier of the resilience policy that governed the execution.        |
| `operationName` | `string`  | Name of the operation being executed.                                   |
| `success`       | `bool`    | Whether the execution completed successfully.                           |
| `durationMs`    | `long`    | Duration of the execution in milliseconds.                              |
| `exception`     | `string?` | The exception message or stack trace if the execution failed; otherwise `null`. |
| `message`       | `string?` | An optional diagnostic message or context string.                       |

**`public List<LogEntry> GetLogs()`**

Returns a copy of all currently stored log entries in chronological order (oldest first). The returned list is independent of the internal store; modifications to it do not affect the middleware.

**`public List<LogEntry> GetLogsByPolicy(string policyName)`**

Returns a list of log entries filtered to those whose `PolicyName` matches the given value. The comparison is case-sensitive. Returns an empty list if no entries match.

**`public List<LogEntry> GetLogsBetween(DateTime start, DateTime end)`**

Returns a list of log entries whose `Timestamp` falls within the inclusive range `[start, end]`. Entries are ordered chronologically. If `start` is later than `end`, the result is an empty list.

**`public List<LogEntry> GetFailedLogs()`**

Returns a list of log entries where `Success` is `false`. This includes executions that threw exceptions or were otherwise marked as unsuccessful.

**`public void Clear()`**

Removes all log entries from the internal store. After calling this method, `GetLogs()` returns an empty list and `GetSummary()` reflects zero total entries.

**`public LogSummary GetSummary()`**

Aggregates the current log entries into a `LogSummary` instance. The summary includes `TotalEntries` and `SuccessfulExecutions` (see `LogSummary` below). The method computes these values from the live data at the time of the call.

### Nested Type: `LogEntry`

Represents a single recorded execution.

**`public string Id`**

A unique identifier for the log entry, typically a GUID string generated at record time.

**`public DateTime Timestamp`**

The UTC time at which the entry was recorded.

**`public string PolicyName`**

The resilience policy name supplied to `LogExecution`.

**`public string OperationName`**

The operation name supplied to `LogExecution`.

**`public bool Success`**

Whether the execution succeeded.

**`public long DurationMs`**

The execution duration in milliseconds.

**`public string? Exception`**

The exception string, or `null` if no exception occurred.

**`public string? Message`**

The optional diagnostic message, or `null`.

**`public override string ToString()`**

Returns a formatted string containing the `Id`, `Timestamp`, `PolicyName`, `OperationName`, `Success`, and `DurationMs`. The exact format is implementation-defined but suitable for logging and debugging.

### Nested Type: `LogSummary`

A lightweight aggregation result returned by `GetSummary()`.

**`public int TotalEntries`**

The total number of log entries counted.

**`public int SuccessfulExecutions`**

The number of entries where `Success` is `true`.

## Usage

### Example 1: Basic Recording and Retrieval

```csharp
var middleware = new ResiliencyLoggingMiddleware { MaxLogEntries = 1000 };

// Record a successful execution
middleware.LogExecution(
    policyName: "RetryPolicy",
    operationName: "FetchOrders",
    success: true,
    durationMs: 245,
    exception: null,
    message: "Completed on first attempt"
);

// Record a failed execution
middleware.LogExecution(
    policyName: "CircuitBreaker",
    operationName: "CheckInventory",
    success: false,
    durationMs: 5120,
    exception: "TimeoutException: The request timed out after 5 seconds.",
    message: null
);

// Retrieve all logs
List<LogEntry> allLogs = middleware.GetLogs();
foreach (var entry in allLogs)
{
    Console.WriteLine(entry.ToString());
}

// Get summary
LogSummary summary = middleware.GetSummary();
Console.WriteLine($"Total: {summary.TotalEntries}, Successful: {summary.SuccessfulExecutions}");
```

### Example 2: Filtered Queries and Maintenance

```csharp
var middleware = new ResiliencyLoggingMiddleware();

// Simulate multiple executions across policies
middleware.LogExecution("RetryPolicy", "OpA", true, 100, null, null);
middleware.LogExecution("RetryPolicy", "OpB", false, 3000, "InvalidOperationException", null);
middleware.LogExecution("TimeoutPolicy", "OpA", true, 500, null, "Fallback used");
middleware.LogExecution("TimeoutPolicy", "OpC", false, 8000, "TaskCanceledException", "Timeout exceeded");

// Query by policy
List<LogEntry> retryLogs = middleware.GetLogsByPolicy("RetryPolicy");
Console.WriteLine($"RetryPolicy entries: {retryLogs.Count}");

// Query failures
List<LogEntry> failures = middleware.GetFailedLogs();
foreach (var fail in failures)
{
    Console.WriteLine($"Failed: {fail.OperationName} - {fail.Exception}");
}

// Query by time range
DateTime start = DateTime.UtcNow.AddMinutes(-10);
DateTime end = DateTime.UtcNow;
List<LogEntry> recentLogs = middleware.GetLogsBetween(start, end);

// Clear logs after processing
middleware.Clear();
```

## Notes

- **Thread Safety:** `LogExecution`, `Clear`, and all query methods (`GetLogs`, `GetLogsByPolicy`, `GetLogsBetween`, `GetFailedLogs`, `GetSummary`) access shared internal state. The implementation is expected to use synchronization mechanisms (e.g., locks) to ensure consistency. Concurrent calls are safe, but query results reflect a point-in-time snapshot; entries recorded during a query may or may not appear in that result.
- **Capacity and Eviction:** When `MaxLogEntries` is set and the limit is exceeded, eviction occurs during `LogExecution`. The oldest entry is removed. If `MaxLogEntries` is reduced after entries have accumulated, excess entries beyond the new limit are not immediately evicted; eviction only happens on subsequent calls to `LogExecution`.
- **Timestamp Granularity:** `LogEntry.Timestamp` is set at record time using the system clock. Entries recorded in rapid succession may share the same timestamp value depending on system clock resolution.
- **Memory Considerations:** The middleware holds log entries in memory. Long-lived instances with high `MaxLogEntries` values can consume significant memory. Callers should monitor log volume and call `Clear()` periodically if logs are only needed transiently.
- **`LogSummary` Consistency:** `GetSummary()` computes `TotalEntries` and `SuccessfulExecutions` from the same snapshot, so the two values are always consistent with each other for that call.
- **Null Parameters:** `LogExecution` accepts `null` for `exception` and `message`. These are stored as `null` in the corresponding `LogEntry` fields. Filtering methods do not treat `null` specially beyond standard equality comparisons.
