# BulkheadServiceTests

Unit tests for the `BulkheadService` class, verifying correct behavior of bulkhead policy integration including slot acquisition, request queuing, and utilization tracking. The test suite ensures proper delegation to the underlying bulkhead policy and validates edge cases such as null policies and disabled configurations.

## API

### `TryAcquireSlot_WithNullPolicy_ThrowsArgumentNullException`
Ensures that calling `TryAcquireSlot` with a null bulkhead policy throws an `ArgumentNullException`. Validates input validation at the service boundary.

### `TryAcquireSlot_WithDisabledPolicy_ReturnsTrue`
Verifies that when the bulkhead policy is disabled, `TryAcquireSlot` immediately returns `true`, indicating that no slot acquisition is required and the operation can proceed without throttling.

### `TryAcquireSlot_WithEnabledPolicy_DelegatesToPolicy`
Confirms that `TryAcquireSlot` correctly delegates slot acquisition logic to the underlying bulkhead policy when the policy is enabled. The test does not assert specific policy behavior but ensures the call path is correct.

### `ReleaseSlot_WithNullPolicy_ThrowsArgumentNullException`
Validates that invoking `ReleaseSlot` with a null bulkhead policy throws an `ArgumentNullException`, enforcing defensive programming at the service interface.

### `ReleaseSlot_CallsPolicyReleaseSlot`
Ensures that `ReleaseSlot` properly invokes the `ReleaseSlot` method on the underlying bulkhead policy, confirming correct delegation of resource release.

### `DequeueRequest_WithNullPolicy_ThrowsArgumentNullException`
Tests that `DequeueRequest` throws an `ArgumentNullException` when the bulkhead policy is null, maintaining consistent input validation across all public methods.

### `DequeueRequest_CallsPolicyDequeueRequest`
Verifies that `DequeueRequest` correctly delegates to the `DequeueRequest` method of the underlying bulkhead policy, ensuring queued request handling is properly offloaded.

### `RecordQueueWaitTime_WithNullPolicy_ThrowsArgumentNullException`
Confirms that `RecordQueueWaitTime` throws an `ArgumentNullException` when the bulkhead policy is null, preserving input validation consistency.

### `RecordQueueWaitTime_CallsPolicyRecordQueueWaitTime`
Ensures that `RecordQueueWaitTime` delegates the timing recording operation to the corresponding method on the bulkhead policy, validating the integration point.

### `GetUtilizationPercentage_WithNullPolicy_ReturnsZero`
Verifies that when the bulkhead policy is null, `GetUtilizationPercentage` returns `0`, providing a safe default rather than throwing an exception for metrics queries.

### `GetUtilizationPercentage_DelegatesToPolicy`
Confirms that `GetUtilizationPercentage` delegates the utilization calculation to the underlying bulkhead policy when the policy is available, ensuring accurate metric reporting.

### `GetActiveExecutionCount_WithNullPolicy_ReturnsZero`
Ensures that `GetActiveExecutionCount` returns `0` when the bulkhead policy is null, avoiding null reference exceptions during monitoring operations.

### `GetActiveExecutionCount_ReturnsActiveExecutions`
Validates that `GetActiveExecutionCount` returns the current count of active executions as reported by the bulkhead policy, confirming accurate concurrency tracking.

### `GetQueuedRequestCount_WithNullPolicy_ReturnsZero`
Verifies that `GetQueuedRequestCount` returns `0` when the bulkhead policy is null, preventing null reference exceptions during queue monitoring.

### `GetQueuedRequestCount_ReturnsQueuedRequests`
Confirms that `GetQueuedRequestCount` returns the number of queued requests as reported by the bulkhead policy, ensuring accurate queue depth tracking.

### `IsValidConfiguration_WithNullPolicy_ReturnsFalse`
Ensures that `IsValidConfiguration` returns `false` when the bulkhead policy is null, treating missing configuration as invalid for safety.

### `IsValidConfiguration_DelegatesToPolicy`
Verifies that `IsValidConfiguration` delegates the validation logic to the underlying bulkhead policy when available, ensuring consistent configuration checks.

### `IsValidConfiguration_WithValidPolicy_ReturnsTrue`
Confirms that `IsValidConfiguration` returns `true` when the bulkhead policy is valid and properly initialized, validating successful configuration scenarios.

## Usage

### Example 1: Validating Bulkhead Configuration
