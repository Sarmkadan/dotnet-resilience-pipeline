# PolicyComparisonBenchmarksExtensions

Provides extension methods for extracting standardized performance and health metrics from resilience policies during comparative benchmarking. These methods enable uniform measurement of retry delays, circuit breaker failure rates, bulkhead utilization, and overall policy consumption across different resilience strategies.

## API

### GetAverageRetryDelayMs

```csharp
public static double GetAverageRetryDelayMs(this RetryPolicy policy)
```

Returns the mean delay in milliseconds between retry attempts for a given retry policy. The value is computed from all recorded retry intervals during the policy's lifetime.

**Parameters:**
- `policy` — The `RetryPolicy` instance to measure.

**Returns:**
- `double` — The average retry delay in milliseconds. Returns `0.0` if no retries have occurred.

**Throws:**
- `ArgumentNullException` — if `policy` is `null`.

---

### GetCircuitBreakerFailureRate

```csharp
public static double GetCircuitBreakerFailureRate(this CircuitBreakerPolicy policy)
```

Calculates the ratio of failed operations to total operations passing through the circuit breaker, expressed as a value between `0.0` and `1.0`.

**Parameters:**
- `policy` — The `CircuitBreakerPolicy` instance to measure.

**Returns:**
- `double` — Failure rate as a fraction. Returns `0.0` if no operations have been recorded.

**Throws:**
- `ArgumentNullException` — if `policy` is `null`.

---

### GetBulkheadUtilizationMetrics

```csharp
public static Dictionary<string, double> GetBulkheadUtilizationMetrics(this BulkheadPolicy policy)
```

Produces a dictionary of utilization metrics for a bulkhead isolation policy, including current concurrency level, peak concurrency, and available capacity slots.

**Parameters:**
- `policy` — The `BulkheadPolicy` instance to measure.

**Returns:**
- `Dictionary<string, double>` — A map where keys are metric names (e.g., `"CurrentConcurrency"`, `"PeakConcurrency"`, `"AvailableSlots"`) and values are the corresponding numeric measurements.

**Throws:**
- `ArgumentNullException` — if `policy` is `null`.

---

### GetTotalPolicyUtilization

```csharp
public static double GetTotalPolicyUtilization(this ResiliencePipeline pipeline)
```

Aggregates utilization across all policies within a resilience pipeline into a single normalized score between `0.0` and `1.0`, where higher values indicate greater resource consumption or stress on the pipeline.

**Parameters:**
- `pipeline` — The `ResiliencePipeline` instance to measure.

**Returns:**
- `double` — Normalized utilization score.

**Throws:**
- `ArgumentNullException` — if `pipeline` is `null`.

## Usage

### Example 1: Benchmarking a Retry Policy with Circuit Breaker

```csharp
var retryPolicy = Policy
    .Handle<HttpRequestException>()
    .WaitAndRetryAsync(3, attempt => TimeSpan.FromMilliseconds(200 * attempt));

var circuitBreakerPolicy = Policy
    .Handle<HttpRequestException>()
    .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30));

// Execute some operations against the policies
for (int i = 0; i < 20; i++)
{
    try
    {
        await retryPolicy.ExecuteAsync(() => SimulateHttpCall());
    }
    catch { }
}

for (int i = 0; i < 20; i++)
{
    try
    {
        await circuitBreakerPolicy.ExecuteAsync(() => SimulateHttpCall());
    }
    catch { }
}

double avgRetryDelay = retryPolicy.GetAverageRetryDelayMs();
double failureRate = circuitBreakerPolicy.GetCircuitBreakerFailureRate();

Console.WriteLine($"Average retry delay: {avgRetryDelay:F2} ms");
Console.WriteLine($"Circuit breaker failure rate: {failureRate:P2}");
```

### Example 2: Measuring Bulkhead and Pipeline Utilization

```csharp
var bulkheadPolicy = Policy.BulkheadAsync(maxParallelization: 5, maxQueuingActions: 10);

var pipeline = new ResiliencePipelineBuilder()
    .AddRetry(new RetryStrategyOptions())
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions())
    .AddBulkhead(bulkheadPolicy)
    .Build();

// Simulate concurrent load
var tasks = Enumerable.Range(0, 8).Select(_ =>
    pipeline.ExecuteAsync(async token =>
    {
        await Task.Delay(100, token);
        return 42;
    }, CancellationToken.None)
);

await Task.WhenAll(tasks);

var bulkheadMetrics = bulkheadPolicy.GetBulkheadUtilizationMetrics();
double totalUtilization = pipeline.GetTotalPolicyUtilization();

Console.WriteLine("Bulkhead metrics:");
foreach (var kvp in bulkheadMetrics)
{
    Console.WriteLine($"  {kvp.Key}: {kvp.Value:F2}");
}
Console.WriteLine($"Total pipeline utilization: {totalUtilization:P2}");
```

## Notes

- All methods are designed for benchmarking and diagnostic purposes; they read from internal counters that are updated during policy execution and do not modify policy state.
- `GetAverageRetryDelayMs` returns `0.0` when no retries have been recorded, which may be indistinguishable from a policy that retries with zero delay. Callers should separately verify whether retries occurred if this distinction matters.
- `GetCircuitBreakerFailureRate` reflects the ratio at the moment of invocation. In the half-open state, the rate may temporarily decrease as successful trial operations are counted.
- `GetBulkheadUtilizationMetrics` returns a snapshot of instantaneous and peak values. The `"AvailableSlots"` metric reflects capacity at the exact time of the call and may change immediately afterward under concurrent load.
- `GetTotalPolicyUtilization` computes a composite score across all policies in the pipeline. The exact weighting formula is implementation-defined and may change across versions; rely on it for relative comparisons rather than absolute thresholds.
- These methods are not thread-safe for resetting or modifying underlying counters. Concurrent calls to the measured policies while reading metrics may produce values that reflect in-flight operations. For consistent snapshots, quiesce workload before measurement or treat results as approximate.
