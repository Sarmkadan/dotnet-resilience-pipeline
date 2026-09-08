# ExecutionHistoryRepository

`ExecutionHistoryRepository` is an in-memory store for `ExecutionRecord` instances. It supports recording executions, querying recent history, and calculating aggregate success and latency metrics. Its contents are process-local and are not persisted across application restarts.

The type is defined in `src/Data/ExecutionHistoryRepository.cs` in the `DotNetResiliencePipeline.Data` namespace.

## Construction and retention

```csharp
var history = new ExecutionHistoryRepository();                    // 60 minutes
var shortLivedHistory = new ExecutionHistoryRepository(15);        // 15 minutes
```

The constructor accepts `maxRetentionMinutes`, which defaults to `60`. The value is used to calculate a cutoff of `DateTime.UtcNow.AddMinutes(-maxRetentionMinutes)`; the constructor does not validate it.

Retention works in two ways:

- `DeleteOldRecords()` immediately removes records whose `ExecutedAt` value is strictly earlier than the cutoff and returns the number removed. A record exactly at the cutoff is retained.
- `Record(...)` checks whether at least five minutes have elapsed since the repository's last automatic cleanup. If so, it calls `DeleteOldRecords()` after adding the new record and then updates the cleanup timestamp.

Automatic cleanup is therefore opportunistic: it runs only while records are being added, not on a timer or during reads. Records older than the retention window can remain available until a qualifying `Record` call or an explicit `DeleteOldRecords()` call. Calling `DeleteOldRecords()` directly does not reset the automatic-cleanup timestamp.

## Recording executions

```csharp
public void Record(ExecutionRecord record)
```

`Record` validates the supplied record, appends it to the in-memory history, and performs the retention check described above.

It throws:

- `ArgumentNullException` when `record` is `null`.
- `ValidationException` when `PolicyName` is null, empty, or whitespace.
- `ValidationException` when `PolicyId` is null, empty, or whitespace.
- `ValidationException` when `ExecutionTimeMs` is negative.

The repository stores the supplied object reference; it does not clone the record.

## Querying history

### `GetByPolicyId`

```csharp
public List<ExecutionRecord> GetByPolicyId(string policyId)
```

Returns a new list containing records whose `PolicyId` exactly equals `policyId`, preserving their order in the history. It throws `ArgumentException` when `policyId` is null or empty. Whitespace-only values are accepted, although `Record` does not allow a whitespace-only `PolicyId`.

### `GetByTimeRange`

```csharp
public List<ExecutionRecord> GetByTimeRange(DateTime startTime, DateTime endTime)
```

Returns a new list of records whose `ExecutedAt` timestamp is within the inclusive range: `ExecutedAt >= startTime && ExecutedAt <= endTime`. It preserves history order. If `startTime` is later than `endTime`, the method throws `ValidationException`.

The method compares the supplied `DateTime` values directly and does not normalize them. Use values with a consistent time basis—normally UTC, matching the default value of `ExecutionRecord.ExecutedAt`.

### `GetLatest`

```csharp
public List<ExecutionRecord> GetLatest(int count)
```

Orders all records by `ExecutedAt` descending and returns at most `count` records in a new list. It throws `ValidationException` when `count` is zero or negative. If fewer records exist, all available records are returned; an empty repository returns an empty list.

## Success rate

```csharp
public double GetSuccessRate()
```

`GetSuccessRate` calculates the percentage of all retained records whose `IsSuccess` property is `true`:

```text
successful record count * 100.0 / total record count
```

The result is a percentage from `0.0` to `100.0`, not a fraction from `0.0` to `1.0`. An empty repository returns `0.0`. The calculation always covers the entire current history; there is no policy or time-range parameter.

## Latency percentiles

```csharp
public LatencyPercentiles GetLatencyPercentiles()
```

This method sorts every retained record's `ExecutionTimeMs` value in ascending order and returns `P50`, `P90`, and `P99` as `double` values in a `LatencyPercentiles` struct. If the repository is empty, all three values are `0.0`.

The implementation uses the nearest-rank method independently for each percentile:

```text
index = ceiling((percentile / 100.0) * count) - 1
```

The index is then clamped to the valid range, and the value at that zero-based index is returned. No interpolation or averaging is performed. For example, for sorted latencies `[10, 20, 30, 40, 50]`, P50 is `30`, while P90 and P99 are both `50`.

## Thread safety

Access to the internal `List<ExecutionRecord>` is synchronized with a single private lock. Recording, querying, clearing, deleting old records, and computing metrics all execute while holding that lock, so those repository operations can safely be called concurrently. Automatic cleanup calls `DeleteOldRecords()` while `Record` already holds the same lock; C# monitor locks are reentrant, so this nested acquisition is supported.

Query methods return new list or dictionary containers, which prevents callers from directly changing the repository's internal list. The contained `ExecutionRecord` instances are not copied, however. A caller can mutate a record after passing it to `Record`, or through a record returned by a query. Those property and `Metadata` mutations are outside the repository lock and are not made thread-safe by the repository. Enumerate or mutate shared record objects with external synchronization when concurrent access is possible.

Because one lock protects all operations, longer operations such as sorting latency values temporarily block both readers and writers.

## Usage

```csharp
using DotNetResiliencePipeline.Data;

var history = new ExecutionHistoryRepository(maxRetentionMinutes: 30);

history.Record(new ExecutionRecord
{
    PolicyName = "Orders retry",
    PolicyId = "orders-retry",
    IsSuccess = true,
    ExecutionTimeMs = 125,
    AttemptCount = 1,
    ExecutedAt = DateTime.UtcNow
});

history.Record(new ExecutionRecord
{
    PolicyName = "Orders retry",
    PolicyId = "orders-retry",
    IsSuccess = false,
    ExecutionTimeMs = 480,
    AttemptCount = 3,
    ErrorType = typeof(TimeoutException).FullName,
    ErrorMessage = "The operation timed out.",
    ExecutedAt = DateTime.UtcNow
});

var policyHistory = history.GetByPolicyId("orders-retry");
var lastHour = history.GetByTimeRange(
    DateTime.UtcNow.AddHours(-1),
    DateTime.UtcNow);
var latest = history.GetLatest(10);

var successRate = history.GetSuccessRate();
var latency = history.GetLatencyPercentiles();

Console.WriteLine($"Success rate: {successRate:F1}%");
Console.WriteLine($"P50/P90/P99: {latency.P50}/{latency.P90}/{latency.P99} ms");
```
