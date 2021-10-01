# CommandOptions

Configuration container for defining resilience pipeline execution behavior, including retry policies, timeouts, and output formatting. Used to customize how commands are executed and how their results are reported.

## API

### `Command`
- **Purpose**: The primary command to execute (e.g., `curl`, `dotnet`, `git`).
- **Type**: `string`
- **Remarks**: Must not be null or empty when executing a command.

### `Subcommand`
- **Purpose**: Optional subcommand or argument following the primary command (e.g., `add`, `commit`).
- **Type**: `string?`
- **Remarks**: Optional; if provided, it is appended directly after `Command`.

### `Arguments`
- **Purpose**: Dictionary of named arguments for the command (e.g., `{"url": "https://example.com", "timeout": "30"}`).
- **Type**: `Dictionary<string, string>`
- **Remarks**: Keys represent argument names; values are their corresponding values. Empty dictionary is valid.

### `Flags`
- **Purpose**: List of boolean flags to include in the command (e.g., `["--force", "--verbose"]`).
- **Type**: `List<string>`
- **Remarks**: Flags are appended as-is; duplicates are allowed but may cause command parsing issues.

### `PolicyName`
- **Purpose**: Name of the resilience policy to apply (e.g., `"retry-linear"`).
- **Type**: `string?`
- **Remarks**: Optional; if `null`, no resilience policy is applied.

### `PolicyType`
- **Purpose**: Type or category of resilience policy (e.g., `"retry"`, `"circuit-breaker"`).
- **Type**: `string?`
- **Remarks**: Optional; used to select or configure a specific policy type when `PolicyName` is ambiguous.

### `MaxRetries`
- **Purpose**: Maximum number of retry attempts for transient failures.
- **Type**: `int?`
- **Remarks**: Must be non-negative if specified. Ignored if `PolicyName` or `PolicyType` does not support retries.

### `FailureThreshold`
- **Purpose**: Number of failures that must occur before a circuit breaker opens or a retry policy aborts.
- **Type**: `int?`
- **Remarks**: Must be positive if specified. Behavior depends on the selected policy.

### `MaxParallelization`
- **Purpose**: Maximum number of parallel executions allowed for the command.
- **Type**: `int?`
- **Remarks**: Must be positive if specified. Used to limit concurrency in batch scenarios.

### `Timeout`
- **Purpose**: Maximum duration allowed for command execution before cancellation.
- **Type**: `TimeSpan?`
- **Remarks**: Must be positive if specified. If exceeded, the command is aborted.

### `OpenDuration`
- **Purpose**: Duration for which a circuit breaker remains open after tripping.
- **Type**: `TimeSpan?`
- **Remarks**: Must be positive if specified. Only applicable if the selected policy is a circuit breaker.

### `Verbose`
- **Purpose**: Enables detailed logging of command execution and resilience pipeline events.
- **Type**: `bool`
- **Remarks**: Defaults to `false`. When `true`, increases output verbosity for debugging.

### `JsonOutput`
- **Purpose**: Formats command output as JSON instead of plain text.
- **Type**: `bool`
- **Remarks**: Defaults to `false`. When `true`, output is serialized to JSON format.

### `OutputFile`
- **Purpose**: Path to a file where command output should be written.
- **Type**: `string?`
- **Remarks**: If `null`, output is written to standard output. File is overwritten if it exists.

### `ConfigFile`
- **Purpose**: Path to a configuration file from which additional options may be loaded.
- **Type**: `string?`
- **Remarks**: Optional; if provided, options in the file may override or supplement in-memory settings.

### `HasFlag`
- **Purpose**: Indicates whether a specific flag is present in the `Flags` list.
- **Type**: `bool`
- **Remarks**: Read-only property. Returns `true` if the flag exists in `Flags`; otherwise, `false`.

### `GetArgument`
- **Purpose**: Retrieves the value of a named argument by key.
- **Parameters**:
  - `key` (`string`): The name of the argument to retrieve.
- **Returns**: The value associated with `key`, or `null` if the key does not exist.
- **Remarks**: Case-sensitive lookup. Returns `null` for missing keys.

### `Validate`
- **Purpose**: Validates the current configuration for logical consistency and completeness.
- **Returns**: A list of validation error messages. Empty list indicates valid configuration.
- **Remarks**: Checks for mutually exclusive options, invalid values, and required fields based on context.

## Usage

### Example 1: Basic Command with Retry Policy
