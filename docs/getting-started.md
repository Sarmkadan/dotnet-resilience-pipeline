# Getting Started with DotNet Resilience Pipeline

This guide walks you through setting up and using the DotNet Resilience Pipeline in your .NET application.

## Prerequisites

- .NET 10.0 or later
- Basic understanding of async/await patterns
- Familiarity with dependency injection

## Installation

### Step 1: Install NuGet Package

```bash
dotnet add package DotNetResiliencePipeline
```

### Step 2: Register Services

In your `Program.cs`:

```csharp
using DotNetResiliencePipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Register resilience pipeline
builder.Services.AddResiliencePipeline(pipelineBuilder =>
{
    pipelineBuilder.WithCircuitBreaker("default", policy =>
    {
        policy.FailureThreshold = 5;
        policy.OpenDuration = TimeSpan.FromSeconds(30);
    });
});

var app = builder.Build();
app.Run();
```

## Basic Usage

### Simple Circuit Breaker

```csharp
using DotNetResiliencePipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;

// Setup
var services = new ServiceCollection();
services.AddResiliencePipeline(builder =>
{
    builder.WithCircuitBreaker("payment-service", policy =>
    {
        policy.FailureThreshold = 5;
        policy.OpenDuration = TimeSpan.FromSeconds(30);
    });
});

var provider = services.BuildServiceProvider();
var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();
var policyRepository = provider.GetRequiredService<PolicyRepository>();

// Get policy
var circuitBreaker = policyRepository
    .GetPolicy<CircuitBreakerPolicy>("payment-service");

// Execute operation
try
{
    var result = await pipeline.ExecuteAsync(
        async ct => await ProcessPaymentAsync(ct),
        circuitBreaker: circuitBreaker
    );

    if (result.IsSuccess)
        Console.WriteLine("Payment processed successfully");
    else
        Console.WriteLine($"Payment failed: {result.Error?.Message}");
}
catch (Exception ex)
{
    Console.WriteLine($"Unexpected error: {ex.Message}");
}

async Task<bool> ProcessPaymentAsync(CancellationToken ct)
{
    // Payment processing logic
    return true;
}
```

### Adding Retry Logic

```csharp
// Configure retry
builder.Services.AddResiliencePipeline(pipelineBuilder =>
{
    pipelineBuilder
        .WithCircuitBreaker("api-calls", policy =>
        {
            policy.FailureThreshold = 5;
            policy.OpenDuration = TimeSpan.FromSeconds(30);
        })
        .WithRetry("api-calls", policy =>
        {
            policy.MaxRetries = 3;
            policy.InitialDelay = TimeSpan.FromMilliseconds(100);
            policy.Strategy = RetryPolicy.BackoffStrategy.Exponential;
            policy.BackoffMultiplier = 2.0;
        });
});

// Usage
var circuitBreaker = policyRepository
    .GetPolicy<CircuitBreakerPolicy>("api-calls");
var retryPolicy = policyRepository
    .GetPolicy<RetryPolicy>("api-calls");

var result = await pipeline.ExecuteAsync(
    async ct => await externalApi.CallAsync(ct),
    circuitBreaker: circuitBreaker,
    retry: retryPolicy
);
```

### Adding Timeout Protection

```csharp
// Configure timeout
builder.Services.AddResiliencePipeline(pipelineBuilder =>
{
    pipelineBuilder.WithTimeout("long-operations", TimeSpan.FromSeconds(30));
});

// Usage
var timeout = policyRepository
    .GetPolicy<TimeoutPolicy>("long-operations");

try
{
    var result = await pipeline.ExecuteAsync(
        async ct => await LongRunningOperationAsync(ct),
        timeout: timeout
    );
}
catch (OperationTimeoutException)
{
    Console.WriteLine("Operation exceeded timeout");
}
```

### Resource Isolation with Bulkhead

```csharp
// Configure bulkhead
builder.Services.AddResiliencePipeline(pipelineBuilder =>
{
    pipelineBuilder.WithBulkhead("database", maxParallelization: 10, maxQueueLength: 50);
});

// Usage
var bulkhead = policyRepository
    .GetPolicy<BulkheadPolicy>("database");

try
{
    var result = await pipeline.ExecuteAsync(
        async ct => await databaseQuery.ExecuteAsync(ct),
        bulkhead: bulkhead
    );
}
catch (BulkheadRejectedException)
{
    Console.WriteLine("Database resource pool at capacity, request rejected");
}
```

