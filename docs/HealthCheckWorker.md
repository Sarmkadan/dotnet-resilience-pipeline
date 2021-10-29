# HealthCheckWorker

`HealthCheckWorker` is a utility class designed to monitor the health of resilience pipelines by periodically executing health checks and aggregating their results. It tracks execution statistics, success rates, and thresholds for healthy and degraded states to provide a comprehensive view of pipeline health over time.

## API

### `public TimeSpan CheckInterval`
The interval between consecutive health checks. This determines how frequently the worker will execute its checks.

- **Type:** `TimeSpan`
- **Default:** Typically set to a reasonable default (e.g., 5 seconds) unless explicitly configured.
- **Usage:** Adjust this value to balance monitoring granularity and system load.

---

### `public double HealthyThreshold`
The minimum success rate (as a value between 0.0 and 1.0) required for the pipeline to be considered healthy. If the success rate meets or exceeds this threshold, the pipeline is marked as healthy.

- **Type:** `double`
- **Range:** `0.0 <= HealthyThreshold <= 1.0`
- **Default:** Typically set to a high value (e.g., 0.95) to ensure strict health criteria.
- **Usage:** Lower this value to relax health requirements; increase to tighten them.

---

### `public double DegradedThreshold`
The success rate threshold (as a value between 0.0 and 1.0) below which the pipeline is considered degraded. If the success rate falls below this threshold but is still above `HealthyThreshold`, the pipeline is marked as degraded.

- **Type:** `double`
- **Range:** `0.0 <= DegradedThreshold <= 1.0`
- **Default:** Typically set to a moderate value (e.g., 0.80) to distinguish between healthy and degraded states.
- **Usage:** Adjust this value to control the sensitivity of the degraded state detection.

---

### `public bool IsRunning`
Indicates whether the health check worker is currently running and actively performing checks.

- **Type:** `bool`
- **Read-only:** This property reflects the current operational state of the worker.
- **Usage:** Use this to determine if the worker is active before querying health status or other metrics.

---

### `public HealthCheckWorker()`
Constructs a new instance of `HealthCheckWorker` with default values for all properties.

- **Parameters:** None.
- **Initial State:** The worker is not running by default. Properties like `CheckInterval`, `HealthyThreshold`, and `DegradedThreshold` are initialized to sensible defaults.

---

### `public void Start()`
Begins executing health checks at the interval specified by `CheckInterval`. The worker will continue running until explicitly stopped via `StopAsync`.

- **Parameters:** None.
- **Behavior:** Starts a background task that periodically invokes health checks and updates internal state.
- **Thread Safety:** Safe to call from any thread. Multiple calls to `Start` have no additional effect after the first.
- **Exceptions:** Throws `InvalidOperationException` if the worker is already running.

---

### `public async Task StopAsync()`
Stops the health check worker gracefully, allowing any ongoing health check to complete before terminating.

- **Parameters:** None.
- **Behavior:** Asynchronously waits for the current health check (if any) to finish before stopping. Subsequent calls to `Start` can resume monitoring.
- **Thread Safety:** Safe to call from any thread. Multiple calls to `StopAsync` have no additional effect after the first.
- **Exceptions:** Throws `InvalidOperationException` if the worker is not running.

---
### `public HealthCheckStatus GetStatus()`
Retrieves the current health status of the pipeline based on the latest aggregated results.

- **Returns:** A `HealthCheckStatus` enum value indicating the current state (`Healthy`, `Degraded`, or `Unhealthy`).
- **Behavior:** The status is determined by comparing the latest `PipelineSuccessRate` against `HealthyThreshold` and `DegradedThreshold`.
- **Thread Safety:** Safe to call from any thread. The returned status reflects the state at the time of the call.

---
### `public DateTime LastCheckTime`
The timestamp of the most recent health check execution.

- **Type:** `DateTime`
- **Read-only:** This property reflects the time of the last completed check.
- **Usage:** Use this to determine how stale the health data is or to log check execution times.

---
### `public double PipelineSuccessRate`
The success rate of the resilience pipeline over the monitoring window, expressed as a value between 0.0 and 1.0.

- **Type:** `double`
- **Range:** `0.0 <= PipelineSuccessRate <= 1.0`
- **Read-only:** This value is updated after each health check execution.
- **Usage:** Monitor this value to track trends in pipeline performance over time.

---
### `public string OverallHealth`
A human-readable summary of the pipeline's health status based on the latest checks.

- **Type:** `string`
- **Read-only:** This value is derived from `GetStatus()` and formatted for display.
- **Possible Values:** `"Healthy"`, `"Degraded"`, or `"Unhealthy"`.
- **Usage:** Use this for logging or dashboard display purposes where a textual representation is preferred.

---
### `public int TotalPolicies`
The total number of resilience policies being monitored by this worker.

- **Type:** `int`
- **Read-only:** This value is set during construction or configuration and does not change during runtime.
- **Usage:** Use this to understand the scope of the health monitoring.

---
### `public long TotalExecutions`
The cumulative count of all health check executions performed by this worker.

- **Type:** `long`
- **Read-only:** This value increments with each completed health check.
- **Usage:** Monitor this to track the volume of checks over time or to detect anomalies in check frequency.

## Usage

### Example 1: Basic Monitoring Setup
