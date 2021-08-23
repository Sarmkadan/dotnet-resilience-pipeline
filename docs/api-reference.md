# API Reference

Complete API documentation for the DotNet Resilience Pipeline.

## ResiliencyPipelineService

Main orchestrator for executing operations with resilience policies.

### ExecuteAsync\<T\>

Executes an operation with optional resilience policies.

```csharp
Task<PolicyResult<T>> ExecuteAsync<T>(
    Func<CancellationToken, Task<T>> operation,
    CircuitBreakerPolicy? circuitBreaker = null,
    RetryPolicy? retry = null,
    TimeoutPolicy? timeout = null,
    BulkheadPolicy? bulkhead = null,
    FallbackPolicy? fallback = null,
    CancellationToken cancellationToken = default)
```

**Parameters:**
- `operation`: Main operation to execute
- `circuitBreaker`: Optional circuit breaker policy
- `retry`: Optional retry policy
- `timeout`: Optional timeout policy
- `bulkhead`: Optional bulkhead policy
- `fallback`: Optional fallback policy
- `cancellationToken`: Cancellation token

**Returns:** PolicyResult<T> containing execution outcome

**Throws:**
- `CircuitBreakerOpenException`: If circuit breaker is open
- `BulkheadRejectedException`: If bulkhead capacity exceeded
- `OperationTimeoutException`: If operation exceeds timeout
- `FallbackFailedException`: If both primary and fallback fail

**Example:**
```csharp
var result = await pipeline.ExecuteAsync(
    async ct => await api.GetUserAsync(userId, ct),
    circuitBreaker: cbPolicy,
    retry: retryPolicy,
    timeout: timeoutPolicy
);
```

### GetStatistics

Retrieves execution statistics.

```csharp
PipelineStatistics GetStatistics()
```

**Returns:** PipelineStatistics object

**Example:**
```csharp
var stats = pipeline.GetStatistics();
Console.WriteLine($"Success Rate: {stats.SuccessRate:P}");
Console.WriteLine($"Total Executions: {stats.TotalExecutions}");
```

## PolicyResult\<T\>

Generic wrapper for operation results.

### Properties

```csharp
public bool IsSuccess { get; }           // True if operation succeeded
public T? Value { get; }                 // Result value if successful
public Exception? Error { get; }         // Exception if failed
public TimeSpan Duration { get; }        // Total execution duration
public int RetryCount { get; }           // Number of retry attempts
public string? CircuitBreakerState { get; } // Circuit breaker state
```

**Example:**
```csharp
if (result.IsSuccess)
{
    Console.WriteLine($"Result: {result.Value}");
}
else
{
    Console.WriteLine($"Error: {result.Error?.Message}");
    Console.WriteLine($"Retry attempts: {result.RetryCount}");
}
```

## CircuitBreakerPolicy

Prevents cascading failures by monitoring failure rates.

### Properties

```csharp
public string Name { get; set; }                    // Policy name
public int FailureThreshold { get; set; }          // Failures before open
public TimeSpan OpenDuration { get; set; }         // Time in open state
public int SuccessThresholdInHalfOpen { get; set; } // Successes to close
public string State { get; }                       // Current state
public int ConsecutiveFailures { get; }            // Current failure count
```

### Methods

```csharp
// Check if circuit is open
bool IsOpen()

// Get current state
string GetState()

// Reset circuit to closed
void Reset()
```

**States:**
- `Closed`: Normal operation, requests pass through
- `Open`: Rejection mode, requests fail immediately
- `HalfOpen`: Testing recovery with limited requests

**Example:**
```csharp
var policy = new CircuitBreakerPolicy("api-calls")
{
    FailureThreshold = 5,
    OpenDuration = TimeSpan.FromSeconds(30),
    SuccessThresholdInHalfOpen = 3
};

var isOpen = policy.IsOpen();
```

## RetryPolicy

Automatic retry with configurable backoff strategies.

### Properties

```csharp
public string Name { get; set; }                    // Policy name
public int MaxRetries { get; set; }                 // Maximum retry attempts
public TimeSpan InitialDelay { get; set; }          // First retry delay
public BackoffStrategy Strategy { get; set; }       // Backoff algorithm
public double BackoffMultiplier { get; set; }       // Multiplier for exponential
public TimeSpan MaxDelay { get; set; }              // Maximum delay between retries
public List<Type> RetryableExceptions { get; set; } // Exception types to retry
```

### BackoffStrategy Enum

```csharp
public enum BackoffStrategy
{
    Fixed = 0,      // Constant delay
    Linear = 1,     // Linearly increasing
    Exponential = 2 // Exponentially increasing
}
```

