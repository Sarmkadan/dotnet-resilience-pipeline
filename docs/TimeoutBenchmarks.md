# TimeoutBenchmarks

The `TimeoutBenchmarks` class provides performance measurement utilities for timeout policies in the `dotnet-resilience-pipeline` library. It tracks execution times, timeout occurrences, and statistical distributions to evaluate timeout policy behavior under various conditions.

## API

### `void Setup()`

Initializes the benchmarking infrastructure. This method must be called before any other public members to ensure proper state. Throws `InvalidOperationException` if called after any benchmarking operation has already been performed.

### `void TimeoutPolicy_RecordExecutionTime(long executionTimeTicks)`

Records an execution time measurement for timeout policy analysis. The value is expected in `Ticks` (1 tick = 100 nanoseconds).

- **executionTimeTicks**: The duration of the operation in ticks.
- Throws `ArgumentOutOfRangeException` if `executionTimeTicks` is negative.

### `void TimeoutPolicy_RecordTimeout()`

Increments the internal timeout counter. Call this method whenever a timeout policy triggers a timeout event.

### `bool TimeoutPolicy_IsTimedOut_Within(TimeSpan timeout)`

Determines whether a timeout event occurred within a specified timeout duration.

- **timeout**: The timeout threshold to evaluate against.
- Returns `true` if a timeout occurred within the given `timeout`; otherwise, `false`.
- Throws `ArgumentOutOfRangeException` if `timeout` is negative.

### `bool TimeoutPolicy_IsTimedOut_Exceeds(TimeSpan timeout)`

Determines whether a timeout event occurred that exceeded a specified timeout duration.

- **timeout**: The timeout threshold to evaluate against.
- Returns `true` if a timeout occurred that exceeded the given `timeout`; otherwise, `false`.
- Throws `ArgumentOutOfRangeException` if `timeout` is negative.

### `long TimeoutPolicy_GetPercentile95ExecutionTime()`

Calculates the 95th percentile execution time based on recorded measurements.

- Returns the 95th percentile execution time in ticks.
- Throws `InvalidOperationException` if no execution times have been recorded.

### `long TimeoutPolicy_GetPercentile99ExecutionTime()`

Calculates the 99th percentile execution time based on recorded measurements.

- Returns the 99th percentile execution time in ticks.
- Throws `InvalidOperationException` if no execution times have been recorded.

### `double TimeoutPolicy_GetTimeoutPercentage()`

Calculates the percentage of operations that resulted in a timeout.

- Returns a value between 0.0 and 100.0 representing the timeout rate.
- Throws `InvalidOperationException` if no timeout events have been recorded.

### `TimeSpan TimeoutPolicy_Get_Timeout()`

Gets the configured timeout duration for the policy being benchmarked.

- Returns the configured `TimeSpan` representing the timeout threshold.

### `long TimeoutPolicy_Get_TimeoutCount()`

Gets the total number of timeout events recorded.

- Returns the count of recorded timeout events.

## Usage

### Example 1: Basic Benchmarking
