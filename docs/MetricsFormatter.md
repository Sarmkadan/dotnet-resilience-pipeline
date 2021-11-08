# MetricsFormatter

`MetricsFormatter` is a utility class that provides formatting capabilities for resilience pipeline metrics, including tabular representations, aggregated summaries, trend analysis, health status, and comparative views of metric data.

## API

### `public string FormatMetricsTable(Metrics metrics)`

Formats the provided metrics as a markdown table with columns for metric name, value, unit, and status.

- **Parameters**
  - `metrics`: The `Metrics` object containing the raw metric data to format.
- **Return value**
  - A markdown-formatted string representing the metrics in a table layout.
- **Exceptions**
  - Throws `ArgumentNullException` if `metrics` is `null`.

---

### `public string FormatAggregatedMetrics(AggregatedMetrics aggregatedMetrics)`

Formats aggregated metrics (e.g., averages, percentiles) into a human-readable summary.

- **Parameters**
  - `aggregatedMetrics`: The `AggregatedMetrics` object containing precomputed aggregates.
- **Return value**
  - A markdown-formatted string summarizing the aggregated values.
- **Exceptions**
  - Throws `ArgumentNullException` if `aggregatedMetrics` is `null`.

---

### `public string FormatTrend(Trend trend)`

Formats trend data (e.g., changes over time) into a readable representation.

- **Parameters**
  - `trend`: The `Trend` object containing trend analysis results.
- **Return value**
  - A markdown-formatted string describing the trend direction and magnitude.
- **Exceptions**
  - Throws `ArgumentNullException` if `trend` is `null`.

---
### `public string FormatHealthStatus(HealthStatus healthStatus)`

Formats a health status indicator (e.g., "Healthy", "Degraded") into a standardized output.

- **Parameters**
  - `healthStatus`: The `HealthStatus` enum value representing the current system health.
- **Return value**
  - A markdown-formatted string with the health status and optional details.
- **Exceptions**
  - Throws `ArgumentNullException` if `healthStatus` is `null`.

---
### `public string FormatComparison(Comparison comparison)`

Formats a comparison between two sets of metrics (e.g., before/after, baseline/current).

- **Parameters**
  - `comparison`: The `Comparison` object containing the two metric sets to compare.
- **Return value**
  - A markdown-formatted string highlighting differences and deltas.
- **Exceptions**
  - Throws `ArgumentNullException` if `comparison` is `null`.

## Usage

### Example 1: Formatting Metrics Table