**Example:**
```csharp
var policy = new RetryPolicy("api-retry")
{
    MaxRetries = 3,
    InitialDelay = TimeSpan.FromMilliseconds(100),
    Strategy = RetryPolicy.BackoffStrategy.Exponential,
    BackoffMultiplier = 2.0,
    MaxDelay = TimeSpan.FromSeconds(30)
};
```

## TimeoutPolicy

Enforces maximum execution duration.

### Properties

```csharp
public string Name { get; set; }        // Policy name
public TimeSpan Timeout { get; set; }   // Maximum execution time
```

**Example:**
```csharp
var policy = new TimeoutPolicy("operations")
{
    Timeout = TimeSpan.FromSeconds(30)
};
```

## BulkheadPolicy

Resource isolation through parallelization limits.

### Properties

```csharp
public string Name { get; set; }                    // Policy name
public int MaxParallelization { get; set; }         // Max concurrent executions
public int MaxQueueLength { get; set; }             // Max queued requests
public int CurrentExecutions { get; }               // Current execution count
public int CurrentQueueLength { get; }              // Current queue length
```

**Example:**
```csharp
var policy = new BulkheadPolicy("database")
{
    MaxParallelization = 10,
    MaxQueueLength = 50
};

var available = policy.MaxParallelization - policy.CurrentExecutions;
```

## FallbackPolicy

Alternative execution paths for failed operations.

### Properties

```csharp
public string Name { get; set; }                    // Policy name
public bool FallbackOnAnyException { get; set; }    // Fallback on any error
public TimeSpan FallbackTimeout { get; set; }       // Fallback execution timeout
public List<Type> FallbackExceptions { get; set; }  // Exception types for fallback
```

**Example:**
```csharp
var policy = new FallbackPolicy("user-service")
{
    FallbackOnAnyException = true,
    FallbackTimeout = TimeSpan.FromSeconds(5)
};
```

## ResiliencyPipelineBuilder

Fluent configuration builder.

### WithCircuitBreaker

```csharp
public ResiliencyPipelineBuilder WithCircuitBreaker(
    string policyName,
    Action<CircuitBreakerPolicy> configurePolicy)
```

**Example:**
```csharp
builder.WithCircuitBreaker("api", policy =>
{
    policy.FailureThreshold = 5;
    policy.OpenDuration = TimeSpan.FromSeconds(30);
});
```

### WithRetry

```csharp
public ResiliencyPipelineBuilder WithRetry(
    string policyName,
    Action<RetryPolicy> configurePolicy)
```

**Example:**
```csharp
builder.WithRetry("api", policy =>
{
    policy.MaxRetries = 3;
    policy.InitialDelay = TimeSpan.FromMilliseconds(100);
    policy.Strategy = RetryPolicy.BackoffStrategy.Exponential;
});
```

### WithTimeout

```csharp
public ResiliencyPipelineBuilder WithTimeout(
    string policyName,
    TimeSpan timeout)
```

**Example:**
```csharp
builder.WithTimeout("operations", TimeSpan.FromSeconds(30));
```

### WithBulkhead

```csharp
public ResiliencyPipelineBuilder WithBulkhead(
    string policyName,
    int maxParallelization,
    int maxQueueLength)
```

**Example:**
```csharp
builder.WithBulkhead("database", maxParallelization: 10, maxQueueLength: 50);
```

### WithFallback

```csharp
public ResiliencyPipelineBuilder WithFallback(
    string policyName,
    Action<FallbackPolicy>? configurePolicy = null)
```

**Example:**
```csharp
builder.WithFallback("user-service", policy =>
{
    policy.FallbackOnAnyException = true;
    policy.FallbackTimeout = TimeSpan.FromSeconds(5);
});
```

## PolicyRepository

Policy persistence and retrieval.

### GetPolicy\<T\>

```csharp
T? GetPolicy<T>(string policyName) where T : ResiliencyPolicy
```

**Parameters:**
- `policyName`: Name of the policy to retrieve

**Returns:** Policy instance or null if not found

**Example:**
```csharp
var cbPolicy = repository.GetPolicy<CircuitBreakerPolicy>("api-calls");
var retryPolicy = repository.GetPolicy<RetryPolicy>("api-calls");
```

### SavePolicy

```csharp
Task SavePolicyAsync<T>(string policyName, T policy) where T : ResiliencyPolicy
```

**Parameters:**
- `policyName`: Policy identifier
- `policy`: Policy instance to save

**Example:**
```csharp
var policy = new CircuitBreakerPolicy("api") { FailureThreshold = 5 };
await repository.SavePolicyAsync("api", policy);
```

### GetAllPolicies