### Graceful Fallback

```csharp
// Configure fallback
builder.Services.AddResiliencePipeline(pipelineBuilder =>
{
    pipelineBuilder.WithFallback("user-service");
});

// Usage
var fallback = policyRepository
    .GetPolicy<FallbackPolicy>("user-service");

var user = await pipeline.ExecuteAsync(
    async ct => await primaryService.GetUserAsync(userId, ct),
    fallback: fallback
);
```

## Complete Example: Web API with All Policies

```csharp
using DotNetResiliencePipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;

var builder = WebApplicationBuilder.CreateBuilder(args);

// Register resilience pipeline
builder.Services.AddResiliencePipeline(pipelineBuilder =>
{
    // Circuit breaker for external API
    pipelineBuilder.WithCircuitBreaker("external-api", policy =>
    {
        policy.FailureThreshold = 5;
        policy.OpenDuration = TimeSpan.FromSeconds(60);
        policy.SuccessThresholdInHalfOpen = 3;
    });

    // Retry with exponential backoff
    pipelineBuilder.WithRetry("external-api", policy =>
    {
        policy.MaxRetries = 4;
        policy.InitialDelay = TimeSpan.FromMilliseconds(100);
        policy.Strategy = RetryPolicy.BackoffStrategy.Exponential;
        policy.BackoffMultiplier = 2.0;
        policy.MaxDelay = TimeSpan.FromSeconds(30);
    });

    // Timeout for API calls
    pipelineBuilder.WithTimeout("external-api", TimeSpan.FromSeconds(10));

    // Bulkhead for database
    pipelineBuilder.WithBulkhead("database", maxParallelization: 20, maxQueueLength: 100);

    // Fallback for user service
    pipelineBuilder.WithFallback("user-service");
});

var app = builder.Build();

// Get services
var pipeline = app.Services.GetRequiredService<ResiliencyPipelineService>();
var policyRepository = app.Services.GetRequiredService<PolicyRepository>();

// Endpoint example
app.MapGet("/api/users/{id}", async (int id, CancellationToken ct) =>
{
    var userServicePolicies = new
    {
        CircuitBreaker = policyRepository.GetPolicy<CircuitBreakerPolicy>("external-api"),
        Retry = policyRepository.GetPolicy<RetryPolicy>("external-api"),
        Timeout = policyRepository.GetPolicy<TimeoutPolicy>("external-api"),
        Fallback = policyRepository.GetPolicy<FallbackPolicy>("user-service")
    };

    try
    {
        var result = await pipeline.ExecuteAsync(
            async cancellationToken => await FetchUserAsync(id, cancellationToken),
            circuitBreaker: userServicePolicies.CircuitBreaker,
            retry: userServicePolicies.Retry,
            timeout: userServicePolicies.Timeout,
            fallback: userServicePolicies.Fallback,
            cancellationToken: ct
        );

        if (result.IsSuccess)
            return Results.Ok(result.Value);
        else
            return Results.InternalServerError();
    }
    catch (OperationTimeoutException)
    {
        return Results.StatusCode(504);
    }
    catch (CircuitBreakerOpenException)
    {
        return Results.StatusCode(503);
    }
});

app.Run();

async Task<User> FetchUserAsync(int id, CancellationToken ct)
{
    // Simulate external API call
    return new User { Id = id, Name = "John Doe" };
}

record User(int Id, string Name);
```

## Monitoring and Observability

### View Metrics

```csharp
// Get pipeline statistics
var stats = pipeline.GetStatistics();

Console.WriteLine($"Total Executions: {stats.TotalExecutions}");
Console.WriteLine($"Success Rate: {stats.SuccessRate:P}");
Console.WriteLine($"Average Duration: {stats.AverageDurationMs}ms");
```

### Generate Health Report

```csharp
var history = provider.GetRequiredService<ExecutionHistoryRepository>();
var healthReport = ResiliencyHelper.GenerateHealthReport(pipeline, history);

Console.WriteLine($"Pipeline Health: {healthReport.HealthStatus}");
Console.WriteLine($"Circuit Breaker Health: {healthReport.CircuitBreakerHealth}");
```

### Subscribe to Events

