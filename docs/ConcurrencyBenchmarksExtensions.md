# ConcurrencyBenchmarksExtensions

Provides extension methods for executing and analyzing concurrency-related benchmarks, including circuit breaker statistics, bulkhead utilization metrics, and stress test execution for resilience pipeline components.

## API

### ExecuteAllBenchmarks

```csharp
public static Dictionary<string, double> ExecuteAllBenchmarks(
    int iterations = 1000,
    int concurrencyLevel = Environment.ProcessorCount,
    CancellationToken cancellationToken = default)
```

Executes a comprehensive suite of concurrency benchmarks against the configured resilience pipeline.

**Parameters**
- `iterations`: Number of benchmark iterations to run per test case. Must be greater than zero.
- `concurrencyLevel`: Degree of parallelism for benchmark execution. Defaults to processor count.
- `cancellationToken`: Token to observe for cancellation requests.

**Returns**
A dictionary mapping benchmark names to their measured execution times in milliseconds.

**Exceptions**
- `ArgumentOutOfRangeException`: Thrown when `iterations` is less than or equal to zero, or `concurrencyLevel` is less than 1.
- `OperationCanceledException`: Thrown when `cancellationToken` is canceled during execution.
- `InvalidOperationException`: Thrown when the resilience pipeline has not been properly configured.

---

### GetCircuitBreakerStatistics

```csharp
public static (double SuccessRate, double FailureRate, long StateAccessCount) GetCircuitBreakerStatistics(
    TimeSpan observationWindow)
```

Retrieves statistical metrics for circuit breaker behavior over a specified observation window.

**Parameters**
- `observationWindow`: The time span over which to calculate statistics. Must be positive.

**Returns**
A tuple containing:
- `SuccessRate`: Ratio of successful calls to total calls (0.0 to 1.0).
- `FailureRate`: Ratio of failed calls to total calls (0.0 to 1.0).
- `StateAccessCount`: Total number of circuit breaker state transitions observed.

**Exceptions**
- `ArgumentOutOfRangeException`: Thrown when `observationWindow` is less than or equal to `TimeSpan.Zero`.
- `InvalidOperationException`: Thrown when no circuit breaker telemetry data is available.

---

### GetAverageBulkheadUtilization

```csharp
public static double GetAverageBulkheadUtilization(
    TimeSpan measurementPeriod,
    int sampleIntervalMs = 100)
```

Calculates the average bulkhead utilization percentage over a measurement period.

**Parameters**
- `measurementPeriod`: Duration over which to measure utilization. Must be positive.
- `sampleIntervalMs`: Interval between utilization samples in milliseconds. Must be greater than zero.

**Returns**
Average utilization as a percentage (0.0 to 100.0).

**Exceptions**
- `ArgumentOutOfRangeException`: Thrown when `measurementPeriod` is not positive or `sampleIntervalMs` is less than or equal to zero.
- `InvalidOperationException`: Thrown when bulkhead telemetry is not enabled or no samples were collected.

---

### RunStressTest

```csharp
public static (int SuccessCount, int FailureCount, TimeSpan ExecutionTime) RunStressTest(
    Func<Task> workload,
    int parallelTasks,
    TimeSpan duration,
    CancellationToken cancellationToken = default)
```

Executes a stress test by running a workload concurrently for a specified duration.

**Parameters**
- `workload`: Async delegate representing the operation to stress test. Must not be null.
- `parallelTasks`: Number of concurrent tasks to execute. Must be greater than zero.
- `duration`: Total time to run the stress test. Must be positive.
- `cancellationToken`: Token to observe for cancellation requests.

**Returns**
A tuple containing:
- `SuccessCount`: Number of workload executions that completed without exception.
- `FailureCount`: Number of workload executions that threw an exception.
- `ExecutionTime`: Actual elapsed time of the stress test run.

**Exceptions**
- `ArgumentNullException`: Thrown when `workload` is null.
- `ArgumentOutOfRangeException`: Thrown when `parallelTasks` is less than 1 or `duration` is not positive.
- `OperationCanceledException`: Thrown when `cancellationToken` is canceled before or during execution.

## Usage

### Comprehensive Benchmark Suite Execution

```csharp
using Microsoft.Extensions.Resilience;
using System.Diagnostics;

var pipeline = new ResiliencePipelineBuilder()
    .AddCircuitBreaker(new CircuitBreakerStrategyOptions
    {
        FailureRatio = 0.5,
        MinimumThroughput = 10,
        SamplingDuration = TimeSpan.FromSeconds(30)
    })
    .AddBulkhead(new BulkheadStrategyOptions
    {
        MaxParallelism = 50,
        MaxQueuedActions = 100
    })
    .Build();

var results = ConcurrencyBenchmarksExtensions.ExecuteAllBenchmarks(
    iterations: 5000,
    concurrencyLevel: 32);

foreach (var (benchmark, elapsedMs) in results)
{
    Console.WriteLine($"{benchmark}: {elapsedMs:F2} ms");
}
```

### Stress Testing with Circuit Breaker Analysis

```csharp
using Microsoft.Extensions.Resilience;

var circuitBreaker = new CircuitBreakerStrategyOptions
{
    FailureRatio = 0.3,
    MinimumThroughput = 20,
    BreakDuration = TimeSpan.FromSeconds(10)
};

var (successes, failures, elapsed) = ConcurrencyBenchmarksExtensions.RunStressTest(
    workload: async () =>
    {
        await pipeline.ExecuteAsync(async ct =>
        {
            await ExternalService.CallAsync(ct);
        });
    },
    parallelTasks: 100,
    duration: TimeSpan.FromMinutes(2));

var stats = ConcurrencyBenchmarksExtensions.GetCircuitBreakerStatistics(TimeSpan.FromMinutes(5));

Console.WriteLine($"Stress test completed in {elapsed.TotalSeconds:F1}s");
Console.WriteLine($"Success: {successes}, Failures: {failures}");
Console.WriteLine($"Circuit breaker - Success rate: {stats.SuccessRate:P2}, Failure rate: {stats.FailureRate:P2}");
Console.WriteLine($"State transitions: {stats.StateAccessCount}");
```

## Notes

- All methods are thread-safe and can be called concurrently from multiple threads. Internal synchronization uses lock-free primitives where possible to minimize benchmark interference.
- `ExecuteAllBenchmarks` and `RunStressTest` allocate significant memory during execution; consider running in a dedicated process for accurate memory profiling.
- `GetCircuitBreakerStatistics` and `GetAverageBulkheadUtilization` require telemetry to be enabled on the corresponding resilience strategies. If telemetry is disabled, these methods throw `InvalidOperationException`.
- The `observationWindow` in `GetCircuitBreakerStatistics` should align with the circuit breaker's `SamplingDuration` for meaningful results. Windows shorter than the sampling duration may return incomplete data.
- `RunStressTest` does not guarantee exact `duration` adherence; the actual `ExecutionTime` in the return tuple reflects the precise elapsed time, which may exceed the requested duration due to task completion latency.
- Cancellation tokens are checked at strategy boundaries. Long-running individual workload executions may not respond to cancellation immediately.
