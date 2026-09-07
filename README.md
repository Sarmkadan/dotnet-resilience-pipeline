// README.md
# DotNet Resilience Pipeline

A .NET library and demo application implementing classic resilience patterns - retry, circuit breaker, timeout, bulkhead and fallback - behind a single orchestrator (`ResiliencyPipelineService`) with a fluent builder and `Microsoft.Extensions.DependencyInjection` integration.

## Features

- **Retry** with fixed/linear/exponential backoff and optional jitter (`RetryPolicy` + `RetryService`)
- **Circuit breaker** with Closed / Open / Half-Open state machine (`CircuitBreakerPolicy` + `CircuitBreakerService`)
- **Timeout** via linked `CancellationTokenSource` (`TimeoutPolicy` + `TimeoutService`), plus an adaptive variant (`AdaptiveTimeoutPolicy`)
- **Bulkhead** fail-fast concurrency limiting (`BulkheadPolicy` + `BulkheadService`)
- **Fallback** execution on failure with metadata in the result (`FallbackPolicy` + `FallbackService`)
- Per-policy and pipeline-level statistics, snapshots, Prometheus-style export (`MetricsExporter`), event publishing and execution history repositories

## Quick Start

```csharp
var services = new ServiceCollection();
services.AddResiliencePipeline(builder =>
{
    builder
        .WithCircuitBreaker("payment-circuit", p => { p.FailureThreshold = 5; p.OpenDuration = TimeSpan.FromSeconds(30); })
        .WithRetry("api-retry", p => { p.MaxRetries = 3; p.InitialDelay = TimeSpan.FromMilliseconds(100); })
        .WithTimeout("operation-timeout", TimeSpan.FromSeconds(10))
        .WithBulkhead("resource-bulkhead", maxParallelization: 10)
        .WithFallback("graceful-fallback", p => p.FallbackOnAnyException = true);
});

var provider = services.BuildServiceProvider();
var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();

var retry = pipeline.GetPolicyByName("api-retry") as RetryPolicy;
var result = await pipeline.ExecuteAsync(
    async ct => await CallExternalServiceAsync(ct),
    CancellationToken.None,
    retry: retry);

if (result.IsSuccess)
    Console.WriteLine(result.Data);
```

Note: registered policies are not applied implicitly - you pass the ones you want to each `ExecuteAsync` call.

Run the demo:

```bash
dotnet run --project DotNetResiliencePipeline.csproj
```

## Architecture

See [docs/architecture.md](docs/architecture.md) for the component breakdown, extension points and known limitations. Per-type reference docs live in [docs/](docs/), and [QUICK_REFERENCE.md](QUICK_REFERENCE.md) has a condensed API cheat sheet.

### Policy Composition Order

The library enforces a **canonical ordering** for policy composition to prevent semantic errors. The recommended order (outermost to innermost) is:

```
┌─────────────────┐    ┌──────────────────┐    ┌─────────────────┐    ┌─────────────────┐    ┌─────────────────┐
│  Fallback     │    │ Circuit Breaker │    │     Retry      │    │    Bulkhead    │    │    Timeout     │
│ (Graceful     │───▶│ (Prevent        │───▶│ (Retry          │───▶│ (Concurrency    │───▶│ (Per-attempt    │
│  degradation) │    │  cascading      │    │  transient      │    │  limiting)     │    │  timeout)       │
└─────────────────┘    └──────────────────┘    └─────────────────┘    └─────────────────┘    └─────────────────┘
       ▲                                                                                     │
       │                                                                                     ▼
┌─────────────────┐                                                                   ┌─────────────────┐
│   Operation   │                                                                   │   Operation   │
└─────────────────┘                                                                   └─────────────────┘
```

**Execution Flow:**
1. **Fallback** catches exceptions from all inner policies
2. **Circuit Breaker** checks state before allowing execution
3. **Retry** loop with backoff around the operation
4. **Timeout** applied per retry attempt
5. **Bulkhead** limits concurrent executions
6. **User operation** executes

**Which Exception Each Layer Sees:**

