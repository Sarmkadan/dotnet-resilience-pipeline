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

## ResiliencyPipelineServiceTests

The `ResiliencyPipelineServiceTests` class provides unit tests for the `ResiliencyPipelineService` class, verifying its functionality for executing operations with resiliency policies. These tests cover policy registration, successful operation execution, failure scenarios, fallback policies, and execution statistics tracking.

Here's an example usage based on its real public members:

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

// Create a resiliency pipeline service
var pipelineService = new ResiliencyPipelineService();

// Register a retry policy
var retryPolicy = new RetryPolicy("order-processing-retry")
{
    MaxRetryCount = 3,
    DelayBetweenRetries = TimeSpan.FromSeconds(1)
};
pipelineService.RegisterPolicy(retryPolicy);

// Register a fallback policy
var fallbackPolicy = new FallbackPolicy("order-processing-fallback")
{
    IsEnabled = true
};
fallbackPolicy.SetFallbackAction(async _ => await Task.FromResult("fallback-order-id"));
fallbackPolicy.AddFallbackTrigger(typeof(InvalidOperationException));
pipelineService.RegisterPolicy(fallbackPolicy);

// Execute an operation that succeeds
var successResult = await pipelineService.ExecuteAsync(async _ => 
{
    Console.WriteLine("Executing successful operation...");
    return "order-123";
});

if (successResult.IsSuccess)
{
    Console.WriteLine($"Success: {successResult.Data}");
}

// Execute an operation that fails and uses fallback
var fallbackResult = await pipelineService.ExecuteAsync(
    async _ => await Task.FromException<string>(new InvalidOperationException("Payment failed")),
    fallback: fallbackPolicy
);

if (fallbackResult.IsSuccess)
{
    Console.WriteLine($"Fallback used: {fallbackResult.Data}");
    if (fallbackResult.Metadata.TryGetValue("FallbackUsed", out var fallbackUsed))
    {
        Console.WriteLine($"Fallback triggered: {fallbackUsed}");
    }
}

// Check execution statistics
var stats = pipelineService.GetStats();
Console.WriteLine($"Total executions: {stats.TotalExecutions}");
Console.WriteLine($"Successful executions: {stats.SuccessfulExecutions}");
Console.WriteLine($"Failed executions: {stats.FailedExecutions}");

// Get all registered policies
var policies = pipelineService.GetAllPolicies();
foreach (var policy in policies)
{
    Console.WriteLine($"Policy: {policy.Id} ({policy.GetType().Name})");
}
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
// Note: This method is not in the tests but is part of the public API
generator.UnregisterName("svc-retry-1");

// List all registered names
var allRegistered = generator.GetAllRegisteredNames();
Console.WriteLine(string.Join(", ", allRegistered));

// Clear all registrations and reset counters
generator.Clear();
var freshName = generator.GenerateName("svc", "retry");
Console.WriteLine(freshName); // svc-retry-1
```

## EndToEndWorkflowTests

The `EndToEndWorkflowTests` class provides comprehensive end-to-end integration tests that validate realistic multi-policy workflows and realistic usage patterns for the resiliency pipeline system. These tests cover realistic scenarios including retry policies that eventually succeed, circuit breakers that trip and recover, fallback mechanisms that provide alternative values, bulkhead concurrency limits, timeout policies, and complete pipeline configurations with statistics tracking.

Here's an example usage based on its real public members:

```csharp
using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

// Build a complete resilience pipeline with retry, circuit breaker, and fallback
var pipeline = new ResiliencyPipelineBuilder()
    .WithCircuitBreaker("order-cb", p =>
    {
        p.FailureThreshold = 3;
        p.OpenDuration = TimeSpan.FromSeconds(30);
    })
    .WithRetry("order-retry", p =>
    {
        p.MaxRetries = 2;
        p.InitialDelay = TimeSpan.FromMilliseconds(1);
        p.Strategy = RetryPolicy.BackoffStrategy.Fixed;
        p.UseJitter = false;
    })
    .WithFallback("order-fallback", p =>
    {
        p.FallbackOnAnyException = true;
        p.SetFallbackAction<string>(async ct => "fallback-order-data");
    })
    .Build();

// Verify the pipeline was configured correctly
var allPolicies = pipeline.GetAllPolicies();
Console.WriteLine($"Pipeline has {allPolicies.Count} policies configured");

var cbPolicy = pipeline.GetPolicyByName("order-cb") as CircuitBreakerPolicy;
var retryPolicy = pipeline.GetPolicyByName("order-retry") as RetryPolicy;
var fallbackPolicy = pipeline.GetPolicyByName("order-fallback") as FallbackPolicy;

Console.WriteLine($"Circuit breaker threshold: {cbPolicy?.FailureThreshold}");
Console.WriteLine($"Retry max attempts: {retryPolicy?.MaxRetries}");
Console.WriteLine($"Fallback enabled: {fallbackPolicy?.FallbackOnAnyException}");

