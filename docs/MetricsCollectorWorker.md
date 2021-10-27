# MetricsCollectorWorker

A background worker that periodically collects metrics, aggregates them, and provides queryable snapshots and reports for monitoring and analysis.

## API

### Constructor
```csharp
public MetricsCollectorWorker()
```
Initializes a new instance with default settings. The worker is not started automatically. No exceptions are thrown under normal conditions.

### CollectionInterval
```csharp
public TimeSpan CollectionInterval { get; set; }
```
Gets or sets the time span between successive metric collections. Setting the property to a value less than or equal to `TimeSpan.Zero` throws an `ArgumentOutOfRangeException`. Changing the interval while the worker is running does not affect the current cycle; the new value applies to the next cycle.

### IsRunning
```csharp
public bool IsRunning { get; }
```
Returns `true` if the worker's collection loop is active; otherwise `false`. This property is read‑only and does not throw exceptions.

### TotalCollections
```csharp
public int TotalCollections { get; }
```
Returns the number of completed collection cycles since the worker was started. The counter is reset when `Start` is called again after a stop. No exceptions are thrown.

### LastCollectionTime
```csharp
public DateTime LastCollectionTime { get; }
```
Gets the UTC timestamp of the most recent metric collection. If no collection has occurred yet, the value is `DateTime.MinValue`. This property is thread‑safe for concurrent reads.

### RecentMetrics
```csharp
public AggregatedMetrics RecentMetrics { get; }
```
Provides a snapshot of the most recently aggregated metrics. If no data has been collected, the returned instance contains default (zero) values. Accessing this property does not throw exceptions.

### Start
```csharp
public void Start()
```
Begins the metric collection loop using the current `CollectionInterval`. Throws an `InvalidOperationException` if the worker is already running or if `CollectionInterval` is not set to a positive value.

### StopAsync
```csharp
public async Task StopAsync()
```
Signals the worker to stop the collection loop and awaits completion of any in‑progress collection. Returns a `Task` that completes when the worker has fully stopped. Throws an `ObjectDisposedException` if the worker has been disposed, and an `InvalidOperationException` if called before `Start`.

### GetStatus
```csharp
public MetricsCollectorStatus GetStatus()
```
Returns a snapshot of the worker's current operating state, including `IsRunning`, `CollectionInterval`, `TotalCollections`, and `LastCollectionTime`. The method does not throw exceptions.

### GetMetricsForTimeRange
```csharp
public AggregatedMetrics GetMetricsForTimeRange(DateTime start, DateTime end)
```
Retrieves aggregated metrics for the half‑open interval `[start, end)`. Parameters must be UTC times; `start` must be earlier than `end`. Throws an `ArgumentException` if the range is invalid or exceeds the available data window.

### GetTrendAnalysis
```csharp
public MetricsTrend GetTrendAnalysis()
```
Computes trend information (e.g., moving averages, rate of change) over all collected metrics. Throws an `InvalidOperationException` if no metrics have been collected yet.

### GenerateReport
```csharp
public PerformanceReport GenerateReport()
```
Produces a comprehensive performance report based on the entirety of collected metrics. Throws an `InvalidOperationException` if the worker has never been started or if no metrics are available.

## Usage

### Example 1: Basic start‑stop workflow
```csharp
using System;
using System.Threading.Tasks;

var worker = new MetricsCollectorWorker
{
    CollectionInterval = TimeSpan.FromSeconds(30)
};

worker.Start();
// Simulate some work...
await Task.Delay(TimeSpan.FromMinutes(5));

await worker.StopAsync();

Console.WriteLine($"Collections: {worker.TotalCollections}");
Console.WriteLine($"Last run: {worker.LastCollectionTime}");
```

### Example 2: Querying metrics and generating a report
```csharp
using System;

var worker = new MetricsCollectorWorker { CollectionInterval = TimeSpan.FromMinutes(1) };
worker.Start();

// Allow collection to occur...
await Task.Delay(TimeSpan.FromMinutes(10));

var recent = worker.GetMetricsForTimeRange(
    DateTime.UtcNow.AddMinutes(-5),
    DateTime.UtcNow);

var trend = worker.GetTrendAnalysis();
var report = worker.GenerateReport();

Console.WriteLine($"Recent CPU avg: {recent.AverageCpu}");
Console.WriteLine($"Trend direction: {trend.Direction}");
Console.WriteLine(report.Summary);
```

## Notes
- The worker is not thread‑safe for concurrent calls to `Start` or `StopAsync`; external synchronization is required if multiple threads may invoke these methods.
- Reading properties (`IsRunning`, `TotalCollections`, `LastCollectionTime`, `RecentMetrics`) and calling `GetStatus` are safe to invoke from any thread after the worker has been started, as they return snapshots or atomic values.
- Modifying `CollectionInterval` while the worker is running does not interrupt the current collection cycle; the new interval takes effect after the ongoing cycle completes.
- If `StopAsync` is awaited, the method guarantees that no further collections will occur after the returned task completes.
- Calling `GetMetricsForTimeRange` with a range that extends beyond the retained data window results in an `ArgumentException`; the retention period is implementation‑specific and not exposed via the public API.
- `GetTrendAnalysis` and `GenerateReport` will throw `InvalidOperationException` if invoked before any collection has taken place; ensure the worker has run at least once before calling these methods.
