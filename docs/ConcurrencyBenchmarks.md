# ConcurrencyBenchmarks

The `ConcurrencyBenchmarks` type contains a set of benchmark methods designed to measure the concurrent behavior of the resilience policies provided by the `dotnet-resilience-pipeline` library. Each method isolates a specific policy interaction under concurrent load, allowing performance characteristics such as throughput, latency, and resource utilization to be observed and compared.

## API

### Setup
**Purpose**  
Initializes shared state and resources required by the benchmark methods (e.g., policy instances, counters, and synchronization primitives).

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if called after the benchmarks have already been executed.  
- `ObjectDisposedException` if the underlying resources have been disposed prior to invocation.

### CircuitBreaker_Concurrent_Success_Recording
**Purpose**  
Executes a number of successful operations through a `CircuitBreakerPolicy` concurrently and records the outcome for each invocation.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if `Setup` has not been called first.  
- `AggregateException` wrapping any policy‑specific exceptions that occur during execution.

### CircuitBreaker_Concurrent_Failure_Recording
**Purpose**  
Executes a number of failing operations through a `CircuitBreakerPolicy` concurrently and records how failures are handled (e.g., trips, open state transitions).

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if `Setup` has not been called first.  
- `AggregateException` wrapping any policy‑specific exceptions that occur during execution.

### CircuitBreaker_Concurrent_State_Access
**Purpose**  
Performs concurrent reads of the `CircuitBreakerPolicy` state (Closed, Open, Half‑Open) while other threads may be triggering state transitions.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if `Setup` has not been called first.  
- `AggregateException` if any thread observes an inconsistent state due to a race condition.

### RetryPolicy_Concurrent_Retry_Recording
**Purpose**  
Runs concurrent invocations that deliberately fail a configurable number of times before succeeding, measuring how the `RetryPolicy` records each retry attempt.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if `Setup` has not been called first.  
- `AggregateException` wrapping exceptions from the underlying operation after retries are exhausted.

### RetryPolicy_Concurrent_Delay_Calculation
**Purpose**  
Measures the overhead of computing delay intervals in a `RetryPolicy` when many threads request retries simultaneously.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if `Setup` has not been called first.  
- `AggregateException` if the delay calculation throws (e.g., due to overflow).

### TimeoutPolicy_Concurrent_Execution_Recording
**Purpose**  
Executes concurrent operations wrapped in a `TimeoutPolicy` that complete within the allotted time, recording successful executions.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if `Setup` has not been called first.  
- `AggregateException` wrapping any `TimeoutRejectedException` that erroneously occurs.

### TimeoutPolicy_Concurrent_Timeout_Recording
**Purpose**  
Executes concurrent operations that exceed the timeout threshold, recording how the `TimeoutPolicy` handles each timeout.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if `Setup` has not been called first.  
- `AggregateException` wrapping `TimeoutRejectedException` instances expected from the policy.

### BulkheadPolicy_Concurrent_Slot_Acquisition
**Purpose**  
Measures the contention and acquisition latency of slots in a `BulkheadPolicy` when many threads attempt to enter the bulkhead simultaneously.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if `Setup` has not been called first.  
- `AggregateException` wrapping `BulkheadRejectedException` when the bulkhead is saturated.

### BulkheadPolicy_Concurrent_Queue_Wait_Recording
**Purpose**  
Records the waiting time for threads that are queued when the bulkhead has no available slots, under concurrent load.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if `Setup` has not been called first.  
- `AggregateException` wrapping `BulkheadRejectedException` for threads that exceed the queue length.

### FallbackPolicy_Concurrent_Fallback_Recording
**Purpose**  
Executes concurrent operations that intentionally fail, capturing how often the `FallbackPolicy` invokes the fallback delegate.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if `Setup` has not been called first.  
- `AggregateException` wrapping exceptions from either the primary operation or the fallback.

### FallbackPolicy_Concurrent_Fallback_Check
**Purpose**  
Performs concurrent checks of whether a fallback would be invoked (without actually executing the fallback) to evaluate the decision‑making overhead.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if `Setup` has not been called first.  
- `AggregateException` if the fallback predicate throws.

### All_Policies_Concurrent_Mixed_Operations
**Purpose**  
Runs a mixed workload where each thread randomly selects and applies one of the available resilience policies, measuring overall throughput and policy interaction overhead.

**Parameters**  
None.

**Return value**  
`void`.

**Exceptions**  
- `InvalidOperationException` if `Setup` has not been called first.  
- `AggregateException` wrapping any exceptions propagated by the selected policies.

### CircuitBreaker_Get_CircuitBreakerTrips_Concurrent
**Purpose**  
Retrieves the total number of times the circuit breaker has transitioned to the open state during the concurrent benchmark runs.

**Parameters**  
None.

**Return value**  
`Int64` representing the cumulative trip count.

**Exceptions**  
- `InvalidOperationException` if called before any benchmark that uses the circuit breaker has been executed.  
- `ObjectDisposedException` if the underlying circuit breaker instance has been disposed.

### Bulkhead_Get_Utilization_Concurrent
**Purpose**  
Returns the average utilization percentage of the bulkhead slots observed across the concurrent benchmark executions.

**Parameters**  
None.

**Return value**  
`Double` ranging from 0.0 (no utilization) to 1.0 (full utilization).

**Exceptions**  
- `InvalidOperationException` if called before any bulkhead‑related benchmark has been executed.  
- `ObjectDisposedException` if the bulkhead instance has been disposed.

## Usage

```csharp
using System;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Benchmarks;

public class Program
{
    public static async Task Main()
    {
        var benchmarks = new ConcurrencyBenchmarks();
        benchmarks.Setup();

        // Measure how the circuit breaker records successful executions under load.
        benchmarks.CircuitBreaker_Concurrent_Success_Recording();

        // Retrieve the number of times the circuit breaker tripped during the test.
        long trips = benchmarks.CircuitBreaker_Get_CircuitBreakerTrips_Concurrent;
        Console.WriteLine($"Circuit breaker trips: {trips}");
    }
}
```

```csharp
using System;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Benchmarks;

public class BenchmarkDemo
{
    public static void Run()
    {
        var benchmarks = new ConcurrencyBenchmarks();
        benchmarks.Setup();

        // Execute a mixed policy workload to observe overall concurrency behavior.
        benchmarks.All_Policies_Concurrent_Mixed_Operations();

        // Check bulkhead utilization after the mixed run.
        double utilization = benchmarks.Bulkhead_Get_Utilization_Concurrent;
        Console.WriteLine($"Bulkhead utilization: {utilization:P2}");
    }
}
```

## Notes

- All benchmark methods assume that `Setup` has been invoked exactly once before any measurement calls. Re‑calling `Setup` after a benchmark run will result in an `InvalidOperationException` to prevent state corruption.
- The methods are **not** thread‑safe for concurrent invocation; they are intended to be called sequentially by a single orchestrating thread (typically the test harness). Internal synchronization protects the shared policy instances, but calling two benchmark methods at the same time may lead to undefined metrics.
- Exceptions wrapped in `AggregateException` reflect failures generated by the underlying resilience policies; they are propagated to allow the benchmark framework to record failure rates.
- The return‑value methods (`CircuitBreaker_Get_CircuitBreakerTrips_Concurrent` and `Bulkhead_Get_Utilization_Concurrent`) should only be called after the corresponding scenario benchmarks have completed; otherwise they throw `InvalidOperationException` because no meaningful data has been collected.
- Utilization values are calculated as the average ratio of acquired slots to total slots over the measurement window; transient spikes above 1.0 are not possible because the bulkhead enforces a hard limit.
