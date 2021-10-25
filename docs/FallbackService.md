# FallbackService

`FallbackService` provides a configurable fallback mechanism for asynchronous operations. It maintains a set of fallback triggers and tracks the success rate of fallback executions. When an operation is executed via `ExecuteAsync<T>`, the service evaluates whether a fallback should be triggered based on the registered triggers and the operation’s outcome. The service is designed to be used within a resilience pipeline to handle transient failures by falling back to alternative logic.

## API

### `ExecuteAsync<T>(Func<Task<T>> operation)`

Executes the provided asynchronous operation and applies fallback logic if a trigger condition is met.

- **Type parameters**: `T` – The return type of the operation.
- **Parameters**:
  - `operation` – A delegate representing the primary operation to execute.
- **Returns**: `Task<PolicyResult<T>>` – A task that resolves to a `PolicyResult<T>` containing either the successful result or failure information, including whether a fallback was executed.
- **Throws**:
  - `ArgumentNullException` – if `operation` is `null`.
  - `InvalidOperationException` – if no fallback triggers have been registered.

### `ShouldTriggerFallback()`

Determines whether the current state of the service indicates that a fallback should be triggered for the next operation.

- **Parameters**: None.
- **Returns**: `bool` – `true` if a fallback should be triggered; otherwise `false`.
- **Throws**: None.

### `GetFallbackSuccessRate()`

Returns the overall success rate of fallback executions since the service was created or last reset.

- **Parameters**: None.
- **Returns**: `double` – A value between 0.0 and 1.0 representing the proportion of successful fallback attempts. Returns `0.0` if no fallback has been executed.
- **Throws**: None.

### `AddFallbackTrigger(string triggerId)`

Registers a fallback trigger identified by the given string. The trigger is used by `ShouldTriggerFallback` and `ExecuteAsync` to decide when to activate fallback logic.

- **Parameters**:
  - `triggerId` – A unique identifier for the trigger.
- **Returns**: `void`.
- **Throws**:
  - `ArgumentNullException` – if `triggerId` is `null`.
  - `ArgumentException` – if a trigger with the same `triggerId` already exists.

### `RemoveFallbackTrigger(string triggerId)`

Removes a previously registered fallback trigger.

- **Parameters**:
  - `triggerId` – The identifier of the trigger to remove.
- **Returns**: `void`.
- **Throws**:
  - `ArgumentNullException` – if `triggerId` is `null`.
  - `KeyNotFoundException` – if no trigger with the given `triggerId` exists.

## Usage

### Example 1: Basic fallback with a single trigger

```csharp
using System;
using System.Threading.Tasks;
using DotNet.Resilience.Pipeline;

public class Example
{
    public async Task RunAsync()
    {
        var fallbackService = new FallbackService();
        fallbackService.AddFallbackTrigger("transient-failure");

        // Simulate an operation that may fail
        var result = await fallbackService.ExecuteAsync(async () =>
        {
            // Primary operation
            await Task.Delay(10);
            throw new TimeoutException("Operation timed out");
        });

        if (result.Outcome == OutcomeType.Success)
        {
            Console.WriteLine($"Success: {result.Result}");
        }
        else
        {
            Console.WriteLine($"Failure: {result.FailureReason}");
        }

        Console.WriteLine($"Fallback success rate: {fallbackService.GetFallbackSuccessRate():P}");
    }
}
```

### Example 2: Conditional fallback based on trigger state

```csharp
using System;
using System.Threading.Tasks;
using DotNet.Resilience.Pipeline;

public class CircuitBreakerExample
{
    public async Task RunAsync()
    {
        var fallbackService = new FallbackService();
        fallbackService.AddFallbackTrigger("circuit-open");

        // Simulate a circuit breaker that opens after repeated failures
        bool circuitOpen = true;

        // Check if fallback should be triggered
        if (fallbackService.ShouldTriggerFallback())
        {
            Console.WriteLine("Fallback activated – using cached response.");
            // Execute fallback operation directly (not shown here)
        }
        else
        {
            var result = await fallbackService.ExecuteAsync(async () =>
            {
                if (circuitOpen)
                    throw new InvalidOperationException("Circuit is open");
                return "Primary data";
            });

            Console.WriteLine(result.Outcome == OutcomeType.Success
                ? $"Result: {result.Result}"
                : "Fallback was attempted");
        }
    }
}
```

## Notes

- **Thread safety**: All public members of `FallbackService` are thread-safe. The service uses internal synchronization to protect the trigger registry and success rate counters, allowing concurrent calls from multiple tasks.
- **Edge cases**:
  - If `GetFallbackSuccessRate()` is called before any fallback execution, it returns `0.0`.
  - Adding a trigger with an identifier that already exists throws an `ArgumentException`. Use `RemoveFallbackTrigger` first if replacement is needed.
  - `ExecuteAsync<T>` throws `InvalidOperationException` when no triggers are registered because the service cannot determine whether a fallback should be attempted.
  - The `ShouldTriggerFallback` method returns `false` when no triggers are registered, even if the service has previously recorded failures.
- **Trigger semantics**: The exact condition that causes `ShouldTriggerFallback` to return `true` depends on the internal implementation. Typically, a trigger becomes active after a configurable number of consecutive failures or based on a time window. The service does not expose these thresholds directly; they are set during construction or via configuration.
