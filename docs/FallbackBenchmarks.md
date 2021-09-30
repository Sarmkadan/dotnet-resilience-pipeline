# FallbackBenchmarks

`FallbackBenchmarks` is a benchmarking harness for testing the behavior and performance of fallback policies in resilience pipelines. It measures metrics such as fallback invocation counts, success rates, timeouts, and conditional fallback triggers under various execution scenarios. This type is intended for internal use in validating fallback policy implementations and comparing their runtime characteristics.

## API

### `void Setup()`

Initializes the benchmark environment before each test run. This method sets up required dependencies, resets internal counters, and configures default fallback policy settings. It is called automatically by the benchmarking framework prior to each benchmark invocation.

- **Parameters:** None
- **Return Value:** None
- **Throws:** May throw if initialization fails due to missing services or configuration errors.

---

### `void FallbackPolicy_RecordSuccessfulFallback()`

Simulates a successful fallback execution and records the outcome in internal metrics. This method is used to benchmark how quickly and reliably fallback policies log successful fallback events.

- **Parameters:** None
- **Return Value:** None
- **Throws:** May throw if the underlying metrics system is unavailable.

---

### `void FallbackPolicy_RecordFailedFallback()`

Simulates a failed fallback execution and records the outcome in internal metrics. This method is used to benchmark error handling and metric tracking during fallback failures.

- **Parameters:** None
- **Return Value:** None
- **Throws:** May throw if the underlying metrics system is unavailable.

---
### `bool FallbackPolicy_ShouldTriggerFallback_Any()`

Evaluates whether a fallback should be triggered based on a generic condition (e.g., any exception type). Returns `true` if fallback is warranted, otherwise `false`.

- **Parameters:** None
- **Return Value:** `true` if fallback should be triggered; `false` otherwise.
- **Throws:** None

---
### `bool FallbackPolicy_ShouldTriggerFallback_Specific()`

Evaluates whether a fallback should be triggered based on a specific exception type (e.g., `TimeoutException`). Returns `true` only when the exact condition is met.

- **Parameters:** None
- **Return Value:** `true` if the specific condition for fallback is satisfied; `false` otherwise.
- **Throws:** None

---
### `double FallbackPolicy_GetFallbackSuccessRate()`

Computes the ratio of successful fallback invocations to total fallback invocations over the current benchmark run. The value is a double between 0.0 and 1.0.

- **Parameters:** None
- **Return Value:** A `double` representing the success rate of fallback operations.
- **Throws:** None

---
### `double FallbackPolicy_GetFallbackInvocationPercentage()`

Calculates the percentage of total operations that resulted in a fallback being invoked. This is derived from internal invocation counters and may reflect both successful and failed fallback attempts.

- **Parameters:** None
- **Return Value:** A `double` between 0.0 and 100.0 representing the fallback invocation rate.
- **Throws:** None

---
### `TimeSpan FallbackPolicy_Get_FallbackTimeout()`

Returns the configured timeout duration for fallback operations. This value determines how long the system waits before aborting a fallback attempt.

- **Parameters:** None
- **Return Value:** A `TimeSpan` representing the fallback timeout.
- **Throws:** None

---
### `long FallbackPolicy_Get_FallbackInvocationCount()`

Returns the total number of fallback invocations recorded during the current benchmark run, including both successful and failed attempts.

- **Parameters:** None
- **Return Value:** A `long` representing the total fallback invocation count.
- **Throws:** None

---

## Usage

### Example 1: Basic Fallback Metrics Monitoring
