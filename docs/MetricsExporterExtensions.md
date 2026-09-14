# MetricsExporterExtensions Documentation

This document provides detailed information about the extension methods in `MetricsExporterExtensions.cs` and `CsvReportFormatterJsonExtensions.cs`.

## MetricsExporterExtensions

Extension methods for the `MetricsExporter` class that provide functionality for formatting and exporting pipeline metrics.

### CreateSummary

Creates a human-readable summary string of pipeline metrics.

**Signature:**
```csharp
public static string CreateSummary(this MetricsExporter exporter, PipelineMetricsSnapshot snapshot)
```

**Parameters:**
- `exporter`: The metrics exporter instance.
- `snapshot`: The pipeline metrics snapshot containing the data to summarize.

**Returns:**
A formatted summary string with pipeline performance, policy statistics, and additional metrics.

**Exceptions:**
- `ArgumentNullException`: If `exporter` or `snapshot` is null.

**Example Usage:**
```csharp
var exporter = new MetricsExporter();
var snapshot = GetPipelineMetricsSnapshot(); // Assume this method exists
string summary = exporter.CreateSummary(snapshot);
Console.WriteLine(summary);
```

**Sample Output:**
```
📊 Resilience Pipeline Metrics Summary
=====================================
Exported at: 2026-09-14 10:30:00 UTC

📈 Pipeline Performance:
 Total executions: 1,250
 Successful: 1,200 (96.0%)
 Failed: 50
 Success rate: 96.00%

🔄 Policy Statistics:
 RetryPolicy          Executions: 500 | Success: 480 (96.0%)
 Failures: 20 | State: N/A
 CircuitBreakerPolicy Executions: 300 | Success: 288 (96.0%)
 Failures: 12 | State: Closed
 TimeoutPolicy        Executions: 200 | Success: 192 (96.0%)
 Failures: 8 | State: N/A

⚡ Additional Metrics:
 Retries: 25
 Circuit breaker trips: 3
 Timeouts: 8
```

### ExportConsoleTable

Exports metrics in a tabular format suitable for console output.

**Signature:**
```csharp
public static string ExportConsoleTable(this MetricsExporter exporter, PipelineMetricsSnapshot snapshot, bool includePolicyDetails = true)
```

**Parameters:**
- `exporter`: The metrics exporter instance.
- `snapshot`: The pipeline metrics snapshot.
- `includePolicyDetails`: Whether to include detailed policy breakdown (default: true).

**Returns:**
A formatted table string with box-drawing characters.

**Exceptions:**
- `ArgumentNullException`: If `exporter` or `snapshot` is null.

**Example Usage:**
```csharp
var exporter = new MetricsExporter();
var snapshot = GetPipelineMetricsSnapshot();
string table = exporter.ExportConsoleTable(snapshot);
Console.WriteLine(table);

// To exclude policy details:
string tableWithoutDetails = exporter.ExportConsoleTable(snapshot, includePolicyDetails: false);
```

**Sample Output (with policy details):**
```
╔═════════════════════════════════════════════════════════════════╗
║ Resilience Pipeline Metrics                                   ║
╠═════════════════════════════════════════════════════════════════╣
║ Exported at:              2026-09-14 10:30:00               ║
║ Total executions:         1,250                             ║
║ Successful executions:    1,200                             ║
║ Failed executions:          50                              ║
║ Success rate:                96.00%                         ║
╠═════════════════════════════════════════════════════════════════╣
║ Policy Name                 Type               Executions   Success   Rate    ║
╠═════════════════════════════════════════════════════════════════╣
║ RetryPolicy                 RetryPolicy              500        480    96.0% ║
║                             CircuitBreakerPolicy     300        288    96.0% ║
║                             TimeoutPolicy            200        192    96.0% ║
║                                                     Failures: 20 State: N/A   ║
║                                                     Failures: 12 State: Closed║
║                                                     Failures:  8 State: N/A   ║
╚═════════════════════════════════════════════════════════════════╝
```

**Sample Output (without policy details):**
```
╔═════════════════════════════════════════════════════════════════╗
║ Resilience Pipeline Metrics                                   ║
╠═════════════════════════════════════════════════════════════════╣
║ Exported at:              2026-09-14 10:30:00               ║
║ Total executions:         1,250                             ║
║ Successful executions:    1,200                             ║
║ Failed executions:          50                              ║
║ Success rate:                96.00%                         ║
╚═════════════════════════════════════════════════════════════════╝
```

### ExportMarkdown

Exports metrics in Markdown format for documentation or reporting.

**Signature:**
```csharp
public static string ExportMarkdown(this MetricsExporter exporter, PipelineMetricsSnapshot snapshot)
```

**Parameters:**
- `exporter`: The metrics exporter instance.
- `snapshot`: The pipeline metrics snapshot.

**Returns:**
A Markdown formatted string suitable for README files, reports, or documentation.

**Exceptions:**
- `ArgumentNullException`: If `exporter` or `snapshot` is null.

**Example Usage:**
```csharp
var exporter = new MetricsExporter();
var snapshot = GetPipelineMetricsSnapshot();
string markdown = exporter.ExportMarkdown(snapshot);
File.WriteAllText("metrics-report.md", markdown);
```

