# ResiliencyExceptionExtensions

Provides diagnostic and classification extension methods for exceptions encountered during resilience pipeline execution. These methods enable consistent error inspection, severity assessment, and formatting of exception details for logging, telemetry, and retry decision logic.

## API

### ToDetailedErrorMessage

```csharp
public static string ToDetailedErrorMessage(this Exception exception)
```

Produces a comprehensive error message string from an exception, including its type name, message, and recursively aggregated details from inner exceptions.

**Parameters:**
- `exception` — The exception to format. Must not be null.

**Return Value:**
A string containing the exception's full type name, primary message, and concatenated inner exception details, separated by standard delimiters.

**Throws:**
- `ArgumentNullException` when `exception` is null.

---

### IsRetryable

```csharp
public static bool IsRetryable(this Exception exception)
```

Determines whether an exception represents a transient fault that warrants a retry attempt within a resilience pipeline.

**Parameters:**
- `exception` — The exception to evaluate. Must not be null.

**Return Value:**
`true` if the exception is classified as transient and potentially recoverable through retry; otherwise `false`.

**Throws:**
- `ArgumentNullException` when `exception` is null.

---

### GetFriendlyName

```csharp
public static string GetFriendlyName(this Exception exception)
```

Returns a human-readable, simplified name for the exception type, suitable for display in logs, dashboards, or user-facing diagnostics.

**Parameters:**
- `exception` — The exception whose type name to resolve. Must not be null.

**Return Value:**
A string representing a concise, friendly identifier for the exception category (e.g., "Timeout", "ConnectionError").

**Throws:**
- `ArgumentNullException` when `exception` is null.

---

### GetSeverityLevel

```csharp
public static string GetSeverityLevel(this Exception exception)
```

Assigns a severity classification to the exception based on its type and characteristics, supporting standardized logging levels across resilience pipelines.

**Parameters:**
- `exception` — The exception to classify. Must not be null.

**Return Value:**
A string indicating severity, such as `"Low"`, `"Medium"`, `"High"`, or `"Critical"`.

**Throws:**
- `ArgumentNullException` when `exception` is null.

## Usage

### Example 1: Logging a Transient Failure with Retry Decision

```csharp
async Task ExecuteWithRetryAwareLogging(Func<Task> operation, ILogger logger)
{
    try
    {
        await operation();
    }
    catch (Exception ex)
    {
        bool retryable = ex.IsRetryable();
        string severity = ex.GetSeverityLevel();
        string friendlyName = ex.GetFriendlyName();

        logger.Log(
            retryable ? LogLevel.Warning : LogLevel.Error,
            "[{Severity}] {FriendlyName} encountered. Retryable: {Retryable}. Detail: {Detail}",
            severity,
            friendlyName,
            retryable,
            ex.ToDetailedErrorMessage());

        if (!retryable)
        {
            throw; // Do not retry; propagate immediately
        }

        // Allow pipeline retry logic to handle the transient fault
    }
}
```

### Example 2: Building Custom Telemetry from Pipeline Exceptions

```csharp
void RecordExceptionTelemetry(Exception ex, ITelemetryClient telemetry)
{
    var properties = new Dictionary<string, string>
    {
        ["exception.friendlyName"] = ex.GetFriendlyName(),
        ["exception.severity"] = ex.GetSeverityLevel(),
        ["exception.retryable"] = ex.IsRetryable().ToString(),
        ["exception.detail"] = ex.ToDetailedErrorMessage()
    };

    telemetry.TrackEvent("ResiliencePipelineException", properties);
}
```

## Notes

- **Null handling:** All methods throw `ArgumentNullException` when passed a null reference. Callers must guard against null before invocation.
- **Thread safety:** These methods are pure functions operating solely on the provided exception instance and its type metadata. They maintain no mutable state and are safe to call concurrently from multiple threads without synchronization.
- **Inner exception aggregation:** `ToDetailedErrorMessage` recursively traverses the full inner exception chain. Exception graphs with cycles or extremely deep nesting may produce long strings; callers should consider truncation for storage-constrained sinks.
- **Classification stability:** `IsRetryable`, `GetFriendlyName`, and `GetSeverityLevel` rely on exception type mapping. Custom exception types not explicitly recognized by the library may receive default classifications (non-retryable, generic friendly name, medium severity). Extending the mapping requires source-level changes.
- **Culture invariance:** Returned strings are intended for diagnostic and machine-processing purposes and are not localized. Severity levels and friendly names use invariant identifiers.
