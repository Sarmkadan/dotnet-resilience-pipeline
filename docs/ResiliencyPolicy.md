# ResiliencyPolicy

A policy object that tracks execution metrics and provides resiliency features for operations in .NET applications. It records success and failure events, calculates success rates, and exposes snapshots of its state for monitoring and diagnostics.

## API

### `Id`
A unique identifier for the policy instance. Read-only.

### `Name`
A human-readable name for the policy. Read-only.

### `IsEnabled`
Indicates whether the policy is currently active. When set to `false`, the policy will not record executions or update statistics.

### `CreatedAt`
The timestamp when the policy was instantiated. Read-only.

### `ModifiedAt`
The timestamp when the policy was last modified. Updated on every call to `RecordSuccess`, `RecordFailure`, or `ResetStatistics`.

### `TotalExecutions`
The total number of executions tracked by the policy.

### `SuccessfulExecutions`
The number of successful executions tracked by the policy.

### `FailedExecutions`
The number of failed executions tracked by the policy.

### `Tags`
A list of string tags associated with the policy for categorization or filtering.

### `Metadata`
A dictionary of key-value pairs containing additional metadata about the policy.

### `RecordSuccess()`
Increments the successful execution counter and updates `ModifiedAt`.

### `RecordFailure()`
Increments the failed execution counter and updates `ModifiedAt`.

### `GetSuccessRate()`
Computes and returns the success rate as a value between `0.0` and `1.0`. Returns `0.0` if no executions have occurred.

### `ResetStatistics()`
Resets all execution counters (`TotalExecutions`, `SuccessfulExecutions`, `FailedExecutions`) to zero and updates `ModifiedAt`.

### `GetSnapshot()`
Returns a `PolicySnapshot` object capturing the current state of the policy, including all statistics and metadata.

### `PolicyId`
A unique identifier for the policy type or template. Read-only.

### `PolicyName`
The name of the policy type or template. Read-only.

### `PolicyType`
The category or kind of resiliency policy (e.g., retry, circuit breaker). Read-only.

## Usage
