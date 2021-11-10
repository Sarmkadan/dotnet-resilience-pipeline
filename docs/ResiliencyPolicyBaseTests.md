# ResiliencyPolicyBaseTests

This test class contains unit tests that verify the behavior of the base resiliency policy implementation in the `dotnet-resilience-pipeline` library. The tests focus on constructor validation, statistics recording, success‑rate calculation, state reset, property defaults, snapshot generation, identifier uniqueness, timestamp updates, policy validation warnings, anti‑pattern detection, and optimization suggestions.

## API

### Constructor_WithNullName_ThrowsArgumentException
- **Purpose:** Confirms that constructing a policy with a `null` name throws an `ArgumentException`.
- **Parameters:** None.
- **Return Value:** None (the test method returns `void`).
- **Throws:** The test passes if the constructor throws an `ArgumentException`; otherwise the test fails.

### Constructor_WithEmptyName_ThrowsArgumentException
- **Purpose:** Confirms that constructing a policy with an empty string name throws an `ArgumentException`.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** The test passes if the constructor throws an `ArgumentException`; otherwise the test fails.

### RecordSuccess_IncrementsBothTotals
- **Purpose:** Verifies that calling `RecordSuccess` increments both the success counter and the total execution counter.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test asserts the expected counter values after the call.

### RecordFailure_IncrementsFailureAndTotals
- **Purpose:** Verifies that calling `RecordFailure` increments the failure counter and the total execution counter.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test asserts the expected counter values after the call.

### GetSuccessRate_NoExecutions_ReturnsZero
- **Purpose:** Ensures that `GetSuccessRate` returns `0` when no executions have been recorded.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test checks the return value equals `0`.

### GetSuccessRate_MixedExecutions_CalculatesCorrectly
- **Purpose:** Validates that `GetSuccessRate` computes the correct percentage based on recorded successes and failures.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test compares the returned rate to the expected value.

### ResetStatistics_ClearsAllCounters
- **Purpose:** Checks that invoking `ResetStatistics` sets success, failure, and total counters back to zero.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test asserts all counters are zero after reset.

### IsEnabled_DefaultsToTrue
- **Purpose:** Confirms that the `IsEnabled` property defaults to `true` for a newly created policy instance.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test asserts the property value.

### Tags_DefaultsToEmpty
- **Purpose:** Ensures that the `Tags` collection is empty by default.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test asserts the collection has zero entries.

### Metadata_DefaultsToEmpty
- **Purpose:** Ensures that the `Metadata` dictionary is empty by default.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test asserts the dictionary has zero entries.

### GetSnapshot_PopulatesAllBaseFields
- **Purpose:** Verifies that the snapshot returned by `GetSnapshot` contains correctly populated base fields (Id, Name, IsEnabled, Tags, Metadata, counters, and timestamps).
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test asserts each field matches the expected value.

### Id_IsUniquePerInstance
- **Purpose:** Confirms that each policy instance receives a unique identifier.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test creates two instances and asserts their `Id` values differ.

### ModifiedAt_UpdatesAfterRecordSuccess
- **Purpose:** Ensures that the `ModifiedAt` timestamp is updated after a call to `RecordSuccess`.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test records a timestamp before and after the call and asserts the later timestamp is greater.

### ValidatePolicy_CircuitBreakerWithHighFailureThreshold_AddsWarning
- **Purpose:** Checks that validating a circuit breaker policy with an excessively high failure threshold produces a warning.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test inspects the validation result for the expected warning message.

### ValidatePolicy_CircuitBreakerWithVeryShortOpenDuration_AddsWarning
- **Purpose:** Checks that validating a circuit breaker policy with an extremely short open duration produces a warning.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test inspects the validation result for the expected warning message.

### ValidatePolicy_BulkheadWithLargeQueue_AddsWarning
- **Purpose:** Checks that validating a bulkhead policy with an overly large queue size produces a warning.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test inspects the validation result for the expected warning message.

