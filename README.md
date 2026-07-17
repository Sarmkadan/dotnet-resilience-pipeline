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

See [docs/architecture.md](docs/architecture.md) for the component breakdown, actual policy composition order (circuit breaker → bulkhead → timeout → retry → operation, fallback on failure), extension points and known limitations. Per-type reference docs live in [docs/](docs/), and [QUICK_REFERENCE.md](QUICK_REFERENCE.md) has a condensed API cheat sheet.

## TimeoutPolicyTestsExtensions

`TimeoutPolicyTestsExtensions` provides a collection of helper extension methods that simplify writing unit tests for `TimeoutPolicy`. The methods enable quick creation of test policies, recording of execution times and timeouts, generation of deterministic or random execution sequences, and fluent assertions on statistics and configuration validity.

## MetricsAggregatorTestsExtensions

`MetricsAggregatorTestsExtensions` provides extension methods for testing `MetricsAggregator` functionality. It includes methods to create aggregators with predefined snapshots, verify aggregated metrics, and validate trend analysis results.

## CircuitBreakerHalfOpenBugTestsExtensions

`CircuitBreakerHalfOpenBugTestsExtensions` provides extension methods for testing circuit breaker policies in the half-open state. The methods enable creating test policies with configurable thresholds, transitioning between states, recording successes and failures, and verifying circuit breaker state and statistics.

## PolicyNameGeneratorTestsExtensions

Provides fluent test helpers for `PolicyNameGenerator`, allowing creation, generation, validation, and registration of policy names in unit tests.

**Example usage**

```csharp
using DotNetResiliencePipeline.Tests;
using DotNetResiliencePipeline.Utilities;

public void Example()
{
    var test = new PolicyNameGeneratorTests();

    // Create a clean generator
    var generator = test.CreateCleanGenerator();

    // Generate several names
    var names = test.GenerateMultipleNames("payment", "cb", 3);

    // Verify uniqueness and pattern
    test.AllNamesShouldBeUnique(names);
    test.NamesShouldMatchPattern(names, "payment-cb-");

    // Generate a descriptive name and verify its parts
    var descriptive = test.GenerateAndVerifyDescriptiveName(
        "order", "retry", "highload",
        new[] { "order", "retry", "highload", "0" });

    // Validate a name
    test.NameShouldBeValid(descriptive, true);

    // Register names and ensure they are tracked
    test.RegisterMultipleNames(names);

    // Generate a name with a custom prefix
    var prefixed = test.GenerateNameWithPrefixAndVerify("custom", "inventory", "bulkhead");
}
```

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

## Project Layout

- `src/Domain/Policies/` - policy configuration types (data + counters)
- `src/Services/` - execution logic per policy and the `ResiliencyPipelineService` orchestrator
- `src/Configuration/` - fluent builder, options, DI extensions
- `src/Data/`, `src/Events/`, `src/Formatters/` - execution history, event publishing, metrics export
- `tests/` - xUnit test suite; `benchmarks/` - BenchmarkDotNet project

## License

See [LICENSE](LICENSE).
