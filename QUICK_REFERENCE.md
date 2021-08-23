# DotNet Resilience Pipeline - Quick Reference

Quick access to common tasks and patterns.

## Installation

```bash
# NuGet package
dotnet add package DotNetResiliencePipeline

# From source
git clone https://github.com/sarmkadan/dotnet-resilience-pipeline.git
dotnet build
```

## Basic Setup

```csharp
using DotNetResiliencePipeline.Configuration;

services.AddResiliencePipeline(builder =>
{
    builder
        .WithCircuitBreaker("api", policy => 
        {
            policy.FailureThreshold = 5;
            policy.OpenDuration = TimeSpan.FromSeconds(30);
        })
        .WithRetry("api", policy =>
        {
            policy.MaxRetries = 3;
            policy.InitialDelay = TimeSpan.FromMilliseconds(100);
            policy.Strategy = RetryPolicy.BackoffStrategy.Exponential;
        })
        .WithTimeout("api", TimeSpan.FromSeconds(10))
        .WithBulkhead("api", maxParallelization: 20, maxQueueLength: 100)
        .WithFallback("api");
});
```

## Execution Patterns

### Basic Pattern
```csharp
var result = await pipeline.ExecuteAsync(
    async ct => await operation(ct)
);
```

### With Circuit Breaker
```csharp
var result = await pipeline.ExecuteAsync(
    async ct => await externalApi.CallAsync(ct),
    circuitBreaker: cbPolicy
);
```

### With Retry
```csharp
var result = await pipeline.ExecuteAsync(
    async ct => await api.CallAsync(ct),
    retry: retryPolicy
);
```

### With Timeout
```csharp
var result = await pipeline.ExecuteAsync(
    async ct => await operation(ct),
    timeout: timeoutPolicy
);
```

### With Bulkhead
```csharp
var result = await pipeline.ExecuteAsync(
    async ct => await databaseQuery(ct),
    bulkhead: bulkheadPolicy
);
```

### With Fallback
```csharp
var result = await pipeline.ExecuteAsync(
    async ct => await primaryService.GetAsync(ct),
    fallback: fallbackPolicy
);
```

### Complete Pattern
```csharp
var result = await pipeline.ExecuteAsync(
    async ct => await operation(ct),
    circuitBreaker: cbPolicy,
    retry: retryPolicy,
    timeout: timeoutPolicy,
    bulkhead: bulkheadPolicy,
    fallback: fallbackPolicy
);
```

## Configuration Quick Start

### Strict (Safe)
```csharp
// For critical operations
CircuitBreaker: threshold=3, duration=60s
Retry: attempts=5, exponential backoff
Timeout: 30s
Bulkhead: max=5
```

### Balanced (Default)
```csharp
// For normal operations
CircuitBreaker: threshold=5, duration=30s
Retry: attempts=3, exponential backoff
Timeout: 10s
Bulkhead: max=20
```

### Lenient (Fast)
```csharp
// For non-critical operations
CircuitBreaker: threshold=10, duration=10s
Retry: attempts=1, no backoff
Timeout: 5s
Bulkhead: max=50
```

## Metrics & Monitoring

```csharp
// Get statistics
var stats = pipeline.GetStatistics();
Console.WriteLine($"Success Rate: {stats.SuccessRate:P}");
Console.WriteLine($"Avg Duration: {stats.AverageDurationMs}ms");

// Generate health report
var report = ResiliencyHelper.GenerateHealthReport(pipeline, history);

// Subscribe to events
eventPublisher.Subscribe((event) =>
{
    Console.WriteLine($"Event: {event.EventType}");
});
```

## Docker

```bash
# Build image
docker build -t dotnet-resilience-pipeline .

# Run container
docker run -p 5000:5000 dotnet-resilience-pipeline

# Docker Compose
docker-compose up -d
```

## Kubernetes

