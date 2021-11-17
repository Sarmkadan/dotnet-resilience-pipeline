# BulkheadBenchmarksExtensions

Static class that provides benchmark‑oriented extension methods for measuring and diagnosing the behavior of a `Bulkhead` instance.

## API

### TryAcquireAndRelease
**Purpose:** Attempts to acquire a permit from the bulkhead and immediately releases it, indicating whether the acquisition succeeded.  
**Parameters:** `this Bulkhead bulkhead` – the bulkhead to operate on.  
**Return value:** `true` if a permit was acquired and released; `false` if the bulkhead rejected the acquisition (e.g., at capacity).  
**Throws:**  
- `ArgumentNullException` if `bulkhead` is `null`.  
- `ObjectDisposedException` if the bulkhead has been disposed.

### RecordQueueWaitAndGetUtilization
**Purpose:** Measures the time a request spends waiting in the bulkhead's queue and returns the current utilization ratio (0.0 – 1.0).  
**Parameters:** `this Bulkhead bulkhead` – the bulkhead to measure.  
**Return value:** A `double` representing the utilization after recording the wait time.  
**Throws:**  
- `ArgumentNullException` if `bulkhead` is `null`.  
- `ObjectDisposedException` if the bulkhead has been disposed.

### GetPerformanceSummary
**Purpose:** Returns a formatted string summarizing key performance counters of the bulkhead (total acquisitions, rejections, average queue wait, etc.).  
**Parameters:** `this Bulkhead bulkhead` – the bulkhead to summarize.  
**Return value:** A `string` containing the summary.  
**Throws:**  
- `ArgumentNullException` if `bulkhead` is `null`.  
- `ObjectDisposedException` if the bulkhead has been disposed.

### IsOverloaded
**Purpose:** Determines whether the bulkhead is currently overloaded (i.e., its queue length exceeds the configured limit or all permits are taken).  
**Parameters:** `this Bulkhead bulkhead` – the bulkhead to evaluate.  
**Return value:** `true` if overloaded; otherwise `false`.  
**Throws:**  
- `ArgumentNullException` if `bulkhead` is `null`.  
- `ObjectDisposedException` if the bulkhead has been disposed.

## Usage

Example 1: Simple acquisition test.

```csharp
var bulkhead = new BulkheadOptions { MaxParallelism = 10, MaxQueuingActions = 5 }.ToBulkhead();
bool acquired = bulkhead.TryAcquireAndRelease();
Console.WriteLine($"Acquired: {acquired}");
```

Example 2: Monitoring utilization under load.

```csharp
var bulkhead = new BulkheadOptions { MaxParallelism = 3, MaxQueuingActions = 2 }.ToBulkhead();

// Simulate concurrent work that may queue
Task[] tasks = Enumerable.Range(0, 8).Select(i => Task.Run(() =>
{
    using (bulkhead.WaitAsync())   // acquire and hold a permit
    {
        Thread.Sleep(100);        // simulate work
    }
})).ToArray();

Task.WaitAll(tasks);

double utilization = bulkhead.RecordQueueWaitAndGetUtilization();
bool overloaded = bulkhead.IsOverloaded();

Console.WriteLine($"Utilization: {utilization:P2}, Overloaded: {overloaded}");
Console.WriteLine(bulkhead.GetPerformanceSummary());
```

## Notes

- All methods are extension methods; they require a non‑null, non‑disposed `Bulkhead` instance. Violating this precondition results in `ArgumentNullException` or `ObjectDisposedException`.
- The methods are thread‑safe with respect to the bulkhead’s internal state, but concurrent invocations (e.g., multiple threads calling `TryAcquireAndRelease`) will reflect the bulkhead’s current occupancy and may interleave.
- `RecordQueueWaitAndGetUtilization` internally measures wait time; excessive calls can add overhead and slightly affect the measured latency.
- `IsOverloaded` compares the current queue length to `MaxQueuingActions`. If the bulkhead is configured with an unbounded queue, it will always return `false`.
- The string returned by `GetPerformanceSummary` is intended for diagnostic logging; its exact format may change between versions, so avoid parsing it programmatically.
