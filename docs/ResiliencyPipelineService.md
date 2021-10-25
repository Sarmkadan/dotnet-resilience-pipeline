# ResiliencyPipelineService

A service class that manages the lifecycle and execution of resiliency policies within a named pipeline. It tracks policy registrations, execution statistics, and provides metrics about policy performance and pipeline health.

## API

### `PipelineId`
A read-only identifier for the pipeline instance. This value is set at construction and cannot be modified.

### `CreatedAt`
A read-only timestamp indicating when the pipeline instance was created. This value is set at construction and cannot be modified.

### `TotalExecutions`
A read-only counter of the total number of executions performed across all policies in the pipeline. This value increments with every call to `ExecuteAsync`.

### `SuccessfulExecutions`
A read-only counter of the number of executions that completed successfully across all policies in the pipeline. This value increments only when an execution completes without throwing an exception.

### `FailedExecutions`
A read-only counter of the number of executions that resulted in a failure across all policies in the pipeline. This value increments when an execution throws an exception that is not handled by the policy.

### `ResiliencyPipelineService()`
Constructs a new instance of `ResiliencyPipelineService` with a generated unique identifier and the current UTC timestamp.

### `RegisterPolicy(ResiliencyPolicy policy)`
Registers a new resiliency policy with the pipeline.

- **Parameters**:
  - `policy`: The policy to register. Must not be `null`.
- **Returns**: `void`
- **Throws**:
  - `ArgumentNullException`: If `policy` is `null`.
  - `InvalidOperationException`: If a policy with the same name already exists in the pipeline.

### `GetPolicy(string name)`
Retrieves a registered policy by its name.

- **Parameters**:
  - `name`: The name of the policy to retrieve.
- **Returns**: The `ResiliencyPolicy` instance if found; otherwise, `null`.
- **Throws**:
  - `ArgumentNullException`: If `name` is `null`.

### `GetPolicyByName(string name)`
Alias for `GetPolicy`. Retrieves a registered policy by its name.

- **Parameters**:
  - `name`: The name of the policy to retrieve.
- **Returns**: The `ResiliencyPolicy` instance if found; otherwise, `null`.
- **Throws**:
  - `ArgumentNullException`: If `name` is `null`.

### `GetAllPolicies()`
Retrieves a list of all registered policies in the pipeline.

- **Returns**: A `List<ResiliencyPolicy>` containing all registered policies. The list is a copy and modifications will not affect the internal state.

### `RemovePolicy(string name)`
Removes a registered policy by its name.

- **Parameters**:
  - `name`: The name of the policy to remove.
- **Returns**: `true` if the policy was found and removed; otherwise, `false`.
- **Throws**:
  - `ArgumentNullException`: If `name` is `null`.

### `ExecuteAsync<T>(string name, Func<CancellationToken, Task<T>> action, CancellationToken cancellationToken = default)`
Executes the specified action wrapped in the named policy.

- **Parameters**:
  - `name`: The name of the policy to use for execution.
  - `action`: The asynchronous action to execute. Must not be `null`.
  - `cancellationToken`: A cancellation token to observe while waiting for the task to complete.
- **Returns**: A `PolicyResult<T>` containing the outcome of the execution, including any result or exception.
- **Throws**:
  - `ArgumentNullException`: If `name` or `action` is `null`.
  - `KeyNotFoundException`: If no policy with the given `name` is registered.

### `ExecuteAsync(string name, Func<CancellationToken, Task> action, CancellationToken cancellationToken = default)`
Executes the specified action wrapped in the named policy.

- **Parameters**:
  - `name`: The name of the policy to use for execution.
  - `action`: The asynchronous action to execute. Must not be `null`.
  - `cancellationToken`: A cancellation token to observe while waiting for the task to complete.
- **Returns**: A `PolicyResult` containing the outcome of the execution, including any exception.
- **Throws**:
  - `ArgumentNullException`: If `name` or `action` is `null`.
  - `KeyNotFoundException`: If no policy with the given `name` is registered.

### `GetStatistics()`
Retrieves aggregated execution statistics for the pipeline.

- **Returns**: A `PipelineStatistics` object containing counts of total, successful, and failed executions.

### `ResetStatistics()`
Resets all execution counters (`TotalExecutions`, `SuccessfulExecutions`, `FailedExecutions`) to zero.

### `GetStats()`
Retrieves a snapshot of current pipeline metrics, including execution statistics and registered policies.

- **Returns**: A `PipelineMetricsSnapshot` object containing a point-in-time view of pipeline state.

## Usage

### Registering and executing a retry policy
