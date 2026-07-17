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

## ResiliencyPipelineIntegrationTests

The `ResiliencyPipelineIntegrationTests` class provides integration tests for the resiliency pipeline system, verifying the composition and interaction of multiple resilience policies within a complete pipeline. These tests validate that policies work together correctly, track execution statistics across the entire pipeline, and ensure proper error handling and fallback behavior.

Here's an example usage based on its real public members:

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

// Create a resiliency pipeline service
var pipelineService = new ResiliencyPipelineService();

// FullPipeline_WithMultiplePolicies_RegistersAllPolicies
// Register multiple policies to build a complete resilience pipeline
var circuitBreakerPolicy = new CircuitBreakerPolicy("payment-circuit-breaker")
{
    IsEnabled = true,
    FailureThreshold = 5,
    SamplingDuration = TimeSpan.FromSeconds(30),
    MinimumThroughput = 10,
    TimeToReset = TimeSpan.FromSeconds(60)
};
pipelineService.RegisterPolicy(circuitBreakerPolicy);

var retryPolicy = new RetryPolicy("payment-retry")
{
    IsEnabled = true,
    MaxRetryCount = 3,
    DelayBetweenRetries = TimeSpan.FromSeconds(1),
    BackoffType = RetryBackoffType.Exponential
};
pipelineService.RegisterPolicy(retryPolicy);

var timeoutPolicy = new TimeoutPolicy("payment-timeout")
{
    IsEnabled = true,
    TimeoutDuration = TimeSpan.FromSeconds(30)
};
pipelineService.RegisterPolicy(timeoutPolicy);

var bulkheadPolicy = new BulkheadPolicy("payment-bulkhead")
{
    IsEnabled = true,
    MaxParallelization = 10,
    MaxQueuedRequests = 50
};
pipelineService.RegisterPolicy(bulkheadPolicy);

var fallbackPolicy = new FallbackPolicy("payment-fallback")
{
    IsEnabled = true
};
pipelineService.RegisterPolicy(fallbackPolicy);

// FullPipeline_WithFallback_ConfiguresFallbackPolicy
// Configure fallback behavior with exception triggers
fallbackPolicy.SetFallbackAction(async _ => await Task.FromResult("fallback-payment-id"));
fallbackPolicy.AddFallbackTrigger(typeof(InvalidOperationException));
fallbackPolicy.AddFallbackTrigger(typeof(TimeoutException));

// BulkheadPolicy_WithMultipleSlots_LimitsParallelization
// Execute operations through the bulkhead to verify parallelization limits
var tasks = new List<Task>();
for (int i = 0; i < 15; i++)
{
    int taskId = i;
    tasks.Add(Task.Run(async () =>
    {
        try
        {
            var result = await pipelineService.ExecuteAsync(async _ =>
            {
                Console.WriteLine($"Executing task {taskId}...");
                await Task.Delay(100);
                return $"success-{taskId}";
            }, bulkhead: bulkheadPolicy);
            
            Console.WriteLine($"Task {taskId} completed: {result.Data}");
        }
        catch (BulkheadRejectedException ex)
        {
            Console.WriteLine($"Task {taskId} rejected: {ex.Message}");
        }
    }));
}

await Task.WhenAll(tasks);

// CircuitBreakerService_WithFailures_TracksFailureCount
// Execute operations that will trigger circuit breaker
for (int i = 0; i < 6; i++)
{
    try
    {
        await pipelineService.ExecuteAsync(async _ =>
        {
            if (i < 5)
                throw new InvalidOperationException("Payment processing failed");
            return "success";
        }, circuitBreaker: circuitBreakerPolicy);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Execution {i} failed: {ex.Message}");
    }
}

Console.WriteLine($"Circuit breaker trips: {circuitBreakerPolicy.CircuitBreakerTrips}");
Console.WriteLine($"Circuit state: {circuitBreakerPolicy.CurrentState}");

// PipelineService_TracksTotalExecutions
// Verify pipeline statistics tracking
var stats = pipelineService.GetStatistics();
Console.WriteLine($"Total executions: {stats.TotalExecutions}");
Console.WriteLine($"Successful executions: {stats.SuccessfulExecutions}");
Console.WriteLine($"Failed executions: {stats.FailedExecutions}");
Console.WriteLine($"Success rate: {stats.SuccessRate:P}");

// PipelineBuilder_FluentConfiguration_CreatesValidPipeline
// Build a pipeline with fluent configuration
var fluentPipeline = new ResiliencyPipelineService();
fluentPipeline.RegisterPolicy(new CircuitBreakerPolicy("fluent-circuit")
{
    IsEnabled = true,
    FailureThreshold = 3,
    SamplingDuration = TimeSpan.FromSeconds(10),
    MinimumThroughput = 5,
    TimeToReset = TimeSpan.FromSeconds(30)
});

