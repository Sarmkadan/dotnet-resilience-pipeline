# ResiliencyException

`ResiliencyException` serves as the base exception type for the `dotnet-resilience-pipeline` library, representing failures that occur during the execution of a resilience strategy. Derived exception types such as `CircuitBreakerOpenException`, `BulkheadRejectedException`, `OperationTimeoutException`, and `MaxRetriesExceededException` provide specific context for distinct failure modes. The base class captures common diagnostic properties—policy metadata, timing information, and failure counts—enabling consistent error handling and logging across all pipeline strategies.

## API

### Properties

- **`public string? PolicyName`**  
  Gets the name assigned to the resilience policy that produced this exception. Returns `null` if no name was configured.

- **`public string? PolicyType`**  
  Gets the type identifier of the resilience policy (e.g., `"Retry"`, `"CircuitBreaker"`, `"Bulkhead"`, `"Timeout"`). Returns `null` if the type cannot be resolved.

- **`public DateTime OccurredAt`**  
  Gets the UTC timestamp at which the exception was raised. This value is captured at the moment the resilience strategy decides to throw.

- **`public TimeSpan TimeUntilRetry`**  
  Gets the delay interval before the next retry attempt is scheduled. Only meaningful for retry-related exceptions; otherwise returns `TimeSpan.Zero`.

- **`public int ConsecutiveFailures`**  
  Gets the number of consecutive failures recorded by the circuit breaker at the moment the exception was thrown. Only meaningful for `CircuitBreakerOpenException`; otherwise returns zero.

- **`public int CurrentExecutions`**  
  Gets the number of currently executing operations at the time the bulkhead rejected the request. Only meaningful for `BulkheadRejectedException`; otherwise returns zero.

- **`public int MaxExecutions`**  
  Gets the maximum number of concurrent executions allowed by the bulkhead. Only meaningful for `BulkheadRejectedException`; otherwise returns zero.

- **`public int QueuedRequests`**  
  Gets the number of requests waiting in the bulkhead queue at the time of rejection. Only meaningful for `BulkheadRejectedException`; otherwise returns zero.

- **`public TimeSpan Timeout`**  
  Gets the timeout duration that was exceeded. Only meaningful for `OperationTimeoutException`; otherwise returns `TimeSpan.Zero`.

- **`public long ActualExecutionTimeMs`**  
  Gets the actual execution time in milliseconds before the timeout occurred. Only meaningful for `OperationTimeoutException`; otherwise returns zero.

- **`public int AttemptCount`**  
  Gets the total number of execution attempts made before the retry strategy exhausted its configured limit. Only meaningful for `MaxRetriesExceededException`; otherwise returns zero.

- **`public List<Exception>? AttemptExceptions`**  
  Gets the list of exceptions captured during each failed retry attempt. Only meaningful for `MaxRetriesExceededException`; otherwise returns `null`.

- **`public Exception? PrimaryException`**  
  Gets the exception thrown by the primary (original) execution path before the fallback was invoked. Only meaningful when a fallback strategy has executed; otherwise returns `null`.

- **`public Exception? FallbackException`**  
  Gets the exception thrown by the fallback action itself, if the fallback also failed. Only meaningful when a fallback strategy has executed and the fallback action threw; otherwise returns `null`.

### Constructors

- **`public ResiliencyException()`**  
  Initializes a new instance of the `ResiliencyException` class with default property values.

- **`public ResiliencyException(string message)`**  
  Initializes a new instance with a specified error message.  
  *Parameters*: `message` — the message that describes the error.

### Derived Types

- **`CircuitBreakerOpenException`**  
  Thrown when an operation is rejected because the circuit breaker is in the open state. Populates `ConsecutiveFailures`.

- **`BulkheadRejectedException`**  
  Thrown when an operation is rejected due to bulkhead capacity exhaustion. Populates `CurrentExecutions`, `MaxExecutions`, and `QueuedRequests`.

- **`OperationTimeoutException`**  
  Thrown when an operation exceeds its configured timeout. Populates `Timeout` and `ActualExecutionTimeMs`.

- **`MaxRetriesExceededException`**  
  Thrown when all retry attempts have been exhausted without success. Populates `AttemptCount` and `AttemptExceptions`.

## Usage

### Example 1: Handling Specific Resilience Exceptions

