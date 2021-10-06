# AdaptiveTimeoutPolicy

`AdaptiveTimeoutPolicy` is a resilience policy that dynamically adjusts the timeout duration applied to operations based on observed execution times and timeout occurrences. It maintains a sliding window of execution samples and uses a configurable target percentile and headroom factor to compute an adaptive timeout that balances responsiveness against unnecessary timeouts. The policy is designed for use within a resilience pipeline where operation latency characteristics may change over time.

## API

### Properties

- **`public TimeSpan InitialTimeout`**  
  The timeout value used before any adjustments have been made. This is the starting point for the adaptive algorithm.

- **`public TimeSpan MinTimeout`**  
  The lower bound for the adaptive timeout. The policy will never reduce the timeout below this value.

- **`public TimeSpan MaxTimeout`**  
  The upper bound for the adaptive timeout. The policy will never increase the timeout above this value.

- **`public TimeSpan CurrentTimeout`**  
  The current effective timeout value after the most recent adjustment. This value is used for subsequent operations.

- **`public double TargetPercentile`**  
  The percentile (0.0 to 1.0) of observed execution times that the policy aims to cover. For example, 0.95 means the timeout should be large enough to accommodate 95% of successful executions.

- **`public double HeadroomFactor`**  
  A multiplier applied to the computed percentile execution time to produce the new timeout. A value greater than 1.0 adds headroom to reduce the likelihood of timeouts.

- **`public int WindowSize`**  
  The maximum number of execution time samples retained in the sliding window. Older samples are discarded when the window is full.

- **`public int MinSampleSize`**  
  The minimum number of samples required before the policy performs an adjustment. If fewer samples are available, the timeout remains unchanged.

- **`public TimeSpan AdjustmentInterval`**  
  The minimum time that must elapse between successive adjustments. This prevents the timeout from changing too frequently.

- **`public int TotalAdjustments`**  
  The total number of times the timeout has been adjusted since the policy was created or last reset.

- **`public DateTime LastAdjustmentAt`**  
  The UTC timestamp of the most recent adjustment. `DateTime.MinValue` if no adjustment has occurred.

- **`public long TimeoutCount`**  
  The total number of timeouts recorded since the policy was created or last reset.

### Constructor

- **`public AdaptiveTimeoutPolicy(string name)`**  
  Initializes a new instance of the policy with the specified name. The name is passed to the base class constructor.  
  **Parameters:**  
  - `name` – A string identifier for the policy instance.  
  **Throws:**  
  - `ArgumentNullException` if `name` is `null`.  
  - `ArgumentException` if `name` is empty or consists only of whitespace.

### Methods

- **`public void RecordExecutionTime(TimeSpan executionTime)`**  
  Records the duration of a successful operation (one that did not time out). The execution time is added to the sliding window and may trigger an adjustment if the sample size and interval conditions are met.  
  **Parameters:**  
  - `executionTime` – The elapsed time of the operation.  
  **Throws:**  
  - `ArgumentOutOfRangeException` if `executionTime` is negative.

- **`public void RecordTimeout()`**  
  Records that a timeout occurred. Increments `TimeoutCount` and may also trigger an adjustment (typically by increasing the timeout).

- **`public double GetTimeoutPercentage()`**  
  Returns the percentage of operations that have timed out, calculated as `TimeoutCount / (TimeoutCount + number of recorded execution times)`.  
  **Returns:** A value between 0.0 and 1.0. Returns 0.0 if no operations have been recorded.

- **`public long GetPercentileExecutionTime(double percentile)`**  
  Computes the execution time (in ticks) at the specified percentile from the current sliding window of recorded execution times.  
  **Parameters:**  
  - `percentile` – A value between 0.0 and 1.0.  
  **Returns:** The execution time in ticks at the given percentile.  
  **Throws:**  
  - `ArgumentOutOfRangeException` if `percentile` is not in the range [0.0, 1.0].  
  - `InvalidOperationException` if the window contains fewer than `MinSampleSize` samples.

- **`public bool IsValidConfiguration()`**  
  Validates the current property values for consistency. Returns `true` if all constraints are satisfied:  
  - `InitialTimeout` is between `MinTimeout` and `MaxTimeout` (inclusive).  
  - `MinTimeout` ≤ `MaxTimeout`.  
  - `TargetPercentile` is between 0.0 and 1.0.  
  - `HeadroomFactor` ≥ 1.0.  
  - `WindowSize` ≥ `MinSampleSize` ≥ 1.  
  - `AdjustmentInterval` ≥ `TimeSpan.Zero`.  
  **Returns:** `true` if the configuration is valid; otherwise `false`.

