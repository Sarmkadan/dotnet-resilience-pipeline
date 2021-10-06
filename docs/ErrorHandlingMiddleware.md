# ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` component collects and surfaces information about exceptions that occur within a resilience pipeline. It stores a bounded history of error contexts, derives statistics per policy, and provides helper methods to inspect, query, and reset the collected data. This enables observability, debugging, and policy‑tuning scenarios without interfering with the normal flow of the pipeline.

## API

### MaxContexts (int)
**Purpose**  
Gets or sets the maximum number of `ErrorContext` instances the middleware will retain. When the limit is exceeded, the oldest entries are discarded.

**Parameters**  
None.

**Return value**  
The current limit.

**Exceptions**  
- `ArgumentOutOfRangeException` if a value less than 1 is assigned.

### HandleException (ErrorContext)
**Purpose**  
Invoked by the pipeline when an exception is caught. Populates and returns an `ErrorContext` describing the exception, updates internal stores, and respects `MaxContexts`.

**Parameters**  
None (the middleware receives the exception implicitly from the pipeline).

**Return value**  
An `ErrorContext` instance containing details such as exception type, message, policy name, operation name, recoverability, and a recovery recommendation.

**Exceptions**  
- May propagate any exception thrown by user‑provided error handling logic inside the context (the middleware itself does not throw unless internal state is corrupted).

### GetErrorContexts (List<ErrorContext>)
**Purpose**  
Returns a snapshot of all stored error contexts, ordered from most recent to oldest.

**Parameters**  
None.

**Return value**  
A new `List<ErrorContext>` containing copies of the stored contexts. Modifying the returned list does not affect the middleware’s internal state.

**Exceptions**  
- `ObjectDisposedException` if the middleware has been disposed (if applicable).  
- `InvalidOperationException` if the internal collection is in an inconsistent state (rare).

### GetErrorStatistics (Dictionary<string, ErrorStatistics>)
**Purpose**  
Provides per‑policy error statistics, such as total counts and frequency of recoverable vs. non‑recoverable errors.

**Parameters**  
None.

**Return value**  
A read‑only dictionary where the key is the policy name and the value is an `ErrorStatistics` object summarizing errors for that policy.

**Exceptions**  
- `InvalidOperationException` if the statistics have not been initialized (e.g., before any exception has been processed).

### GetErrorsForPolicy (List<ErrorContext>)
**Purpose**  
Returns all error contexts associated with the policy identified by the `PolicyName` property.

**Parameters**  
None.

**Return value**  
A new `List<ErrorContext>` containing copies of the contexts for the specified policy.

**Exceptions**  
- `InvalidOperationException` if `PolicyName` is null or empty.  
- `ObjectDisposedException` if the middleware has been disposed.

### GetMostCommonErrors (List<(string Error, int Count)>)
**Purpose**  
Returns a list of distinct error messages together with their occurrence counts, sorted descending by count.

**Parameters**  
None.

**Return value**  
A list of value tuples where `Error` is the exception message (or a formatted representation) and `Count` is how many times that message has been seen.

**Exceptions**  
- `InvalidOperationException` if no errors have been recorded.

### Clear (void)
**Purpose**  
Removes all stored error contexts and resets statistics.

**Parameters**  
None.

**Return value**  
None.

**Exceptions**  
- None under normal operation. Throws only if the middleware is in a disposed state.

### Id (string)
**Purpose**  
A unique identifier for the middleware instance, useful for correlating logs.

**Parameters**  
None.

**Return value**  
A GUID‑based string assigned at construction.

**Exceptions**  
- None.

### Timestamp (DateTime Timestamp
**Purpose**  
The UTC date and time when the middleware instance was created (or last cleared, depending on implementation).

**Parameters**  
None.

**Return value**  
A `DateTime` value.

**Exceptions**  
- None.

### ExceptionType (string)
**Purpose**  
The CLR type name of the most recent exception processed by the middleware.

**Parameters**  
None.

**Return value**  
The `Exception.GetType().Name` of the last handled exception, or `null` if no exception has been processed.

**Exceptions**  
- None.

### ExceptionMessage (string)
**Purpose**  
The message of the most recent exception processed.

**Parameters**  
None.

**Return value**  
The `Exception.Message` of the last handled exception, or `null` if none.

**Exceptions**  
- None.

### PolicyName (string)
**Purpose**  
The name of the resilience policy associated with this middleware instance (set at construction or configuration).

**Parameters**  
None.

**Return value**  
The policy name string.

**Exceptions**  
- None.

### OperationName (string)
**Purpose**  
An optional descriptor of the operation being executed when the error occurred.

**Parameters**  
None.

**Return value**  
The operation name string, or `null` if not supplied.

**Exceptions**  
- None.

