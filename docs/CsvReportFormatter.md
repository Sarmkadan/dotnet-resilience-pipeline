# CsvReportFormatter

A utility class for formatting resilience pipeline metrics, policies, execution history, performance metrics, logs, and errors into CSV-formatted strings or exporting them to files. Designed to provide structured, machine-readable output for monitoring and analysis of pipeline behavior.

## API

### `FormatPipelineMetrics`
Formats pipeline-specific metrics into a CSV string. Each metric is represented as a row with columns for metric name and value.

- **Returns**: `string` – A CSV-formatted string containing pipeline metrics.
- **Throws**: `InvalidOperationException` if required pipeline metrics are not available.

### `FormatPolicies`
Formats the resilience policies applied in the pipeline into a CSV string. Each policy is represented as a row with columns for policy type, name, and configuration.

- **Returns**: `string` – A CSV-formatted string containing policy definitions.
- **Throws**: `InvalidOperationException` if the pipeline policies cannot be accessed.

### `FormatExecutionHistory`
Formats the execution history of the pipeline into a CSV string. Each execution event is represented as a row with columns for timestamp, event type, and details.

- **Returns**: `string` – A CSV-formatted string containing execution history.
- **Throws**: `InvalidOperationException` if the execution history is unavailable.

### `FormatPerformanceMetrics`
Formats performance-related metrics (e.g., latency, throughput) into a CSV string. Each metric is represented as a row with columns for metric name and value.

- **Returns**: `string` – A CSV-formatted string containing performance metrics.
- **Throws**: `InvalidOperationException` if required performance data is missing.

### `FormatLogs`
Formats structured logs generated during pipeline execution into a CSV string. Each log entry is represented as a row with columns for timestamp, log level, and message.

- **Returns**: `string` – A CSV-formatted string containing logs.
- **Throws**: `InvalidOperationException` if logs are not available.

### `FormatErrors`
Formats error and exception details encountered during pipeline execution into a CSV string. Each error is represented as a row with columns for timestamp, error type, message, and stack trace.

- **Returns**: `string` – A CSV-formatted string containing error details.
- **Throws**: `InvalidOperationException` if no errors are recorded.

### `ExportToFileAsync`
Asynchronously exports all formatted reports (metrics, policies, execution history, performance metrics, logs, and errors) to a specified file path. The file is overwritten if it exists.

- **Parameters**:
  - `filePath` (`string`) – The path to the output file.
- **Returns**: `Task` – A task representing the asynchronous operation.
- **Throws**:
  - `ArgumentNullException` if `filePath` is `null`.
  - `ArgumentException` if `filePath` is empty or contains invalid characters.
  - `UnauthorizedAccessException` if the caller lacks permissions to write to the file.
  - `DirectoryNotFoundException` if the parent directory does not exist.
  - `IOException` if an I/O error occurs during file operations.

### `Timestamp`
Gets the timestamp associated with the report, indicating when the data was collected.

- **Type**: `DateTime`
- **Access**: Public read-only property.

### `PolicyName`
Gets the name of the resilience policy being reported on.

- **Type**: `string`
- **Access**: Public read-only property.

### `IsSuccess`
Indicates whether the pipeline execution completed successfully without unhandled errors.

- **Type**: `bool`
- **Access**: Public read-only property.

### `ExecutionTimeMs`
Gets the total execution time of the pipeline in milliseconds.

- **Type**: `long`
- **Access**: Public read-only property.

## Usage

### Example 1: Basic Reporting