```csharp
using DotNetResiliencePipeline;

async Task ExecuteWithHandling()
{
    var pipeline = ResiliencePipelineBuilder.Create()
        .AddRetry(new RetryOptions { MaxRetries = 3, Delay = TimeSpan.FromSeconds(1) })
        .AddCircuitBreaker(new CircuitBreakerOptions { FailureThreshold = 5, BreakDuration = TimeSpan.FromSeconds(30) })
        .Build();

    try
    {
        await pipeline.ExecuteAsync(async ct =>
        {
            // Operation that may fail
            await Task.Delay(100, ct);
            throw new InvalidOperationException("Transient failure");
        });
    }
    catch (MaxRetriesExceededException ex)
    {
        Console.WriteLine($"Retries exhausted after {ex.AttemptCount} attempts.");
        Console.WriteLine($"Policy: {ex.PolicyName}, Occurred at: {ex.OccurredAt}");
        foreach (var attemptEx in ex.AttemptExceptions ?? Enumerable.Empty<Exception>())
        {
            Console.WriteLine($"  Attempt failed: {attemptEx.Message}");
        }
    }
    catch (CircuitBreakerOpenException ex)
    {
        Console.WriteLine($"Circuit breaker open after {ex.ConsecutiveFailures} consecutive failures.");
        Console.WriteLine($"Retry after: {ex.TimeUntilRetry}");
    }
    catch (ResiliencyException ex)
    {
        Console.WriteLine($"Unhandled resilience failure: {ex.PolicyType} at {ex.OccurredAt}");
    }
}
```

### Example 2: Logging All Resilience Exception Properties

```csharp
using DotNetResiliencePipeline;

void LogResiliencyException(ResiliencyException ex)
{
    var logEntry = new Dictionary<string, object?>
    {
        ["PolicyName"] = ex.PolicyName,
        ["PolicyType"] = ex.PolicyType,
        ["OccurredAt"] = ex.OccurredAt,
        ["ExceptionType"] = ex.GetType().Name,
        ["Message"] = ex.Message
    };

    switch (ex)
    {
        case MaxRetriesExceededException retryEx:
            logEntry["AttemptCount"] = retryEx.AttemptCount;
            logEntry["AttemptExceptions"] = retryEx.AttemptExceptions?.Select(e => e.Message).ToList();
            break;
        case CircuitBreakerOpenException cbEx:
            logEntry["ConsecutiveFailures"] = cbEx.ConsecutiveFailures;
            logEntry["TimeUntilRetry"] = cbEx.TimeUntilRetry;
            break;
        case BulkheadRejectedException bhEx:
            logEntry["CurrentExecutions"] = bhEx.CurrentExecutions;
            logEntry["MaxExecutions"] = bhEx.MaxExecutions;
            logEntry["QueuedRequests"] = bhEx.QueuedRequests;
            break;
        case OperationTimeoutException toEx:
            logEntry["Timeout"] = toEx.Timeout;
            logEntry["ActualExecutionTimeMs"] = toEx.ActualExecutionTimeMs;
            break;
    }

    if (ex.PrimaryException != null)
        logEntry["PrimaryException"] = ex.PrimaryException.Message;
    if (ex.FallbackException != null)
        logEntry["FallbackException"] = ex.FallbackException.Message;

    // Serialize and write logEntry to structured logging sink
    Console.WriteLine(System.Text.Json.JsonSerializer.Serialize(logEntry));
}
```

## Notes

- **Cross-cutting properties**: Properties specific to one derived exception type return neutral default values (`null`, zero, `TimeSpan.Zero`) when accessed on an incompatible type. Always check the runtime type before interpreting strategy-specific properties.
- **Exception nesting**: `PrimaryException` and `FallbackException` are set only when a fallback strategy executes. If the fallback succeeds, `FallbackException` remains `null`. If no fallback is configured, both remain `null`.
- **Thread safety**: Instances of `ResiliencyException` and its derived types are immutable after construction. They are safe to read from multiple threads concurrently without synchronization.
- **Serialization**: All public properties are read-only and populated at construction time. Standard .NET exception serialization applies; custom serialization is not implemented.
- **Timestamps**: `OccurredAt` is captured as UTC at the point the exception is instantiated. Consumers should not assume correlation with the actual failure time of the wrapped operation, as pipeline processing may introduce small delays.
