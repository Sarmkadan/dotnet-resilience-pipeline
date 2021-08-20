# DotNet Resilience Pipeline

A comprehensive, production-grade resilience library for .NET applications featuring circuit breaker, bulkhead, retry, timeout, and fallback patterns with fluent configuration.

## Features

- **Circuit Breaker Pattern**: Prevent cascading failures with intelligent state management (Closed → Open → Half-Open)
- **Bulkhead Pattern**: Isolate resources to prevent resource exhaustion with configurable parallelization limits
- **Retry Policy**: Exponential backoff with jitter for transient failure recovery
- **Timeout Policy**: Enforce maximum execution times with detailed metrics
- **Fallback Pattern**: Graceful degradation with automatic fallback execution
- **Fluent Configuration**: Intuitive builder pattern for pipeline setup
- **Dependency Injection**: Full Microsoft.Extensions integration
- **Comprehensive Metrics**: Detailed execution statistics and health reporting
- **Thread-Safe**: Concurrent execution support with proper synchronization

## Installation

```bash
dotnet add package DotNetResiliencePipeline
```

## Quick Start

```csharp
using DotNetResiliencePipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;

var services = new ServiceCollection();

services.AddResiliencePipeline(builder =>
{
    builder
        .WithCircuitBreaker("payment-service", policy =>
        {
            policy.FailureThreshold = 5;
            policy.OpenDuration = TimeSpan.FromSeconds(30);
        })
        .WithRetry("api-call", policy =>
        {
            policy.MaxRetries = 3;
            policy.InitialDelay = TimeSpan.FromMilliseconds(100);
        })
        .WithTimeout("operation", TimeSpan.FromSeconds(10))
        .WithBulkhead("resource-pool", maxParallelization: 10, maxQueueLength: 50)
        .WithFallback("graceful-fallback");
});

var provider = services.BuildServiceProvider();
var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();

// Execute with combined policies
var result = await pipeline.ExecuteAsync(
    async ct => await MyOperationAsync(ct),
    circuitBreaker: circuitBreakerPolicy,
    retry: retryPolicy,
    timeout: timeoutPolicy,
    bulkhead: bulkheadPolicy,
    fallback: fallbackPolicy
);
```

## Policy Types

### Circuit Breaker
Prevents cascading failures by monitoring failure rates and temporarily blocking requests.

```csharp
var cbPolicy = new CircuitBreakerPolicy("payment-circuit")
{
    FailureThreshold = 5,
    OpenDuration = TimeSpan.FromSeconds(30),
    SuccessThresholdInHalfOpen = 3
};
```

### Retry
Automatically retries failed operations with configurable backoff strategies.

```csharp
var retryPolicy = new RetryPolicy("api-retry")
{
    MaxRetries = 3,
    InitialDelay = TimeSpan.FromMilliseconds(100),
    Strategy = BackoffStrategy.Exponential,
    BackoffMultiplier = 2.0
};
```

### Timeout
Enforces maximum execution time for operations.

```csharp
var timeoutPolicy = new TimeoutPolicy("operation-timeout")
{
    Timeout = TimeSpan.FromSeconds(10)
};
```

### Bulkhead
Isolates resources to prevent system overload.

```csharp
var bulkheadPolicy = new BulkheadPolicy("resource-isolation")
{
    MaxParallelization = 10,
    MaxQueueLength = 50
};
```

### Fallback
Provides alternative execution paths when primary operations fail.

```csharp
var fallbackPolicy = new FallbackPolicy("graceful-degradation")
{
    FallbackOnAnyException = true,
    FallbackTimeout = TimeSpan.FromSeconds(5)
};
```

## Architecture

### Domain Layer
- `Domain/Policies/` - Policy implementations (CircuitBreaker, Retry, Timeout, Bulkhead, Fallback)
- `Domain/PolicyResult.cs` - Generic result wrapper with metadata

### Service Layer
- `Services/ResiliencyPipelineService.cs` - Main orchestrator
- Individual service classes for each policy type

### Data Layer
- `Data/PolicyRepository.cs` - Policy persistence and retrieval
- `Data/ExecutionHistoryRepository.cs` - Execution metrics and history

### Configuration
- `Configuration/ResiliencyPipelineBuilder.cs` - Fluent builder API
- `Configuration/DependencyInjectionExtensions.cs` - DI integration

## Metrics and Monitoring

Track detailed execution statistics:

```csharp
var stats = pipeline.GetStatistics();
Console.WriteLine($"Success Rate: {stats.SuccessRate}%");
Console.WriteLine($"Total Executions: {stats.TotalExecutions}");

var healthReport = ResiliencyHelper.GenerateHealthReport(pipeline, history);
Console.WriteLine($"Pipeline Health: {healthReport.HealthStatus}");
```

## Thread Safety

All policies and services are thread-safe with proper synchronization primitives:
- Lock-based synchronization for shared state
- Concurrent execution support
- Safe statistics updates

## Advanced Usage

### Custom Backoff Strategies

```csharp
var backoffStrategies = new[] 
{
    RetryPolicy.BackoffStrategy.Fixed,
    RetryPolicy.BackoffStrategy.Linear,
    RetryPolicy.BackoffStrategy.Exponential
};
```

### Policy Configuration Validation

```csharp
var errors = ResiliencyHelper.ValidatePolicy(myPolicy);
if (errors.Count > 0)
{
    foreach (var error in errors)
        Console.WriteLine($"Validation error: {error}");
}
```

### Execution History Analysis

```csharp
var failedExecutions = history.GetFailedExecutions();
var successRate = history.GetSuccessRate();
var errorStats = history.GetErrorStatistics();
```

## Exception Types

The library provides specific exception types for different failure scenarios:

- `CircuitBreakerOpenException` - Circuit breaker is open
- `BulkheadRejectedException` - Bulkhead capacity exceeded
- `OperationTimeoutException` - Operation exceeded timeout
- `MaxRetriesExceededException` - All retry attempts exhausted
- `FallbackFailedException` - Both primary and fallback failed
- `InvalidPolicyConfigurationException` - Invalid policy configuration

## Performance Characteristics

- **Circuit Breaker**: O(1) state transitions
- **Retry**: Configurable exponential backoff (default 100ms-30s)
- **Timeout**: CancellationToken-based with <1ms overhead
- **Bulkhead**: O(1) slot acquisition/release
- **Fallback**: Minimal overhead, timeout-aware

## License

MIT License - Copyright (c) 2026 Vladyslav Zaiets

See LICENSE file for details.

## Author

**Vladyslav Zaiets**
- Website: https://sarmkadan.com
- CTO & Software Architect

## Contributing

Contributions are welcome! Please ensure all code:
- Follows .NET coding standards
- Includes comprehensive comments
- Maintains thread safety
- Includes proper error handling
- Targets .NET 10.0 or later
