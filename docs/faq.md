# Frequently Asked Questions

Common questions and answers about the DotNet Resilience Pipeline.

## General Questions

### What is DotNet Resilience Pipeline?

DotNet Resilience Pipeline is a production-grade library that provides battle-tested resilience patterns for .NET applications. It implements circuit breaker, retry, timeout, bulkhead, and fallback patterns with fluent configuration and comprehensive observability.

### Why should I use it?

Distributed systems are inherently unreliable. Network failures, service degradation, and resource exhaustion are inevitable. This library helps you:
- Prevent cascading failures with circuit breakers
- Handle transient failures with intelligent retry logic
- Enforce execution time limits with timeouts
- Isolate resources with bulkheads
- Gracefully degrade with fallbacks

### What .NET versions are supported?

The library targets **.NET 10.0 and later** exclusively. This enables use of the latest C# language features and performance improvements.

### Is it thread-safe?

Yes, all policies and services are fully thread-safe. They use proper synchronization primitives (locks, semaphores, atomic operations) to ensure safe concurrent execution.

### What are the performance characteristics?

- Circuit Breaker: O(1) state transitions, <100ns overhead
- Retry: Configurable backoff with minimal overhead
- Timeout: <1ms overhead via CancellationToken
- Bulkhead: O(1) slot management, <500ns overhead
- Fallback: Minimal overhead, timeout-aware

## Configuration Questions

### How do I configure multiple policies for the same operation?

Use the fluent builder to configure each policy independently, then pass them all to ExecuteAsync:

```csharp
services.AddResiliencePipeline(builder =>
{
    builder
        .WithCircuitBreaker("api", policy => { /* ... */ })
        .WithRetry("api", policy => { /* ... */ })
        .WithTimeout("api", timeoutValue);
});

var result = await pipeline.ExecuteAsync(
    operation,
    circuitBreaker: cbPolicy,
    retry: retryPolicy,
    timeout: timeoutPolicy
);
```

### What order are policies applied?

Policies are applied in this order:
1. Circuit Breaker (short-circuit if open)
2. Retry loop (with backoff)
3. Timeout (within retry loop)
4. Bulkhead (slot acquisition)
5. User operation
6. Bulkhead (slot release)
7. Fallback (if primary fails)

### Can I use different policy configurations for different operations?

Yes, create separate policy configurations with unique names:

```csharp
builder
    .WithCircuitBreaker("payment-api", policy => { /* strict */ })
    .WithCircuitBreaker("logging-api", policy => { /* lenient */ });
```

### How do I apply policies conditionally?

Pass null for policies you don't want to apply:

```csharp
var cbPolicy = isProduction ? productionPolicy : null;
var result = await pipeline.ExecuteAsync(
    operation,
    circuitBreaker: cbPolicy,
    retry: retryPolicy
);
```

## Circuit Breaker Questions

### What's the difference between states?

- **Closed:** Normal operation, all requests pass through
- **Open:** Service unavailable, requests fail immediately
- **Half-Open:** Testing recovery, limited requests allowed

### How long should the open duration be?

Typically 30-60 seconds, but depends on:
- How long it takes your downstream service to recover
- Your tolerance for degraded service
- Your monitoring and alerting capabilities

Start with 30 seconds and adjust based on observations.

### Can I manually close a circuit breaker?

Yes, access the policy and call Reset():

```csharp
var policy = policyRepository.GetPolicy<CircuitBreakerPolicy>("api");
policy.Reset();
```

### What causes the circuit breaker to transition to half-open?

After `OpenDuration` has elapsed, the circuit automatically transitions to half-open to test if the service has recovered.

## Retry Questions

### What backoff strategy should I use?

- **Fixed:** For simple, stable services (100ms constant)
- **Linear:** For gradual increasing delays (100ms, 200ms, 300ms...)
- **Exponential:** For preventing thundering herd (100ms, 200ms, 400ms, 800ms...)

Exponential backoff is recommended for most scenarios.

### What's the difference between MaxRetries and retry loop?

`MaxRetries` specifies the number of additional attempts after the first attempt fails. So with `MaxRetries=3`, you have 4 total attempts.

### How do I avoid retry loops?

1. Set reasonable `MaxDelay` to prevent infinite backoff
2. Don't retry non-transient errors
3. Combine with circuit breaker to fast-fail
4. Monitor retry rates and adjust thresholds

### Can I retry only specific exception types?

Yes, configure `RetryableExceptions`:

```csharp
var policy = new RetryPolicy("api")
{
    MaxRetries = 3,
    RetryableExceptions = new List<Type>
    {
        typeof(TimeoutException),
        typeof(HttpRequestException)
    }
};
```

## Timeout Questions

### How do timeouts work with retries?

The timeout applies to each individual attempt, not the total retry duration:

```
Attempt 1: timeout 10s
  (fails after 8s)
Attempt 2: timeout 10s
  (fails after 9s)
Attempt 3: timeout 10s
  (fails after 7s)
```

Total time: ~24 seconds with 3 attempts

### Should timeout be longer than backoff max delay?

The timeout applies to the operation execution time, while backoff applies to wait time between attempts. They're independent, but usually:
- Timeout: 10-30 seconds (actual operation time)
- Max Backoff: 30-60 seconds (between retry attempts)

### What happens when timeout occurs?

An `OperationTimeoutException` is thrown, which can be caught or handled by fallback policy.

### How precise is the timeout?

CancellationToken-based, so ~1ms precision. The actual operation must respect the token to enforce timeout.

## Bulkhead Questions

### What's the difference between max parallelization and queue length?

- **MaxParallelization:** Number of concurrent operations allowed
- **MaxQueueLength:** Number of waiting operations in queue

So with MaxParallelization=10 and MaxQueueLength=50, you can have 10 running and 50 waiting.

### Should I use bulkheads for all operations?

Use bulkheads for:
- Database connections (prevent pool exhaustion)
- External API calls (prevent overwhelming downstream)
- CPU-intensive operations (limit concurrency)

Skip for:
- Low-latency in-memory operations
- Operations already protected by other mechanisms

### How do I determine optimal max parallelization?

Start with `Environment.ProcessorCount * 2` and adjust based on:
- Database connection pool size
- Available memory
- Downstream service capacity
- Load testing results

### What happens when bulkhead is full?

A `BulkheadRejectedException` is thrown. Consider:
- Increasing capacity
- Optimizing operation duration
- Using fallback policy

## Fallback Questions

### When should I use fallback?

Use fallback for operations where:
- An alternative result is acceptable
- You want graceful degradation
- The fallback is significantly faster
- Examples: reading from cache, stale data, defaults

### Can I fallback to another operation?

Yes, the fallback parameter is a function:

```csharp
var result = await pipeline.ExecuteAsync(
    async ct => await primaryService.GetAsync(ct),  // primary
    fallback: fallbackPolicy,
    // The fallback service is called if primary fails
);
```

### How do I provide a fallback value?

The fallback is a complete async operation:

```csharp
async Task<User> GetUserWithFallback(int id, CancellationToken ct)
{
    return await pipeline.ExecuteAsync(
        async ct => await api.GetUserAsync(id, ct),
        fallback: new FallbackPolicy("default-user")
    );
}
```

### What if fallback also fails?

A `FallbackFailedException` is thrown, indicating both primary and fallback failed.

## Monitoring Questions

### How do I monitor the pipeline?

Multiple approaches:
1. **Metrics API:** `pipeline.GetStatistics()`
2. **Health Reports:** `ResiliencyHelper.GenerateHealthReport()`
3. **Event Subscription:** `eventPublisher.Subscribe(...)`
4. **Logging:** Microsoft.Extensions.Logging integration

### What metrics should I monitor?

Key metrics:
- Success rate (should be >99%)
- Average operation duration
- Circuit breaker state transitions
- Bulkhead rejection rate
- Retry rate

### How do I set up alerts?

Integrate with your monitoring system:

```csharp
// Example: Alert on low success rate
var stats = pipeline.GetStatistics();
if (stats.SuccessRate < 0.95) // 95% threshold
{
    alerting.NotifyCritical("Low success rate");
}
```

### What's the overhead of monitoring?

Minimal (<1% for statistics). Event subscription adds overhead only if you subscribe to many events.

## Adaptive Timeout Questions

### How does the adaptive timeout work?

`AdaptiveTimeoutPolicy` maintains a sliding window of recent execution times. Once the window
contains at least `MinSampleSize` observations it recomputes the timeout as:

```
newTimeout = Percentile(window, TargetPercentile) × HeadroomFactor
```

The result is clamped to `[MinTimeout, MaxTimeout]`. Recomputation happens at most once per
`AdjustmentInterval`.

Before the window is populated, `InitialTimeout` is used.

### Does it support p99-based timeout adjustment?