| Policy | Sees Exception | Notes |
|--------|---------------|-------|
| **Fallback** | Any exception from inner policies | Handles all failure scenarios |
| **Circuit Breaker** | `OperationTimeoutException`, `MaxRetriesExceededException`, `BulkheadRejectedException` | Treats as failures |
| **Retry** | `OperationTimeoutException`, transient exceptions | Retries on transient failures |
| **Timeout** | Operation timeout | Per-attempt timeout |
| **Bulkhead** | All exceptions | Limits concurrency |
| **Operation** | Original exceptions | Innermost layer |

**Example:**
```csharp
builder
    .WithFallback("graceful-fallback", p => p.FallbackOnAnyException = true)
    .WithCircuitBreaker("api-circuit", p => {
        p.FailureThreshold = 5;
        p.OpenDuration = TimeSpan.FromSeconds(30);
    })
    .WithRetry("api-retry", p => {
        p.MaxRetries = 3;
        p.Strategy = RetryStrategy.Exponential;
    })
    .WithBulkhead("resource-bulkhead", maxParallelization: 10)
    .WithTimeout("operation-timeout", TimeSpan.FromSeconds(10));
```

**Non-Canonical Ordering:**
If you need to deviate from the recommended order, call `AllowCustomOrder()`:
```csharp
builder.AllowCustomOrder() // Opt-in for non-canonical ordering
    .WithTimeout("early-timeout", TimeSpan.FromSeconds(5))
    .WithCircuitBreaker("my-circuit", ...);
```

⚠️ **Warning:** Non-canonical ordering can lead to unexpected behavior where policies don't work as expected. Use only when you have a specific reason.

## RetryService

`RetryService` is the execution engine behind `RetryPolicy`: it runs an asynchronous operation, re-executes it while the failure is considered retryable, and waits between attempts according to the configured backoff strategy (fixed, linear or exponential) or an AWS-style decorrelated jitter schedule. When the retry budget is exhausted it throws a `MaxRetriesExceededException` carrying every exception observed during the attempts. Beyond executing operations, the service exposes its decision helpers - `CalculateRetryDelay`, `IsRetryable` and `ComputeDecorrelatedJitterDelay` - so callers can reuse the same backoff and retryability rules without running an operation.

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

var retryService = new RetryService();

var policy = new RetryPolicy
{
    MaxRetries = 3,
    InitialDelay = TimeSpan.FromMilliseconds(100),
    Strategy = RetryStrategy.Exponential,
};

using var httpClient = new HttpClient();

// Executes the call, retrying transient failures with exponential backoff.
string body = await retryService.ExecuteAsync(
    policy,
    async ct => await httpClient.GetStringAsync("https://api.example.com/orders", ct),
    CancellationToken.None);

// The backoff and retryability logic can also be used on its own:
for (int attempt = 0; attempt < policy.MaxRetries; attempt++)
{
    TimeSpan delay = retryService.CalculateRetryDelay(policy, attempt);
    Console.WriteLine($"Attempt {attempt}: waiting {delay.TotalMilliseconds:F0} ms");
}

