# BulkheadPolicyTests

Unit tests for the `BulkheadPolicy` class, verifying behavior related to concurrency limits, queueing, and statistics tracking. These tests ensure that the bulkhead policy correctly enforces maximum parallel executions, handles queue overflow, and maintains accurate metrics under concurrent access.

## API

### `Constructor_WithValidName_Succeeds`
Validates that a `BulkheadPolicy` can be constructed with a non-empty, non-whitespace name without throwing exceptions.

### `Constructor_WithWhitespaceName_ThrowsArgumentException`
Ensures that constructing a `BulkheadPolicy` with a whitespace-only name throws an `ArgumentException`.

### `TryAcquireSlot_WhenBelowMaxParallelization_ReturnsTrue`
Verifies that `TryAcquireSlot` returns `true` when the number of active executions is below the configured maximum parallelization.

### `TryAcquireSlot_WhenAtMaxParallelization_ReturnsFalseAndQueues`
Checks that `TryAcquireSlot` returns `false` when the maximum parallelization is reached, and that the request is queued.

### `TryAcquireSlot_WhenQueueFull_ReturnsFalseAndIncrementsRejectedCount`
Confirms that `TryAcquireSlot` returns `false` and increments the rejected count when the queue is full.

### `ReleaseSlot_DecreasesActiveExecutions`
Ensures that calling `ReleaseSlot` reduces the count of active executions.

### `ReleaseSlot_WhenNoActiveExecutions_DoesNotGoNegative`
Validates that `ReleaseSlot` does not decrement the active execution count below zero.

### `DequeueRequest_DecreasesQueuedRequests`
Checks that `DequeueRequest` reduces the count of queued requests.

### `RecordQueueWaitTime_WithNegativeTime_ThrowsArgumentException`
Ensures that `RecordQueueWaitTime` throws an `ArgumentException` when provided with a negative time value.

### `RecordQueueWaitTime_UpdatesStatistics`
Verifies that `RecordQueueWaitTime` correctly updates the queue wait time statistics.

### `GetUtilizationPercentage_CalculatesCorrectly`
Validates that `GetUtilizationPercentage` returns the correct utilization percentage based on active executions and maximum parallelization.

### `GetQueuedPercentage_CalculatesCorrectly`
Ensures that `GetQueuedPercentage` returns the correct percentage of queued requests relative to the queue capacity.

### `GetRejectionPercentage_CalculatesCorrectly`
Checks that `GetRejectionPercentage` returns the correct percentage of rejected requests relative to total requests.

### `IsValidConfiguration_WithZeroMaxParallelization_ReturnsFalse`
Verifies that `IsValidConfiguration` returns `false` when `MaxParallelization` is set to zero.

### `IsValidConfiguration_WithNegativeQueueLength_ReturnsFalse`
Ensures that `IsValidConfiguration` returns `false` when `QueueLength` is negative.

### `IsValidConfiguration_WithValidSettings_ReturnsTrue`
Validates that `IsValidConfiguration` returns `true` when both `MaxParallelization` and `QueueLength` are within valid ranges.

### `ResetStatistics_ClearsAllMetrics`
Confirms that `ResetStatistics` clears all tracked metrics (e.g., active executions, queue wait times, rejections).

### `ThreadSafety_ConcurrentAcquisitions_AllSucceed`
Ensures that concurrent calls to `TryAcquireSlot` and `ReleaseSlot` operate correctly without race conditions or deadlocks.

## Usage

### Example 1: Basic Bulkhead Usage
