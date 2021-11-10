# RetryServiceTests

Unit tests for the `RetryService` class, verifying retry behavior, delay calculations, and exception handling across various scenarios including transient failures, non-retryable exceptions, cancellation, and policy configuration.

## API

### `ExecuteAsync_OperationSucceeds_ReturnsValueWithoutRetrying`
Ensures that when an operation completes successfully on the first attempt, the retry mechanism does not perform any additional retries and returns the expected value immediately.

- **Parameters**: None
- **Return value**: `Task<T>` where `T` is the result of the successful operation
- **Throws**: None

### `ExecuteAsync_TransientFailureThenSuccess_RetriesAndReturnsValue`
Validates that the retry mechanism correctly handles transient failures by retrying the operation until success, returning the final result after the last successful attempt.

- **Parameters**: None
- **Return value**: `Task<T>` where `T` is the result of the successful operation
- **Throws**: None

### `ExecuteAsync_AllAttemptsExhausted_ThrowsMaxRetriesExceededException`
Confirms that when all retry attempts are exhausted due to repeated transient failures, the service throws a `MaxRetriesExceededException` to indicate the operation could not be completed.

- **Parameters**: None
- **Return value**: `Task` (void)
- **Throws**: `MaxRetriesExceededException`

### `ExecuteAsync_NonRetryableException_ThrowsImmediatelyWithoutRetrying`
Tests that non-retryable exceptions (e.g., `ArgumentNullException`) are not retried and are thrown immediately without invoking further attempts.

- **Parameters**: None
- **Return value**: `Task` (void)
- **Throws**: The original non-retryable exception

### `ExecuteAsync_NullPolicy_ThrowsArgumentNullException`
Verifies that passing a `null` retry policy to the service results in an `ArgumentNullException` being thrown immediately.

- **Parameters**: None
- **Return value**: `Task` (void)
- **Throws**: `ArgumentNullException`

### `ExecuteAsync_InvalidPolicyConfiguration_ThrowsInvalidPolicyConfigurationException`
Ensures that invalid retry policy configurations (e.g., negative retry counts) are detected and result in an `InvalidPolicyConfigurationException`.

- **Parameters**: None
- **Return value**: `Task` (void)
- **Throws**: `InvalidPolicyConfigurationException`

### `ExecuteAsync_CancellationRequested_StopsRetrying`
Checks that when a cancellation is requested during retry attempts, the service stops retrying and propagates the cancellation without further attempts.

- **Parameters**: None
- **Return value**: `Task` (void)
- **Throws**: `OperationCanceledException`

### `ExecuteAsync_DisabledPolicy_ExecutesOnce`
Validates that when the retry policy is disabled, the operation executes exactly once without any retry attempts, regardless of outcome.

- **Parameters**: None
- **Return value**: `Task<T>` where `T` is the result of the operation
- **Throws**: None

### `CalculateRetryDelay_DelegatesToPolicy`
Ensures that the `CalculateRetryDelay` method correctly delegates the delay calculation to the underlying retry policy and returns the expected delay value.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: None

### `CalculateRetryDelay_NullPolicy_ThrowsArgumentNullException`
Confirms that invoking `CalculateRetryDelay` with a `null` policy results in an `ArgumentNullException`.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: `ArgumentNullException`

### `IsRetryable_WithMatchingException_ReturnsTrue`
Tests that the `IsRetryable` method returns `true` when the provided exception matches the retryable exception type defined by the policy.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: None

### `IsRetryable_WithNonMatchingException_ReturnsFalse`
Ensures that the `IsRetryable` method returns `false` when the provided exception does not match the retryable exception type.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: None

### `IsRetryable_NullPolicy_ReturnsFalse`
Validates that when a `null` policy is provided, the `IsRetryable` method returns `false` instead of throwing an exception.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: None

## Usage

### Example 1: Basic Retry with Transient Failure
