# MicroserviceIntegrationExample

The `MicroserviceIntegrationExample` class serves as a demonstration entry point for configuring and executing resilience patterns within a .NET microservice architecture. It encapsulates instances of standard Polly policies—Circuit Breaker, Retry, Timeout, Bulkhead, and Fallback—to illustrate how these components are instantiated and potentially combined to handle transient faults, latency issues, and resource contention in distributed systems.

## API

### `Main`
```csharp
public static async Task Main
```
The primary entry point for the application execution. This method orchestrates the lifecycle of the microservice integration example, typically initializing the resilience policies defined in the class properties and executing a sample workflow to demonstrate their behavior.
*   **Parameters**: None (accepts command-line arguments via standard runtime injection, though not explicitly typed here).
*   **Return Value**: A `Task` representing the asynchronous operation, completing when the demonstration workflow finishes.
*   **Throws**: Propagates any unhandled exceptions occurring during the initialization or execution of the resilience pipeline.

### `CircuitBreaker`
```csharp
public CircuitBreakerPolicy? CircuitBreaker
```
Gets or sets the circuit breaker policy instance used to stop processing requests when a failure threshold is exceeded.
*   **Purpose**: Prevents cascading failures by failing fast when the downstream service is deemed unhealthy.
*   **Value**: An instance of `CircuitBreakerPolicy` if configured; otherwise, `null`.
*   **Throws**: No exceptions are thrown by the property accessor itself.

### `Retry`
```csharp
public RetryPolicy? Retry
```
Gets or sets the retry policy instance configured to handle transient exceptions by re-executing the operation.
*   **Purpose**: Mitigates temporary network glitches or service hiccups by attempting the operation multiple times with a defined strategy.
*   **Value**: An instance of `RetryPolicy` if configured; otherwise, `null`.
*   **Throws**: No exceptions are thrown by the property accessor itself.

### `Timeout`
```csharp
public TimeoutPolicy? Timeout
```
Gets or sets the timeout policy instance used to cancel operations that exceed a specified duration.
*   **Purpose**: Ensures that the system does not wait indefinitely for a non-responsive resource, freeing up threads and resources.
*   **Value**: An instance of `TimeoutPolicy` if configured; otherwise, `null`.
*   **Throws**: No exceptions are thrown by the property accessor itself.

### `Bulkhead`
```csharp
public BulkheadPolicy? Bulkhead
```
Gets or sets the bulkhead policy instance used to limit the concurrency of operations.
*   **Purpose**: Isolates resource usage to prevent a failure in one part of the system from exhausting resources (such as thread pool threads) required by other parts.
*   **Value**: An instance of `BulkheadPolicy` if configured; otherwise, `null`.
*   **Throws**: No exceptions are thrown by the property accessor itself.

### `Fallback`
```csharp
public FallbackPolicy? Fallback
```
Gets or sets the fallback policy instance used to provide a default result or action when all other resilience strategies fail.
*   **Purpose**: Provides a graceful degradation path, ensuring the system returns a meaningful response even when the primary operation cannot be completed.
*   **Value**: An instance of `FallbackPolicy` if configured; otherwise, `null`.
*   **Throws**: No exceptions are thrown by the property accessor itself.

## Usage

### Example 1: Basic Policy Initialization and Inspection
This example demonstrates how to inspect the configured policies within the `Main` method to verify their presence before execution.

```csharp
using System;
using System.Threading.Tasks;

public class MicroserviceIntegrationExample
{
    public static async Task Main(string[] args)
    {
        var example = new MicroserviceIntegrationExample();
        
        // Initialize policies here (logic omitted for brevity)
        
        if (example.CircuitBreaker != null)
        {
            Console.WriteLine("Circuit Breaker is active.");
        }

        if (example.Retry != null)
        {
            Console.WriteLine("Retry policy is configured.");
        }

        await Task.CompletedTask;
    }

    public CircuitBreakerPolicy? CircuitBreaker { get; set; }
    public RetryPolicy? Retry { get; set; }
    public TimeoutPolicy? Timeout { get; set; }
    public BulkheadPolicy? Bulkhead { get; set; }
    public FallbackPolicy? Fallback { get; set; }
}
```

### Example 2: Executing a Resilient Operation
This example illustrates how a specific policy (Retry) might be invoked within the context of the example to execute a hypothetical remote call.

```csharp
using System;
using System.Threading.Tasks;
using Polly;

public class Program
{
    public static async Task Main()
    {
        var example = new MicroserviceIntegrationExample
        {
            Retry = Policy.Handle<Exception>().WaitAndRetryAsync(3, attempt => TimeSpan.FromSeconds(1))
        };

        if (example.Retry != null)
        {
            try
            {
                await example.Retry.ExecuteAsync(async () =>
                {
                    // Simulate a microservice call
                    await Task.Delay(100);
                    Console.WriteLine("Operation executed successfully.");
                });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Operation failed after retries: {ex.Message}");
            }
        }
    }
}
```

## Notes

*   **Nullability**: All policy properties (`CircuitBreaker`, `Retry`, `Timeout`, `Bulkhead`, `Fallback`) are nullable. Consumers must perform null checks before invoking `.ExecuteAsync` or `.Wrap` methods on these instances to avoid `NullReferenceException`.
*   **Thread Safety**: The policy instances themselves (e.g., `CircuitBreakerPolicy`, `RetryPolicy`) are generally thread-safe for execution once initialized. However, the properties on `MicroserviceIntegrationExample` are simple auto-properties; if these properties are modified after the application starts (e.g., swapping policy instances), external synchronization is required to ensure visibility across threads.
*   **Initialization Order**: While the properties are independent, logical resilience pipelines often require a specific wrapping order (e.g., Timeout inside Retry, inside Circuit Breaker). This class exposes them as separate members, leaving the responsibility of composing them into a single `PolicyWrap` to the implementation logic within `Main` or other consumer code.
*   **Disposal**: Some policies may hold resources or timers. If the policies implement `IDisposable`, the owning scope (typically the `Main` method lifecycle) is responsible for disposing of them appropriately when the application shuts down.
