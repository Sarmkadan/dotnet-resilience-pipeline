# ErrorHandlingMiddlewareExtensions

Provides extension methods for querying and reporting error information collected by the resilience pipeline’s error handling middleware. These static helpers enable consumers to inspect aggregated error contexts, generate diagnostic reports, and check error state without accessing internal pipeline state directly.

## API

### GetErrorsByType
```csharp
public static List<ErrorContext> GetErrorsByType(Type errorType)
```
**Purpose** – Returns all error contexts whose exception type matches the supplied `errorType`.  
**Parameters**  
- `errorType`: The `System.Type` of the exception to filter by. Must not be `null`.  
**Return value** – A list containing zero or more `ErrorContext` instances; the list is empty when no matching errors are recorded.  
**Exceptions** – Throws `ArgumentNullException` if `errorType` is `null`.

### GetErrorsByRecoverability
```csharp
public static List<ErrorContext> GetErrorsByRecoverability(bool isRecoverable)
```
**Purpose** – Returns error contexts filtered by their recoverability flag.  
**Parameters**  
- `isRecoverable`: `true` to retrieve errors marked as recoverable, `false` for non‑recoverable errors.  
**Return value** – A list of `ErrorContext` objects matching the recoverability criterion; empty list if none match.  
**Exceptions** – None.

### GetErrorsForOperation
```csharp
public static List<ErrorContext> GetErrorsForOperation(string operationName)
```
**Purpose** – Retrieves all error contexts associated with a specific logical operation name.  
**Parameters**  
- `operationName`: The name of the operation as recorded by the middleware. Must not be `null` or whitespace.  
**Return value** – A list of `ErrorContext` instances for the given operation; empty list when no errors are tied to that operation.  
**Exceptions** – Throws `ArgumentException` if `operationName` is `null`, empty, or consists only of white‑space characters.

### GenerateErrorReport
```csharp
public static string GenerateErrorReport()
```
**Purpose** – Produces a human‑readable multi‑line string summarizing all currently recorded errors, including type, message, timestamp, operation, and recoverability.  
**Return value** – A formatted report; returns an empty string when no errors have been recorded.  
**Exceptions** – None.

### HasErrorOccurredRecently
```csharp
public static bool HasErrorOccurredRecently()
```
**Purpose** – Indicates whether any error has been logged within the internal recent‑error window (configured by the middleware).  
**Return value** – `true` if at least one error occurred recently; otherwise `false`.  
**Exceptions** – None.

### GetTotalErrorCount
```csharp
public static int GetTotalErrorCount()
```
**Purpose** – Returns the cumulative count of all error contexts stored by the middleware since the process started or since the last reset.  
**Return value** – An integer ≥ 0 representing the total number of recorded errors.  
**Exceptions** – None.

## Usage

### Example 1: Filtering recoverable errors for a specific operation
```csharp
using DotNetResiliencePipeline.ErrorHandling;

// Assume the pipeline has already processed some requests.
var recentRecoverable = ErrorHandlingMiddlewareExtensions
    .GetErrorsByRecoverable(true);

var operationErrors = ErrorHandlingMiddlewareExtensions
    .GetErrorsForOrder("ProcessPayment");

var relevant = recentRecoverable
    .Intersect(operationErrors)
    .ToList();

if (relevant.Any())
{
    var report = ErrorHandlingMiddlewareExtensions.GenerateErrorReport();
    File.WriteAllText("error-report.txt", report);
}
```

### Example 2: Checking error health before emitting metrics
```csharp
if (ErrorHandlingMiddlewareExtensions.HasErrorOccurredRecently())
{
    int total = ErrorHandlingMiddlewareExtensions.GetTotalErrorCount();
    // Emit a health metric indicating degraded state.
    Metrics.Record("pipeline.error.count", total);
}
else
{
    Metrics.Record("pipeline.error.count", 0);
}
```

## Notes
- The methods operate on a shared, internally synchronized error store; concurrent calls from multiple threads are safe and will not corrupt state.  
- `GetErrorsByType` and `GetErrorsForOperation` return snapshots; modifications to the returned `List<ErrorContext>` do not affect the internal store.  
- If the underlying middleware has been reset or cleared, all query methods will return empty collections or default values (`false` for `HasErrorOccurredRecently`, `0` for `GetTotalErrorCount`).  
- `GenerateErrorReport` uses the current culture’s default formatting for dates and numbers; for invariant output, callers should post‑process the string or invoke an overload if provided in a future version.  
- No method throws exceptions under normal operation except for explicit argument validation as described. Passing invalid arguments (e.g., `null` for type or operation name) will result in an `ArgumentNullException` or `ArgumentException`.  
- The recent‑error window used by `HasErrorOccurredRecently` is internal to the middleware and not exposed via these extensions; callers should treat the result as a best‑effort indicator of recent error activity.
