# TimeoutPolicyTests
The `TimeoutPolicyTests` class is a test suite designed to validate the functionality of the `TimeoutPolicy` class, which is part of the resilience pipeline in .NET applications. This test suite covers various scenarios to ensure the `TimeoutPolicy` behaves correctly under different conditions, such as valid and invalid configurations, timeout occurrences, and statistical calculations.

## API
The `TimeoutPolicyTests` class contains several test methods that cover the following scenarios:
- `Constructor_WithValidName_Succeeds`: Verifies that the constructor succeeds when given a valid name.
- `Constructor_WithWhitespaceName_ThrowsArgumentException`: Tests that the constructor throws an `ArgumentException` when the name contains only whitespace.
- `IsTimedOut_WithExecutionTimeLessThanTimeout_ReturnsFalse`: Checks that `IsTimedOut` returns `false` when the execution time is less than the timeout.
- `IsTimedOut_WithExecutionTimeGreaterThanTimeout_ReturnsTrue`: Verifies that `IsTimedOut` returns `true` when the execution time exceeds the timeout.
- `IsTimedOut_WithExecutionTimeEqualToTimeout_ReturnsFalse`: Confirms that `IsTimedOut` returns `false` when the execution time equals the timeout.
- `IsTimedOutMs_WithTimeGreaterThanTimeout_ReturnsTrue`: Tests that `IsTimedOutMs` returns `true` when the time in milliseconds is greater than the timeout.
- `IsTimedOutMs_WithTimeLessThanTimeout_ReturnsFalse`: Checks that `IsTimedOutMs` returns `false` when the time in milliseconds is less than the timeout.
- `RecordExecutionTime_WithNegativeTime_ThrowsArgumentException`: Verifies that `RecordExecutionTime` throws an `ArgumentException` when given a negative time.
- `RecordExecutionTime_UpdatesStatistics`: Tests that `RecordExecutionTime` correctly updates the execution time statistics.
- `RecordTimeout_IncreasesTimeoutCountAndRecordsFailure`: Confirms that `RecordTimeout` increases the timeout count and records the failure.
- `RecordTimeout_StoresLastTimeoutTime`: Checks that `RecordTimeout` stores the last timeout time.
- `GetTimeoutPercentage_CalculatesCorrectly`: Verifies that `GetTimeoutPercentage` calculates the timeout percentage correctly.
- `GetTimeoutPercentage_WithNoExecutions_ReturnsZero`: Tests that `GetTimeoutPercentage` returns zero when there are no executions.
- `GetPercentile95ExecutionTime_CalculatesCorrectly`: Confirms that `GetPercentile95ExecutionTime` calculates the 95th percentile execution time correctly.
- `GetPercentile99ExecutionTime_CalculatesCorrectly`: Checks that `GetPercentile99ExecutionTime` calculates the 99th percentile execution time correctly.
- `GetPercentileExecutionTime_WithSmallSample_ReturnsSensibleValue`: Verifies that `GetPercentileExecutionTime` returns a sensible value for a small sample.
- `IsValidConfiguration_WithZeroTimeout_ReturnsFalse`: Tests that `IsValidConfiguration` returns `false` for a zero timeout.
- `IsValidConfiguration_WithNegativeTimeout_ReturnsFalse`: Confirms that `IsValidConfiguration` returns `false` for a negative timeout.
- `IsValidConfiguration_WithValidTimeout_ReturnsTrue`: Checks that `IsValidConfiguration` returns `true` for a valid timeout.
- `ResetStatistics_ClearsAllMetrics`: Verifies that `ResetStatistics` clears all metrics.

## Usage
Here are two examples of using the `TimeoutPolicyTests` class in a C# application:
```csharp
// Example 1: Testing the constructor with a valid name
[TestMethod]
public void TestConstructorValidName()
{
    var policy = new TimeoutPolicy("TestPolicy");
    Assert.IsNotNull(policy);
}

// Example 2: Testing the IsTimedOut method
[TestMethod]
public void TestIsTimedOut()
{
    var policy = new TimeoutPolicy("TestPolicy", 1000); // 1 second timeout
    var executionTime = 500; // less than the timeout
    var isTimedOut = policy.IsTimedOut(executionTime);
    Assert.IsFalse(isTimedOut);
}
```

## Notes
When using the `TimeoutPolicyTests` class, consider the following edge cases and thread-safety remarks:
- The `TimeoutPolicy` class is designed to be thread-safe, allowing multiple threads to access and update its metrics concurrently.
- When the timeout is set to zero or a negative value, the `IsValidConfiguration` method returns `false`, indicating an invalid configuration.
- The `GetTimeoutPercentage` and percentile methods may return zero or a default value if there are no executions or the sample size is too small.
- The `RecordExecutionTime` and `RecordTimeout` methods update the internal metrics, which can be reset using the `ResetStatistics` method.
- The `IsTimedOut` and `IsTimedOutMs` methods rely on the configured timeout value, so ensure it is set correctly before using these methods.
