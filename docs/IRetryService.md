# IRetryService
The `IRetryService` interface defines the contract for a service that handles retry policy execution, enabling dependency injection and easier testing by abstracting the retry logic implementation.

## API
### ExecuteAsync<T>(RetryPolicy policy, Func<CancellationToken, Task<T>> operation, CancellationToken cancellationToken)
Executes an operation with retry logic, accepting a cancellation token-aware operation delegate.
* Parameters:
  * `policy`: The retry policy to apply.
  * `operation`: An asynchronous operation that accepts a cancellation token.
  * `cancellationToken`: A token to monitor for cancellation requests.
* Return Value: A task that represents the asynchronous operation, returning a value of type `T`.
* Exceptions: May throw exceptions if all retry attempts fail or if an unretryable exception occurs.

### ExecuteAsync<T>(RetryPolicy policy, Func<Task<T>> operation, CancellationToken cancellationToken)
Executes an operation through the retry policy (without explicit cancellation token support in the operation delegate).
* Parameters:
  * `policy`: The retry policy to apply.
  * `operation`: An asynchronous operation that does not accept a cancellation token.
  * `cancellationToken`: A token to monitor for cancellation requests.
* Return Value: A task that represents the asynchronous operation, returning a value of type `T`.
* Exceptions: May throw exceptions if all retry attempts fail or if an unretryable exception occurs.

### CalculateRetryDelay(RetryPolicy policy, int attemptNumber)
Calculates retry delay with configured backoff strategy.
* Parameters:
  * `policy`: The retry policy containing backoff configuration.
  * `attemptNumber`: The current attempt number (starting from 0 for the first retry).
* Return Value: A `TimeSpan` representing the delay before the next retry attempt.
* Exceptions: None.

### IsRetryable(RetryPolicy policy, Exception exception)
Determines if an exception is retryable based on policy configuration.
* Parameters:
  * `policy`: The retry policy containing exception filters.
  * `exception`: The exception to evaluate.
* Return Value: `true` if the exception is retryable; otherwise, `false`.
* Exceptions: None.

### ComputeDecorrelatedJitterDelay(RetryPolicy policy, TimeSpan previousDelay)
Computes the next delay using the decorrelated jitter algorithm.
* Parameters:
  * `policy`: The retry policy containing jitter configuration.
  * `previousDelay`: The delay applied in the previous retry attempt.
* Return Value: A `TimeSpan` representing the computed delay for the next retry.
* Exceptions: None.

## Why the Interface Exists
The `IRetryService` interface exists to:
* Enable dependency injection, allowing different retry service implementations to be swapped without changing dependent code.
* Facilitate unit testing by allowing mocks or stubs of the retry service to be injected.
* Promote loose coupling between the retry logic and the classes that use it.

## Usage
The following examples demonstrate how to register and resolve the `IRetryService` interface using a typical dependency injection container (e.g., Microsoft.Extensions.DependencyInjection):

```csharp
// Registering the RetryService as the implementation for IRetryService
services.AddSingleton<IRetryService, RetryService>();

// Resolving the IRetryService in a class constructor
public class MyService
{
    private readonly IRetryService _retryService;

    public MyService(IRetryService retryService)
    {
        _retryService = retryService;
    }

    public async Task<string> GetDataAsync()
    {
        return await _retryService.ExecuteAsync<string>(
            new RetryPolicy { MaxAttempts = 3, Delay = TimeSpan.FromSeconds(1) },
            async (ct) =>
            {
                // Simulate an operation that may fail
                await Task.Delay(100, ct);
                return "Data retrieved successfully";
            },
            CancellationToken.None);
    }
}
```

## Notes
* The interface allows for multiple implementations (e.g., a different retry strategy) without altering the consuming code.
* When implementing `IRetryService`, ensure that all methods handle cancellation tokens appropriately to support graceful shutdowns.
* The interface is designed to be stateless; implementations should not rely on instance state that could affect thread safety.