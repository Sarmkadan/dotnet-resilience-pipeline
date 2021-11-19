# DotnetResiliencePipelineOptionsExtensions

The `DotnetResiliencePipelineOptionsExtensions` class provides a set of static extension methods designed to streamline the configuration of resilience strategies within the `dotnet-resilience-pipeline` framework. By offering predefined configuration presets for common operational scenarios—such as production deployments, transient fault handling, critical operation timeouts, and resource isolation—this utility reduces boilerplate code and ensures consistent application of resilience patterns across different components of a distributed system.

## API

### ToPipelineBuilder
Converts a configured options instance into a `ResiliencyPipelineBuilder`, enabling the final construction of the execution pipeline.
*   **Purpose**: Facilitates the transition from configuration objects to the mutable builder required to assemble the final resilience pipeline.
*   **Parameters**: Accepts the source options instance upon which the method is extended.
*   **Return Value**: Returns a `ResiliencyPipelineBuilder` initialized with the current configuration state.
*   **Throws**: Throws an `ArgumentNullException` if the source options instance is null.

### ConfigureForProduction
Applies a robust set of default settings optimized for stable, high-availability production environments, specifically configuring circuit breaker behavior.
*   **Purpose**: Initializes `CircuitBreakerOptions` with thresholds and durations suitable for live traffic, preventing cascading failures while allowing recovery.
*   **Parameters**: Accepts the target `CircuitBreakerOptions` instance.
*   **Return Value**: Returns the same `CircuitBreakerOptions` instance with production-grade values applied, allowing for method chaining.
*   **Throws**: Throws an `ArgumentNullException` if the options instance is null.

### ConfigureForTransientFaults
Configures retry logic specifically tailored to handle temporary infrastructure glitches, network hiccups, or sporadic service unavailability.
*   **Purpose**: Sets up `RetryOptions` with exponential backoff strategies and appropriate retry counts to mitigate transient errors without overwhelming the system.
*   **Parameters**: Accepts the target `RetryOptions` instance.
*   **Return Value**: Returns the modified `RetryOptions` instance for fluent configuration.
*   **Throws**: Throws an `ArgumentNullException` if the options instance is null.

### ConfigureForCriticalOperations
Establishes strict timeout constraints for operations where prolonged execution is unacceptable or indicates a systemic hang.
*   **Purpose**: Defines `TimeoutOptions` with aggressive duration limits to ensure critical paths fail fast rather than blocking resources indefinitely.
*   **Parameters**: Accepts the target `TimeoutOptions` instance.
*   **Return Value**: Returns the configured `TimeoutOptions` instance.
*   **Throws**: Throws an `ArgumentNullException` if the options instance is null.

### ConfigureForIsolation
Configures bulkhead patterns to isolate specific workloads, ensuring that resource exhaustion in one area does not impact the availability of others.
*   **Purpose**: Initializes `BulkheadOptions` with concurrency limits and queue capacities to enforce resource partitioning.
*   **Parameters**: Accepts the target `BulkheadOptions` instance.
*   **Return Value**: Returns the updated `BulkheadOptions` instance.
*   **Throws**: Throws an `ArgumentNullException` if the options instance is null.

## Usage

The following example demonstrates how to configure a comprehensive resilience pipeline for a production API client by chaining the specific configuration extensions for retries, circuit breaking, and timeouts.

```csharp
using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Extensions;

// Initialize individual option objects
var retryOptions = new DotnetResiliencePipelineOptions.RetryOptions();
var circuitBreakerOptions = new DotnetResiliencePipelineOptions.CircuitBreakerOptions();
var timeoutOptions = new DotnetResiliencePipelineOptions.TimeoutOptions();

// Apply scenario-specific presets
retryOptions.ConfigureForTransientFaults();
circuitBreakerOptions.ConfigureForProduction();
timeoutOptions.ConfigureForCriticalOperations();

// Build the final pipeline
var builder = retryOptions.ToPipelineBuilder()
    .AddRetry(retryOptions)
    .AddCircuitBreaker(circuitBreakerOptions)
    .AddTimeout(timeoutOptions);

var pipeline = builder.Build();
```

The next example illustrates configuring isolation boundaries for a background processing worker, ensuring that batch jobs do not consume all available threads required for real-time requests.

```csharp
using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Extensions;

// Configure bulkhead specifically for isolation
var bulkheadOptions = new DotnetResiliencePipelineOptions.BulkheadOptions();
bulkheadOptions.ConfigureForIsolation();

// Convert to builder and add the strategy
var builder = bulkheadOptions.ToPipelineBuilder()
    .AddBulkhead(bulkheadOptions);

var isolatedPipeline = builder.Build();

// Execute within the isolated context
await isolatedPipeline.ExecuteAsync(async (cancellationToken) => 
{
    await ProcessBatchAsync(cancellationToken);
}, CancellationToken.None);
```

## Notes

*   **Null Safety**: All extension methods strictly validate their input; passing a `null` instance as the target of any `Configure` method or `ToPipelineBuilder` will result in an immediate `ArgumentNullException`. Callers must ensure options instances are instantiated before invoking these extensions.
*   **Fluent Chaining**: The `Configure` methods return the same instance passed in, designed explicitly for fluent API usage. However, since they modify the object in place, care should be taken not to share the same options instance across multiple distinct pipelines if independent configurations are required.
*   **Thread Safety**: The configuration methods themselves are stateless and thread-safe regarding their execution logic. However, the returned option objects (`RetryOptions`, `CircuitBreakerOptions`, etc.) are mutable. Once an options object is passed to a pipeline builder and the pipeline is constructed, the options instance should be treated as immutable to prevent race conditions during pipeline execution.
*   **Configuration Overwrite**: Invoking a `Configure` method will overwrite any existing property values on the target options object with the preset defaults defined for that specific scenario. Custom adjustments should be applied either before calling these extensions or by modifying the properties directly after the extension call returns.