fluentPipeline.RegisterPolicy(new RetryPolicy("fluent-retry")
{
    IsEnabled = true,
    MaxRetryCount = 2,
    DelayBetweenRetries = TimeSpan.FromMilliseconds(500)
});

// Execute through the fluent pipeline
var fluentResult = await fluentPipeline.ExecuteAsync(async _ =>
{
    Console.WriteLine("Executing fluent pipeline operation...");
    return "fluent-success";
});

if (fluentResult.IsSuccess)
{
    Console.WriteLine($"Fluent pipeline result: {fluentResult.Data}");
}

// CircuitBreakerOpenState_PreventsFurtherExecutions
// Verify circuit breaker prevents execution when open
circuitBreakerPolicy.RecordFailure(); // Force circuit to open
circuitBreakerPolicy.RecordFailure();
circuitBreakerPolicy.RecordFailure();
circuitBreakerPolicy.RecordFailure();
circuitBreakerPolicy.RecordFailure(); // Should trip the circuit

try
{
    await pipelineService.ExecuteAsync(async _ => "should-not-execute", 
        circuitBreaker: circuitBreakerPolicy);
}
catch (CircuitBreakerOpenException ex)
{
    Console.WriteLine($"Circuit breaker prevented execution: {ex.Message}");
}

// RetryWithBackoff_CalculatesExponentialDelay
// Execute with retry policy to verify exponential backoff
var exponentialRetry = new RetryPolicy("exponential-retry")
{
    IsEnabled = true,
    MaxRetryCount = 5,
    DelayBetweenRetries = TimeSpan.FromSeconds(1),
    BackoffType = RetryBackoffType.Exponential
};

pipelineService.RegisterPolicy(exponentialRetry);

var retryAttempts = 0;
var sw = Stopwatch.StartNew();

try
{
    await pipelineService.ExecuteAsync(async _ =>
    {
        retryAttempts++;
        throw new InvalidOperationException("Operation failed");
    }, retry: exponentialRetry);
}
catch { }

sw.Stop();
Console.WriteLine($"Retry attempts: {retryAttempts}");
Console.WriteLine($"Total retry time: {sw.ElapsedMilliseconds}ms");

// BulkheadWithQueueing_ManagesQueuedRequests
// Test bulkhead queue management
var queuedBulkhead = new BulkheadPolicy("queued-bulkhead")
{
    IsEnabled = true,
    MaxParallelization = 2,
    MaxQueuedRequests = 3
};

pipelineService.RegisterPolicy(queuedBulkhead);

var queueTasks = new List<Task>();
for (int i = 0; i < 7; i++)
{
    int taskId = i;
    queueTasks.Add(pipelineService.ExecuteAsync(async _ =>
    {
        Console.WriteLine($"Queued task {taskId} executing...");
        await Task.Delay(50);
        return $"queued-success-{taskId}";
    }, bulkhead: queuedBulkhead));
}

await Task.WhenAll(queueTasks);
Console.WriteLine($"Bulkhead active executions: {queuedBulkhead.ActiveExecutions}");
Console.WriteLine($"Bulkhead queued requests: {queuedBulkhead.QueuedRequests}");

// TimeoutPolicy_ConfiguresTimeout
// Test timeout policy configuration
var timeoutPolicy = new TimeoutPolicy("strict-timeout")
{
    IsEnabled = true,
    TimeoutDuration = TimeSpan.FromMilliseconds(100)
};

pipelineService.RegisterPolicy(timeoutPolicy);

try
{
    await pipelineService.ExecuteAsync(async _ =>
    {
        await Task.Delay(200); // Exceeds timeout
        return "should-timeout";
    }, timeout: timeoutPolicy);
}
catch (TimeoutException)
{
    Console.WriteLine("Operation timed out as expected");
}

// PolicyValidation_CatchesInvalidConfiguration
// Validate policy configurations
var invalidPolicy = new CircuitBreakerPolicy("invalid")
{
    IsEnabled = true,
    FailureThreshold = 0, // Invalid: must be > 0
    SamplingDuration = TimeSpan.Zero, // Invalid: must be > TimeSpan.Zero
    MinimumThroughput = 0 // Invalid: must be > 0
};

bool isValid = invalidPolicy.IsValidConfiguration(out var validationError);
Console.WriteLine($"Policy validation - valid: {isValid}, error: {validationError}");

// PipelineSnapshot_IncludesPolicies
// Get pipeline snapshot with all registered policies
var pipelineSnapshot = pipelineService.GetStatistics();
Console.WriteLine($"Registered policies: {pipelineSnapshot.PolicyCount}");
foreach (var policy in pipelineSnapshot.RegisteredPolicies)
{
    Console.WriteLine($"  - {policy.PolicyName} ({policy.PolicyType}): " +
                     $"Success rate: {policy.SuccessRate:P}, " +
                     $"Total executions: {policy.TotalExecutions}");
}
```
