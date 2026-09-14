# Event Json Extensions Documentation

This document provides detailed information about the JSON serialization extensions for resilience events and exceptions in the DotNetResiliencePipeline library.

## ResiliencyEventPublisherJsonExtensions

Located in: `src/Events/ResiliencyEventPublisherJsonExtensions.cs`

Provides System.Text.Json serialization extensions for the `ResiliencyEventPublisher` class.

### Methods

#### ToJson

```csharp
public static string ToJson(this ResiliencyEventPublisher value, bool indented = false)
```

Serializes the `ResiliencyEventPublisher` to a JSON string.

**Parameters:**
- `value`: The publisher instance to serialize.
- `indented`: Whether to format the JSON with indentation for readability (default: false).

**Returns:** A JSON string representation of the publisher.

**Exceptions:**
- `ArgumentNullException`: Thrown when `value` is null.

**Sample JSON Output:**
```json
{
  "TotalEventsEmitted": 42,
  "SuccessfulExecutions": 35,
  "FailedExecutions": 7,
  "CircuitBreakerChanges": 3,
  "BulkheadRejections": 2,
  "Timeouts": 1,
  "FallbacksTriggered": 4,
  "PolicyHealthChanged": 2,
  "MaxHistorySize": 1000
}
```

**Example Usage:**
```csharp
var publisher = new ResiliencyEventPublisher();
// ... publish some events ...
string json = publisher.ToJson(); // Compact JSON
string prettyJson = publisher.ToJson(indented: true); // Indented JSON
```

#### FromJson

```csharp
public static ResiliencyEventPublisher FromJson(string json)
```

Deserializes a JSON string to a `ResiliencyEventPublisher` instance.

**Parameters:**
- `json`: The JSON string to deserialize.

**Returns:** The deserialized publisher instance.

**Exceptions:**
- `ArgumentNullException`: Thrown when `json` is null.
- `ArgumentException`: Thrown when `json` is empty or whitespace.
- `JsonException`: Thrown when the JSON is invalid or cannot be deserialized.

**Example Usage:**
```csharp
string json = /* ... */;
var publisher = ResiliencyEventPublisherJsonExtensions.FromJson(json);
```

#### TryFromJson

```csharp
public static bool TryFromJson(string json, out ResiliencyEventPublisher? value)
```

Attempts to deserialize a JSON string to a `ResiliencyEventPublisher` instance.

**Parameters:**
- `json`: The JSON string to deserialize.
- `value`: Receives the deserialized publisher instance if successful.

**Returns:** True if deserialization succeeded; otherwise, false.

**Exceptions:**
- `ArgumentNullException`: Thrown when `json` is null.

**Example Usage:**
```csharp
string json = /* ... */;
if (ResiliencyEventPublisherJsonExtensions.TryFromJson(json, out var publisher))
{
    // Use publisher
}
else
{
    // Handle invalid JSON
}
```

## ResiliencyExceptionJsonExtensions

Located in: `src/Exceptions/ResiliencyExceptionJsonExtensions.cs`

Provides System.Text.Json serialization and deserialization extensions for `ResiliencyException` and derived types.

### Methods

#### ToJson

```csharp
public static string ToJson(this ResiliencyException value, bool indented = false)
```

Serializes the `ResiliencyException` to a JSON string.

**Parameters:**
- `value`: The exception to serialize.
- `indented`: Whether to format the JSON with indentation (default: false).

**Returns:** A JSON string representation of the exception.

**Exceptions:**
- `ArgumentNullException`: Thrown when `value` is null.

**Sample JSON Output by Exception Type:**

1. **Base ResiliencyException:**
```json
{
  "PolicyName": "RetryPolicy",
  "PolicyType": "Retry",
  "OccurredAt": "2026-09-14T10:30:00.1234567Z",
  "HelpLinks": [],
  "Data": {},
  "TargetSite": null,
  "StackTrace": "   at MyMethod() in File.cs:line 42",
  "Message": "All 3 retry attempts failed."
}
```

2. **CircuitBreakerOpenException:**
```json
{
  "PolicyName": "CBPolicy",
  "PolicyType": "CircuitBreaker",
  "OccurredAt": "2026-09-14T10:30:00.1234567Z",
  "TimeUntilRetry": "00:00:30",
  "ConsecutiveFailures": 5,
  "CurrentExecutions": "N/A",
  "Message": "Circuit breaker 'CBPolicy' is open. Retry after 30.00 seconds."
}
```

3. **BulkheadRejectedException:**
```json
{
  "PolicyName": "BulkheadPolicy",
  "PolicyType": "Bulkhead",
  "OccurredAt": "2026-09-14T10:30:00.1234567Z",
  "CurrentExecutions": 10,
  "MaxExecutions": 10,
  "QueuedRequests": 5,
  "TimeUntilRetry": "N/A",
  "ConsecutiveFailures": "N/A",
  "Message": "Bulkhead 'BulkheadPolicy' is saturated (10/10 slots in use, 5 queued)."
}
```

4. **OperationTimeoutException:**
```json
{
  "PolicyName": "TimeoutPolicy",
  "PolicyType": "Timeout",
  "OccurredAt": "2026-09-14T10:30:00.1234567Z",
  "Timeout": "00:00:05",
  "ActualExecutionTimeMs": 5200,
  "TimeUntilRetry": "N/A",
  "ConsecutiveFailures": "N/A",
  "CurrentExecutions": "N/A",
  "Message": "Operation exceeded timeout of 5.00 seconds (5200ms)."
}
```

