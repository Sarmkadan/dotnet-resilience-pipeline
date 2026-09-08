# Examples

The [`examples/`](../examples/) directory contains standalone demonstrations of configuring and using the resilience pipeline. These files are reference samples rather than entry points in the main application: `DotNetResiliencePipeline.csproj` contains `<Compile Remove="examples/**/*.cs" />` (alongside similar exclusions for tests and benchmarks), so the examples are not compiled by the main project.

## Example index

### [`BasicUsage.cs`](../examples/BasicUsage.cs)

Shows the basic dependency-injection setup for a named pipeline with circuit-breaker, retry, and timeout behavior, then runs successful and failing calls and prints aggregate statistics. It uses `AddResiliencePipeline`, `PolicyRepository`, `ResiliencyPipelineService`, `CircuitBreakerPolicy`, `RetryPolicy`, and `TimeoutPolicy`.

### [`AdvancedUsage.cs`](../examples/AdvancedUsage.cs)

Demonstrates fluent configuration of circuit-breaker, exponential-retry, bulkhead, and asynchronous fallback behavior for a fragile operation; the execution call explicitly supplies the retrieved circuit-breaker and fallback policies. It uses `AddResiliencePipeline`, `PolicyRepository`, `ResiliencyPipelineService`, `CircuitBreakerPolicy`, `RetryPolicy.BackoffStrategy`, and `FallbackPolicy`, together with `WithBulkhead` and `WithFallbackAction<T>`.

### [`BulkheadPatternExample.cs`](../examples/BulkheadPatternExample.cs)

Models resource isolation by defining separate database and API bulkheads, launching concurrent work, and reporting active execution and queue counts. It uses `AddResiliencePipeline`, `PolicyRepository`, `ResiliencyPipelineService`, and `BulkheadPolicy`.

### [`CircuitBreakerSimulation.cs`](../examples/CircuitBreakerSimulation.cs)

Walks a payment-service circuit breaker through failure accumulation, open-state fast rejection, the open-duration wait, half-open recovery attempts, and normal closed-state calls. It uses `AddResiliencePipeline`, `PolicyRepository`, `ResiliencyPipelineService`, and `CircuitBreakerPolicy`, including the policy's `State`, `IsOpen()`, and `ConsecutiveFailures` members.

### [`FallbackPatternExample.cs`](../examples/FallbackPatternExample.cs)

Demonstrates graceful degradation for user-profile reads across primary success, intermittent primary failure, repeated failure, and an open circuit, with a cached-profile delegate used as the fallback path when execution throws. It uses `AddResiliencePipeline`, `PolicyRepository`, `ResiliencyPipelineService`, `FallbackPolicy`, `CircuitBreakerPolicy`, and `RetryPolicy`.

### [`IntegrationExample.cs`](../examples/IntegrationExample.cs)

Shows application-host integration: policies and an application service are registered with `Host.CreateApplicationBuilder`, and the service receives pipeline dependencies through constructor injection. It uses `AddResiliencePipeline`, `ResiliencyPipelineService`, `PolicyRepository`, `RetryPolicy`, and `TimeoutPolicy` in `MyApiService`.

### [`MetricsMonitoringExample.cs`](../examples/MetricsMonitoringExample.cs)

Runs a small randomized load simulation, periodically reads pipeline statistics, and produces a health report from execution history. It uses `AddResiliencePipeline`, `ResiliencyPipelineService`, `PolicyRepository`, `ExecutionHistoryRepository`, `ResiliencyHelper`, and `RetryPolicy.BackoffStrategy`, while configuring circuit-breaker, retry, timeout, and bulkhead behavior.

### [`MicroserviceIntegration.cs`](../examples/MicroserviceIntegration.cs)

Models user, order, and notification service calls with independently named policy sets, nested operations, result handling, and final metrics and circuit-state output. It uses `AddResiliencePipeline`, `PolicyRepository`, `ResiliencyPipelineService`, `CircuitBreakerPolicy`, `RetryPolicy`, `TimeoutPolicy`, `BulkheadPolicy`, and `FallbackPolicy`.

## Running an example

Because the examples are excluded from the main project, copy the chosen `.cs` file into a console project that references `DotNetResiliencePipeline`, ensure that project has the Microsoft Extensions dependencies required by the sample, and use the example's `Main` method as its entry point. Alternatively, explicitly include one example file in a console project's compile items (for example, with a linked `<Compile Include="../dotnet-resilience-pipeline/examples/BasicUsage.cs" Link="BasicUsage.cs" />` item) and add a project reference to `DotNetResiliencePipeline.csproj`; include only one sample with a `Main` method unless you also set `StartupObject`.
