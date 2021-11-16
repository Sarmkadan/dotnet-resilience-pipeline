# ResiliencyHelperTests

The `ResiliencyHelperTests` class serves as the comprehensive unit test suite for the `ResiliencyHelper` utility within the `dotnet-resilience-pipeline` project. It validates the core logic responsible for assessing pipeline health based on success rate thresholds, exporting policy configurations to structured formats, and enforcing strict validation rules for various resilience policies including Circuit Breaker, Retry, Timeout, Bulkhead, and Fallback strategies. This class ensures that health status determinations are accurate across defined percentage ranges and that invalid policy configurations are correctly identified and rejected with appropriate exceptions or error collections.

## API

### Health Determination Methods

*   **`public void DeterminePipelineHealth_SuccessRateAbove95_ReturnsHealthy`**
    Verifies that when the calculated success rate of a pipeline exceeds 95%, the system correctly classifies the health status as "Healthy". This method takes no parameters and asserts the expected state change or return value within the test context.

*   **`public void DeterminePipelineHealth_SuccessRateBetween80And94_ReturnsDegraded`**
    Confirms that a success rate falling inclusively between 80% and 94% results in a "Degraded" health status. No parameters are exposed; the test internally simulates the specific success rate scenario.

*   **`public void DeterminePipelineHealth_SuccessRateBetween50And79_ReturnsUnhealthy`**
    Validates that success rates between 50% and 79% trigger an "Unhealthy" status classification. This method ensures the threshold logic correctly identifies significant performance degradation without reaching critical failure.

*   **`public void DeterminePipelineHealth_SuccessRateBelow50_ReturnsCritical`**
    Ensures that any success rate below 50% is classified as "Critical". This test case verifies the lowest health tier logic, indicating severe pipeline instability.

### Configuration Export Methods

*   **`public void ExportPolicyConfig_CircuitBreaker_ContainsAllBaseFields`**
    Tests the export functionality for Circuit Breaker policies, asserting that the resulting configuration object or dictionary includes all mandatory base fields required for serialization or display.

*   **`public void ExportPolicyConfig_WithTags_IncludesTags`**
    Verifies that when a policy includes metadata tags, the `ExportPolicyConfig` method correctly serializes or includes these tags in the output configuration.

*   **`public void ExportPolicyConfig_NullPolicy_ThrowsArgumentNullException`**
    Ensures that passing a `null` policy instance to the export method results in an `ArgumentNullException`. This guards against null reference errors during configuration extraction.

### Policy Validation Methods

*   **`public void ValidatePolicy_NullPolicy_ThrowsArgumentNullException`**
    Confirms that the validation logic immediately throws an `ArgumentNullException` when the input policy object is `null`, preventing further processing of invalid references.

*   **`public void ValidatePolicy_ValidCircuitBreaker_ReturnsEmptyErrors`**
    Validates that a correctly configured Circuit Breaker policy (with valid threshold and duration values) passes validation, resulting in an empty collection of error messages.

*   **`public void ValidatePolicy_CircuitBreakerWithZeroThreshold_ThrowsErrors`**
    Checks that a Circuit Breaker policy with a failure threshold of zero is flagged as invalid, returning specific error messages indicating the configuration violation.

*   **`public void ValidatePolicy_CircuitBreakerWithZeroOpenDuration_ThrowsErrors`**
    Ensures that a Circuit Breaker policy defined with a zero-duration open state is rejected during validation, producing appropriate error details.

*   **`public void ValidatePolicy_ValidRetryPolicy_ReturnsEmptyErrors`**
    Asserts that a Retry policy meeting all standard constraints (e.g., positive retry count, valid delay strategy) yields no validation errors.

*   **`public void ValidatePolicy_InvalidRetryPolicy_ReturnsErrors`**
    Verifies that a Retry policy with invalid parameters (such as negative retry counts or malformed delays) generates a non-empty list of validation errors.

*   **`public void ValidatePolicy_ValidTimeoutPolicy_ReturnsEmptyErrors`**
    Confirms that a Timeout policy with a positive, non-zero timeout value passes validation successfully.