```csharp
IEnumerable<(string Name, ResiliencyPolicy Policy)> GetAllPolicies()
```

**Returns:** All stored policies with their names

## ExecutionHistoryRepository

Execution metrics and history storage.

### RecordExecution

```csharp
Task RecordExecutionAsync(ExecutionRecord record)
```

**Parameters:**
- `record`: Execution record containing outcome and metadata

### GetExecutionHistory

```csharp
IEnumerable<ExecutionRecord> GetExecutionHistory(
    int maxRecords = 1000,
    DateTime? since = null)
```

**Parameters:**
- `maxRecords`: Maximum records to return
- `since`: Optional filter by timestamp

### GetMetrics

```csharp
ExecutionMetrics GetMetrics(string? policyName = null)
```

**Parameters:**
- `policyName`: Optional filter by policy name

**Returns:** Aggregated metrics

## PipelineStatistics

Execution statistics snapshot.

### Properties

```csharp
public long TotalExecutions { get; }          // Total operations executed
public long SuccessfulExecutions { get; }     // Successful operations
public long FailedExecutions { get; }         // Failed operations
public double SuccessRate { get; }            // Success percentage
public double AverageDurationMs { get; }      // Average execution time
public double MinDurationMs { get; }          // Minimum execution time
public double MaxDurationMs { get; }          // Maximum execution time
public int ActiveCircuitBreakers { get; }     // Currently open circuits
public double AverageBulkheadUtilization { get; } // Bulkhead usage
```

## ResiliencyHelper

Utility methods for policy management.

### ValidatePolicy

```csharp
public static List<string> ValidatePolicy(ResiliencyPolicy policy)
```

**Returns:** List of validation error messages (empty if valid)

**Example:**
```csharp
var errors = ResiliencyHelper.ValidatePolicy(myPolicy);
if (errors.Any())
{
    foreach (var error in errors)
        Console.WriteLine($"Error: {error}");
}
```

### GenerateHealthReport

```csharp
public static HealthReport GenerateHealthReport(
    ResiliencyPipelineService pipeline,
    ExecutionHistoryRepository history)
```

**Returns:** HealthReport with overall status

## Exception Types

### CircuitBreakerOpenException

Thrown when circuit breaker is open and rejecting requests.

```csharp
try
{
    await pipeline.ExecuteAsync(operation, circuitBreaker: policy);
}
catch (CircuitBreakerOpenException ex)
{
    Console.WriteLine($"Circuit open: {ex.Message}");
}
```

### BulkheadRejectedException

Thrown when bulkhead capacity is exceeded.

```csharp
try
{
    await pipeline.ExecuteAsync(operation, bulkhead: policy);
}
catch (BulkheadRejectedException ex)
{
    Console.WriteLine($"Bulkhead full: {ex.Message}");
}
```

### OperationTimeoutException

Thrown when operation exceeds timeout duration.

```csharp
try
{
    await pipeline.ExecuteAsync(operation, timeout: policy);
}
catch (OperationTimeoutException ex)
{
    Console.WriteLine($"Timeout: {ex.Message}");
}
```

### MaxRetriesExceededException

Thrown when all retry attempts are exhausted.

```csharp
try
{
    await pipeline.ExecuteAsync(operation, retry: policy);
}
catch (MaxRetriesExceededException ex)
{
    Console.WriteLine($"Max retries exceeded: {ex.Message}");
}
```

### FallbackFailedException

Thrown when both primary and fallback operations fail.

```csharp
try
{
    await pipeline.ExecuteAsync(operation, fallback: policy);
}
catch (FallbackFailedException ex)
{
    Console.WriteLine($"Fallback failed: {ex.Message}");
}
```

## Extension Methods

### AddResiliencePipeline

Registers resilience pipeline in dependency injection container.

```csharp
public static IServiceCollection AddResiliencePipeline(
    this IServiceCollection services,
    Action<ResiliencyPipelineBuilder> configureBuilder)
```

**Example:**
```csharp
services.AddResiliencePipeline(builder =>
{
    builder.WithCircuitBreaker("api", options =>
    {
        options.FailureThreshold = 5;
    });
});
```

## Events

### PolicyEvent

Event published on policy state changes and executions.

**Properties:**
```csharp
public string EventType { get; }        // Event type identifier
public string PolicyName { get; }       // Associated policy name
public DateTime Timestamp { get; }      // Event occurrence time
public Dictionary<string, object> Data { get; } // Additional context
```

**Example:**
```csharp
eventPublisher.Subscribe((PolicyEvent @event) =>
{
    if (@event.EventType == "CircuitBreakerOpened")
    {
        Console.WriteLine($"Circuit opened: {@event.PolicyName}");
    }
});
```
