# MetricsExporter

The `MetricsExporter` type provides a convenient way to retrieve resilience‑pipeline metrics in various serialized formats. It aggregates execution statistics for a pipeline and its constituent policies, allowing consumers to export the data as JSON, CSV, or Prometheus‑compatible text for monitoring, reporting, or debugging purposes.

## API

| Member | Type | Purpose | Parameters | Return Value | Exceptions |
|--------|------|---------|------------|--------------|------------|
| `ExportJson` | `string` | Returns a JSON representation of the current metrics snapshot. | none | A UTF‑8 encoded JSON string containing all metric values. | May throw `InvalidOperationException` if the exporter has not been initialized with pipeline data. |
| `ExportCsv` | `string` | Returns a CSV representation of the current metrics snapshot. | none | A CSV‑formatted string with a header row and one data row per metric. | May throw `InvalidOperationException` if the exporter has not been initialized. |
| `ExportPrometheus` | `string` | Returns a Prometheus‑exposition format string of the metrics. | none | A string suitable for scraping by a Prometheus server. | May throw `InvalidOperationException` if the exporter has not been initialized. |
| `ExportedAt` | `DateTime` | The UTC timestamp when the last export operation (`ExportJson`, `ExportCsv`, or `ExportPrometheus`) was performed. | none | The date and time of the most recent export. | None; defaults to `DateTime.MinValue` if no export has occurred. |
| `Format` | `string` | Indicates the format that was used for the most recent export (e.g., `"json"`, `"csv"`, `"prometheus"`). | none | The format string of the last successful export, or `null` if none. | None. |
| `Pipeline` | `PipelineSummaryExport` | Summary metrics for the overall pipeline (executions, success rate, etc.). | none | An object containing aggregated pipeline‑level statistics. | None; property returns the current snapshot. |
| `Policies` | `List<PolicyExport>` | Detailed metrics for each policy that participated in the pipeline execution. | none | A read‑only list of `PolicyExport` instances, one per policy. | None; returns an empty list if no policies have been tracked. |
| `TotalExecutions` | `long` | Total number of times the pipeline was invoked. | none | Cumulative execution count. | None. |
| `SuccessfulExecutions` | `long` | Number of pipeline executions that completed without failure. | none | Count of successful runs. | None. |
| `FailedExecutions` | `long` | Number of pipeline executions that resulted in a failure. | none | Count of failed runs. | None. |
| `SuccessRate` | `double` | Ratio of successful executions to total executions, expressed as a value between 0 and 1. | none | Computed as `SuccessfulExecutions / (double)TotalExecutions`; returns 0 when `TotalExecutions` is 0. | None; may return `double.NaN` if both numerator and denominator are zero (implementation‑dependent). |
| `RetryCount` | `long` | Total number of retry attempts executed across all policies. | none | Cumulative retry count. | None. |
| `CircuitBreakerTrips` | `long` | Number of times the circuit‑breaker policy transitioned to the open state. | none | Count of circuit‑breaker trips. | None. |
| `TimeoutCount` | `long` | Number of executions that were terminated due to a timeout policy. | none | Count of timeout occurrences. | None. |
| `PolicyId` | `string` | Identifier of the policy associated with the metrics (when accessed via a `PolicyExport` instance). | none | Unique ID string for the policy. | None. |
| `PolicyName` | `string` | Human‑readable name of the policy. | none | Display name of the policy. | None. |
| `PolicyType` | `string` | The type of policy (e.g., `"Retry"`, `"CircuitBreaker"`, `"Timeout"`). | none | Policy type identifier. | None. |
| `IsEnabled` | `bool` | Indicates whether the policy is currently enabled in the pipeline. | none | `true` if the policy is active; otherwise `false`. | None. |

*Note: The members `TotalExecutions` and `SuccessfulExecutions` appear twice in the source listing; they are documented once above.*

## Usage

### Example 1: Exporting metrics as JSON for logging

```csharp
using DotNetResiliencePipeline;

// Assume 'pipeline' is a configured ResiliencePipeline instance.
var exporter = new MetricsExporter(pipeline);

// Simulate some workload.
for (int i = 0; i < 100; i++)
{
    pipeline.Execute(() => 
    {
        // Work that may trigger retries, timeouts, or circuit breaks.
    });
}

// Retrieve JSON metrics.
string json = exporter.ExportJson;

// Log or transmit the JSON as needed.
logger.Information("Pipeline metrics: {Metrics}", json);
```

### Example 2: Consuming Prometheus‑formatted metrics in a monitoring endpoint

```csharp
using DotNetResiliencePipeline;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplication.CreateBuilder(args);
var app = builder.Build();

// Exporter is updated periodically by a background service.
app.MapGet("/metrics", () =>
{
    string prometheus = exporter.ExportPrometheus;
    return Results.Content(prometheus, "text/plain");
});

app.Run();
```

## Notes

- **Initialization**: The exporter must be associated with a pipeline that has collected metrics before any of the `Export*` properties are accessed; otherwise an `InvalidOperationException` may be thrown.
- **Thread safety**: The properties are not synchronized. Concurrent modifications to the underlying pipeline while reading metric values can lead to inconsistent snapshots. Consumers should either ensure exclusive access during updates or employ external locking when reading and writing from multiple threads.
- **Duplicate properties**: The source lists `TotalExecutions` and `SuccessfulExecutions` twice; they refer to the same underlying fields. Accessing either name yields the identical value.
- **Empty state**: If no executions have occurred, `TotalExecutions`, `SuccessfulExecutions`, and `FailedExecutions` will be zero, `SuccessRate` will return `0` (or `NaN` depending on implementation), and the `Policies` list will be empty.
- **ExportedAt timing**: The `ExportedAt` timestamp is updated only when one of the export properties (`ExportJson`, `ExportCsv`, `ExportPrometheus`) is accessed. Simply reading other metric properties does not modify this value.
- **Format property**: Reflects the format of the most successful export call; if multiple export formats are requested, `Format` will correspond to the last property accessed.
