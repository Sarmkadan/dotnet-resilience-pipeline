# HealthCheckWorkerExtensions

`HealthCheckWorkerExtensions` provides a set of static convenience methods that operate on or query the state of a `HealthCheckWorker`. These extension-like utilities centralize common health-check operations—creating a preconfigured worker, interpreting its current health status, waiting for a stable state, and retrieving human-readable diagnostic strings—so that consumers do not need to interact with internal state flags directly.

## API

### CreateConfigured

```csharp
public static HealthCheckWorker CreateConfigured(/* configuration parameters */)
```

Creates and returns a fully initialized `HealthCheckWorker` instance based on the supplied configuration. The exact parameter list is determined by the underlying `HealthCheckWorker` constructor overloads that this method wraps. The returned worker is ready for immediate use; no additional setup is required.

- **Returns:** A new `HealthCheckWorker` instance.
- **Throws:** `ArgumentNullException` if a required configuration argument is `null`; `ArgumentException` if any configuration value is invalid (e.g., negative timeouts).

### IsHealthy

```csharp
public static bool IsHealthy(HealthCheckWorker worker)
```

Determines whether the specified worker is currently in a *Healthy* state.

- **Parameters:**
  - `worker` — the `HealthCheckWorker` to inspect.
- **Returns:** `true` if the worker’s status is Healthy; otherwise `false`.
- **Throws:** `ArgumentNullException` if `worker` is `null`.

### IsDegraded

```csharp
public static bool IsDegraded(HealthCheckWorker worker)
```

Determines whether the specified worker is currently in a *Degraded* state.

- **Parameters:**
  - `worker` — the `HealthCheckWorker` to inspect.
- **Returns:** `true` if the worker’s status is Degraded; otherwise `false`.
- **Throws:** `ArgumentNullException` if `worker` is `null`.

### IsUnhealthy

```csharp
public static bool IsUnhealthy(HealthCheckWorker worker)
```

Determines whether the specified worker is currently in an *Unhealthy* state.

- **Parameters:**
  - `worker` — the `HealthCheckWorker` to inspect.
- **Returns:** `true` if the worker’s status is Unhealthy; otherwise `false`.
- **Throws:** `ArgumentNullException` if `worker` is `null`.

### GetHealthStatusString

```csharp
public static string GetHealthStatusString(HealthCheckWorker worker)
```

Returns a string representation of the worker’s current health status (e.g., `"Healthy"`, `"Degraded"`, `"Unhealthy"`). This is intended for logging, diagnostics, and user-facing dashboards.

- **Parameters:**
  - `worker` — the `HealthCheckWorker` to query.
- **Returns:** A non-null, non-empty string describing the health status.
- **Throws:** `ArgumentNullException` if `worker` is `null`.

### WaitForStableStateAsync

```csharp
public static async Task<bool> WaitForStableStateAsync(
    HealthCheckWorker worker,
    CancellationToken cancellationToken = default)
```

Asynchronously waits until the worker reaches a stable state (Healthy or Unhealthy) and is no longer in a transitional phase (e.g., Degraded or initializing). The method blocks the calling task until stability is achieved, the operation is cancelled, or an internal timeout expires.

- **Parameters:**
  - `worker` — the `HealthCheckWorker` to monitor.
  - `cancellationToken` — optional token to cancel the wait.
- **Returns:** `true` if the worker reached a stable state; `false` if the wait was cancelled or timed out before stability was achieved.
- **Throws:** `ArgumentNullException` if `worker` is `null`; `OperationCanceledException` if the cancellation token is signaled.

### GetStatisticsString

```csharp
public static string GetStatisticsString(HealthCheckWorker worker)
```

Produces a formatted string containing key statistics gathered by the worker, such as execution counts, success/failure ratios, and latency summaries. The exact content depends on the metrics tracked internally by `HealthCheckWorker`.

- **Parameters:**
  - `worker` — the `HealthCheckWorker` to query.
- **Returns:** A non-null string with statistical information; may be empty if no data has been collected yet.
- **Throws:** `ArgumentNullException` if `worker` is `null`.

## Usage

### Example 1: Creating a worker and checking its status

```csharp
var worker = HealthCheckWorkerExtensions.CreateConfigured(
    healthCheckFunc: async ct => { /* perform check */ },
    threshold: TimeSpan.FromSeconds(5));

if (HealthCheckWorkerExtensions.IsHealthy(worker))
{
    Console.WriteLine("Worker is healthy.");
}
else
{
    Console.WriteLine(
        $"Worker status: {HealthCheckWorkerExtensions.GetHealthStatusString(worker)}");
    Console.WriteLine(
        $"Stats: {HealthCheckWorkerExtensions.GetStatisticsString(worker)}");
}
```

### Example 2: Waiting for stability before proceeding

```csharp
var worker = HealthCheckWorkerExtensions.CreateConfigured(/* ... */);

using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(30));
bool becameStable = await HealthCheckWorkerExtensions.WaitForStableStateAsync(
    worker, cts.Token);

if (becameStable && HealthCheckWorkerExtensions.IsHealthy(worker))
{
    // Proceed with critical operation that requires a healthy dependency.
    await PerformBusinessLogicAsync();
}
else
{
    // Escalate or fall back to a degraded path.
    await HandleUnavailableDependencyAsync();
}
```

## Notes

- All methods that accept a `HealthCheckWorker` instance throw `ArgumentNullException` when the argument is `null`. Callers should guard against this at the boundary where the worker is obtained.
- `IsHealthy`, `IsDegraded`, and `IsUnhealthy` are mutually exclusive at any given instant; exactly one returns `true` for a properly initialized worker. However, during state transitions there may be a brief window where the internal status is indeterminate—consumers should prefer `WaitForStableStateAsync` when a definitive state is required.
- `WaitForStableStateAsync` may internally enforce a timeout even if no cancellation token is provided (or if the token never fires). The exact timeout is implementation-defined and should be verified against the current version’s defaults.
- The string returned by `GetStatisticsString` is not guaranteed to have a fixed format across versions; it is intended for human consumption, not for machine parsing.
- These methods are static and do not maintain mutable shared state themselves. Thread safety therefore depends entirely on the thread safety of the `HealthCheckWorker` instance passed to them. If the same worker is used concurrently, its own synchronization mechanisms apply.