*   **`public void ValidatePolicy_InvalidTimeoutPolicy_ReturnsErrors`**
    Ensures that a Timeout policy with an invalid duration (e.g., zero or negative timespan) is caught by the validator and returns descriptive errors.

*   **`public void ValidatePolicy_ValidBulkheadPolicy_ReturnsEmptyErrors`**
    Tests that a Bulkhead policy with a valid maximum parallelization count passes validation without errors.

*   **`public void ValidatePolicy_BulkheadWithZeroParallelization_ReturnsErrors`**
    Validates that a Bulkhead policy configured with a maximum parallelization limit of zero is considered invalid and returns specific error messages.

*   **`public void ValidatePolicy_ValidFallbackPolicy_ReturnsEmptyErrors`**
    Asserts that a properly configured Fallback policy, including a valid action delegate, passes all validation checks.

## Usage

The following examples demonstrate how the logic covered by `ResiliencyHelperTests` is typically consumed in a real-world testing scenario using xUnit.

### Example 1: Validating Policy Configuration
This example illustrates how to validate a Circuit Breaker policy, ensuring that invalid configurations (like a zero threshold) are caught before the pipeline is built.

```csharp
using Xunit;
using System.Collections.Generic;

public class PolicyValidationScenario
{
    [Fact]
    public void Validate_CircuitBreakerConfiguration()
    {
        // Arrange
        var invalidPolicy = new CircuitBreakerPolicy 
        { 
            FailureThreshold = 0, // Invalid: must be > 0
            OpenDuration = TimeSpan.FromSeconds(30) 
        };

        // Act
        var errors = ResiliencyHelper.ValidatePolicy(invalidPolicy);

        // Assert
        // Based on ValidatePolicy_CircuitBreakerWithZeroThreshold_ReturnsErrors
        Assert.NotEmpty(errors);
        Assert.Contains(errors, e => e.Contains("threshold"));
    }
}
```

### Example 2: Determining Pipeline Health Status
This example shows how to determine the health status of a running pipeline based on its current success rate, covering the logic verified by the health determination tests.

```csharp
using Xunit;

public class HealthMonitoringScenario
{
    [Theory]
    [InlineData(96.5, HealthStatus.Healthy)]
    [InlineData(85.0, HealthStatus.Degraded)]
    [InlineData(60.0, HealthStatus.Unhealthy)]
    [InlineData(40.0, HealthStatus.Critical)]
    public void Assess_PipelineHealth_BasedOnSuccessRate(double successRate, HealthStatus expectedStatus)
    {
        // Arrange
        var metrics = new PipelineMetrics { SuccessRate = successRate };

        // Act
        var actualStatus = ResiliencyHelper.DeterminePipelineHealth(metrics);

        // Assert
        // Covers logic from all four DeterminePipelineHealth_* tests
        Assert.Equal(expectedStatus, actualStatus);
    }
}
```

## Notes

*   **Threshold Boundaries**: The health determination logic relies on strict boundary conditions. Success rates exactly at 95%, 80%, or 50% should be carefully reviewed against the implementation to ensure they fall into the intended category (e.g., whether 95% is "Healthy" or "Degraded"). The tests explicitly cover ranges "Above 95", "Between 80 and 94", "Between 50 and 79", and "Below 50".
*   **Null Safety**: Both `ExportPolicyConfig` and `ValidatePolicy` methods enforce strict null checking. Callers must ensure policy instances are instantiated before invocation to avoid `ArgumentNullException`.
*   **Validation Granularity**: The validation methods do not throw exceptions for logical errors (e.g., zero threshold); instead, they return a collection of error strings. Exceptions are reserved strictly for null references.
*   **Thread Safety**: As this class represents a suite of unit tests verifying stateless helper methods, the underlying `ResiliencyHelper` methods being tested should be assumed thread-safe if they operate only on input parameters without modifying static state. However, the test methods themselves are designed to run in isolation and are not intended for concurrent execution within the same test context instance.
*   **Zero Value Constraints**: A recurring validation rule across multiple policy types (Circuit Breaker, Bulkhead, Timeout) is the rejection of zero values for critical duration or count parameters, as these would render the policy non-functional.
