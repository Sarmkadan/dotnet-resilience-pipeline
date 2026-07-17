// ... existing content ...

## ResiliencyHelperTests

The `ResiliencyHelperTests` class provides unit tests for the `ResiliencyHelper` class, verifying its functionality in determining pipeline health status and validating/exorting resiliency policies. These tests cover various scenarios, including success rates, policy configurations, and validation errors.

Here's a realistic usage example based on its public members:

```csharp
using DotNetResiliencePipeline.Utilities;
using Xunit;

public class ResiliencyHelperUsage
{
    public void RunExamples()
    {
        // Verifies: DeterminePipelineHealth_SuccessRateAbove95_ReturnsHealthy
        var health = ResiliencyHelper.DeterminePipelineHealth(97);
        health.Should().Be(HealthStatus.Healthy);

        // Verifies: ExportPolicyConfig_CircuitBreaker_ContainsAllBaseFields
        var policy = new CircuitBreakerPolicy("export-cb") { IsEnabled = true };
        var config = ResiliencyHelper.ExportPolicyConfig(policy);
        config.Should().ContainKey("Id");
        config.Should().ContainKey("Name");
        config.Should().ContainKey("Type");
        config.Should().ContainKey("IsEnabled");
        config.Should().ContainKey("CreatedAt");
        config.Should().ContainKey("ModifiedAt");
        config.Should().ContainKey("Tags");
        config.Should().ContainKey("Metadata");
        config["Name"].Should().Be("export-cb");
        config["Type"].Should().Be("CircuitBreakerPolicy");
        config["IsEnabled"].Should().Be(true);

        // Verifies: ValidatePolicy_ValidCircuitBreaker_ReturnsEmptyErrors
        var errors = ResiliencyHelper.ValidatePolicy(policy);
        errors.Should().BeEmpty();
    }
}
```

// ... rest of existing content