5. **MaxRetriesExceededException:**
```json
{
  "PolicyName": "RetryPolicy",
  "PolicyType": "Retry",
  "OccurredAt": "2026-09-14T10:30:00.1234567Z",
  "AttemptCount": 3,
  "AttemptExceptions": [
    {
      "ClassName": "System.IO.IOException",
      "Message": "The file is locked.",
      "Data": {},
      "InnerException": null,
      "HelpLinks": [],
      "StackTrace": "   at File.Read()",
      "HelpLink": null,
      "Source": null,
      "HResult": -2146232800
    }
  ],
  "TimeUntilRetry": "N/A",
  "ConsecutiveFailures": "N/A",
  "CurrentExecutions": "N/A",
  "Message": "All 3 retry attempts failed."
}
```

6. **FallbackFailedException:**
```json
{
  "PolicyName": "FallbackPolicy",
  "PolicyType": "Fallback",
  "OccurredAt": "2026-09-14T10:30:00.1234567Z",
  "PrimaryException": {
    "ClassName": "System.Net.Http.HttpRequestException",
    "Message": "Connection refused",
    "Data": {},
    "InnerException": null,
    "HelpLinks": [],
    "StackTrace": "   at HttpClient.Send()",
    "HelpLink": null,
    "Source": null,
    "HResult": -2146233088
  },
  "FallbackException": {
    "ClassName": "System.InvalidOperationException",
    "Message": "Fallback service unavailable",
    "Data": {},
    "InnerException": null,
    "HelpLinks": [],
    "StackTrace": "   at FallbackService.Execute()",
    "HelpLink": null,
    "Source": null,
    "HResult": -2146233078
  },
  "TimeUntilRetry": "N/A",
  "ConsecutiveFailures": "N/A",
  "CurrentExecutions": "N/A",
  "Message": "Both primary operation and fallback failed. Primary: Connection refused, Fallback: Fallback service unavailable"
}
```

7. **InvalidPolicyConfigurationException:**
```json
{
  "PolicyName": "InvalidPolicy",
  "PolicyType": "Configuration",
  "OccurredAt": "2026-09-14T10:30:00.1234567Z",
  "ConfigurationErrors": [
    "Timeout value must be positive",
    "Retry count must be between 1 and 10"
  ],
  "TimeUntilRetry": "N/A",
  "ConsecutiveFailures": "N/A",
  "CurrentExecutions": "N/A",
  "Message": "The policy configuration is invalid."
}
```

8. **PipelineExecutionException:**
```json
{
  "PolicyName": "Timeout",
  "PolicyType": "Pipeline",
  "OccurredAt": "2026-09-14T10:30:00.1234567Z",
  "ExecutionId": "exec_12345",
  "AppliedPolicies": [
    "Timeout",
    "Retry",
    "CircuitBreaker"
  ],
  "TimeUntilRetry": "N/A",
  "ConsecutiveFailures": "N/A",
  "CurrentExecutions": "N/A",
  "Message": "Pipeline execution failed due to timeout."
}
```

#### FromJson

```csharp
public static ResiliencyException? FromJson(string json)
```

Deserializes a `ResiliencyException` from a JSON string.

**Parameters:**
- `json`: The JSON string to deserialize.

**Returns:** The deserialized exception, or null if the JSON is invalid.

**Exceptions:**
- `ArgumentException`: Thrown when `json` is null, empty, or whitespace.
- `JsonException`: Thrown when the JSON is malformed.

**Example Usage:**
```csharp
string json = /* ... */;
var exception = ResiliencyExceptionJsonExtensions.FromJson(json);
if (exception != null)
{
    // Handle exception
}
```

#### TryFromJson

```csharp
public static bool TryFromJson(string json, out ResiliencyException? value)
```

Attempts to deserialize a `ResiliencyException` from a JSON string.

**Parameters:**
- `json`: The JSON string to deserialize.
- `value`: The deserialized exception, or null if deserialization fails.

**Returns:** True if deserialization succeeds; otherwise, false.

**Exceptions:**
- `ArgumentException`: Thrown when `json` is null or empty.

**Example Usage:**
```csharp
string json = /* ... */;
if (ResiliencyExceptionJsonExtensions.TryFromJson(json, out var exception))
{
    // Handle exception
}
else
{
    // Handle invalid JSON
}
```

## Usage Notes

1. All extension methods use `JsonSerializerOptionsProvider.SharedOptions` for consistent serialization settings.
2. The `ToJson` methods support optional indentation for readable output.
3. The `FromJson` methods throw exceptions on invalid input, while `TryFromJson` methods return boolean success indicators.
4. Exception serialization includes all public properties, including inherited ones from `System.Exception`.
5. Null values are handled appropriately according to System.Text.Json defaults.

## Related Types

- `ResiliencyEventPublisher` - The event publisher class being serialized
- `ResiliencyEvent` - Base class for all resilience events
- `ResiliencyException` - Base exception for all resilience pipeline failures
- Derived exception types: `CircuitBreakerOpenException`, `BulkheadRejectedException`, `OperationTimeoutException`, `MaxRetriesExceededException`, `FallbackFailedException`, `InvalidPolicyConfigurationException`, `PipelineExecutionException`