bool shouldRetry = retryService.IsRetryable(policy, new TimeoutException());
TimeSpan jittered = retryService.ComputeDecorrelatedJitterDelay(policy, TimeSpan.FromSeconds(2));
```

## TimeoutPolicyTestsExtensions

`TimeoutPolicyTestsExtensions` provides a collection of helper extension methods that simplify writing unit tests for `TimeoutPolicy`. The methods enable quick creation of test policies, recording of execution times and timeouts, generation of deterministic or random execution sequences, and fluent assertions on statistics and configuration validity.

## MetricsAggregatorTestsExtensions

`MetricsAggregatorTestsExtensions` provides extension methods for testing `MetricsAggregator` functionality. It includes methods to create aggregators with predefined snapshots, verify aggregated metrics, and validate trend analysis results.

## CircuitBreakerHalfOpenBugTestsExtensions

`CircuitBreakerHalfOpenBugTestsExtensions` provides extension methods for testing circuit breaker policies in the half-open state. The methods enable creating test policies with configurable thresholds, transitioning between states, recording successes and failures, and verifying circuit breaker state and statistics.

## PolicyNameGeneratorTestsExtensions

Provides fluent test helpers for `PolicyNameGenerator`, allowing creation, generation, validation, and registration of policy names in unit tests.

## BulkheadServiceTestsExtensions

`BulkheadServiceTestsExtensions` offers a set of fluent extension methods that make it easy to create bulkhead policies for tests and assert on their configuration, state, utilization, and validation results. These helpers reduce boilerplate when verifying bulkhead behavior in unit tests.

**Example usage**

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Tests;
using Xunit;

public class BulkheadExtensionsDemo
{
    [Fact]
    public void DemonstrateBulkheadExtensions()
    {
        var service = new BulkheadService();

        // Create a test bulkhead policy
        var policy = service.CreateTestPolicy(
            name: "demo-bulkhead",
            maxParallelization: 2,
            maxQueueLength: 4,
            isEnabled: true);

        // Verify the policy's configuration
        service.ShouldHaveConfiguration(policy, 2, 4, true);

        // Validate the configuration is considered valid
        service.ShouldValidateConfiguration(policy, expectedIsValid: true);

        // Check utilization and execution state (initially zero)
        service.ShouldHaveUtilizationPercentage(policy, expectedUtilization: 0);
        service.ShouldHaveExecutionState(policy, expectedActiveExecutions: 0, expectedQueuedRequests: 0);

        // Retrieve metrics dictionary for further assertions
        var metrics = service.GetMetrics(policy);
        metrics["MaxParallelization"].Should().Be(2);
        metrics["IsEnabled"].Should().BeTrue();

        // Verify all metrics together
        service.ShouldHaveMetrics(policy, expectedActiveExecutions: 0, expectedQueuedRequests: 0, expectedUtilizationPercentage: 0);
    }
}
```

## AdaptiveTimeoutServiceTests

`AdaptiveTimeoutServiceTests` contains unit tests for the `AdaptiveTimeoutService` class, which implements an adaptive timeout mechanism that adjusts timeout durations based on historical execution times. The tests verify behavior such as timeout growth after slow samples, shrinkage after fast samples, respect for min/max bounds, and proper handling of null policies and disabled policies.

**Example usage**

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;
using Microsoft.Extensions.Logging.Abstractions;
using Xunit;

public class AdaptiveTimeoutServiceUsageExample
{
    [Fact]
    public void Timeout_Grows_After_Slow_Samples()
    {
        var service = new AdaptiveTimeoutService(NullLogger<AdaptiveTimeoutService>.Instance);
        var policy = new AdaptiveTimeoutPolicy("grow-test")
        {
            InitialTimeout = TimeSpan.FromMilliseconds(100),
            MinTimeout = TimeSpan.FromMilliseconds(50),
            MaxTimeout = TimeSpan.FromSeconds(10),
            TargetPercentile = 90.0,
            HeadroomFactor = 1.5,
            WindowSize = 10,
            MinSampleSize = 5,
            AdjustmentInterval = TimeSpan.Zero // Disable interval for immediate adaptation
        };

        // Record several slow executions to trigger timeout growth
        var slowTimes = new long[] { 200, 250, 300, 350, 400 };
        foreach (var time in slowTimes)
        {
            policy.RecordExecutionTime(time);
        }

        // After recording slow samples, timeout should have grown
        Assert.True(policy.CurrentTimeout > policy.InitialTimeout);
        Assert.True(policy.CurrentTimeout <= policy.MaxTimeout);
    }
}
```

## WebhookExceptionTests

`WebhookExceptionTests` contains unit tests for the webhook exception types exposed by the `DotNetResiliencePipeline.Exceptions` namespace. The tests verify constructor parameter handling, property assignment, generated message formatting, defaults for optional arguments, and the inheritance hierarchy rooted at `ResiliencyException`.

**Example usage**

```csharp
using DotNetResiliencePipeline.Exceptions;
using FluentAssertions;
using Xunit;

