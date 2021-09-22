# ResiliencePipelineBenchmarks

Provides a suite of benchmark methods for measuring the performance characteristics of resilience pipelines under various configurations including retry, circuit breaker, timeout, bulkhead, and fallback policies. The class is designed for use with benchmarking frameworks such as BenchmarkDotNet to evaluate throughput, latency, and resource utilization of resilience strategies.

## API

### `public void Setup()`
Initializes the benchmark environment by creating resilience pipelines with different policy configurations and preparing test dependencies. This method is typically invoked once per benchmark iteration by the benchmark runner.

**Parameters:** None  
**Returns:** `void`  
**Throws:** `InvalidOperationException` if pipeline construction fails due to invalid policy configuration.

---

### `public async Task ResiliencePipeline_Execute_Successful_Operation()`
Executes a single successful operation through a baseline resilience pipeline without any fault-handling policies to establish a performance baseline.

**Parameters:** None  
**Returns:** `Task` representing the asynchronous execution.  
**Throws:** `Exception` if the underlying operation fails unexpectedly.

---

### `public async Task ResiliencePipeline_Execute_With_Retry()`
Executes an operation through a pipeline configured with a retry policy, simulating transient failures that succeed on subsequent attempts.

**Parameters:** None  
**Returns:** `Task` representing the asynchronous execution.  
**Throws:** `Exception` if the operation exceeds the maximum retry attempts.

---

### `public async Task ResiliencePipeline_Execute_With_CircuitBreaker()`
Executes an operation through a pipeline configured with a circuit breaker policy, measuring overhead when the circuit is closed and tracking state transitions.

**Parameters:** None  
**Returns:** `Task` representing the asynchronous execution.  
**Throws:** `BrokenCircuitException` when the circuit is open and the operation is rejected.

---

### `public async Task ResiliencePipeline_Execute_With_Timeout()`
Executes an operation through a pipeline configured with a timeout policy, measuring the cost of timeout enforcement on both timely and delayed operations.

**Parameters:** None  
**Returns:** `Task` representing the asynchronous execution.  
**Throws:** `TimeoutRejectedException` when the operation exceeds the configured timeout duration.

---

### `public async Task ResiliencePipeline_Execute_With_Bulkhead()`
Executes an operation through a pipeline configured with a bulkhead policy, measuring concurrency limiting and queueing behavior under load.

**Parameters:** None  
**Returns:** `Task` representing the asynchronous execution.  
**Throws:** `BulkheadRejectedException` when the maximum concurrency or queue capacity is exceeded.

---

### `public async Task ResiliencePipeline_Execute_With_Fallback()`
Executes an operation through a pipeline configured with a fallback policy, measuring the overhead of fallback delegate invocation on failure.

**Parameters:** None  
**Returns:** `Task` representing the asynchronous execution.  
**Throws:** `Exception` if both the primary operation and fallback delegate fail.

---

### `public async Task ResiliencePipeline_Execute_Full_Pipeline()`
Executes an operation through a pipeline combining all resilience policies (retry, circuit breaker, timeout, bulkhead, fallback) to measure composite overhead.

**Parameters:** None  
**Returns:** `Task` representing the asynchronous execution.  
**Throws:** `Exception` for any unhandled failure after all policies are exhausted.

---

### `public PipelineStatistics ResiliencePipeline_Get_Statistics()`
Retrieves aggregated execution statistics from the configured pipeline including total executions, success/failure counts, and policy-specific metrics.

**Parameters:** None  
**Returns:** `PipelineStatistics` containing counters for executions, successes, failures, retries, circuit breaker state changes, timeouts, bulkhead rejections, and fallback invocations.  
**Throws:** `InvalidOperationException` if called before `Setup()` has been executed.

---

### `public async Task ResiliencePipeline_Execute_Multiple_Operations_Parallel()`
Executes multiple operations concurrently through the configured pipeline to measure throughput and contention under parallel workloads.

**Parameters:** None  
**Returns:** `Task` representing the asynchronous execution of all parallel operations.  
**Throws:** `AggregateException` containing exceptions from any failed operations.

## Usage

### Example 1: Running Benchmarks with BenchmarkDotNet

```csharp
using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;

var config = DefaultConfig.Instance
    .AddJob(Job.Default
        .WithIterationCount(10)
        .WithWarmupCount(3)
        .WithMinIterationTime(TimeSpan.FromMilliseconds(500)));

BenchmarkRunner.Run<ResiliencePipelineBenchmarks>(config);
```

### Example 2: Programmatic Benchmark Execution for Custom Reporting

```csharp
var benchmarks = new ResiliencePipelineBenchmarks();
benchmarks.Setup();

// Warm-up
await benchmarks.ResiliencePipeline_Execute_Successful_Operation();
await benchmarks.ResiliencePipeline_Execute_With_Retry();

// Measured runs
var stopwatch = Stopwatch.StartNew();
for (int i = 0; i < 10000; i++)
{
    await benchmarks.ResiliencePipeline_Execute_With_Retry();
}
stopwatch.Stop();

var stats = benchmarks.ResiliencePipeline_Get_Statistics();
Console.WriteLine($"Elapsed: {stopwatch.ElapsedMilliseconds}ms");
Console.WriteLine($"Total executions: {stats.TotalExecutions}");
Console.WriteLine($"Retries: {stats.RetryCount}");
Console.WriteLine($"Failures: {stats.FailureCount}");
```

## Notes

- **Thread Safety**: The benchmark instance is not thread-safe. Each benchmark method should be executed sequentially by the benchmark runner. Parallel execution of multiple benchmark methods on the same instance will corrupt internal state and statistics.
- **State Isolation**: `Setup()` must be called before any benchmark method. Calling benchmark methods without prior setup throws `InvalidOperationException`. Statistics accumulate across invocations; create a new instance for isolated measurements.
- **Async Context**: All async methods capture the synchronization context by default. In benchmark scenarios, configure `ConfigureAwait(false)` in the test harness to avoid context marshaling overhead skewing results.
- **Exception Handling**: Benchmark methods propagate exceptions from the resilience pipeline. The benchmark runner should be configured to treat exceptions as failed iterations rather than crashes.
- **Resource Cleanup**: Pipelines created in `Setup()` implement `IDisposable` internally. The benchmark class does not expose a cleanup method; rely on process termination or implement a custom teardown if running in long-lived test hosts.
- **Statistical Validity**: The `ResiliencePipeline_Execute_Multiple_Operations_Parallel` method uses a fixed concurrency level defined in `Setup()`. Results vary significantly with thread pool saturation; pin the thread pool minimum threads for reproducible measurements.