- **`public override void ResetStatistics()`**  
  Resets all runtime statistics: `TotalAdjustments`, `LastAdjustmentAt`, `TimeoutCount`, and the sliding window of execution times. The `CurrentTimeout` is reset to `InitialTimeout`.

- **`public override PolicySnapshot GetSnapshot()`**  
  Returns a `PolicySnapshot` object containing a copy of the current state of the policy (including all properties and statistics). This snapshot can be used for diagnostics or monitoring without affecting the live policy.

## Usage

### Example 1: Basic configuration and operation

```csharp
using ResiliencePipeline;

var timeoutPolicy = new AdaptiveTimeoutPolicy("MyServiceTimeout")
{
    InitialTimeout = TimeSpan.FromSeconds(2),
    MinTimeout = TimeSpan.FromMilliseconds(500),
    MaxTimeout = TimeSpan.FromSeconds(10),
    TargetPercentile = 0.95,
    HeadroomFactor = 1.2,
    WindowSize = 100,
    MinSampleSize = 10,
    AdjustmentInterval = TimeSpan.FromSeconds(30)
};

// Simulate recording execution times
for (int i = 0; i < 50; i++)
{
    var elapsed = TimeSpan.FromMilliseconds(new Random().Next(100, 3000));
    if (elapsed < timeoutPolicy.CurrentTimeout)
        timeoutPolicy.RecordExecutionTime(elapsed);
    else
        timeoutPolicy.RecordTimeout();
}

Console.WriteLine($"Current timeout: {timeoutPolicy.CurrentTimeout.TotalMilliseconds} ms");
Console.WriteLine($"Timeout percentage: {timeoutPolicy.GetTimeoutPercentage():P1}");
```

### Example 2: Using the policy in a resilience pipeline with validation

```csharp
using ResiliencePipeline;

var policy = new AdaptiveTimeoutPolicy("DatabaseTimeout")
{
    InitialTimeout = TimeSpan.FromSeconds(1),
    MinTimeout = TimeSpan.FromSeconds(0.5),
    MaxTimeout = TimeSpan.FromSeconds(5),
    TargetPercentile = 0.9,
    HeadroomFactor = 1.5,
    WindowSize = 200,
    MinSampleSize = 20,
    AdjustmentInterval = TimeSpan.FromMinutes(1)
};

if (!policy.IsValidConfiguration())
{
    Console.Error.WriteLine("Invalid policy configuration.");
    return;
}

// In a real pipeline, the policy would be invoked automatically.
// Here we manually simulate a call and record the outcome.
var stopwatch = System.Diagnostics.Stopwatch.StartNew();
try
{
    // Simulate an operation that may time out
    await Task.Delay(TimeSpan.FromMilliseconds(800));
    stopwatch.Stop();
    policy.RecordExecutionTime(stopwatch.Elapsed);
}
catch (TimeoutException)
{
    policy.RecordTimeout();
}

var snapshot = policy.GetSnapshot();
Console.WriteLine($"Adjustments made: {snapshot.TotalAdjustments}");
```

## Notes

- **Thread safety:** This policy is **not thread-safe**. Concurrent calls to `RecordExecutionTime`, `RecordTimeout`, `ResetStatistics`, or property writes from multiple threads may corrupt internal state. External synchronization (e.g., a lock) is required when the same policy instance is used across threads.
- **Configuration validation:** Always call `IsValidConfiguration()` after setting properties and before using the policy. An invalid configuration may cause undefined behavior or exceptions during adjustment calculations.
- **Edge cases:**
  - If `MinSampleSize` is never reached, the policy will never adjust from `InitialTimeout`.
  - If all recorded execution times are zero (e.g., extremely fast operations), the computed percentile may be zero, and with `HeadroomFactor` applied the timeout could become zero. Ensure `MinTimeout` is set appropriately.
  - `GetPercentileExecutionTime` throws `InvalidOperationException` if fewer than `MinSampleSize` samples are available. Check the sample count or handle the exception.
  - `RecordExecutionTime` with a negative `executionTime` throws immediately; no sample is recorded.
  - The `LastAdjustmentAt` property is in UTC. Comparisons with `DateTime.UtcNow` are safe.
- **Snapshot isolation:** The `GetSnapshot` method returns a deep copy of the current state. Subsequent modifications to the policy do not affect the snapshot, and vice versa.