public class WebhookExceptionTestsExample
{
    [Fact]
    public void WebhookException_WithMessageOnly_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Test webhook exception message";

        // Act
        var exception = new WebhookException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.WebhookId.Should().BeNull();
        exception.WebhookUrl.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }
}

## MicroserviceIntegrationExample

The `MicroserviceIntegrationExample` class demonstrates how to integrate multiple resilience policies into a microservice architecture. It provides a `Main` method to execute a sample workflow, and properties to access the circuit breaker, retry, timeout, bulkhead, and fallback policies. Here’s an example of how to use it:

```csharp
var example = new MicroserviceIntegrationExample();
await example.Main();
Console.WriteLine(example.CircuitBreaker?.State);
Console.WriteLine(example.Retry?.MaxRetries);
Console.WriteLine(example.Timeout?.TimeoutDuration);
Console.WriteLine(example.Bulkhead?.MaxParallelization);
Console.WriteLine(example.Fallback?.FallbackOnAnyException);
```

## HttpClientExceptionTests

`HttpClientExceptionTests` contains unit tests for the `HttpClientException` and related exception classes (`InvalidHttpRequestException`, `HttpResponseException`, `HttpTimeoutException`). 
The tests verify constructor parameter handling, property assignment, nullability of properties, and inheritance hierarchy.

```csharp
using DotNetResiliencePipeline.Exceptions;

// Creating an HttpClientException with all parameters
var httpEx = new HttpClientException(
    "Failed to connect",
    "MyHttpClient",
    "https://api.example.com/data",
    new TimeoutException("The request timed out"));

// Properties can be read and set after construction
httpEx.ClientName = "UpdatedClient";
httpEx.RequestUrl = "https://updated.example.com";

// Creating a derived exception
var responseEx = new HttpResponseException(
    "Not found",
    404,
    "MyHttpClient",
    "https://api.example.com/missing");
```

## ResiliencyExceptionTests

`ResiliencyExceptionTests` contains unit tests for the `ResiliencyException` base class and all derived exception types in the `DotNetResiliencePipeline.Exceptions` namespace. 
The tests verify constructor behavior, property assignment, message formatting, and inheritance hierarchy.

```csharp
using DotNetResiliencePipeline.Exceptions;
using System;

// Creating a ResiliencyException with a message and policy information
var exception = new ResiliencyException(
    "Something went wrong in the retry policy",
    "RetryPolicy",
    "Retry");

// Properties can be read
Console.WriteLine(exception.Message); // "Something went wrong in the retry policy"
Console.WriteLine(exception.PolicyName); // "RetryPolicy"
Console.WriteLine(exception.PolicyType); // "Retry"
```

## FailureInjectionService

`src/Services/FailureInjectionService.cs` provides `FailureInjectionService` for chaos-style testing of resilience policies and application behavior. An `InjectionRule` identifies a scenario by key and configures its `InjectionType` (`Exception`, `Latency`, or `Timeout`), probability, enabled state, and type-specific settings. Register or replace rules with `AddRule`, remove them with `RemoveRule`, and wrap asynchronous operations with either the generic or non-generic `ExecuteAsync` overload. If a rule does not apply, the wrapped operation runs normally; otherwise the service throws an exception, adds latency before running the operation, or simulates a timeout.

Rules can also define inclusive `StartTime` and `EndTime` values in 24-hour `HH:mm` format. `IsActiveAt` checks these windows deterministically for a supplied `DateTimeOffset`; rules without a window are always active. Exception rules use their configured `ExceptionFactory`, or throw `InjectedFaultException` by default. That exception exposes the `RuleKey` that caused the injected failure.

