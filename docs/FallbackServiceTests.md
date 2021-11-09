# FallbackServiceTests

The `FallbackServiceTests` class contains unit tests for the fallback service component of the `dotnet-resilience-pipeline` library. It validates the behavior of the fallback policy under various conditions, including policy configuration errors, disabled policies, successful and failed fallback actions, metrics recording, timeout handling, and typed fallback results. The tests ensure that the fallback service correctly delegates to the underlying policy, throws appropriate exceptions for invalid states, and records telemetry as expected.

## API

### `ExecuteAsync_WithNullPolicy_ThrowsArgumentNullException`
Tests that executing a fallback with a `null` policy throws an `ArgumentNullException`.

### `ExecuteAsync_WithInvalidPolicy_ThrowsInvalidPolicyConfigurationException`
Tests that executing a fallback with an improperly configured policy throws an `InvalidPolicyConfigurationException`.

### `ExecuteAsync_WithDisabledPolicy_ReturnsPrimaryFailure`
Verifies that when the fallback policy is disabled, the primary operation’s failure is returned without attempting a fallback.

### `ExecuteAsync_WhenFallbackNotTriggered_ReturnsPrimaryFailure`
Ensures that if the fallback condition is not met, the primary failure result is returned as-is.

### `ExecuteAsync_WithSuccessfulFallback_ReturnsFallbackResult`
Confirms that when the fallback action succeeds, its result is returned to the caller.

### `ExecuteAsync_WithFailedFallback_ThrowsFallbackFailedException`
Validates that if the fallback action itself fails, a `FallbackFailedException` is thrown.

### `ExecuteAsync_WithoutFallbackActionSet_RethrowsPrimaryException`
Tests that when no fallback action is configured, the original exception from the primary operation is rethrown.

### `ExecuteAsync_RecordsSuccessfulFallbackMetrics`
Asserts that a successful fallback execution increments the appropriate success metric counters.

### `ExecuteAsync_RecordsFailedFallbackMetrics`
Asserts that a failed fallback execution increments the appropriate failure metric counters.

### `ShouldTriggerFallback_WithNullPolicy_ReturnsFalse`
Tests that `ShouldTriggerFallback` returns `false` when the policy is `null`.

### `ShouldTriggerFallback_DelegatesToPolicy`
Verifies that `ShouldTriggerFallback` correctly delegates the decision to the underlying policy.

### `GetFallbackSuccessRate_WithNullPolicy_ReturnsZero`
Confirms that `GetFallbackSuccessRate` returns `0` when the policy is `null`.

### `GetFallbackSuccessRate_DelegatesToPolicy`
Ensures that `GetFallbackSuccessRate` retrieves the success rate from the underlying policy.

### `AddFallbackTrigger_DelegatesToPolicy`
Tests that `AddFallbackTrigger` correctly registers a trigger condition with the underlying policy.

### `RemoveFallbackTrigger_DelegatesToPolicy`
Tests that `RemoveFallbackTrigger` correctly unregisters a trigger condition from the underlying policy.

### `ExecuteAsync_WithFallbackTimeout_ThrowsOnTimeout`
Verifies that if the fallback action exceeds the configured timeout, a timeout exception is thrown.

### `ExecuteAsync_WithTypedFallback_ReturnsCorrectType`
Confirms that when a typed fallback is used, the result is of the expected type and contains the correct data.

## Usage

The following examples demonstrate how to configure and use the fallback service in a typical resilience pipeline.

**Example 1: Basic fallback with a default value**

```csharp
var fallbackPolicy = new FallbackPolicy<int>
{
    FallbackAction = () => Task.FromResult(42),
    ShouldHandle = ex => ex is HttpRequestException
};

var service = new FallbackService<int>(fallbackPolicy);

int result = await service.ExecuteAsync(async () =>
{
    // Primary operation that may throw HttpRequestException
    return await FetchDataFromRemoteAsync();
});

// If the primary operation throws HttpRequestException, result will be 42.
```

**Example 2: Fallback with metrics and timeout**

```csharp
var fallbackPolicy = new FallbackPolicy<string>
{
    FallbackAction = async ct =>
    {
        await Task.Delay(TimeSpan.FromMilliseconds(100), ct);
        return "cached value";
    },
    ShouldHandle = ex => ex is TimeoutException,
    Timeout = TimeSpan.FromSeconds(2)
};

var service = new FallbackService<string>(fallbackPolicy);

try
{
    string data = await service.ExecuteAsync(async () =>
    {
        // Primary operation that may time out
        return await QueryDatabaseAsync();
    });
    Console.WriteLine($"Result: {data}");
}
catch (FallbackFailedException ex)
{
    // Both primary and fallback failed
    Console.WriteLine($"Fallback failed: {ex.Message}");
}
```

## Notes

- All test methods that accept a policy parameter validate that `null` policies are handled gracefully by throwing `ArgumentNullException` or returning a safe default.
- The `ExecuteAsync_WithDisabledPolicy_ReturnsPrimaryFailure` test confirms that a disabled policy does not attempt fallback, which is critical for feature flag scenarios.
- The `ExecuteAsync_WithFallbackTimeout_ThrowsOnTimeout` test ensures that fallback actions respect the configured timeout and do not hang indefinitely.
- The metrics tests (`ExecuteAsync_RecordsSuccessfulFallbackMetrics`, `ExecuteAsync_RecordsFailedFallbackMetrics`) rely on a mock metrics sink and verify that counters are incremented exactly once per fallback attempt.
- The `ShouldTriggerFallback`, `GetFallbackSuccessRate`, `AddFallbackTrigger`, and `RemoveFallbackTrigger` methods are synchronous and delegate directly to the underlying policy; they do not perform any additional validation beyond null checks.
- Thread safety: The fallback service itself is designed to be thread-safe for concurrent calls. The test methods are written as asynchronous unit tests and can be run in parallel without interference, provided the underlying policy implementations are also thread-safe.
