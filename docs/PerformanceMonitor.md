# PerformanceMonitor

`PerformanceMonitor` tracks execution metrics for resilience pipeline policies, recording durations, success/failure counts, and providing analytical methods to identify performance issues and compare execution patterns across different policies or time windows.

## API

### RecordExecution

```csharp
public void RecordExecution(long durationMs, bool success)
```

Records a single execution attempt with its duration in milliseconds and outcome. The duration is appended to the internal duration list, and the appropriate success or failure counter is incremented along with the total execution count and cumulative duration.

**Parameters:**
- `durationMs` — The execution time in milliseconds. Must be non-negative.
- `success` — `true` if the execution succeeded, `false` if it failed.

**Return value:** None.

**Throws:** `ArgumentOutOfRangeException` when `durationMs` is negative.

---

### GetMetrics

```csharp
public PerformanceMetrics GetMetrics()
```

Computes and returns a snapshot of the current performance metrics for this monitor instance. The returned object contains aggregated statistics derived from all recorded executions up to the point of the call.

**Return value:** A `PerformanceMetrics` instance populated with current aggregate data.

**Throws:** `InvalidOperationException` when no executions have been recorded.

---

### GetAllMetrics

```csharp
public List<PerformanceMetrics> GetAllMetrics()
```

Returns a list of `PerformanceMetrics` snapshots representing the monitor's state at different points in time. Each entry corresponds to a distinct sampling interval or checkpoint maintained internally.

**Return value:** A `List<PerformanceMetrics>` containing historical metric snapshots. The list may be empty if no snapshots exist.

**Throws:** None.

---

### IdentifyPerformanceIssues

```csharp
public List<PerformanceIssue> IdentifyPerformanceIssues()
```

Analyzes recorded execution data to detect performance anomalies. Returns a list of `PerformanceIssue` objects, each describing a detected issue with its type, severity, and associated policy name.

**Return value:** A `List<PerformanceIssue>` containing zero or more identified issues.

**Throws:** `InvalidOperationException` when insufficient data exists for meaningful analysis (fewer than two recorded executions).

---

### Clear

```csharp
public void Clear()
```

Resets all counters, duration lists, and internal snapshots to their initial empty state. After calling this method, the monitor behaves as if no executions have been recorded.

**Return value:** None.

**Throws:** None.

---

### ComparePerformance

```csharp
public List<PerformanceComparison> ComparePerformance()
```

Compares performance across different policies or time segments tracked by this monitor. Each `PerformanceComparison` entry pairs two `PerformanceMetrics` snapshots with a calculated percentage indicating how one compares to the other.

**Return value:** A `List<PerformanceComparison>` containing comparison results. May be empty if fewer than two comparable data sets exist.

**Throws:** None.

---

### PolicyName

```csharp
public string PolicyName { get; }
```

Gets the name of the resilience policy this monitor is associated with.

---

### TotalExecutions

```csharp
public long TotalExecutions { get; }
```

Gets the total number of executions recorded, both successful and failed.

---

### TotalDurationMs

```csharp
public long TotalDurationMs { get; }
```

Gets the cumulative sum of all recorded execution durations in milliseconds.

---

### SuccessfulExecutions

```csharp
public long SuccessfulExecutions { get; }
```

Gets the count of executions recorded as successful.

---

### FailedExecutions

```csharp
public long FailedExecutions { get; }
```

Gets the count of executions recorded as failed.

---

### AllDurations

```csharp
public List<long> AllDurations { get; }
```

Gets the list of all individual execution durations in milliseconds, in the order they were recorded.

---

### PerformanceMetrics.PolicyName

```csharp
public string PolicyName { get; }
```

The name of the policy from which these metrics were derived.

---

### PerformanceMetrics.AverageDurationMs

```csharp
public double AverageDurationMs { get; }
```

The arithmetic mean of all execution durations included in this metrics snapshot.

---

### PerformanceMetrics.FailureRate

```csharp
public double FailureRate { get; }
```

The proportion of failed executions relative to total executions, expressed as a value between 0.0 and 1.0.

---

### PerformanceIssue.PolicyName

