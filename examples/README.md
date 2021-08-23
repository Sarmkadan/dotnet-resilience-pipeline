# DotNet Resilience Pipeline - Examples

This directory contains comprehensive examples demonstrating all resilience patterns and use cases.

## Examples Overview

### 1. BasicUsage.cs
**Demonstrates:** Circuit Breaker + Retry + Timeout

Simple introduction to the resilience pipeline covering:
- Setting up DI container with resilience policies
- Configuring circuit breaker, retry, and timeout
- Executing operations with combined policies
- Monitoring basic statistics

**Run:**
```bash
dotnet run --project BasicUsage.cs
```

### 2. MicroserviceIntegration.cs
**Demonstrates:** Realistic microservice scenarios with multiple policies

Shows how to:
- Configure different policies for different services
- Chain multiple operations (user lookup → order fetch → notification)
- Handle service-specific configurations
- Display service-level metrics

**Run:**
```bash
dotnet run --project MicroserviceIntegration.cs
```

### 3. BulkheadPatternExample.cs
**Demonstrates:** Resource isolation and concurrent load

Shows:
- Bulkhead pattern for limiting concurrent executions
- Managing queue length for waiting requests
- Concurrent task execution with proper slot management
- How bulkhead prevents resource exhaustion

**Run:**
```bash
dotnet run --project BulkheadPatternExample.cs
```

### 4. CircuitBreakerSimulation.cs
**Demonstrates:** Circuit breaker state machine transitions

Shows all states:
- **CLOSED**: Normal operation with increasing failures
- **OPEN**: Fast failures when threshold exceeded
- **HALF-OPEN**: Testing recovery with limited requests
- Back to **CLOSED** when recovered

**Run:**
```bash
dotnet run --project CircuitBreakerSimulation.cs
```

### 5. FallbackPatternExample.cs
**Demonstrates:** Graceful degradation with fallback operations

Shows:
- Primary service failure scenarios
- Automatic fallback execution
- Using cached/stale data as fallback
- Combined circuit breaker + fallback

**Run:**
```bash
dotnet run --project FallbackPatternExample.cs
```

### 6. MetricsMonitoringExample.cs
**Demonstrates:** Performance tracking and observability

Shows:
- Collecting execution metrics
- Calculating success rates and percentiles
- Health report generation
- Real-time performance statistics

**Run:**
```bash
dotnet run --project MetricsMonitoringExample.cs
```

## Running Examples

### Build All Examples
```bash
cd examples
dotnet build
```

### Run Individual Example
```bash
dotnet run --project BasicUsage.cs
dotnet run --project MicroserviceIntegration.cs
dotnet run --project BulkheadPatternExample.cs
dotnet run --project CircuitBreakerSimulation.cs
dotnet run --project FallbackPatternExample.cs
dotnet run --project MetricsMonitoringExample.cs
```

### Run with Makefile
From project root:
```bash
make examples          # Build and run all examples
make example-basic     # Run basic usage example
make example-micro     # Run microservice example
```

## Learning Path

**Beginner:**
1. Start with `BasicUsage.cs` - Understand basic concepts
2. Read `docs/getting-started.md` - Formal introduction

**Intermediate:**
3. Study `CircuitBreakerSimulation.cs` - Deep dive into circuit breaker
4. Review `BulkheadPatternExample.cs` - Resource isolation
5. Explore `FallbackPatternExample.cs` - Graceful degradation

**Advanced:**
6. Examine `MicroserviceIntegration.cs` - Real-world scenarios
7. Analyze `MetricsMonitoringExample.cs` - Production monitoring

## Key Concepts

### Circuit Breaker
Prevents cascading failures by monitoring failure rates:
- Stops sending requests when failure threshold exceeded
- Automatically tests recovery in half-open state
- Returns to normal when recovery confirmed

### Retry
Handles transient failures with configurable backoff:
- Exponential backoff to prevent thundering herd
- Configurable delay between retries
- Support for specific exception types

### Timeout
Enforces maximum execution duration:
- Uses CancellationToken for graceful cancellation
- Low overhead (<1ms)
- Works with other policies

### Bulkhead
Isolates resources to prevent exhaustion:
- Limits concurrent executions
- Manages queue of waiting requests
- Prevents one resource from starving others

### Fallback
Provides alternative execution paths:
- Used when primary operation fails
- Can use cached data or defaults
- Enables graceful degradation

## Common Patterns

### Pattern 1: External API Call
```csharp
// Circuit Breaker → Retry → Timeout
var result = await pipeline.ExecuteAsync(
    async ct => await externalApi.CallAsync(ct),
    circuitBreaker: cbPolicy,
    retry: retryPolicy,
    timeout: timeoutPolicy
);
```

### Pattern 2: Database Query
```csharp
// Bulkhead → Timeout
var result = await pipeline.ExecuteAsync(
    async ct => await database.QueryAsync(ct),
    bulkhead: bulkheadPolicy,
    timeout: timeoutPolicy
);
```

### Pattern 3: Microservice Call with Fallback
```csharp
// Circuit Breaker → Retry → Timeout → Fallback
var result = await pipeline.ExecuteAsync(
    async ct => await userService.GetAsync(userId, ct),
    circuitBreaker: cbPolicy,
    retry: retryPolicy,
    timeout: timeoutPolicy,
    fallback: fallbackPolicy
);
```

## Configuration Tips

### Conservative (Safe) Configuration
- Circuit Breaker: threshold=5, duration=60s
- Retry: attempts=3, exponential backoff
- Timeout: 30s
- Bulkhead: max=10

### Aggressive (Fast Fail) Configuration
- Circuit Breaker: threshold=2, duration=10s
- Retry: attempts=1, no backoff
- Timeout: 5s
- Bulkhead: max=50

## Troubleshooting Examples

### "Circuit breaker stays open"
Look at `CircuitBreakerSimulation.cs` and increase failure threshold or reduce open duration.

### "Bulkhead rejecting too many requests"
See `BulkheadPatternExample.cs` and increase max parallelization or reduce operation duration.

### "Timeout exceptions occurring"
Check operation duration in examples and adjust timeout policy accordingly.

## Performance

All examples run efficiently:
- <100ms overhead per operation for policy checks
- Examples complete in <10 seconds
- Minimal memory footprint

## Further Reading

- `docs/getting-started.md` - Detailed setup guide
- `docs/architecture.md` - System architecture
- `docs/api-reference.md` - Complete API docs
- `README.md` - Project overview

## Contributing

To add a new example:

1. Create new `.cs` file in `examples/`
2. Follow the header format (author attribution)
3. Include comprehensive comments
4. Add to this README
5. Test thoroughly
6. Submit PR with description

## Questions?

Refer to `docs/faq.md` for common questions and answers.

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