**Sample Output:**
```markdown
# Resilience Pipeline Metrics Report

**Generated at:** 2026-09-14 10:30:00 UTC

## 📊 Pipeline Performance Summary

| Metric | Value |
|--------|-------|
| Total executions | 1,250 |
| Successful executions | 1,200 (96.0%) |
| Failed executions | 50 |
| Success rate | 96.00% |
| Retries | 25 |
| Circuit breaker trips | 3 |
| Timeouts | 8 |

## 🔄 Policy Breakdown

| Policy | Type | Executions | Success | Failures | Success Rate | State |
|--------|------|------------|---------|----------|--------------|-------|
| RetryPolicy | RetryPolicy | 500 | 480 | 20 | 96.0% | N/A |
| CircuitBreakerPolicy | CircuitBreakerPolicy | 300 | 288 | 12 | 96.0% | Closed |
| TimeoutPolicy | TimeoutPolicy | 200 | 192 | 8 | 96.0% | N/A |

---
*Report generated by MetricsExporterExtensions at 2026-09-14T10:30:00.0000000Z*
```

### GetKeyPerformanceIndicators

Gets a dictionary of key performance indicators from the metrics snapshot.

**Signature:**
```csharp
public static Dictionary<string, object> GetKeyPerformanceIndicators(this MetricsExporter exporter, PipelineMetricsSnapshot snapshot)
```

**Parameters:**
- `exporter`: The metrics exporter instance.
- `snapshot`: The pipeline metrics snapshot.

**Returns:**
A dictionary containing KPI values with string keys and object values.

**Exceptions:**
- `ArgumentNullException`: If `exporter` or `snapshot` is null.

**Example Usage:**
```csharp
var exporter = new MetricsExporter();
var snapshot = GetPipelineMetricsSnapshot();
var kpis = exporter.GetKeyPerformanceIndicators(snapshot);

Console.WriteLine($"Total Executions: {kpis["TotalExecutions"]}");
Console.WriteLine($"Success Rate: {kpis["SuccessRate"]:P2}");
Console.WriteLine($"Retry Count: {kpis["RetryCount"]}");
```

**Sample Return Value:**
```csharp
{
    ["TotalExecutions"] = 1250,
    ["SuccessfulExecutions"] = 1200,
    ["FailedExecutions"] = 50,
    ["SuccessRate"] = 0.96,
    ["SuccessRatePercentage"] = 96.0,
    ["RetryCount"] = 25,
    ["CircuitBreakerTrips"] = 3,
    ["TimeoutCount"] = 8,
    ["PolicyCount"] = 3,
    ["EnabledPolicies"] = 3,
    ["DisabledPolicies"] = 0,
    ["ExportedAt"] = 9/14/2026 10:30:00 AM
}
```

## CsvReportFormatterJsonExtensions

Provides System.Text.Json serialization and deserialization extensions for `CsvReportFormatter`.

### ToJson

Serializes the `CsvReportFormatter` instance to a JSON string.

**Signature:**
```csharp
public static string ToJson(this CsvReportFormatter value, bool indented = false)
```

**Parameters:**
- `value`: The formatter instance to serialize.
- `indented`: Whether to format the JSON with indentation for readability (default: false).

**Returns:**
A JSON string representation of the formatter.

**Exceptions:**
- `ArgumentNullException`: Thrown when `value` is null.

**Example Usage:**
```csharp
var formatter = new CsvReportFormatter();
// Configure formatter properties...
string json = formatter.ToJson();
string indentedJson = formatter.ToJson(indented: true);

// Save to file
File.WriteAllText("report.json", indentedJson);
```

**Sample Output (compact):**
```json
{"format":"Csv","delimiter":","}
```

**Sample Output (indented):**
```json
{
  "format": "Csv",
  "delimiter": ","
}
```

### FromJson

Deserializes a JSON string to a `CsvReportFormatter` instance.

**Signature:**
```csharp
public static CsvReportFormatter? FromJson(string json)
```

**Parameters:**
- `json`: The JSON string to deserialize.

**Returns:**
The deserialized `CsvReportFormatter` instance, or null if deserialization fails.

**Exceptions:**
- `ArgumentException`: Thrown when `json` is null or empty.
- `JsonException`: Thrown when the JSON is invalid or cannot be deserialized.

**Example Usage:**
```csharp
string json = File.ReadAllText("report.json");
CsvReportFormatter? formatter = CsvReportFormatterJsonExtensions.FromJson(json);

if (formatter != null)
{
    // Use the formatter
}
else
{
    Console.WriteLine("Failed to deserialize formatter from JSON.");
}
```

### TryFromJson

Attempts to deserialize a JSON string to a `CsvReportFormatter` instance.

**Signature:**
```csharp
public static bool TryFromJson(string json, [NotNullWhen(true)] out CsvReportFormatter? value)
```

**Parameters:**
- `json`: The JSON string to deserialize.
- `value`: Receives the deserialized instance if successful.

**Returns:**
True if deserialization succeeded; otherwise, false.

**Exceptions:**
- `ArgumentException`: Thrown when `json` is null or empty.

**Example Usage:**
```csharp
string json = File.ReadAllText("report.json");
if (CsvReportFormatterJsonExtensions.TryFromJson(json, out var formatter))
{
    // Use the formatter
    Console.WriteLine($"Delimiter: {formatter.Delimiter}");
}
else
{
    Console.WriteLine("Failed to deserialize formatter from JSON.");
}
```