# AdaptiveTimeoutService

The `AdaptiveTimeoutService` provides a mechanism to execute asynchronous operations with a timeout value that adapts based on observed execution times. It maintains internal statistics to adjust the timeout dynamically, aiming to balance responsiveness with tolerance for variability in operation duration.

## API

### AdaptiveTimeoutService()
Creates a new instance of `AdaptiveTimeoutService` with default adaptation parameters. The service starts with an initial timeout of `TimeSpan.FromSeconds(30)` and begins collecting timing data after the first execution.

### Task<T> ExecuteAsync<T>(Func<CancellationToken, Task<T>> operation)
Executes the supplied asynchronous `operation` using the current adaptive timeout.

- **Parameters**
  - `operation`: A delegate that accepts a `CancellationToken` and returns a `Task<T>` representing the work to be performed. The delegate should observe the token to support timely cancellation.
- **Return Value**
  - A `Task<T>` that completes with the result of `operation` if it finishes before the timeout elapses.
- **Exceptions**
  - `OperationCanceledException` if the operation does not complete within the current timeout period; the exception's `CancellationToken` is the one supplied to the delegate.
  - `ArgumentNullException` if `operation` is `null`.
  - Any exception thrown by `operation` is propagated unchanged.

### TimeSpan GetCurrentTimeout()
Retrieves the timeout value that will be applied to the next invocation of `ExecuteAsync<T>`.

- **Parameters**: None.
- **Return Value**: The current timeout as a `TimeSpan`. The value reflects the most recent adaptation based on observed execution times.
- **Exceptions**: None.

### Dictionary<string, object> GetAdaptationSummary()
Provides diagnostic information about the internal state used for timeout adaptation.

- **Parameters**: None.
- **Return Value**: A read‑only dictionary containing keys such as `"SampleCount"`, `"MeanExecutionTime"`, `"StandardDeviation"`, `"MinObserved"`, `"MaxObserved"`, and `"CurrentTimeout"` with corresponding numeric or `TimeSpan` values. The exact set of keys may evolve but will always include sufficient data to understand the adaptation algorithm.
- **Exceptions**: None.

## Usage

```csharp
var timeoutService = new AdaptiveTimeoutService();

// Define an operation that may take variable time.
async Task<string> FetchDataAsync(CancellationToken ct)
{
    // Simulate work that respects cancellation.
    await Task.Delay(TimeSpan.FromSeconds(5), ct);
    return "result";
}

// Execute with adaptive timeout.
try
{
    string result = await timeoutService.ExecuteAsync(FetchDataAsync);
    Console.WriteLine($"Success: {result}");
}
catch (OperationCanceledException) when (!timeoutService.GetCurrentTimeout().Equals(TimeSpan.Zero))
{
    Console.WriteLine("Operation exceeded the adaptive timeout.");
}
```

```csharp
// Monitoring adaptation over multiple calls.
var service = new AdaptiveTimeoutService();

for (int i = 0; i < 10; i++)
{
    await service.ExecuteAsync(ct => Task.Delay(TimeSpan.FromMilliseconds(200 + i * 50), ct));
    var summary = service.GetAdaptationSummary();
    Console.WriteLine($"Iteration {i}: Mean = {summary["MeanExecutionTime"]} ms, Timeout = {summary["CurrentTimeout"]}");
}
```

## Notes

- The service is **thread‑safe**; concurrent calls to `ExecuteAsync<T>` share the same internal statistics and will correctly update the adaptive timeout without external synchronization.
- `GetCurrentTimeout` returns a snapshot; rapid successive calls may observe different values if other threads are completing operations concurrently.
- The first invocation uses the initial timeout (30 seconds) regardless of observed times; subsequent timeouts are influenced by the measured execution history.
- If all observed executions complete significantly faster than the current timeout, the timeout will gradually decrease, but it will never fall below a lower bound of `TimeSpan.FromTicks(1)` to prevent zero‑timeouts.
- Exceptions thrown by the supplied `operation` do **not** affect the adaptation statistics; only successful completions (or cancellations due to timeout) are considered for updating the mean and variance.
- The adaptation algorithm assumes a roughly stable distribution of execution times; sudden, persistent shifts may cause the timeout to lag behind the new behavior until enough samples are collected.
