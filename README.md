// ... existing content ...


## ResiliencyPolicyBaseTests

The `ResiliencyPolicyBaseTests` class provides comprehensive unit tests for the base functionality of resiliency policies, verifying their behavior in various scenarios.

Here's a realistic usage example based on its real public members:

```csharp
using DotNetResiliencePipeline.Domain.Policies;

// Create a circuit breaker policy with a failure threshold
var policy = new CircuitBreakerPolicy("base-test") { FailureThreshold = 99 };

// Record some successes and failures
policy.RecordSuccess();
policy.RecordFailure();

// Check the success rate
var successRate = policy.GetSuccessRate();
Console.WriteLine($"Success rate: {successRate * 100}%");

// Reset the policy statistics
policy.ResetStatistics();

// Verify the policy is still enabled
Console.WriteLine($"Is policy enabled: {policy.IsEnabled}");

// Check the policy tags
Console.WriteLine($"Policy tags: {string.Join(", ", policy.Tags)}");

// Get a snapshot of the policy
var snapshot = policy.GetSnapshot();
Console.WriteLine($"Policy snapshot: {snapshot.PolicyName}");
```

## BulkheadPolicyTests

The `BulkheadPolicyTests` class contains a thorough suite of unit tests that verify the behavior of the `BulkheadPolicy` implementation, covering construction, slot acquisition, queuing, rejection handling, statistics, configuration validation, and thread‑safety. These tests ensure the bulkhead isolation pattern works correctly under both normal and edge‑case scenarios.

Below is a realistic, compiling C# example that demonstrates how the public test methods can be invoked programmatically. This can be useful for custom test runners or for illustrating the expected usage patterns of the underlying `BulkheadPolicy`.

```csharp
using System;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Tests;

class Program
{
    static async Task Main()
    {
        var tests = new BulkheadPolicyTests();

        // Construction tests
        tests.Constructor_WithValidName_Succeeds();
        tests.Constructor_WithWhitespaceName_ThrowsArgumentException();

        // Slot acquisition and queuing tests
        tests.TryAcquireSlot_WhenBelowMaxParallelization_ReturnsTrue();
        tests.TryAcquireSlot_WhenAtMaxParallelization_ReturnsfalseAndQueues();
        tests.TryAcquireSlot_WhenQueueFull_ReturnsFalseAndIncrementsRejectedCount();

        // Release and dequeue tests
        tests.ReleaseSlot_DecreasesActiveExecutions();
        tests.ReleaseSlot_WhenNoActiveExecutions_DoesNotGoNegative();
        tests.DequeueRequest_DecreasesQueuedRequests();

        // Queue wait time tests
        tests.RecordQueueWaitTime_WithNegativeTime_ThrowsArgumentException();
        tests.RecordQueueWaitTime_UpdatesStatistics();

        // Utilization and percentage calculations
        tests.GetUtilizationPercentage_CalculatesCorrectly();
        tests.GetQueuedPercentage_CalculatesCorrectly();
        tests.GetRejectionPercentage_CalculatesCorrectly();

        // Configuration validation
        tests.IsValidConfiguration_WithZeroMaxParallelization_ReturnsFalse();
        tests.IsValidConfiguration_WithNegativeQueueLength_ReturnsFalse();
        tests.IsValidConfiguration_WithValidSettings_ReturnsTrue();

        // Statistics reset
        tests.ResetStatistics_ClearsAllMetrics();

        // Thread‑safety test (async)
        await tests.ThreadSafety_ConcurrentAcquisitions_AllSucceed();

        Console.WriteLine("All BulkheadPolicyTests executed successfully.");
    }
}
```

This example simply creates an instance of the test class and calls each public test method, mirroring how the test suite validates the `BulkheadPolicy` behavior. In a typical development workflow, the tests would be discovered and run automatically by the test runner (e.g., `dotnet test`), but the snippet shows that they can also be invoked directly if needed.