```bash
# Apply manifests
kubectl apply -f docs/kubernetes-deployment.yaml

# Check deployment
kubectl get pods -n resilience
kubectl logs deployment/resilience-pipeline -n resilience -f

# Port forward
kubectl port-forward -n resilience svc/resilience-pipeline 5000:80
```

## Build & Test

```bash
# Build
dotnet build -c Release

# Test
dotnet test -c Release

# Pack
dotnet pack -c Release

# Using Makefile
make build
make test
make pack
```

## Project Structure

```
dotnet-resilience-pipeline/
├── src/
│   ├── Domain/            # Policy implementations
│   ├── Services/          # Execution services
│   ├── Data/              # Repository pattern
│   ├── Configuration/     # DI setup
│   ├── Utilities/         # Helpers
│   ├── Middleware/        # HTTP middleware
│   ├── Events/            # Event system
│   └── Integration/       # External services
├── examples/              # Runnable examples
├── docs/                  # Comprehensive docs
├── tests/                 # Unit tests
├── Dockerfile             # Docker image
├── docker-compose.yml     # Full stack
├── Makefile              # Build automation
├── CHANGELOG.md          # Version history
└── CONTRIBUTING.md       # Contribution guide
```

## Common Issues

### Circuit Breaker Stays Open
- Increase `FailureThreshold` (default: 5)
- Reduce `OpenDuration` (default: 30s)
- Check if service actually recovered

### Timeout Errors
- Increase `Timeout` duration
- Check operation performance
- Verify network conditions

### Bulkhead Rejections
- Increase `MaxParallelization`
- Reduce operation duration
- Add queue or retry logic

### Retry Loop
- Set reasonable `MaxDelay`
- Verify transient error classification
- Consider adding circuit breaker

## Documentation

- **README.md** - Overview and quick start
- **docs/getting-started.md** - Detailed setup
- **docs/architecture.md** - System design
- **docs/api-reference.md** - Complete API
- **docs/deployment.md** - Production deployment
- **docs/faq.md** - Common questions
- **docs/kubernetes-deployment.yaml** - K8s manifests
- **examples/** - Runnable samples
- **CONTRIBUTING.md** - Development guide

## Resources

- GitHub: https://github.com/sarmkadan/dotnet-resilience-pipeline
- Author: https://sarmkadan.com
- NuGet: https://www.nuget.org/packages/DotNetResiliencePipeline

## Examples

```bash
# Build examples
cd examples && dotnet build

# Run specific example
dotnet run --project BasicUsage.cs
dotnet run --project MicroserviceIntegration.cs
dotnet run --project BulkheadPatternExample.cs
dotnet run --project CircuitBreakerSimulation.cs
dotnet run --project FallbackPatternExample.cs
dotnet run --project MetricsMonitoringExample.cs
```

## Policy Policies

| Policy | Purpose | Configuration |
|--------|---------|----------------|
| CircuitBreaker | Prevent cascading failures | threshold, duration, recovery |
| Retry | Handle transient failures | attempts, backoff, strategy |
| Timeout | Enforce execution limits | duration |
| Bulkhead | Isolate resources | parallelization, queue |
| Fallback | Graceful degradation | exceptions, timeout |

## State Machine

### Circuit Breaker States
```
CLOSED  → (failures exceed threshold) → OPEN
  ↑                                        │
  └─ (recovery confirmed) ← HALF-OPEN ←──┘
                  ↑
          (elapsed + test)
```

## Performance Tips

1. **Reuse policies** - Don't create per-request
2. **Set appropriate timeouts** - Not too short, not too long
3. **Use bulkhead for resources** - Prevent exhaustion
4. **Monitor metrics** - Track success rates
5. **Test under load** - Validate configuration

## License

MIT License - Free for commercial use

## Support

For issues or questions:
- GitHub Issues: https://github.com/sarmkadan/dotnet-resilience-pipeline/issues
- Email: rutova2@gmail.com

---

**Built by [Vladyslav Zaiets](https://sarmkadan.com) - CTO & Software Architect**
