# TimeoutService

The `TimeoutService` executes asynchronous operations under an `ITimeoutStrategy`. It validates the strategy, applies its configured timeout when enabled, passes cancellation to the operation, records the outcome and elapsed time, and distinguishes a policy timeout from cancellation requested by the caller.

## Constructor

### `TimeoutService()`

Creates a `TimeoutService`. The service has no constructor parameters and keeps no per-instance configuration; the timeout strategy is supplied to each operation.

## API

### `ExecuteAsync<T>`

```csharp
Task<T> ExecuteAsync<T>(
    ITimeoutStrategy timeoutStrategy,
    Func<CancellationToken, Task<T>> operation,
    CancellationToken cancellationToken = default)
```

Executes `operation` using the supplied timeout strategy.

- **Parameters**:
  - `timeoutStrategy`: Provides the timeout, enabled state, configuration validation, and statistics recording.
  - `operation`: The asynchronous operation to execute. It receives the effective cancellation token.
  - `cancellationToken`: An optional token through which the caller can cancel the operation.
- **Return Value**: The value produced by `operation`.
- **Disabled strategy**: When the strategy is disabled, the operation is invoked directly with the caller's token; no timeout source is created and this method does not record timeout metrics for that execution.
- **Linked cancellation behavior**: When the strategy is enabled, the service creates one `CancellationTokenSource` for the configured timeout and links its token with `cancellationToken`. The linked token passed to `operation` is canceled when either source is canceled. An `OperationCanceledException` caused by the timeout source, while the caller's token has not been canceled, is converted to `OperationTimeoutException`. If the caller's token is canceled, the `OperationCanceledException` is rethrown and is not recorded as a timeout.
- **Metrics**: A successful execution records elapsed time and success. A timeout records a timeout using the measured elapsed milliseconds. Other exceptions record elapsed time and failure before being rethrown.
- **Exceptions**:
  - `ArgumentNullException` when `timeoutStrategy` is `null`.
  - `InvalidPolicyConfigurationException` when the strategy reports an invalid configuration.
  - `OperationTimeoutException` when the operation observes cancellation caused by the configured timeout and external cancellation has not been requested.
  - `OperationCanceledException` when cancellation was requested through `cancellationToken`.
  - Exceptions thrown by `operation` are rethrown unchanged.

The operation must observe the provided cancellation token for a timeout to interrupt it and produce `OperationTimeoutException`.

### `HasExceededTimeout`

```csharp
bool HasExceededTimeout(ITimeoutStrategy timeoutStrategy, long executionTimeMs)
```

Checks whether an elapsed time exceeds a timeout.

- **Parameters**:
  - `timeoutStrategy`: The strategy to inspect.
  - `executionTimeMs`: The elapsed time in milliseconds.
- **Return Value**: For a `TimeoutPolicy`, returns the result of `TimeoutPolicy.IsTimedOutMs(executionTimeMs)`. For `null` or any other `ITimeoutStrategy` implementation, returns `false`.
- **Boundary behavior**: `TimeoutPolicy.IsTimedOutMs` uses a strict greater-than comparison, so a duration equal to the configured timeout has not exceeded it.

### `GetTimeoutMilliseconds`

```csharp
long GetTimeoutMilliseconds(ITimeoutStrategy timeoutStrategy)
```

Returns the strategy's configured timeout as a `long` number of milliseconds. A `null` strategy returns `0`.

The conversion uses `TimeSpan.TotalMilliseconds`, which represents the full duration. It does not use `TimeSpan.Milliseconds`, which returns only the millisecond component from 0 through 999. Casting the `double` returned by `TotalMilliseconds` to `long` discards any fractional millisecond.

## Usage

### Execute an operation with `TimeoutPolicy`

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;

var timeoutService = new TimeoutService();
var timeoutPolicy = new TimeoutPolicy("inventory-request")
{
    Timeout = TimeSpan.FromSeconds(2)
};

try
{
    string result = await timeoutService.ExecuteAsync(
        timeoutPolicy,
        async cancellationToken =>
        {
            await Task.Delay(TimeSpan.FromMilliseconds(250), cancellationToken);
            return "completed";
        });

    Console.WriteLine(result);
}
catch (OperationTimeoutException exception)
{
    Console.WriteLine($"The operation exceeded its timeout: {exception.Message}");
}
```

## Notes

- `TimeoutService` measures elapsed time with `Stopwatch`.
- The timeout and linked cancellation sources are disposed after the execution finishes.
- `HasExceededTimeout` performs its timeout check only for the concrete `TimeoutPolicy`, even though `ExecuteAsync` accepts any `ITimeoutStrategy`.
- `OperationTimeoutException` is produced only when the operation throws `OperationCanceledException` after the timeout token is canceled and the caller's token is not canceled.
