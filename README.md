// ... existing content ...

## BulkheadServiceTests

The `BulkheadServiceTests` class contains comprehensive unit tests for the `BulkheadService` class, verifying its behavior in various scenarios, including slot acquisition, release, queue operations, and configuration validation.

Here's a realistic usage example based on its real public members:

```csharp
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Tests;

class Program
{
    static void Main()
    {
        var tests = new BulkheadServiceTests();

        // Slot acquisition tests
        tests.TryAcquireSlot_WithNullPolicy_ThrowsArgumentNullException();
        tests.TryAcquireSlot_WithDisabledPolicy_ReturnsTrue();
        tests.TryAcquireSlot_WithEnabledPolicy_DelegatesToPolicy();

        // Slot release tests
        tests.ReleaseSlot_WithNullPolicy_ThrowsArgumentNullException();
        tests.ReleaseSlot_CallsPolicyReleaseSlot();

        // Queue operations tests
        tests.DequeueRequest_WithNullPolicy_ThrowsArgumentNullException();
        tests.DequeueRequest_CallsPolicyDequeueRequest();

        // Queue wait time tests
        tests.RecordQueueWaitTime_WithNullPolicy_ThrowsArgumentNullException();
        tests.RecordQueueWaitTime_CallsPolicyRecordQueueWaitTime();

        // Utilization and counts tests
        tests.GetUtilizationPercentage_WithNullPolicy_ReturnsZero();
        tests.GetUtilizationPercentage_DelegatesToPolicy();
        tests.GetActiveExecutionCount_WithNullPolicy_ReturnsZero();
        tests.GetActiveExecutionCount_ReturnsActiveExecutions();
        tests.GetQueuedRequestCount_WithNullPolicy_ReturnsZero();
        tests.GetQueuedRequestCount_ReturnsQueuedRequests();

        // Configuration validation tests
        tests.IsValidConfiguration_WithNullPolicy_ReturnsFalse();
        tests.IsValidConfiguration_DelegatesToPolicy();
        tests.IsValidConfiguration_WithValidPolicy_ReturnsTrue();
    }
}
```

This example demonstrates how to invoke the public test methods of the `BulkheadServiceTests` class programmatically, showcasing the expected usage patterns of the underlying `BulkheadService` behavior.

```csharp
// ... rest of existing content