### IdentifyAntiPatterns_DisabledPolicy_ReturnsAntiPattern
- **Purpose:** Verifies that a disabled policy is flagged as an anti‑pattern by the anti‑pattern detection logic.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test asserts the detection result contains the expected anti‑pattern identifier.

### IdentifyAntiPatterns_ManyRetriesWithExponential_ReturnsAntiPattern
- **Purpose:** Verifies that a retry policy configured with many attempts and exponential backoff is flagged as an anti‑pattern.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test asserts the detection result contains the expected anti‑pattern identifier.

### SuggestOptimizations_BulkheadWithZeroQueue_AddsQueueSuggestion
- **Purpose:** Ensures that a bulkhead policy with a queue size of zero triggers a suggestion to add a queue.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test checks that the optimization suggestion list contains the expected queue‑related recommendation.

### SuggestOptimizations_RetryBelowThreeAttempts_AddsRetryCountSuggestion
- **Purpose:** Ensures that a retry policy configured with fewer than three attempts triggers a suggestion to increase the retry count.
- **Parameters:** None.
- **Return Value:** None.
- **Throws:** None; the test checks that the optimization suggestion list contains the expected retry‑count recommendation.

## Usage

The following examples illustrate how a developer might write similar unit tests for a custom resiliency policy that inherits from the base class.

```csharp
using Xunit;
using DotNetResiliencePipeline.Policies;

public class MyPolicyTests : ResiliencyPolicyBaseTests
{
    [Fact]
    public void Constructor_WithValidName_Succeeds()
    {
        // Arrange & Act
        var policy = new MyResiliencyPolicy("valid-name");

        // Assert
        Assert.NotNull(policy);
        Assert.Equal("valid-name", policy.Name);
    }

    [Fact]
    public void RecordSuccess_ThenGetSuccessRate_ReturnsExpectedValue()
    {
        // Arrange
        var policy = new MyResiliencyPolicy("test-policy");
        policy.RecordSuccess();
        policy.RecordSuccess();
        policy.RecordFailure();

        // Act
        var rate = policy.GetSuccessRate();

        // Assert
        Assert.Equal(2d / 3d, rate); // 2 successes out of 3 total executions
    }
}
```

A second example shows how to verify that validation warnings are produced for misconfigured policies:

```csharp
using System.Collections.Generic;
using DotNetResiliencePipeline.Policies;
using DotNetResiliencePipeline.Validation;

public class ValidationTests
{
    [Fact]
    public void ValidatePolicy_DetectsHighFailureThreshold()
    {
        // Arrange
        var options = new CircuitBreakerOptions
        {
            FailureThreshold = 0.95, // unusually high
            SamplingDuration = TimeSpan.FromMinutes(1),
            MinimumThroughput = 10,
            OpenDuration = TimeSpan.FromSeconds(30)
        };
        var policy = new CircuitBreakerPolicy(options);

        // Act
        IList<string> warnings = PolicyValidator.Validate(policy);

        // Assert
        Assert.Contains(warnings, w => w.Contains("FailureThreshold") && w.Contains("high"));
    }
}
```

## Notes

- The test methods assume that the class under test follows the base contract defined by `ResiliencyPolicyBase`. Deviations (e.g., overridden counters that are not updated by `RecordSuccess`/`RecordFailure`) will cause the corresponding tests to fail.
- All test methods are thread‑safe when executed in isolation because they operate on freshly instantiated policy objects. Running the same test method concurrently on the same instance is not supported and may produce unpredictable results.
- `GetSuccessRate` returns a `double` in the range `[0,1]`. Implementations should guard against division by zero; the test `GetSuccessRate_NoExecutions_ReturnsZero` validates this behavior.
- The `Id` property is expected to be generated once per instance and remain immutable; the test `Id_IsUniquePerInstance` relies on this guarantee.
- `ModifiedAt` is updated only by methods that record an outcome (`RecordSuccess`, `RecordFailure`). Directly modifying internal counters bypasses this update and is not tested.
- Validation and anti‑pattern detection mechanisms are extensible; the tests shown verify specific warning messages but do not exhaustively cover all possible policy configurations. Developers should add additional test cases when extending validation rules.
