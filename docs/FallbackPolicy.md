# FallbackPolicy

A resilience policy that provides fallback behavior when the primary execution fails. It allows specifying fallback actions to be executed when the main operation throws exceptions, and tracks various metrics about fallback invocations and outcomes.

## API

### `SetFallbackAction<T>`
Configures a fallback action to be executed when the primary operation fails.
- **Type Parameters**:
  - `T`: The type of the fallback action delegate.
- **Parameters**: None.
- **Return Value**: None.
- **Exceptions**: Throws `ArgumentNullException` if the fallback action is `null`.

---

### `FallbackInvocationCount`
Gets the total number of times the fallback action was invoked.
- **Type**: `long`
- **Access**: Read-only.

---

### `SuccessfulFallbackCount`
Gets the number of times the fallback action executed successfully.
- **Type**: `long`
- **Access**: Read-only.

---
### `FailedFallbackCount`
Gets the number of times the fallback action failed.
- **Type**: `long`
- **Access**: Read-only.

---
### `FallbackTriggerExceptions`
Gets the list of exception types that trigger fallback behavior.
- **Type**: `List<Type>`
- **Access**: Read-only.

---
### `FallbackOnAnyException`
Gets or sets a value indicating whether fallback should trigger on any exception.
- **Type**: `bool`
- **Access**: Read-write.

---
### `FallbackTimeout`
Gets or sets the timeout duration for fallback execution.
- **Type**: `TimeSpan`
- **Access**: Read-write.

---
### `AverageFallbackExecutionTimeMs`
Gets the average execution time of the fallback action in milliseconds.
- **Type**: `double`
- **Access**: Read-only.

---
### `FallbackPolicy(string name)`
Initializes a new instance of the `FallbackPolicy` class.
- **Parameters**:
  - `name`: The name of the policy.
- **Exceptions**: Throws `ArgumentException` if `name` is `null` or empty.

---
### `ShouldTriggerFallback`
Determines whether the fallback should be triggered based on the current configuration and exception.
- **Parameters**:
  - `exception`: The exception that occurred during primary execution.
- **Return Value**: `bool` indicating whether fallback should be triggered.
- **Exceptions**: None.

---
### `RecordSuccessfulFallback`
Records a successful fallback execution.
- **Parameters**: None.
- **Return Value**: None.
- **Exceptions**: None.

---
### `RecordFailedFallback`
Records a failed fallback execution.
- **Parameters**: None.
- **Return Value**: None.
- **Exceptions**: None.

---
### `GetFallbackSuccessRate`
Calculates the success rate of fallback executions.
- **Parameters**: None.
- **Return Value**: `double` representing the success rate (0.0 to 1.0).
- **Exceptions**: None.

---
### `GetFallbackInvocationPercentage`
Calculates the percentage of total fallback invocations relative to the total policy executions.
- **Parameters**: None.
- **Return Value**: `double` representing the percentage (0.0 to 100.0).
- **Exceptions**: None.

---
### `AddFallbackTrigger`
Adds an exception type that should trigger fallback behavior.
- **Parameters**:
  - `exceptionType`: The type of exception to add.
- **Return Value**: None.
- **Exceptions**: Throws `ArgumentNullException` if `exceptionType` is `null`.

---
### `RemoveFallbackTrigger`
Removes an exception type from the fallback trigger list.
- **Parameters**:
  - `exceptionType`: The type of exception to remove.
- **Return Value**: None.
- **Exceptions**: None.

---
### `IsValidConfiguration`
Validates whether the current policy configuration is valid.
- **Parameters**: None.
- **Return Value**: `bool` indicating whether the configuration is valid.
- **Exceptions**: None.

---
### `ResetStatistics`
Resets all fallback-related statistics to zero.
- **Parameters**: None.
- **Return Value**: None.
- **Exceptions**: None.

---
### `GetSnapshot`
Captures a snapshot of the current policy state.
- **Parameters**: None.
- **Return Value**: `PolicySnapshot` containing the state of the policy.
- **Exceptions**: None.

## Usage

### Example 1: Basic Fallback with Timeout