```csharp
var eventPublisher = provider.GetRequiredService<ResiliencyEventPublisher>();

eventPublisher.Subscribe((PolicyEvent @event) =>
{
    Console.WriteLine($"Policy Event: {@event.EventType} - {@event.PolicyName} - {@event.Timestamp:O}");
});
```

## Policy Composition and Order

When combining multiple resilience policies, the order in which they are applied is crucial and defines their interaction. Policies are composed in an "outermost to innermost" fashion. This means the policy configured first in the `ResiliencyPipelineBuilder` acts as the outermost wrapper, and the policy configured last acts as the innermost.

Consider a typical scenario where you want to apply a Circuit Breaker, then a Bulkhead, then a Retry policy, and finally a Timeout.

**Example Pipeline Configuration:**

```csharp
builder.Services.AddResiliencePipeline(pipelineBuilder =>
{
    pipelineBuilder
        .WithCircuitBreaker("outer-cb", cbOptions => { /* ... */ })      // Outermost
        .WithBulkhead("capacity-limit", maxParallelization: 10)         // Second layer
        .WithRetry("api-retry", retryOptions => { /* ... */ })          // Third layer
        .WithTimeout("per-attempt-timeout", TimeSpan.FromSeconds(5));   // Innermost
});
```

In this setup:
1.  **Circuit Breaker (`outer-cb`)** is the first line of defense. If the circuit is open, the operation is rejected immediately without engaging other policies.
2.  If the circuit is closed, the request proceeds to the **Bulkhead (`capacity-limit`)**. If the bulkhead is full, the request is rejected or queued, again preventing further policies from being invoked.
3.  If a slot is available in the bulkhead, the request then proceeds to the **Retry (`api-retry`)** policy.
4.  Finally, the actual operation is executed, and each *individual attempt* made by the Retry policy will be subject to the **Timeout (`per-attempt-timeout`)** policy.

### Semantic Implications of Policy Order

The placement of policies like Timeout relative to Retry is particularly important:

-   **Timeout *inside* Retry (as shown above):** Each individual retry attempt will have its own timeout. If an attempt takes longer than the timeout, it will be cancelled, and the Retry policy can then decide whether to make another attempt. This is generally the recommended approach for transient network operations.

-   **Timeout *outside* Retry:** If you configure the Timeout policy *before* the Retry policy in the builder chain, the entire sequence of retries will be subject to a single, cumulative timeout. If the total time spent across all retry attempts (including delays) exceeds this outer timeout, the entire operation will be cancelled. This is useful for enforcing a total deadline for an operation that might involve multiple retries.

**Example: Timeout Outside Retry (Total Deadline)**

```csharp
builder.Services.AddResiliencePipeline(pipelineBuilder =>
{
    pipelineBuilder
        .WithTimeout("total-deadline", TimeSpan.FromSeconds(30)) // Outer Timeout (Total Deadline)
        .WithRetry("api-retry", retryOptions => { /* ... */ });  // Inner Retry
});
```

In this case, the `api-retry` policy will perform its retries, but if the accumulated time exceeds 30 seconds, the outer `total-deadline` timeout will cancel the entire operation, regardless of how many retries are left.

## Shared Telemetry and Observability

All policies registered within the `ResiliencyPipelineService` contribute to the overall pipeline statistics and emit events. You can monitor the health and performance of your composed policies through:

-   **Pipeline Statistics**: Use `pipeline.GetStatistics()` to get aggregated metrics across all policies, including total executions, success rates, and average durations. Individual policy snapshots provide detailed insights per policy.
-   **Policy Events**: Subscribe to the `ResiliencyEventPublisher` to receive events about state changes (e.g., Circuit Breaker Opened/Closed), retries, timeouts, and fallbacks. This allows for real-time monitoring and integration with external logging or observability platforms.
-   **Logging**: The library integrates with `Microsoft.Extensions.Logging`. Ensure your logging is configured (as shown in "Monitoring and Observability" section) to capture detailed logs from policy executions.

By understanding policy composition and leveraging the built-in telemetry, you can effectively build and monitor robust, fault-tolerant applications.

## Next Steps

- Explore the [Architecture Guide](architecture.md) for deeper understanding
- Review [API Reference](api-reference.md) for complete API documentation
- Check [Deployment Guide](deployment.md) for production setup
- See [examples/](../examples/) for more code examples