```csharp
public string PolicyName { get; }
```

The name of the policy exhibiting the performance issue.

---

### PerformanceIssue.IssueType

```csharp
public string IssueType { get; }
```

A string categorizing the type of issue detected (e.g., `"HighLatency"`, `"DegradedThroughput"`).

---

### PerformanceIssue.Severity

```csharp
public string Severity { get; }
```

The severity level of the issue (e.g., `"Warning"`, `"Critical"`).

---

### PerformanceComparison.PolicyName

```csharp
public string PolicyName { get; }
```

The policy name associated with the baseline or reference metrics in the comparison.

---

### PerformanceComparison.AverageDurationMs

```csharp
public double AverageDurationMs { get; }
```

The average duration from the baseline metrics used in the comparison.

---

### PerformanceComparison.PercentageOfSlowest

```csharp
public double PercentageOfSlowest { get; }
```

The percentage representing how the compared metrics relate to the slowest observed performance baseline. A value of 100.0 indicates the compared entity is the slowest; lower values indicate proportionally better performance.

## Usage

### Example 1: Basic execution tracking and metrics retrieval

```csharp
var monitor = new PerformanceMonitor("RetryPolicy");

// Simulate recording several executions
monitor.RecordExecution(45, success: true);
monitor.RecordExecution(120, success: true);
monitor.RecordExecution(340, success: false);
monitor.RecordExecution(55, success: true);

var metrics = monitor.GetMetrics();
Console.WriteLine($"Policy: {metrics.PolicyName}");
Console.WriteLine($"Average duration: {metrics.AverageDurationMs:F1} ms");
Console.WriteLine($"Failure rate: {metrics.FailureRate:P1}");
Console.WriteLine($"Total executions: {monitor.TotalExecutions}");
```

### Example 2: Detecting issues and comparing performance

```csharp
var monitor = new PerformanceMonitor("CircuitBreakerPolicy");

// Record a mix of fast and slow executions
for (int i = 0; i < 50; i++)
    monitor.RecordExecution(20 + i % 10, success: true);
for (int i = 0; i < 5; i++)
    monitor.RecordExecution(800 + i * 100, success: false);

// Identify performance anomalies
var issues = monitor.IdentifyPerformanceIssues();
foreach (var issue in issues)
    Console.WriteLine($"[{issue.Severity}] {issue.IssueType} on {issue.PolicyName}");

// Compare performance snapshots
var comparisons = monitor.ComparePerformance();
foreach (var comp in comparisons)
    Console.WriteLine($"Compared to slowest: {comp.PercentageOfSlowest:F1}% (avg {comp.AverageDurationMs:F1} ms)");

// Reset for a new monitoring window
monitor.Clear();
```

## Notes

- **Thread safety:** `RecordExecution` and `Clear` mutate internal state and are not thread-safe by default. Concurrent calls from multiple threads must be externally synchronized. Read-only property accessors and analytical methods (`GetMetrics`, `IdentifyPerformanceIssues`, `ComparePerformance`) operate on snapshots and are safe to call concurrently with each other, but may observe inconsistent state if called concurrently with `RecordExecution` or `Clear` without synchronization.
- **Empty state handling:** `GetMetrics` and `IdentifyPerformanceIssues` throw `InvalidOperationException` when called before any executions are recorded. Always check `TotalExecutions > 0` or handle the exception when the monitor may be empty.
- **Duration values:** `RecordExecution` enforces non-negative durations. Zero-duration executions are permitted and contribute to averages and totals normally.
- **Snapshot semantics:** `GetAllMetrics` returns historical snapshots captured at discrete intervals, not a live view. The list contents do not change after `Clear` is called, but new snapshots accumulate as the monitor continues recording.
- **`ComparePerformance` output:** The `PercentageOfSlowest` value is relative to the slowest baseline within the comparison set. A value above 100.0 is not produced; the slowest entry always reports 100.0.
- **Memory considerations:** `AllDurations` retains every recorded duration until `Clear` is called. For long-lived monitors with high execution counts, periodic clearing or external aggregation is recommended to manage memory growth.
