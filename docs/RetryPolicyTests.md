# RetryPolicyTests

Unit tests for `RetryPolicy` that validate delay calculation strategies and configuration validation logic. The test class ensures that retry policies behave as expected under fixed and exponential backoff strategies, including proper handling of edge cases such as invalid retry counts or misconfigured delays.

## API

### `CalculateDelay_FixedStrategy_ReturnsSameDelayForEveryAttempt`

Validates that the fixed delay strategy returns a consistent delay duration for every retry attempt. This test ensures deterministic behavior when using a fixed backoff policy.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: No exceptions expected under normal conditions.

### `CalculateDelay_ExponentialStrategy_DelayGrowsWithEachAttempt`

Ensures that the exponential backoff strategy increases the delay duration with each retry attempt. This test confirms that the delay grows according to the exponential function applied.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: No exceptions expected under normal conditions.

### `CalculateDelay_AttemptEqualToMaxRetries_ThrowsArgumentOutOfRangeException`

Verifies that attempting to calculate a delay for an attempt number equal to `MaxRetries` throws an `ArgumentOutOfRangeException`. This test ensures that retry policies enforce strict boundaries on the number of allowed attempts.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: `ArgumentOutOfRangeException` when the attempt number equals `MaxRetries`.

### `IsValidConfiguration_WhenMaxDelayIsLessThanInitialDelay_ReturnsFalseWithError`

Checks that a retry configuration is considered invalid when the `MaxDelay` is less than the `InitialDelay`. This test ensures that configuration validation enforces logical ordering of delay parameters.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: No exceptions expected; the method under test returns a boolean and error message.

### `IsRetryable_NullException_ReturnsFalse`

Confirms that the retry policy correctly identifies a `null` exception as non-retryable. This test ensures that retry logic handles edge cases involving null inputs gracefully.

- **Parameters**: None
- **Return value**: `void`
- **Throws**: No exceptions expected; the method under test returns a boolean.

## Usage
