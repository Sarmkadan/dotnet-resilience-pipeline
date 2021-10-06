# TimeoutPolicy
The `TimeoutPolicy` class is designed to track and manage timeouts in a system, providing insights into execution times and timeout frequencies. It allows developers to monitor and analyze the performance of their applications, making it easier to identify and address potential issues related to timeouts.

## API
### Constructors
* `public TimeoutPolicy(string name)`: Initializes a new instance of the `TimeoutPolicy` class with the specified name.

### Properties
* `public TimeSpan Timeout`: Gets the timeout value.
* `public long TimeoutCount`: Gets the number of timeouts that have occurred.
* `public double AverageExecutionTimeMs`: Gets the average execution time in milliseconds.
* `public long LongestExecutionTimeMs`: Gets the longest execution time in milliseconds.
* `public long ShortestExecutionTimeMs`: Gets the shortest execution time in milliseconds.
* `public bool IsTimedOut`: Gets a value indicating whether the policy is currently timed out.
* `public bool IsTimedOutMs`: Gets a value indicating whether the policy is currently timed out in milliseconds.

### Methods
* `public void RecordExecutionTime()`: Records the execution time.
* `public void RecordTimeout()`: Records a timeout.
* `public double GetTimeoutPercentage()`: Gets the percentage of timeouts.
* `public long GetPercentile95ExecutionTime()`: Gets the 95th percentile execution time.
* `public long GetPercentile99ExecutionTime()`: Gets the 99th percentile execution time.
* `public bool IsValidConfiguration()`: Checks if the configuration is valid.
* `public override void ResetStatistics()`: Resets the statistics.
* `public override PolicySnapshot GetSnapshot()`: Gets a snapshot of the policy.

## Usage
The following examples demonstrate how to use the `TimeoutPolicy` class:
```csharp
// Example 1: Creating and using a TimeoutPolicy instance
var policy = new TimeoutPolicy("MyPolicy");
policy.RecordExecutionTime();
Console.WriteLine(policy.AverageExecutionTimeMs);

// Example 2: Using the TimeoutPolicy to track timeouts
var policy2 = new TimeoutPolicy("MyPolicy2");
policy2.RecordTimeout();
Console.WriteLine(policy2.TimeoutCount);
```

## Notes
When using the `TimeoutPolicy` class, consider the following:
* The `Timeout` property is used to determine whether an execution is timed out.
* The `IsTimedOut` and `IsTimedOutMs` properties provide a convenient way to check if the policy is currently timed out.
* The `RecordExecutionTime` and `RecordTimeout` methods should be called from the same thread to ensure accurate statistics.
* The `GetTimeoutPercentage`, `GetPercentile95ExecutionTime`, and `GetPercentile99ExecutionTime` methods can be used to analyze the performance of the system.
* The `ResetStatistics` method can be used to reset the statistics, which can be useful for testing or debugging purposes.
* The `TimeoutPolicy` class is not thread-safe by default, so it's recommended to use synchronization mechanisms when accessing its members from multiple threads.
