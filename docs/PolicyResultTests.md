# PolicyResultTests

The `PolicyResultTests` class serves as the comprehensive test suite for validating the behavior, state transitions, and callback execution logic of the `PolicyResult` type within the resilience pipeline. It ensures that success and failure scenarios correctly populate metadata, trigger appropriate event handlers, and maintain data integrity regarding execution context, timestamps, and attempt counts.

## API

### `Success_SetsIsSuccessTrueWithData`
Verifies that when a policy execution completes successfully, the `IsSuccess` property is set to `true` and the resulting data payload is correctly stored within the result object. This method does not accept parameters and does not return a value; it throws an assertion exception if the state does not match the expected success criteria.

### `Failure_SetsIsSuccessFalseWithException`
Validates that a failed policy execution sets the `IsSuccess` property to `false` and correctly captures the originating exception. It ensures no data payload is present on failure. This method takes no parameters, returns void, and throws an assertion exception if the failure state or exception mapping is incorrect.

### `Fallback_SetsIsSuccessTrueAndFallbackMetadata`
Confirms that when a fallback policy is engaged, the final result is marked as successful (`IsSuccess` is `true`) and specific metadata indicating a fallback occurrence is attached to the result. It does not accept parameters or return values, throwing only on assertion failures regarding the metadata presence.

### `OnSuccess_CalledWhenSuccess`
Ensures that the `OnSuccess` callback delegate is invoked exactly once when the policy execution yields a successful result. This test method takes no arguments, returns void, and fails if the callback invocation count differs from the expectation.

### `OnSuccess_NotCalledWhenFailure`
Verifies that the `OnSuccess` callback is strictly not invoked when the policy execution results in a failure. It validates the isolation of success handlers from failure paths. No parameters are accepted, and it throws an assertion error if the callback was triggered.

### `OnFailure_CalledWhenFailure`
Validates that the `OnFailure` callback delegate is executed when the policy execution fails. It ensures the exception context is passed correctly to the handler. This method accepts no parameters, returns void, and throws if the callback is not invoked.

### `OnFailure_NotCalledWhenSuccess`
Confirms that the `OnFailure` callback is never executed during a successful policy run. This ensures that failure handling logic remains dormant during normal operation. It takes no parameters and throws an assertion exception if the callback was erroneously called.

### `Map_OnSuccess_TransformsData`
Tests the `Map` functionality to ensure that when applied to a successful result, the provided transformation function correctly converts the underlying data type while preserving the success state. It accepts no external parameters, returns void, and throws if the transformed data does not match the expected output.

### `Map_OnFailure_PropagatesFailure`
Verifies that applying the `Map` function to a failed result propagates the failure state without attempting to transform data or altering the original exception. It ensures the mapping logic is short-circuited on failure. No parameters are accepted, and it throws on state mismatch.

### `Success_HasUniqueExecutionId`
Asserts that every successful policy result is assigned a unique execution identifier (`ExecutionId`), distinguishing it from other execution instances. This method validates the uniqueness constraint across multiple invocations. It takes no parameters and throws if duplicate IDs are detected.

### `Success_ExecutedAtIsRecentUtc`
Validates that the `ExecutedAt` timestamp recorded on a successful result is in Coordinated Universal Time (UTC) and falls within an acceptable recent time window relative to the test execution. It ensures temporal accuracy and timezone compliance. No parameters are accepted.

### `Failure_DefaultAttemptCountIsOne`
Confirms that a policy result representing a single immediate failure records an `AttemptCount` of exactly one. This establishes the baseline metric for retry logic evaluation. It accepts no parameters, returns void, and throws if the count differs from one.

## Usage

### Validating Success State and Callbacks
The following example demonstrates how the test suite validates that a successful execution sets the correct state and triggers the `OnSuccess` handler while ignoring the `OnFailure` handler.

```csharp
[Test]
public void Validate_SuccessFlow()
{
    // Arrange
    var result = PolicyResult.Success<int>(42);
    var successCalled = false;
    var failureCalled = false;

    // Act
    result.OnSuccess(_ => successCalled = true);
    result.OnFailure(_ => failureCalled = true);

    // Assert
    Assert.IsTrue(result.IsSuccess);
    Assert.AreEqual(42, result.Data);
    Assert.IsTrue(successCalled, "OnSuccess callback should be invoked");
    Assert.IsFalse(failureCalled, "OnFailure callback should not be invoked");
}
```

### Verifying Failure Propagation and Mapping
This example illustrates testing the behavior of a failed result, ensuring the exception is preserved, the attempt count is correct, and data mapping operations do not alter the failure state.

```csharp
[Test]
public void Validate_FailurePropagation()
{
    // Arrange
    var exception = new InvalidOperationException("Test failure");
    var result = PolicyResult.Failure<int>(exception);
    
    // Act
    var mappedResult = result.Map(data => data * 2);

    // Assert
    Assert.IsFalse(result.IsSuccess);
    Assert.AreEqual(1, result.AttemptCount);
    Assert.AreSame(exception, result.Exception);
    Assert.IsFalse(mappedResult.IsSuccess, "Map should propagate failure state");
    Assert.AreSame(exception, mappedResult.Exception, "Original exception must be preserved");
}
```

## Notes

*   **Thread Safety**: The test methods themselves are designed to be executed in isolation. While the underlying `PolicyResult` type may be immutable, the callbacks (`OnSuccess`, `OnFailure`) captured within these tests often rely on mutable local state (e.g., boolean flags). Care must be taken not to share these callback instances across concurrent test threads without proper synchronization, although the test runner typically isolates test cases.
*   **Time Sensitivity**: The `Success_ExecutedAtIsRecentUtc` test relies on the system clock. In environments with significant clock skew or heavy load causing delays between object creation and assertion, the "recent" time window logic may require adjustment to avoid false negatives.
*   **Execution ID Uniqueness**: The `Success_HasUniqueExecutionId` test assumes a sufficiently large entropy source for ID generation. If the test suite is run in rapid succession in a constrained environment, ensure the ID generation strategy does not rely solely on low-resolution timestamps which could theoretically collide.
*   **Exception Handling**: These tests verify that exceptions are captured, not re-thrown during the creation of the `PolicyResult`. However, if the callbacks provided to `OnSuccess` or `OnFailure` throw internally, that behavior is outside the scope of the `PolicyResult` state validation and may cause the test runner to report an unexpected error rather than an assertion failure.