Yes. Set `TargetPercentile = 99.0` and choose a `HeadroomFactor` (e.g. `1.5` for 50% headroom):

```csharp
var policy = new AdaptiveTimeoutPolicy("payment-api")
{
    InitialTimeout     = TimeSpan.FromSeconds(5),
    TargetPercentile   = 99.0,
    HeadroomFactor     = 1.5,   // timeout = p99 × 1.5
    MinTimeout         = TimeSpan.FromMilliseconds(500),
    MaxTimeout         = TimeSpan.FromSeconds(30),
};
```

You can also query any percentile on demand:

```csharp
long p50 = policy.GetPercentileExecutionTime(50);
long p99 = policy.GetPercentileExecutionTime(99);
```

### What floor and ceiling should I set?

- **MinTimeout** prevents the policy from becoming too aggressive under low-latency bursts.
  A value like 200–500 ms is usually safe.
- **MaxTimeout** acts as a safety net if latency spikes. Set it to the maximum acceptable
  wait time for your SLA (e.g. 30 s).

### How do I monitor adaptation?

Use `AdaptiveTimeoutService.GetAdaptationSummary(policy)` to retrieve a snapshot dictionary
containing `CurrentTimeoutMs`, `TotalAdjustments`, `P95ExecutionTimeMs`, `TimeoutPercentage`,
and more. The `GetSnapshot()` method on the policy also includes `P50ExecutionTimeMs`,
`P95ExecutionTimeMs`, and `P99ExecutionTimeMs`.

### How do I register it with dependency injection?

```csharp
services.AddAdaptiveTimeout("payment-api", TimeSpan.FromSeconds(5), policy =>
{
    policy.TargetPercentile   = 99.0;
    policy.HeadroomFactor     = 1.5;
    policy.AdjustmentInterval = TimeSpan.FromSeconds(60);
});
```

## Troubleshooting Questions

### Why is the circuit breaker staying open?

Possible causes:
1. Failure threshold is too low
2. Upstream service isn't actually recovering
3. OpenDuration is elapsed but circuit stays open? Check for continuous failures
4. Success threshold in half-open isn't being reached

### Why aren't retries working?

Check:
1. Is MaxRetries > 0?
2. Is the exception type in RetryableExceptions?
3. Is the initial delay realistic?
4. Is timeout too short for operation?

### Why is bulkhead rejecting requests?

1. Too many concurrent requests
2. Operations are hanging (timeout not enforced)
3. MaxQueueLength is too small
4. Increase capacity or optimize operation duration

### How do I debug policy execution?

Enable logging:

```csharp
services.AddLogging(builder =>
{
    builder.AddConsole();
    builder.SetMinimumLevel(LogLevel.Debug);
});
```

Subscribe to events:

```csharp
eventPublisher.Subscribe((@event) =>
{
    Console.WriteLine($"{@event.EventType}: {@event.PolicyName}");
});
```

## Performance Questions

### What's the memory footprint?

- Per policy: 1-5 KB
- Per execution: ~100 bytes
- Execution history: 100-200 bytes per record

### Is there a performance impact of using policies?

Minimal:
- Circuit breaker: <100ns
- Retry: depends on backoff
- Timeout: <1ms
- Bulkhead: <500ns
- Fallback: <100ns

For most applications, policy overhead is negligible.

### Should I cache policies?

Yes, retrieve policies once and reuse:

```csharp
// Good: retrieve once
var cbPolicy = repository.GetPolicy<CircuitBreakerPolicy>("api");

for (int i = 0; i < 1000; i++)
{
    var result = await pipeline.ExecuteAsync(
        operation,
        circuitBreaker: cbPolicy
    );
}

// Avoid: retrieve in loop
for (int i = 0; i < 1000; i++)
{
    var cbPolicy = repository.GetPolicy<CircuitBreakerPolicy>("api"); // Don't do this
    var result = await pipeline.ExecuteAsync(operation, circuitBreaker: cbPolicy);
}
```

## Development Questions

### Can I extend the library?

Yes, implement base classes:

```csharp
// Custom policy
public class CustomPolicy : ResiliencyPolicy
{
    public override Task<PolicyResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        // Custom implementation
    }
}

// Custom repository
public class CustomRepository<T> : IRepository<T>
{
    // Implementation
}
```

### How do I contribute?

1. Fork the repository
2. Create a feature branch
3. Make changes with tests
4. Submit a pull request

See CONTRIBUTING.md for guidelines.

### What's the license?

MIT License - free for commercial and personal use.
