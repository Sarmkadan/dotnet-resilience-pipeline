# MetricsExporterTests

The `MetricsExporterTests` class serves as the verification suite for the metrics export functionality within the `dotnet-resilience-pipeline` project. It validates the correctness of data serialization and formatting across JSON, CSV, and Prometheus exposition formats, ensuring that pipeline-level and per-policy metrics are accurately represented. Additionally, it enforces robust error handling by verifying that appropriate exceptions are thrown when invalid input, such as null snapshots, is provided to the exporter methods.

## API

### `ExportJson_ValidSnapshot_ProducesValidJson`
Verifies that when a valid metrics snapshot is provided, the exporter generates a well-formed JSON string. This test ensures the output adheres to standard JSON syntax and can be parsed without errors.
*   **Parameters**: None (uses internally constructed valid snapshot).
*   **Return Value**: `void`.
*   **Throws**: Fails the test assertion if the output is not valid JSON.

### `ExportJson_IncludesAllPipelineLevelCounters`
Asserts that the generated JSON output contains all expected counters aggregated at the pipeline level. This ensures no high-level metric data is omitted during serialization.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Fails the test assertion if any pipeline-level counter is missing from the JSON payload.

### `ExportJson_NullSnapshot_ThrowsArgumentNullException`
Validates that passing a `null` snapshot to the JSON export logic results in an `ArgumentNullException`. This confirms defensive programming practices against invalid input.
*   **Parameters**: None (invokes exporter with `null`).
*   **Return Value**: `void`.
*   **Throws**: Fails the test if `ArgumentNullException` is not thrown.

### `ExportCsv_ValidSnapshot_HasHeaderRow`
Checks that the CSV output generated from a valid snapshot begins with a header row defining the column names. This ensures the data is structured correctly for CSV parsers.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Fails the test assertion if the first line does not match the expected header schema.

### `ExportCsv_TwoPolicies_ProducesThreeLines`
Verifies the line count logic in CSV generation. Specifically, it asserts that a snapshot containing two distinct policies results in an output of exactly three lines (one header row plus one data row per policy).
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Fails the test assertion if the line count differs from three.

### `ExportCsv_SuccessRateIncluded`
Ensures that the calculated success rate metric is explicitly included in the CSV output columns. This validates that derived metrics are not lost during formatting.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Fails the test assertion if the success rate column or value is absent.

### `ExportPrometheus_ContainsPipelineLevelMetrics`
Asserts that the Prometheus exposition format output includes metrics tagged or named at the pipeline level. This verifies compliance with Prometheus labeling conventions for aggregate data.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Fails the test assertion if pipeline-level metrics are missing from the output text.

### `ExportPrometheus_ContainsPerPolicyMetrics`
Validates that the Prometheus output distinguishes and includes metrics for individual policies within the pipeline. This ensures granular observability is maintained.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Fails the test assertion if per-policy metrics are not found.

### `ExportPrometheus_CircuitBreakerStateGaugeIncluded`
Specifically checks for the presence of the Circuit Breaker state gauge in the Prometheus output. This confirms that stateful resilience components are correctly exposed as gauges.
*   **Parameters**: None.
*   **Return Value**: `void`.
*   **Throws**: Fails the test assertion if the circuit breaker gauge metric is missing.

### `ExportPrometheus_NullSnapshot_ThrowsArgumentNullException`
Validates that passing a `null` snapshot to the Prometheus export logic results in an `ArgumentNullException`.
*   **Parameters**: None (invokes exporter with `null`).
*   **Return Value**: `void`.
*   **Throws**: Fails the test if `ArgumentNullException` is not thrown.

## Usage

The following examples demonstrate how these tests might be structured within an xUnit test class to verify the `MetricsExporter` implementation.

**Example 1: Verifying JSON Output Structure**
```csharp
[Fact]
public void ExportJson_ValidSnapshot_ProducesValidJson()
{
    // Arrange
    var snapshot = new ResiliencePipelineSnapshot
    {
        PipelineName = "TestPipeline",
        StartTime = DateTime.UtcNow,
        Counters = new Dictionary<string, long> { { "executions", 10 } }
    };
    var exporter = new MetricsExporter();

    // Act
    string jsonOutput = exporter.ExportJson(snapshot);

    // Assert
    // Verifies the output is parseable and valid
    var jsonDocument = JsonDocument.Parse(jsonOutput);
    Assert.NotNull(jsonDocument.RootElement.GetProperty("pipelineName"));
}
```

**Example 2: Verifying Exception Handling for Null Inputs**
```csharp
[Fact]
public void ExportPrometheus_NullSnapshot_ThrowsArgumentNullException()
{
    // Arrange
    var exporter = new MetricsExporter();

    // Act & Assert
    // Verifies that the exporter defends against null arguments
    Assert.Throws<ArgumentNullException>(() => exporter.ExportPrometheus(null));
}
```

## Notes

*   **Null Safety**: All export methods (`ExportJson`, `ExportCsv`, `ExportPrometheus`) are designed to throw `ArgumentNullException` immediately upon receiving a `null` snapshot. Callers must ensure snapshot objects are instantiated before invocation.
*   **Thread Safety**: As these tests instantiate new exporter instances and snapshots for each method call without shared static state, the underlying implementation is expected to be stateless regarding the export operation itself. However, if the `ResiliencePipelineSnapshot` contains mutable collections, callers should ensure the snapshot is immutable or locked during the export call to prevent race conditions.
*   **Line Counting Logic**: The CSV export logic assumes a strict one-line-per-policy mapping following the header. Tests like `ExportCsv_TwoPolicies_ProducesThreeLines` rely on this deterministic behavior; adding metadata footers or multi-line field values in future updates would require adjusting these assertions.
*   **Metric Completeness**: The tests strictly verify the *presence* of specific metrics (e.g., Circuit Breaker state, Success Rate). They do not validate the mathematical accuracy of the calculations within the snapshot, which is assumed to be handled by the snapshot generation logic prior to export.
