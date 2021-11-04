# MetricsAggregator

The `MetricsAggregator` class collects, stores, and analyzes execution metrics for resilience policies over a configurable time window. It tracks execution counts, success rates, and execution times, providing aggregated views and trend analysis for performance monitoring and diagnostics.

## API

### `MaxSnapshots`
Gets the maximum number of snapshots retained by the aggregator. Snapshots beyond this count are discarded in a FIFO manner.

- **Type**: `int`
- **Access**: Read-only

### `RecordSnapshot()`
Captures the current execution metrics as a snapshot for trend analysis.

- **Parameters**: None
- **Returns**: `void`
- **Throws**: None

### `GetAggregatedMetrics()`
Returns the aggregated metrics over the entire time window.

- **Parameters**: None
- **Returns**: `AggregatedMetrics` – Contains total executions, success rate, average execution time, and other aggregated values.
- **Throws**: None

### `AnalyzeTrend()`
Evaluates the trend of metrics over the collected snapshots to identify performance patterns.

- **Parameters**: None
- **Returns**: `MetricsTrend` – An enum indicating whether the trend is improving, stable, or degrading.
- **Throws**: None

### `ComparePeriods(TimeSpan period1, TimeSpan period2)`
Compares metrics between two specified time periods within the window.

- **Parameters**:
  - `period1` (`TimeSpan`) – The first time period to compare.
  - `period2` (`TimeSpan`) – The second time period to compare.
- **Returns**: `PeriodComparison` – A comparison result indicating which period performed better or if they are similar.
- **Throws**: `ArgumentException` – If either period is invalid or exceeds the time window.

### `GenerateReport()`
Creates a detailed performance report summarizing execution metrics and trends.

- **Parameters**: None
- **Returns**: `PerformanceReport` – A structured report containing key metrics, trends, and comparative insights.
- **Throws**: None

### `Clear()`
Resets all recorded metrics and snapshots.

- **Parameters**: None
- **Returns**: `void`
- **Throws**: None

### `Timestamp`
Gets the timestamp of the most recent snapshot or metric update.

- **Type**: `DateTime`
- **Access**: Read-only

### `TotalExecutions`
Gets the total number of executions recorded.

- **Type**: `long`
- **Access**: Read-only

### `SuccessfulExecutions`
Gets the total number of successful executions.

- **Type**: `long`
- **Access**: Read-only

### `FailedExecutions`
Gets the total number of failed executions.

- **Type**: `long`
- **Access**: Read-only

### `SuccessRate`
Gets the success rate as a value between 0.0 and 1.0.

- **Type**: `double`
- **Access**: Read-only

### `AverageExecutionTimeMs`
Gets the average execution time in milliseconds.

- **Type**: `double`
- **Access**: Read-only

### `ActivePolicies`
Gets the number of active resilience policies being tracked.

- **Type**: `int`
- **Access**: Read-only

### `TimeWindow`
Gets the time window over which metrics are aggregated.

- **Type**: `TimeSpan`
- **Access**: Read-only

### `SnapshotCount`
Gets the number of snapshots currently stored.

- **Type**: `int`
- **Access**: Read-only

### `AverageSuccessRate`
Gets the average success rate across all snapshots.

- **Type**: `double`
- **Access**: Read-only

### `PeakExecutions`
Gets the peak number of executions recorded in a single snapshot.

- **Type**: `long`
- **Access**: Read-only

## Usage

### Example 1: Basic Metrics Tracking