```csharp
using DotNetResiliencePipeline.Services;

var failureInjection = new FailureInjectionService();
var rule = new InjectionRule
{
    Key = "catalog-read",
    Type = InjectionType.Exception,
    InjectionRate = 1.0,
    ExceptionMessage = "The catalog dependency is unavailable",
    StartTime = "09:00",
    EndTime = "17:00"
};

failureInjection.AddRule(rule);

bool activeAtNoon = FailureInjectionService.IsActiveAt(
    rule,
    new DateTimeOffset(2026, 9, 7, 12, 0, 0, TimeSpan.Zero));

if (activeAtNoon)
{
    try
    {
        string result = await failureInjection.ExecuteAsync(
            "catalog-read",
            _ => Task.FromResult("catalog response"));

        Console.WriteLine(result);
    }
    catch (InjectedFaultException exception)
    {
        Console.WriteLine($"Rule {exception.RuleKey} injected: {exception.Message}");
    }
}

failureInjection.RemoveRule("catalog-read");
```

## PolicyCacheService

`src/Caching/PolicyCacheService.cs` provides an in-memory cache for policy configurations. `Get` returns a non-expired `CachedPolicy`, while `GetOrLoad` invokes a loader on a cache miss and caches the returned configuration. `Set` accepts an optional TTL (or uses `DefaultTtl`), and `Invalidate` removes an entry by name. `MaxCacheSize` bounds the cache and causes the least-recently-used entry to be evicted when space is needed. `GetStatistics` returns `CacheStatistics`, including total, valid, and expired entry counts, hit rate, and average TTL.

```csharp
using DotNetResiliencePipeline.Caching;

var cache = new PolicyCacheService
{
    DefaultTtl = TimeSpan.FromMinutes(5),
    MaxCacheSize = 100
};

cache.Set("checkout-retry", new { MaxRetries = 3 }, TimeSpan.FromMinutes(10));

CachedPolicy? cached = cache.Get("checkout-retry");
Console.WriteLine(cached?.Config);

CachedPolicy? loaded = cache.GetOrLoad("catalog-timeout", policyName =>
{
    DateTime now = DateTime.UtcNow;
    return new CachedPolicy
    {
        PolicyName = policyName,
        Config = new { Timeout = TimeSpan.FromSeconds(2) },
        CreatedAt = now,
        ExpiresAt = now.AddMinutes(5),
        LastAccessTime = now,
        AccessCount = 1
    };
});

CacheStatistics statistics = cache.GetStatistics();
Console.WriteLine($"{statistics.ValidEntries}/{statistics.TotalEntries} entries are valid");

bool removed = cache.Invalidate("checkout-retry");
Console.WriteLine($"Entry removed: {removed}");
```

## WebhookManager

`src/Integration/WebhookManager.cs` manages webhook subscriptions and delivery records. `RegisterWebhook` creates an active subscription for one or more event types and returns its ID, while `UnregisterWebhook` removes it. `TriggerEventAsync` sends an event to each active matching subscription with retry handling. `SetWebhookActive` enables or disables a subscription without removing it, and `GetDeliveryHistory` returns recent delivery records, optionally filtered by webhook ID. `GetStatistics` summarizes total, successful, and failed deliveries, the success rate, and the number of active subscriptions.

```csharp
using DotNetResiliencePipeline.Integration;

var webhooks = new WebhookManager();
string webhookId = webhooks.RegisterWebhook(
    "https://example.com/hooks/orders",
    new[] { "order.created" },
    new Dictionary<string, string> { ["X-Webhook-Source"] = "checkout" });

await webhooks.TriggerEventAsync(
    "order.created",
    new { OrderId = "order-123", Total = 49.95m });

List<WebhookDelivery> history = webhooks.GetDeliveryHistory(webhookId, limit: 20);
WebhookStatistics statistics = webhooks.GetStatistics();
Console.WriteLine(
    $"{statistics.SuccessfulDeliveries}/{statistics.TotalDeliveries} deliveries succeeded " +
    $"({statistics.SuccessRate:F1}%); {statistics.ActiveSubscriptions} active subscriptions");

bool deactivated = webhooks.SetWebhookActive(webhookId, false);
bool unregistered = webhooks.UnregisterWebhook(webhookId);
Console.WriteLine($"Deactivated: {deactivated}; unregistered: {unregistered}");
```

## MetricsExporter

