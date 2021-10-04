# PolicyResult

`PolicyResult` is a generic record that represents the outcome of executing a resilience policy in the `dotnet-resilience-pipeline` library. It encapsulates the success or failure state of a policy execution, along with associated data, metadata, and timing information. This type is primarily used to chain policy executions and handle fallback or error scenarios in resilient application flows.

## API

### Properties

#### `IsSuccess`
- **Purpose**: Indicates whether the policy execution completed successfully.
- **Type**: `bool`
- **Remarks**: Returns `true` if the execution succeeded; otherwise, `false`. This property is read-only and reflects the state of the execution.

#### `Data`
- **Purpose**: Contains the result data returned by the policy execution, if applicable.
- **Type**: `T?`
- **Remarks**: May be `null` if the execution failed or no data was produced. This property is read-only.

#### `Exception`
- **Purpose**: Holds the exception that caused the policy execution to fail, if applicable.
- **Type**: `Exception?`
- **Remarks**: Returns `null` if the execution succeeded. This property is read-only.

#### `PolicyName`
- **Purpose**: Identifies the name of the policy that produced this result.
- **Type**: `string`
- **Remarks**: Useful for logging and debugging. This property is read-only.

#### `ExecutionTimeMs`
- **Purpose**: Records the duration of the policy execution in milliseconds.
- **Type**: `long`
- **Remarks**: Always non-negative. This property is read-only.

#### `AttemptCount`
- **Purpose**: Tracks the number of attempts made during the execution.
- **Type**: `int`
- **Remarks**: Starts at `1` for the initial attempt. This property is read-only.

#### `ExecutedAt`
- **Purpose**: Captures the timestamp when the execution completed.
- **Type**: `DateTime`
- **Remarks**: Uses the system clock. This property is read-only.

#### `ExecutionId`
- **Purpose**: Provides a unique identifier for the execution.
- **Type**: `string`
- **Remarks**: Useful for correlating logs and telemetry. This property is read-only.

#### `Metadata`
- **Purpose**: Stores additional contextual information about the execution.
- **Type**: `Dictionary<string, object>`
- **Remarks**: Empty by default; can be populated during policy execution. This property is read-only.

### Methods

#### `OnSuccess`
- **Purpose**: Executes an action when the policy execution succeeds.
- **Parameters**:
  - `action` (`Action<T>`): The action to execute if the result is successful.
- **Remarks**: The action is invoked only if `IsSuccess` is `true`. No return value or exceptions are propagated.

#### `OnFailure`
- **Purpose**: Executes an action when the policy execution fails.
- **Parameters**:
  - `action` (`Action<Exception>`): The action to execute if the result is a failure.
- **Remarks**: The action is invoked only if `Exception` is not `null`. No return value or exceptions are propagated.

#### `Map<TNew>`
- **Purpose**: Transforms the result data into a new type.
- **Parameters**:
  - `mapper` (`Func<T, TNew>`): A function to convert the current data to a new type.
- **Returns**: A new `PolicyResult<TNew>` with the transformed data.
- **Remarks**: If the current result is a failure, the returned result will also be a failure with the same exception. The `ExecutionId` and `Metadata` are preserved.

### Static Methods

#### `Success`
- **Purpose**: Creates a successful `PolicyResult<T>`.
- **Parameters**:
  - `data` (`T`): The result data.
  - `policyName` (`string`): The name of the policy.
  - `executionTimeMs` (`long`): The execution duration in milliseconds.
  - `attemptCount` (`int`): The number of attempts made.
  - `metadata` (`Dictionary<string, object>?`): Optional metadata.
- **Returns**: A `PolicyResult<T>` with `IsSuccess` set to `true`.
- **Remarks**: The `ExecutedAt` timestamp is set to the current time.

#### `Failure`
- **Purpose**: Creates a failed `PolicyResult<T>`.
- **Parameters**:
  - `exception` (`Exception`): The exception that caused the failure.
  - `policyName` (`string`): The name of the policy.
  - `executionTimeMs` (`long`): The execution duration in milliseconds.
  - `attemptCount` (`int`): The number of attempts made.
  - `metadata` (`Dictionary<string, object>?`): Optional metadata.
- **Returns**: A `PolicyResult<T>` with `IsSuccess` set to `false`.
- **Remarks**: The `Exception` property is set to the provided exception.

#### `Fallback`
- **Purpose**: Creates a `PolicyResult<T>` representing a fallback scenario.
- **Parameters**:
  - `data` (`T`): The fallback data.
  - `fallbackException` (`Exception?`): Optional exception that triggered the fallback.
  - `policyName` (`string`): The name of the policy.
  - `executionTimeMs` (`long`): The execution duration in milliseconds.
  - `attemptCount` (`int`): The number of attempts made.
  - `metadata` (`Dictionary<string, object>?`): Optional metadata.
- **Returns**: A `PolicyResult<T>` with `IsSuccess` set to `true` and the fallback data.
- **Remarks**: The `Exception` property may be `null` if the fallback was intentional.

## Usage

### Example 1: Basic Success and Failure Handling
