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