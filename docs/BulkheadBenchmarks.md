# BulkheadBenchmarks

The `BulkheadBenchmarks` class provides a set of benchmark-oriented methods for measuring the performance and behavior of a bulkhead policy implementation. It is intended to be used with a benchmarking framework (such as BenchmarkDotNet) to evaluate throughput, slot acquisition, queue wait time recording, and utilization metrics. The class exposes methods that simulate common bulkhead operations and return key performance indicators.

## API

### `public void Setup()`
Initializes the internal state of the bulkhead policy before any benchmark runs. This method must be called once before invoking any other members. It sets up the maximum parallelization, queue length, and resets all counters (active executions, queued items, rejections, etc.).  
**Parameters:** None.  
**Returns:** Nothing.  
**Throws:** `InvalidOperationException` if called more than once without a reset.

### `public bool BulkheadPolicy_TryAcquireSlot_Available()`
Attempts to acquire a slot in the bulkhead. This simulates a request entering the policy.  
**Parameters:** None.  
**Returns:** `true` if a slot was successfully acquired (i.e., the number of active executions is below the maximum parallelization); otherwise `false`.  
**Throws:** `InvalidOperationException` if `Setup()` has not been called.

### `public void BulkheadPolicy_ReleaseSlot()`
Releases a previously acquired slot, decrementing the active execution count.  
**Parameters:** None.  
**Returns:** Nothing.  
**Throws:** `InvalidOperationException` if no slot is currently held (i.e., active executions is zero).

### `public void BulkheadPolicy_RecordQueueWaitTime()`
Records a simulated queue wait time event. This increments the internal count of queued items and may update the queued percentage metric.  
**Parameters:** None.  
**Returns:** Nothing.  
**Throws:** `InvalidOperationException` if `Setup()` has not been called.

### `public double BulkheadPolicy_GetUtilizationPercentage()`
Returns the current utilization percentage of the bulkhead, calculated as the ratio of active executions to the maximum parallelization.  
**Parameters:** None.  
**Returns:** A `double` between 0.0 and 100.0 representing the utilization percentage.  
**Throws:** `InvalidOperationException` if `Setup()` has not been called.

### `public double BulkheadPolicy_GetQueuedPercentage()`
Returns the current queued percentage, representing the proportion of queued items relative to the maximum queue length.  
**Parameters:** None.  
**Returns:** A `double` between 0.0 and 100.0 representing the queued percentage.  
**Throws:** `InvalidOperationException` if `Setup()` has not been called.

### `public double BulkheadPolicy_GetRejectionPercentage()`
Returns the rejection percentage, calculated as the number of rejected attempts (when `TryAcquireSlot` returned `false`) divided by the total number of acquisition attempts.  
**Parameters:** None.  
**Returns:** A `double` between 0.0 and 100.0 representing the rejection percentage.  
**Throws:** `InvalidOperationException` if `Setup()` has not been called.

### `public int BulkheadPolicy_Get_MaxParallelization()`
Returns the configured maximum number of parallel executions allowed by the bulkhead.  
**Parameters:** None.  
**Returns:** An `int` representing the maximum parallelization.  
**Throws:** `InvalidOperationException` if `Setup()` has not been called.

### `public int BulkheadPolicy_Get_MaxQueueLength()`
Returns the configured maximum queue length for waiting requests.  
**Parameters:** None.  
**Returns:** An `int` representing the maximum queue length.  
**Throws:** `InvalidOperationException` if `Setup()` has not been called.

### `public int BulkheadPolicy_Get_ActiveExecutions()`
Returns the current number of active (in-flight) executions.  
**Parameters:** None.  
**Returns:** An `int` representing the active execution count.  
**Throws:** `InvalidOperationException` if `Setup()` has not been called.

## Usage

### Example 1: Basic benchmark run
```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;

public class BulkheadBenchmarkHarness
{
    private readonly BulkheadBenchmarks _benchmarks = new();

    [GlobalSetup]
    public void Setup() => _benchmarks.Setup();

    [Benchmark]
    public bool TryAcquire() => _benchmarks.BulkheadPolicy_TryAcquireSlot_Available();

    [Benchmark]
    public void Release() => _benchmarks.BulkheadPolicy_ReleaseSlot();

    [Benchmark]
    public double Utilization() => _benchmarks.BulkheadPolicy_GetUtilizationPercentage();
}

public class Program
{
    public static void Main() => BenchmarkRunner.Run<BulkheadBenchmarkHarness>();
}
```

### Example 2: Simulating a mixed workload
```csharp
public void SimulateWorkload()
{
    var bench = new BulkheadBenchmarks();
    bench.Setup();

    // Acquire two slots
    bool slot1 = bench.BulkheadPolicy_TryAcquireSlot_Available(); // true
    bool slot2 = bench.BulkheadPolicy_TryAcquireSlot_Available(); // true

    // Record a queue wait time
    bench.BulkheadPolicy_RecordQueueWaitTime();

    // Check metrics
    double utilization = bench.BulkheadPolicy_GetUtilizationPercentage();
    double queued = bench.BulkheadPolicy_GetQueuedPercentage();
    int active = bench.BulkheadPolicy_Get_ActiveExecutions(); // 2

    // Release one slot
    bench.BulkheadPolicy_ReleaseSlot();
    active = bench.BulkheadPolicy_Get_ActiveExecutions(); // 1
}
```

## Notes

- **Thread safety:** The `BulkheadBenchmarks` class is **not thread-safe**. It is designed for single-threaded benchmarking scenarios. Concurrent calls from multiple threads may produce inconsistent state and undefined behavior.
- **Setup requirement:** All methods except `Setup()` throw `InvalidOperationException` if `Setup()` has not been called first. Calling `Setup()` more than once without an intervening reset also throws.
- **Slot management:** `BulkheadPolicy_ReleaseSlot()` must only be called after a successful `BulkheadPolicy_TryAcquireSlot_Available()` call. Releasing a slot when no slot is held throws an exception.
- **Edge cases:** When `MaxParallelization` is zero, `TryAcquireSlot_Available` always returns `false`, and `GetUtilizationPercentage` returns 0.0 (division by zero is avoided). When `MaxQueueLength` is zero, `RecordQueueWaitTime` does not increment the queue count, and `GetQueuedPercentage` returns 0.0.
- **Benchmarking context:** The class is intended for use with benchmarking frameworks. The returned percentages are calculated from internal counters that are reset only by `Setup()`. Long-running benchmarks may need to reset state between iterations to avoid cumulative effects.
