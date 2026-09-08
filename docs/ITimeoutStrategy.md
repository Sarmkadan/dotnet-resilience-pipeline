# ITimeoutStrategy
The `ITimeoutStrategy` interface defines the policy contract used to select an operation timeout, validate timeout configuration, and track execution outcomes. It allows `TimeoutService` to execute operations with either a fixed or adaptive timeout without depending on a concrete policy type.

## API
### Properties
* `string Id { get; set; }`: Gets or sets the unique identifier for the strategy instance.
* `string Name { get; set; }`: Gets or sets the strategy's friendly name.
* `bool IsEnabled { get; set; }`: Gets or sets whether the strategy is enabled. `TimeoutService` bypasses timeout enforcement and tracking when this is `false`.
* `DateTime CreatedAt { get; }`: Gets the UTC timestamp at which the strategy was created.
* `DateTime ModifiedAt { get; set; }`: Gets or sets the UTC timestamp of the strategy's last modification.
* `long TotalExecutions { get; }`: Gets the total number of successful and failed executions recorded by the strategy.
* `long SuccessfulExecutions { get; }`: Gets the number of successful executions recorded by the strategy.
* `long FailedExecutions { get; }`: Gets the number of failed executions recorded by the strategy, including recorded timeouts.

### Methods
* `TimeSpan GetTimeout()`: Returns the timeout to apply to the next execution.
* `void RecordExecutionTime(long executionTimeMs)`: Records an observed execution duration in milliseconds for statistics and, where supported, timeout adaptation.
* `void RecordSuccess()`: Records a successful execution and updates the execution counters.
* `void RecordFailure()`: Records a failed execution and updates the execution counters.
* `void RecordTimeout(long executionTimeMs)`: Records a timeout and the elapsed execution time in milliseconds. The supplied implementations count a timeout as a failed execution.
* `double GetTimeoutPercentage()`: Returns timed-out executions as a percentage of total recorded executions, or `0` when no executions have been recorded.
* `double GetSuccessRate()`: Returns successful executions as a percentage from `0` to `100`, or `0` when no executions have been recorded.
* `bool IsValidConfiguration(out string? error)`: Validates the strategy configuration. Returns `true` with `error` set to `null` when valid; otherwise returns `false` with a description of the invalid configuration.
* `void ResetStatistics()`: Clears execution statistics. Implementations may also reset their strategy-specific state.

## Implementations
### TimeoutPolicy
`TimeoutPolicy` provides a fixed timeout through its `Timeout` property, which defaults to 10 seconds. `GetTimeout()` returns that value. It records execution durations to calculate average, shortest, longest, P95, and P99 timing statistics, and tracks the number and percentage of timeouts. Its configuration is valid when `Timeout` is greater than zero, and resetting statistics clears all recorded timing and timeout data.

### AdaptiveTimeoutPolicy
`AdaptiveTimeoutPolicy` begins with `InitialTimeout` and exposes `CurrentTimeout` through `GetTimeout()`. It retains a sliding window of recent execution durations and, after `MinSampleSize` observations and the configured `AdjustmentInterval`, derives a timeout from `TargetPercentile`, applies `HeadroomFactor`, and clamps the result between `MinTimeout` and `MaxTimeout`. It also tracks timeout and adjustment statistics. Resetting statistics clears the observation window and restores `CurrentTimeout` to `InitialTimeout`.

## TimeoutService
`TimeoutService.ExecuteAsync<T>` accepts an `ITimeoutStrategy`, validates it, and bypasses timeout enforcement when the strategy is disabled. For an enabled strategy, it obtains the effective duration from `GetTimeout()` and links a timeout cancellation token with the caller's cancellation token.

After a successful operation, the service records its elapsed time and calls `RecordSuccess()`. For a non-cancellation exception, it records elapsed time and calls `RecordFailure()`. When the strategy's timeout expires, it calls `RecordTimeout()` and throws `OperationTimeoutException`; caller-requested cancellation is rethrown without being recorded as a timeout. `GetTimeoutMilliseconds()` also reads `GetTimeout()`, while `HasExceededTimeout()` performs its comparison only when the strategy is a `TimeoutPolicy`.

## Usage
```csharp
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

ITimeoutStrategy strategy = new TimeoutPolicy("remote-api")
{
    Timeout = TimeSpan.FromSeconds(2)
};

var timeoutService = new TimeoutService();
string result = await timeoutService.ExecuteAsync(
    strategy,
    async cancellationToken =>
    {
        await Task.Delay(50, cancellationToken);
        return "completed";
    });
```

## Notes
* Both supplied implementations derive the interface's identity, state, and execution counters from `ResiliencyPolicy`.
* `RecordTimeout()` already records a failure in both supplied implementations; consumers should not call `RecordFailure()` again for the same timeout.
* Execution times passed to `RecordExecutionTime()` and `RecordTimeout()` are measured in milliseconds.
