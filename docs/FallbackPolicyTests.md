# FallbackPolicyTests
The `FallbackPolicyTests` class is designed to test the functionality of the fallback policy in the resilience pipeline. It provides a comprehensive set of test cases to ensure that the fallback policy behaves as expected under various scenarios, including successful and failed fallbacks, different exception types, and configuration validation.

## API
* `Constructor_WithValidName_Succeeds`: Verifies that the constructor succeeds when a valid name is provided.
* `Constructor_WithWhitespaceName_ThrowsArgumentException`: Verifies that the constructor throws an `ArgumentException` when a name containing only whitespace is provided.
* `SetFallbackAction_WithValidFunc_StoresAction`: Verifies that the fallback action is stored successfully when a valid function is provided.
* `ShouldTriggerFallback_WithFallbackOnAnyException_ReturnsTrue`: Verifies that the fallback is triggered when any exception occurs and the fallback policy is configured to trigger on any exception.
* `ShouldTriggerFallback_WithNullException_ReturnsFalse`: Verifies that the fallback is not triggered when a null exception is provided.
* `ShouldTriggerFallback_WithSpecificExceptionAndMatch_ReturnsTrue`: Verifies that the fallback is triggered when a specific exception occurs and the fallback policy is configured to trigger on that exception.
* `ShouldTriggerFallback_WithSpecificExceptionNoMatch_ReturnsFalse`: Verifies that the fallback is not triggered when a specific exception occurs and the fallback policy is not configured to trigger on that exception.
* `RecordSuccessfulFallback_WithNegativeTime_ThrowsArgumentException`: Verifies that an `ArgumentException` is thrown when attempting to record a successful fallback with a negative time.
* `RecordSuccessfulFallback_IncrementCounters`: Verifies that the successful fallback counters are incremented correctly.
* `RecordFailedFallback_WithNegativeTime_ThrowsArgumentException`: Verifies that an `ArgumentException` is thrown when attempting to record a failed fallback with a negative time.
* `RecordFailedFallback_IncrementCounters`: Verifies that the failed fallback counters are incremented correctly.
* `GetFallbackSuccessRate_WithMixedResults_CalculatesCorrectly`: Verifies that the fallback success rate is calculated correctly when there are both successful and failed fallbacks.
* `GetFallbackSuccessRate_WithNoInvocations_ReturnsZero`: Verifies that the fallback success rate returns zero when there are no invocations.
* `GetFallbackInvocationPercentage_CalculatesCorrectly`: Verifies that the fallback invocation percentage is calculated correctly.
* `AddFallbackTrigger_WithNullType_ThrowsArgumentNullException`: Verifies that an `ArgumentNullException` is thrown when attempting to add a null exception type as a fallback trigger.
* `AddFallbackTrigger_WithNonExceptionType_ThrowsArgumentException`: Verifies that an `ArgumentException` is thrown when attempting to add a non-exception type as a fallback trigger.
* `AddFallbackTrigger_WithValidException_Succeeds`: Verifies that a valid exception type can be added as a fallback trigger.
* `AddFallbackTrigger_WithDuplicateType_DoesNotAddTwice`: Verifies that adding a duplicate exception type as a fallback trigger does not result in duplicate entries.
* `RemoveFallbackTrigger_RemovesExceptionType`: Verifies that removing a fallback trigger successfully removes the exception type.
* `IsValidConfiguration_WithZeroTimeout_ReturnsFalse`: Verifies that a configuration with a zero timeout is considered invalid.

## Usage
The following examples demonstrate how to use the `FallbackPolicyTests` class:
```csharp
// Example 1: Testing fallback policy with a valid name
var policy = new FallbackPolicy("MyPolicy");
policy.SetFallbackAction(() => Console.WriteLine("Fallback executed"));
Assert.IsTrue(policy.ShouldTriggerFallback(new Exception()));

// Example 2: Testing fallback policy with a specific exception type
var policy2 = new FallbackPolicy("MyPolicy2");
policy2.AddFallbackTrigger(typeof(TimeoutException));
Assert.IsTrue(policy2.ShouldTriggerFallback(new TimeoutException()));
```

## Notes
When using the `FallbackPolicyTests` class, note that the `RecordSuccessfulFallback` and `RecordFailedFallback` methods will throw an `ArgumentException` if a negative time is provided. Additionally, the `AddFallbackTrigger` method will throw an `ArgumentNullException` if a null exception type is provided, and an `ArgumentException` if a non-exception type is provided. The `RemoveFallbackTrigger` method will successfully remove the exception type from the fallback triggers. The `IsValidConfiguration` method will return false for a configuration with a zero timeout. The `FallbackPolicyTests` class is designed to be thread-safe, but it is still important to ensure that the fallback policy is properly synchronized when used in a multi-threaded environment.
