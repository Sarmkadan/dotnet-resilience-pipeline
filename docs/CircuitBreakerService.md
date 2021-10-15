# CircuitBreakerService

The `CircuitBreakerService` is a resilience component designed to prevent cascading failures in distributed systems by temporarily halting operations when a downstream service becomes unresponsive or consistently fails. It monitors the success and failure rates of executed tasks, automatically transitioning between closed, open, and half-open states to allow the system to recover without overwhelming the failing resource.

## API

### `public CircuitBreakerService`
Initializes a new instance of the `CircuitBreakerService` class. This constructor sets up the internal state machine and default thresholds required for circuit breaking logic. No parameters are required.

### `public async Task<T> ExecuteAsync<T>(Func<Task<T>> action)`
Executes the specified asynchronous function within the context of the circuit breaker.
*   **Parameters**: `action` – The asynchronous delegate to execute.
*   **Return Value**: A `Task<T>` representing the result of the operation if the circuit is closed or half-open and the execution succeeds.
*   **Exceptions**: Throws an exception if the circuit is currently open (preventing execution) or if the provided `action` throws an exception that triggers the failure threshold.

### `public async Task<T> ExecuteAsync<T>(Func<Task<T>> action, CancellationToken cancellationToken)`
Executes the specified asynchronous function within the context of the circuit breaker, supporting cooperative cancellation.
*   **Parameters**: 
    *   `action` – The asynchronous delegate to execute.
    *   `cancellationToken` – A token to monitor for cancellation requests.
*   **Return Value**: A `Task<T>` representing the result of the operation if the circuit is closed or half-open and the execution succeeds.
*   **Exceptions**: Throws `OperationCanceledException` if the `cancellationToken` is triggered. Throws an exception if the circuit is open or if the `action` fails.

### `public void OpenCircuit()`
Forces the circuit breaker into the open state immediately. Any subsequent calls to `ExecuteAsync` will fail fast without invoking the underlying action until the circuit is reset or automatically transitions based on internal timing logic (if configured). This method is typically used for manual intervention during critical incidents.

### `public void ResetCircuit()`
Resets the circuit breaker to the closed state, allowing traffic to flow normally. This clears any accumulated failure counts and state flags, effectively restarting the monitoring process. Use this method after confirming that the downstream dependency has recovered.

### `public string GetCircuitState()`
Returns a string representation of the current state of the circuit breaker (e.g., "Open", "Closed", "Half-Open"). This is primarily intended for diagnostics, logging, or health check endpoints.

## Usage

### Basic Execution with Automatic Handling
The following example demonstrates wrapping a database call in the circuit breaker. If the database is unavailable, the circuit will eventually open, and subsequent calls will fail immediately without attempting the connection.

```csharp
public class DataRepository
{
    private readonly CircuitBreakerService _circuitBreaker;

    public DataRepository(CircuitBreakerService circuitBreaker)
    {
        _circuitBreaker = circuitBreaker;
    }

    public async Task<string> GetDataAsync(int id)
    {
        return await _circuitBreaker.ExecuteAsync(async () =>
        {
            // Simulate downstream I/O operation
            return await Database.FetchAsync(id);
        });
    }
}
```

### Manual State Management and Cancellation
This example illustrates how to pass a cancellation token to support request timeouts and how to manually manipulate the circuit state during a deployment or known outage.

```csharp
public async Task ProcessRequestAsync(CancellationToken cancellationToken)
{
    var service = new CircuitBreakerService();

    try
    {
        var result = await service.ExecuteAsync(async () => 
            await ExternalApi.CallAsync(), 
            cancellationToken);
            
        Console.WriteLine($"Result: {result}");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Execution failed: {ex.Message}");
        
        // Manually open the circuit if a critical failure pattern is detected externally
        if (IsCriticalFailure(ex))
        {
            service.OpenCircuit();
            Console.WriteLine("Circuit manually opened due to critical failure.");
        }
    }

    // Check state for logging
    Console.WriteLine($"Current State: {service.GetCircuitState()}");
}
```

## Notes

*   **Thread Safety**: The public methods `OpenCircuit`, `ResetCircuit`, and `GetCircuitState` are designed to be thread-safe, allowing state inspection and manipulation from multiple threads (e.g., a health check thread and a request handling thread). The `ExecuteAsync` methods handle concurrent invocations internally to ensure accurate failure counting and state transitions.
*   **State Consistency**: Calling `OpenCircuit` overrides any automatic state transition logic. The circuit will remain open until `ResetCircuit` is explicitly called, regardless of any internal timers or success counters that might otherwise close the circuit.
*   **Exception Propagation**: `ExecuteAsync` does not swallow exceptions thrown by the delegated `action`. If the action fails, the exception is propagated to the caller, and the failure is recorded against the circuit breaker's metrics.
*   **Cancellation**: When using the overload accepting a `CancellationToken`, if the token is canceled before the action completes, the task is aborted. Depending on the internal configuration, a cancellation may or may not be counted as a failure; however, the primary effect is the immediate termination of the awaiting task.