// Execute a failing operation that will trigger retry, then circuit breaker, then fallback
var result = await pipeline.ExecuteAsync(async _ =>
{
    // Simulate a transient failure
    throw new TimeoutException("Payment service timeout");
});

if (result.IsSuccess)
{
    Console.WriteLine($"Operation succeeded with result: {result.Data}");
}
else if (result.Metadata.TryGetValue("FallbackUsed", out var fallbackUsed) && fallbackUsed is true)
{
    Console.WriteLine($"Fallback was used: {result.Data}");
}

// Test individual services with realistic scenarios

// 1. Retry service: transient failures that eventually succeed
var retryService = new RetryService();
var retryPolicy = new RetryPolicy("e2e-retry")
{
    MaxRetries = 4,
    InitialDelay = TimeSpan.FromMilliseconds(1),
    Strategy = RetryPolicy.BackoffStrategy.Fixed,
    UseJitter = false
};

int attempts = 0;
var retryResult = await retryService.ExecuteAsync<string>(
    retryPolicy,
    async _ =>
    {
        attempts++;
        if (attempts < 4)
            throw new TimeoutException("transient-failure");
        return "success-after-retries";
    },
    CancellationToken.None
);

Console.WriteLine($"Retry service: {attempts} attempts, result: {retryResult}");

// 2. Circuit breaker: failures that trip the circuit
var cbService = new CircuitBreakerService();
var cbPolicy = new CircuitBreakerPolicy("e2e-cb") { FailureThreshold = 3 };

for (int i = 0; i < 3; i++)
{
    try
    {
        await cbService.ExecuteAsync<string>(cbPolicy, _ => throw new InvalidOperationException("fail"));
    }
    catch (InvalidOperationException) { }
}

Console.WriteLine($"Circuit breaker state: {cbPolicy.CurrentState}");
Console.WriteLine($"Circuit breaker trips: {cbPolicy.CircuitBreakerTrips}");

// 3. Fallback service: alternative value when primary fails
var fallbackService = new FallbackService();
var fallbackPolicy = new FallbackPolicy("e2e-fallback") { FallbackOnAnyException = true };
fallbackPolicy.SetFallbackAction<string>(async ct => "default-response");

var primaryEx = new HttpRequestException("service unavailable");
var fallbackResult = await fallbackService.ExecuteAsync<string>(
    fallbackPolicy,
    primaryEx,
    200,
    CancellationToken.None
);

Console.WriteLine($"Fallback service: success={fallbackResult.IsSuccess}, data={fallbackResult.Data}");

// 4. Bulkhead: concurrency limiting
var bulkheadPolicy = new BulkheadPolicy("e2e-bulkhead")
{
    MaxParallelization = 3,
    MaxQueueLength = 10
};

var bulkheadService = new BulkheadService();
var acquired = new List<bool>();
for (int i = 0; i < 6; i++)
{
    acquired.Add(bulkheadService.TryAcquireSlot(bulkheadPolicy));
}

Console.WriteLine($"Bulkhead: {acquired.Count(x => x)} active slots, {bulkheadService.GetQueuedRequestCount(bulkheadPolicy)} queued");

// 5. Timeout: operations that complete within deadline
var timeoutService = new TimeoutService();
var timeoutPolicy = new TimeoutPolicy("e2e-timeout") { Timeout = TimeSpan.FromSeconds(5) };

var timeoutResult = await timeoutService.ExecuteAsync<string>(
    timeoutPolicy,
    async ct =>
    {
        await Task.Delay(10, ct);
        return "completed-in-time";
    }
);

Console.WriteLine($"Timeout service: {timeoutResult}");

// 6. Complete workflow: execute multiple policies and track statistics
var fullPipeline = new ResiliencyPipelineService();

var workflowCbPolicy = new CircuitBreakerPolicy("workflow-cb") { FailureThreshold = 10 };
var workflowRetryPolicy = new RetryPolicy("workflow-retry")
{
    MaxRetries = 2,
    InitialDelay = TimeSpan.FromMilliseconds(1),
    Strategy = RetryPolicy.BackoffStrategy.Fixed,
    UseJitter = false
};
var workflowTimeoutPolicy = new TimeoutPolicy("workflow-timeout") { Timeout = TimeSpan.FromSeconds(5) };

var timeoutStats = await timeoutService.ExecuteAsync<string>(workflowTimeoutPolicy, ct => Task.FromResult("step1"));
var cbStats = await cbService.ExecuteAsync<string>(workflowCbPolicy, _ => Task.FromResult("step2"));
var retryStats = await retryService.ExecuteAsync<string>(workflowRetryPolicy, _ => Task.FromResult("step3"), CancellationToken.None);

Console.WriteLine($"Workflow completed: timeout={timeoutStats}, cb={cbStats}, retry={retryStats}");
Console.WriteLine($"Timeout stats: {workflowTimeoutPolicy.SuccessfulExecutions} successes");
Console.WriteLine($"Circuit breaker stats: {workflowCbPolicy.SuccessfulExecutions} successes");
Console.WriteLine($"Retry stats: {workflowRetryPolicy.SuccessfulExecutions} successes");
```


