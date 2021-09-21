# TimeoutBenchmarksExtensions

Provides static utility members for retrieving aggregated performance metrics from timeout resilience policies. These members expose execution-time statistics and success-rate data intended for benchmarking and diagnostics scenarios, allowing callers to inspect the observed behavior of a timeout policy without accessing internal instrumentation.

## API

### TimeoutPolicy_GetAverageExecutionTime

```csharp
public static double TimeoutPolicy_GetAverageExecutionTime(/* parameters not publicly documented */);
```

Returns the arithmetic mean of execution times recorded by the timeout policy, expressed in the unit native to the underlying instrumentation (typically milliseconds). The value is a double-precision floating-point number to preserve fractional precision when averaging over many samples.

**Return Value**  
`double` – the average execution time. Returns `0.0` or `NaN` if no executions have been recorded (behavior dependent on the internal accumulator).

**Exceptions**  
May throw `ArgumentNullException` if a required policy argument is null. May throw `InvalidOperationException` if the policy has been disposed or its metrics store is unavailable.

---

### TimeoutPolicy_GetMaxExecutionTime

```csharp
public static long TimeoutPolicy_GetMaxExecutionTime(/* parameters not publicly documented */);
```

Returns the maximum single execution time observed by the timeout policy, as a discrete `long` value. This represents the worst-case latency recorded across all measured invocations.

**Return Value**  
`long` – the maximum execution time. Returns `0` if no executions have been recorded.

**Exceptions**  
May throw `ArgumentNullException` if a required policy argument is null. May throw `InvalidOperationException` if the policy has been disposed or its metrics store is unavailable.

---

### TimeoutPolicy_GetMinExecutionTime

```csharp
public static long TimeoutPolicy_GetMinExecutionTime(/* parameters not publicly documented */);
```

Returns the minimum single execution time observed by the timeout policy, as a discrete `long` value. This represents the best-case latency recorded across all measured invocations.

**Return Value**  
`long` – the minimum execution time. Returns `long.MaxValue` or `0` if no executions have been recorded (behavior dependent on the internal initial sentinel).

**Exceptions**  
May throw `ArgumentNullException` if a required policy argument is null. May throw `InvalidOperationException` if the policy has been disposed or its metrics store is unavailable.

---

### TimeoutPolicy_GetSuccessRate

```csharp
public static double TimeoutPolicy_GetSuccessRate(/* parameters not publicly documented */);
```

Returns the ratio of successful executions to total executions as a `double` in the range `[0.0, 1.0]`. A success is defined as an execution that completed without triggering the timeout policy’s cancellation or rejection behavior.

**Return Value**  
`double` – the success rate. Returns `1.0` if no executions have been recorded (no failures observed), or `0.0` if every recorded execution has failed.

**Exceptions**  
May throw `ArgumentNullException` if a required policy argument is null. May throw `InvalidOperationException` if the policy has been disposed or its metrics store is unavailable.

## Usage

### Example 1: Logging aggregated timeout metrics after a batch of operations

```csharp
TimeoutPolicy policy = TimeoutPolicy.Create(TimeSpan.FromSeconds(5));
// Execute multiple guarded operations...
for (int i = 0; i < 100; i++)
{
    try
    {
        policy.Execute(() => SimulateExternalCall());
    }
    catch (TimeoutRejectedException)
    {
        // Handle timeout
    }
}

double avgMs = TimeoutBenchmarksExtensions.TimeoutPolicy_GetAverageExecutionTime(policy);
long maxMs = TimeoutBenchmarksExtensions.TimeoutPolicy_GetMaxExecutionTime(policy);
long minMs = TimeoutBenchmarksExtensions.TimeoutPolicy_GetMinExecutionTime(policy);
double success = TimeoutBenchmarksExtensions.TimeoutPolicy_GetSuccessRate(policy);

Console.WriteLine($"Avg: {avgMs:F2} ms, Min: {minMs} ms, Max: {maxMs} ms, Success: {success:P2}");
```

### Example 2: Comparing two timeout durations using benchmark metrics

```csharp
TimeoutPolicy aggressive = TimeoutPolicy.Create(TimeSpan.FromMilliseconds(200));
TimeoutPolicy relaxed = TimeoutPolicy.Create(TimeSpan.FromMilliseconds(800));

RunWorkload(aggressive, "Aggressive");
RunWorkload(relaxed, "Relaxed");

static void RunWorkload(TimeoutPolicy policy, string label)
{
    for (int i = 0; i < 50; i++)
    {
        try { policy.Execute(WorkSimulation); }
        catch (TimeoutRejectedException) { }
    }

    double avg = TimeoutBenchmarksExtensions.TimeoutPolicy_GetAverageExecutionTime(policy);
    double rate = TimeoutBenchmarksExtensions.TimeoutPolicy_GetSuccessRate(policy);
    Console.WriteLine($"{label} -> Avg: {avg:F2} ms, Success: {rate:P2}");
}
```

## Notes

- **Empty-metrics state**: When no executions have been recorded through the policy, `GetAverageExecutionTime` may return `NaN` or `0.0`, `GetMinExecutionTime` may return a sentinel such as `long.MaxValue` or `0`, and `GetSuccessRate` typically returns `1.0`. Callers should guard against these boundary values when rendering or comparing results.
- **Thread safety**: The underlying metrics accumulators are expected to use atomic or synchronized updates, making these members safe to call concurrently with ongoing policy execution. However, the values returned are point-in-time snapshots and may already be stale by the time they are read.
- **Disposed policies**: Accessing metrics on a policy that has been disposed is not supported and may throw `InvalidOperationException`. Callers should ensure the policy remains alive for the duration of metric collection.
- **Unit consistency**: All execution-time members return values in the same unit used by the policy’s internal stopwatch or timestamp mechanism (typically milliseconds). Mixing these values with externally measured times in different units will produce incorrect comparisons.
- **Success definition**: The success rate counts only executions that completed without the timeout policy intervening. Executions that failed due to user code exceptions but finished within the timeout window are typically counted as successful unless the policy is configured otherwise. Verify the policy’s behavior if a different definition of success is required.
