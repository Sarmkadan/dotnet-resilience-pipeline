// ... existing content ...

## FailureInjectionServiceTests

The `FailureInjectionServiceTests` class provides a comprehensive set of unit tests for the `FailureInjectionService` class, verifying its functionality for injecting failures into operations. These tests cover various scenarios including rule registration, exception injection, latency injection, rule disabling, and removal.

Here's an example usage based on its real public members:

```csharp
// Create a failure injection service
var failureInjectionService = new FailureInjectionService();

// Add a rule with 100% exception injection rate
failureInjectionService.AddRule(new InjectionRule
{
    Key = "exception-rule",
    Type = InjectionType.Exception,
    InjectionRate = 1.0,
    ExceptionMessage = "Injected exception"
});

// Execute a test operation
try
{
    await failureInjectionService.ExecuteAsync("exception-rule", _ => Task.FromResult(0));
}
catch (InjectedFaultException ex)
{
    Console.WriteLine($"Caught injected exception: {ex.Message}");
}

// Add a rule with 100% latency injection rate
failureInjectionService.AddRule(new InjectionRule
{
    Key = "latency-rule",
    Type = InjectionType.Latency,
    InjectionRate = 1.0,
    LatencyDelay = TimeSpan.FromSeconds(1)
});

// Execute a test operation with latency injection
var sw = Stopwatch.StartNew();
var result = await failureInjectionService.ExecuteAsync("latency-rule", _ => Task.Delay(100));
sw.Stop();
Console.WriteLine($"Execution time: {sw.ElapsedMilliseconds}ms");

// Disable a rule
failureInjectionService.AddRule(new InjectionRule
{
    Key = "disabled-rule",
    Type = InjectionType.Exception,
    InjectionRate = 1.0,
    IsEnabled = false
});

// Execute a test operation with disabled rule
var resultDisabled = await failureInjectionService.ExecuteAsync("disabled-rule", _ => Task.FromResult(0));
Console.WriteLine($"Result with disabled rule: {resultDisabled}");

// Remove a rule
var removed = failureInjectionService.RemoveRule("exception-rule");
Console.WriteLine($"Rule removed: {removed}");
```

## PolicyNameGeneratorTests

The `PolicyNameGeneratorTests` class contains unit tests for the `PolicyNameGenerator` utility, ensuring that policy names are generated, validated, and managed correctly across a variety of scenarios.

Below is a realistic usage example that demonstrates the public members exercised by the tests:

```csharp
using DotNetResiliencePipeline.Utilities;

var generator = new PolicyNameGenerator();

// Generate names for different policy types
var circuitBreakerName = generator.GenerateName("payment", "circuitbreaker");
var retryName = generator.GenerateName("order", "retry");
var timeoutName = generator.GenerateName("catalog", "timeout");
var bulkheadName = generator.GenerateName("inventory", "bulkhead");
var fallbackName = generator.GenerateName("shipping", "fallback");

// Ensure uniqueness for the same service and type
var first = generator.GenerateName("svc", "retry");
var second = generator.GenerateName("svc", "retry");
Console.WriteLine(first); // e.g., svc-retry-1
Console.WriteLine(second); // e.g., svc-retry-2

// Normalize service names with special characters
var normalized = generator.GenerateName("Payment Service", "retry");
Console.WriteLine(normalized); // e.g., payment-service-retry-1

// Use a custom number
var custom = generator.GenerateName("svc", "timeout", customNumber: 42);
Console.WriteLine(custom); // svc-timeout-42

// Build a descriptive name with a purpose
var descriptive = generator.GenerateDescriptiveName("payment", "retry", "network");
Console.WriteLine(descriptive); // payment-network-retry

// Build a descriptive name without a purpose
var simple = generator.GenerateDescriptiveName("payment", "timeout");
Console.WriteLine(simple); // payment-timeout

// Validate generated names
bool isValid = generator.IsValidPolicyName(circuitBreakerName);
Console.WriteLine(isValid); // true

// Suggest a name based on service, operation, and scenario
var suggested = generator.SuggestName("payment", "charge", "network-error");
Console.WriteLine(suggested); // payment-charge-network-error

// Register a name to reserve it
generator.RegisterName("svc-retry-1");

// Attempt to generate the same name (will be avoided)
var avoided = generator.GenerateName("svc", "retry", customNumber: 1);
Console.WriteLine(avoided); // not "svc-retry-1"

// Unregister the name so it can be reused
generator.UnregisterName("svc-retry-1");

// List all registered names
var allRegistered = generator.GetAllRegisteredNames();
Console.WriteLine(string.Join(", ", allRegistered));

// Clear all registrations and reset counters
generator.Clear();
var freshName = generator.GenerateName("svc", "retry");
Console.WriteLine(freshName); // svc-retry-1
```