### IsRecoverable (bool)
**Purpose**  
Indicates whether the last processed exception was deemed recoverable by the policy.

**Parameters**  
None.

**Return value**  
`true` if the exception can be retried or compensated; otherwise `false`. Returns `false` when no exception has been processed.

**Exceptions**  
- None.

### RecoveryRecommendation (string)
**Purpose**  
A human‑readable suggestion for how to address the last exception (e.g., “Retry with exponential backoff”).

**Parameters**  
None.

**Return value**  
A recommendation string, or `null` if no recommendation is available.

**Exceptions**  
- None.

### ToString (override string)
**Purpose**  
Provides a concise string representation of the middleware’s current state, including ID, policy name, and error count.

**Parameters**  
None.

**Return value**  
A formatted string.

**Exceptions**  
- None.

### Count (int)
**Purpose**  
The number of error contexts currently stored.

**Parameters**  
None.

**Return value**  
An integer ≥ 0 and ≤ `MaxContexts`.

**Exceptions**  
- None.

### LastOccurrence (DateTime)
**Purpose**  
The UTC timestamp of the most recent error context stored.

**Parameters**  
None.

**Return value**  
A `DateTime` value; equals `DateTime.MinValue` if no errors have been recorded.

**Exceptions**  
- None.

## Usage

### Basic configuration and error handling
```csharp
using DotNetResiliencePipeline;

// Create middleware with a limit of 100 stored contexts.
var middleware = new ErrorHandlingMiddleware { MaxContexts = 100 };

// Simulate a pipeline execution that catches an exception.
try
{
    // Some operation that may fail.
    throw new InvalidOperationException("Transient service glitch.");
}
catch (Exception ex)
{
    // Let the middleware process the exception.
    var ctx = middleware.HandleException; // populates and returns ErrorContext
    // ctx now contains details such as ExceptionType, ExceptionMessage, etc.
}

// Retrieve statistics for monitoring.
var stats = middleware.GetErrorStatistics;
foreach (var kvp in stats)
{
    Console.WriteLine($"Policy {kvp.Key}: {kvp.Value.TotalErrors} errors");
}

// When diagnostics are no longer needed, clear the store.
middleware.Clear();
```

### Querying specific error information
```csharp
using DotNetResiliencePipeline;
using System.Linq;

var middleware = new ErrorHandlingMiddleware
{
    PolicyName = "HttpRetryPolicy",
    OperationName = "GetUserProfile"
};

// Assume several exceptions have been processed elsewhere.

// Get the three most common error messages.
var topErrors = middleware.GetMostCommonErrors
                          .Take(3)
                          .Select(e => $"{e.Error} (×{e.Count})")
                          .ToList();

Console.WriteLine("Top errors:");
foreach (var msg in topErrors)
{
    Console.WriteLine(msg);
}

// Obtain all contexts for the configured policy.
var policyErrors = middleware.GetErrorsForPolicy;
if (policyErrors.Any())
{
    var latest = policyErrors.First(); // most recent
    Console.WriteLine(
        $"Latest error in {middleware.PolicyName}: {latest.ExceptionMessage} at {latest.Timestamp}");
}
```

## Notes

- **Thread safety**: The middleware is safe for concurrent calls from multiple threads as long as external synchronization is not required for the consumer’s own data. Internal collections are accessed with locks; however, enumerating the results of `GetErrorContexts`, `GetErrorsForPolicy`, or `GetMostCommonErrors` while another thread is modifying the store may produce a snapshot that reflects the state at the moment the copy was made, not a live view.
- **MaxContexts enforcement**: When the limit is reached, the middleware discards the oldest entry. Setting `MaxContexts` to a lower value than the current count will cause an immediate truncation to the new limit on the next insertion.
- **Null handling**: Properties such as `ExceptionType`, `ExceptionMessage`, `PolicyName`, `OperationName`, and `RecoveryRecommendation` may return `null` when no exception has been processed or when the corresponding value was not supplied. Consumers should guard against null reference exceptions.
- **Exception propagation**: `HandleException` does not swallow exceptions thrown by user‑provided recovery logic; those will bubble up to the pipeline caller. The middleware itself only throws on argument validation (e.g., setting `MaxContexts` < 1) or when internal invariants are broken.
- **Disposal**: If the middleware implements `IDisposable` (not shown in the member list), calling `Clear` after disposal may throw `ObjectDisposedException`. Users should dispose of the middleware only after all needed queries have been completed.
- **Time stamps**: All `DateTime` values are expressed in UTC to avoid ambiguity across time zones. `Timestamp` reflects creation/clear time, while `LastOccurrence` reflects the most recent error entry. If no errors have been recorded, `LastOccurrence` returns `DateTime.MinValue`.