The `MetricsExporter` class in `src/Formatters/MetricsExporter.cs` provides methods to export pipeline metrics in various formats.
It takes a `PipelineMetricsSnapshot` (obtained from `ResiliencyPipelineService.GetMetricsSnapshot()`) and can export to JSON, CSV, or Prometheus text format.

Example usage:

```csharp
using DotNetResiliencePipeline.Formatters;
using DotNetResiliencePipeline.Services;

// Assume we have a ResiliencyPipelineService instance
var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();
var snapshot = pipeline.GetMetricsSnapshot();

var exporter = new MetricsExporter();

// Export to JSON
string json = exporter.ExportJson(snapshot);

// Export to CSV
string csv = exporter.ExportCsv(snapshot);

// Export to Prometheus format
string prometheus = exporter.ExportPrometheus(snapshot);
```

## FallbackService

`src/Services/FallbackService.cs` provides the execution engine for `FallbackPolicy`. Its `ExecuteAsync` method receives the exception and execution time from a failed primary operation and, when the policy allows it, runs the configured fallback action within the fallback timeout. `ShouldTriggerFallback` exposes the policy's exception-matching decision, `GetFallbackSuccessRate` reports the percentage of successful fallback executions, and `AddFallbackTrigger` and `RemoveFallbackTrigger` manage the exception types that activate a selective fallback policy.

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

var fallbackService = new FallbackService();
var policy = new FallbackPolicy("catalog-fallback")
{
    FallbackOnAnyException = false,
    FallbackTimeout = TimeSpan.FromSeconds(2)
};

policy.SetFallbackAction<string>(_ => Task.FromResult("cached catalog response"));
fallbackService.AddFallbackTrigger(policy, typeof(TimeoutException));

Exception primaryException = new TimeoutException("The catalog request timed out");
if (fallbackService.ShouldTriggerFallback(policy, primaryException))
{
    var result = await fallbackService.ExecuteAsync<string>(
        policy,
        primaryException,
        primaryExecutionTimeMs: 2_000,
        CancellationToken.None);

    Console.WriteLine(result.Data);
    Console.WriteLine($"Fallback success rate: {fallbackService.GetFallbackSuccessRate(policy):F1}%");
}

fallbackService.RemoveFallbackTrigger(policy, typeof(TimeoutException));
```

## BulkheadService

`src/Services/BulkheadService.cs` provides fail-fast concurrency limiting around `BulkheadPolicy`. `TryAcquireSlot` attempts to reserve capacity immediately, while `TryAcquireSlotAsync` performs the asynchronous attempt and validates its timeout; both return `false` when no execution slot is available. `AcquireSlotAsync` can wait in the bounded queue and throws `BulkheadRejectedException` when the queue is full or its wait expires. Callers release acquired capacity with `ReleaseSlot`; the remaining public helpers are `DequeueRequest`, `RecordQueueWaitTime`, `GetUtilizationPercentage`, `GetActiveExecutionCount`, `GetQueuedRequestCount` and `IsValidConfiguration`, which expose queue maintenance, metrics and policy validation.

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;

var bulkheadService = new BulkheadService();
var policy = new BulkheadPolicy("orders-bulkhead");

try
{
    await bulkheadService.AcquireSlotAsync(policy, CancellationToken.None);
    try
    {
        // Run work while the execution slot is held.
        await Task.Delay(TimeSpan.FromMilliseconds(100));
    }
    finally
    {
        bulkheadService.ReleaseSlot(policy);
    }
}
catch (BulkheadRejectedException exception)
{
    Console.WriteLine($"Bulkhead rejected the request: {exception.Message}");
}
```

## Project Layout

- `src/Domain/Policies/` - policy configuration types (data + counters)
- `src/Services/` - execution logic per policy and the `ResiliencyPipelineService` orchestrator
- `src/Configuration/` - fluent builder, options, DI extensions
- `src/Data/`, `src/Events/`, `src/Formatters/` - execution history, event publishing, metrics export
- `tests/` - xUnit test suite; `benchmarks/` - BenchmarkDotNet project

## License

See [LICENSE](LICENSE).
