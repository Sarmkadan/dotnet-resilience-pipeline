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

**Example usage**

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Tests;
using System;

// Inside a test method
var testHelper = new CircuitBreakerHalfOpenBugTests();

// Create a realistic half-open test policy with 3 success threshold, 2 failure threshold, and 500ms open duration
var policy = testHelper.CreateRealisticHalfOpenTestPolicy(successThreshold: 3, failureThreshold: 2, openDurationMilliseconds: 500);

// Verify the circuit breaker is initially in closed state
policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Closed);

// Transition the circuit breaker from open to half-open state
policy.RecordFailure(); // Record a failure to open the circuit
policy.RecordFailure(); // Ensure we meet failure threshold
policy.AttemptReset(); // Attempt to reset to half-open
policy.ShouldBeInHalfOpenState();

// Record 2 successful operations in half-open state
policy.RecordSuccess();
policy.RecordSuccess();

// Verify the circuit breaker closes after meeting success threshold
policy.RecordSuccess(); // Record one more success to close
policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Closed);

// Get statistics
int consecutiveFailures = testHelper.GetConsecutiveFailures(policy);
int successfulInHalfOpen = testHelper.GetSuccessfulInHalfOpen(policy);
```

**Example usage**

```csharp
using DotNetResiliencePipeline.Utilities;
using DotNetResiliencePipeline.Tests;
using System;
using System.Collections.Generic;

// Inside a test method
var testHelper = new MetricsAggregatorTests();

// Create an aggregator with specific snapshots
var snapshots = new List<MetricsSnapshot>
{
    new MetricsSnapshot
    {
        Timestamp = DateTime.UtcNow,
        SuccessRate = 95.0,
        AverageExecutionTimeMs = 45.2,
        TotalExecutions = 1000,
        SuccessfulExecutions = 950,
        FailedExecutions = 50
    },
    new MetricsSnapshot
    {
        Timestamp = DateTime.UtcNow.AddMinutes(1),
        SuccessRate = 92.5,
        AverageExecutionTimeMs = 48.7,
        TotalExecutions = 1000,
        SuccessfulExecutions = 925,
        FailedExecutions = 75
    }
};
var aggregator = testHelper.WithSnapshots(snapshots);

// Create an aggregator with repeated snapshots (e.g., 10 snapshots with 90% success rate)
var aggregator2 = testHelper.WithRepeatedSnapshots(successRate: 90.0, count: 10);

// Assert aggregated metrics
var metrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(5));
testHelper.ShouldHaveMetrics(
    aggregator,
    expectedSnapshotCount: 2,
    expectedAvgSuccessRate: 93.75,
    expectedTotalExecutions: 2000,
    expectedPeakExecutions: 1000,
    expectedMinSuccessRate: 92.5,
    expectedMaxSuccessRate: 95.0
);

// Create an aggregator with time-spaced snapshots
var successRates = new List<double> { 95.0, 90.0, 85.0 };
var timeIntervals = new List<TimeSpan> { TimeSpan.FromMinutes(1), TimeSpan.FromMinutes(2) };
var aggregator3 = testHelper.WithTimeSpacedSnapshots(successRates, timeIntervals);

// Assert trend analysis
var trend = aggregator.GetTrend(TimeSpan.FromMinutes(5));
testHelper.ShouldHaveTrend(trend, expectedDirection: "Decreasing", expectedIsAnomaly: true);
```

**Example usage**

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Tests;
using System.Collections.Generic;

// Inside a test method
var testHelper = new TimeoutPolicyTests();

// Create a policy with a 500 ms timeout
var policy = testHelper.CreateTestPolicy(timeoutMs: 500);

// Record some execution times and timeouts
testHelper.RecordExecutionTimes(policy, new[] { 100, 200, 300 });
testHelper.RecordTimeouts(policy, new[] { 500, 600 });

// Generate a deterministic sequence of execution times
IEnumerable<int> seq = testHelper.CreateExecutionTimeSequence(policy, count: 5);

// Assert statistics
testHelper.ShouldHaveTimeoutCount(policy, expectedTimeoutCount: 2);
testHelper.ShouldHaveTimeoutPercentage(policy, expectedPercentage: 40);
testHelper.ShouldHaveExecutionStats(policy, expectedAverage: 200, expectedMin: 100, expectedMax: 300);
testHelper.ShouldHavePercentileTimes(policy, expectedP95: 300, expectedP99: 300);
testHelper.ShouldHaveValidConfiguration(policy, shouldBeValid: true);

// Generate a normal‑distributed set of execution times for more advanced scenarios
IEnumerable<int> normalTimes = testHelper.CreateNormalDistributionExecutionTimes(
    policy,
    mean: 250,
    stdDev: 50,
    count: 10);
```

The example demonstrates how the extension methods can be chained together to set up a `TimeoutPolicy`, feed it with synthetic data, and verify its internal statistics without needing to write repetitive boilerplate code.

## MicroserviceIntegrationExample

The `MicroserviceIntegrationExample` class demonstrates how to integrate multiple resilience policies into a microservice architecture. It provides a `Main` method to execute a sample workflow, and properties to access the circuit breaker, retry, timeout, bulkhead, and fallback policies. Here's an example of how to use it:

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
