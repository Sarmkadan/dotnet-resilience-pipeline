# TimeoutServiceTests
The `TimeoutServiceTests` class is designed to test the functionality of the `TimeoutService` class, which is responsible for managing timeouts and handling operations that may exceed a specified time limit. This class provides a comprehensive set of tests to ensure that the `TimeoutService` behaves correctly under various scenarios, including successful operations, timed-out operations, and operations that throw exceptions.

## API
The `TimeoutServiceTests` class contains the following public members:
* `ExecuteAsync_WithNullPolicy_ThrowsArgumentNullException`: Tests that an `ArgumentNullException` is thrown when the policy is null.
* `ExecuteAsync_WithInvalidPolicy_ThrowsInvalidPolicyConfigurationException`: Tests that an `InvalidPolicyConfigurationException` is thrown when the policy is invalid.
* `ExecuteAsync_WithDisabledPolicy_BypassesTimeout`: Tests that the timeout is bypassed when the policy is disabled.
* `ExecuteAsync_WithSuccessfulOperation_RecordsMetrics`: Tests that metrics are recorded when the operation is successful.
* `ExecuteAsync_WithOperationThatTimesOut_ThrowsOperationTimeoutException`: Tests that an `OperationTimeoutException` is thrown when the operation times out.
* `ExecuteAsync_WithExternalCancellation_Rethrows`: Tests that the external cancellation is re-thrown.
* `ExecuteAsync_WithOperationException_RecordsFailure`: Tests that the failure is recorded when an operation exception occurs.
* `ExecuteAsync_RecordsExecutionTime`: Tests that the execution time is recorded.
* `HasExceededTimeout_WithNullPolicy_ReturnsFalse`: Tests that `false` is returned when the policy is null.
* `HasExceededTimeout_WithTimeExceedingTimeout_ReturnsTrue`: Tests that `true` is returned when the time exceeds the timeout.
* `HasExceededTimeout_WithTimeWithinTimeout_ReturnsFalse`: Tests that `false` is returned when the time is within the timeout.
* `GetTimeoutMilliseconds_WithNullPolicy_ReturnsZero`: Tests that 0 is returned when the policy is null.
* `GetTimeoutMilliseconds_ReturnsTimeoutInMs`: Tests that the timeout in milliseconds is returned.
* `GetTimeoutMilliseconds_HandlesFractionalSeconds`: Tests that fractional seconds are handled correctly.

## Usage
Here are two examples of using the `TimeoutServiceTests` class:
```csharp
// Example 1: Testing a successful operation
var timeoutService = new TimeoutService();
var result = await timeoutService.ExecuteAsync(async () =>
{
    // Simulate a successful operation
    await Task.Delay(100);
    return "Success";
});
Assert.AreEqual("Success", result);

// Example 2: Testing an operation that times out
var timeoutService = new TimeoutService();
try
{
    await timeoutService.ExecuteAsync(async () =>
    {
        // Simulate an operation that times out
        await Task.Delay(2000);
        return "Timeout";
    }, TimeSpan.FromMilliseconds(100));
}
catch (OperationTimeoutException ex)
{
    Assert.IsNotNull(ex);
}
```

## Notes
The `TimeoutServiceTests` class is designed to be thread-safe, and all tests are executed asynchronously to ensure that the `TimeoutService` behaves correctly under concurrent scenarios. However, it is essential to note that the `TimeoutService` may not be suitable for all types of operations, especially those that require precise timing or have specific cancellation requirements. Additionally, the `TimeoutService` may throw exceptions if the policy is invalid or if the operation times out, and it is crucial to handle these exceptions properly to ensure that the application remains stable and functional.
