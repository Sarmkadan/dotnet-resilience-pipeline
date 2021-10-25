# ResiliencyPipelineBuilder

Fluent builder used to compose a resilience pipeline by registering policies such as retry, circuit breaker, timeout, bulkhead, and fallback. The builder accumulates the selected policies and produces an immutable `ResiliencyPipelineService` that can be invoked to execute user‑provided operations with the configured resilience behavior.

## API

### Constructor

- **`ResiliencyPipelineBuilder()`**  
  - *Purpose*: Creates a new, empty builder instance.  
  - *Parameters*: None.  
  - *Return Value*: A builder ready for policy configuration.  
  - *Exceptions*: None.

### WithCircuitBreaker

- **`ResiliencyPipelineBuilder WithCircuitBreaker(CircuitBreakerPolicy policy)`**  
  - *Purpose*: Adds a circuit breaker policy to the pipeline.  
  - *Parameters*: `policy` – the circuit breaker policy to apply.  
  - *Return Value*: The same builder instance, allowing further chaining.  
  - *Exceptions*: `ArgumentNullException` if `policy` is `null`.

### WithRetry

- **`ResiliencyPipelineBuilder WithRetry(RetryPolicy policy)`**  
  - *Purpose*: Adds a retry policy to the pipeline.  
  - *Parameters*: `policy` – the retry policy to apply.  
  - *Return Value*: The same builder instance.  
  - *Exceptions*: `ArgumentNullException` if `policy` is `null`.

### WithTimeout

- **`ResiliencyPipelineBuilder WithTimeout(TimeoutPolicy policy)`**  
  - *Purpose*: Adds a timeout policy to the pipeline.  
  - *Parameters*: `policy` – the timeout policy to apply.  
  - *Return Value*: The same builder instance.  
  - *Exceptions*: `ArgumentNullException` if `policy` is `null`.

### WithBulkhead

- **`ResiliencyPipelineBuilder WithBulkhead(BulkheadPolicy policy)`**  
  - *Purpose*: Adds a bulkhead policy to the pipeline.  
  - *Parameters*: `policy` – the bulkhead policy to apply.  
  - *Return Value*: The same builder instance.  
  - *Exceptions*: `ArgumentNullException` if `policy` is `null`.

### WithFallback

- **`ResiliencyPipelineBuilder WithFallback(FallbackPolicy policy)`**  
  - *Purpose*: Adds a fallback policy to the pipeline.  
  - *Parameters*: `policy` – the fallback policy to apply.  
  - *Return Value*: The same builder instance.  
  - *Exceptions*: `ArgumentNullException` if `policy` is `null`.

### WithFallbackAction<T>

- **`ResiliencyPipelineBuilder WithFallbackAction<T>(Func<T> fallbackFactory)`**  
  - *Purpose*: Adds a fallback that invokes a supplied factory when the pipeline fails.  
  - *Parameters*: `fallbackFactory` – a function that produces the fallback value of type `T`.  
  - *Return Value*: The same builder instance.  
  - *Exceptions*: `ArgumentNullException` if `fallbackFactory` is `null`.

### Build

- **`ResiliencyPipelineService Build()`**  
  - *Purpose*: Constructs an immutable pipeline service from the configured policies.  
  - *Parameters*: None.  
  - *Return Value*: A `ResiliencyPipelineService` ready for use.  
  - *Exceptions*:  
    - `InvalidOperationException` if the builder is in an invalid state (e.g., conflicting policies) and a pipeline cannot be assembled.  
    - `ObjectDisposedException` if the builder has already been used to build a service and reuse a disposed instance (if applicable).

### GetCircuitBreakerPolicy

- **`CircuitBreakerPolicy? GetCircuitBreakerPolicy()`**  
  - *Purpose*: Retrieves the circuit breaker policy that has been added, if any.  
  - *Parameters*: None.  
  - *Return Value*: The configured `CircuitBreakerPolicy` or `null` when none was added.  
  - *Exceptions*: None.

### GetRetryPolicy

- **`RetryPolicy? GetRetryPolicy()`**  
  - *Purpose*: Retrieves the retry policy that has been added, if any.  
  - *Parameters*: None.  
  - *Return Value*: The configured `RetryPolicy` or `null`.  
  - *Exceptions*: None.

### GetTimeoutPolicy

- **`TimeoutPolicy? GetTimeoutPolicy()`**  
  - *Purpose*: Retrieves the timeout policy that has been added, if any.  
  - *Parameters*: None.  
  - *Return Value*: The configured `TimeoutPolicy` or `null`.  
  - *Exceptions*: None.

### GetBulkheadPolicy

- **`BulkheadPolicy? GetBulkheadPolicy()`**  
  - *Purpose*: Retrieves the bulkhead policy that has been added, if any.  
  - *Parameters*: None.  
  - *Return Value*: The configured `BulkheadPolicy` or `null`.  
  - *Exceptions*: None.

### GetFallbackPolicy

- **`FallbackPolicy? GetFallbackPolicy()`**  
  - *Purpose*: Retrieves the fallback policy that has been added, if any.  
  - *Parameters*: None.  
  - *Return Value*: The configured `FallbackPolicy` or `null`.  
  - *Exceptions*: None.

## Usage

### Example 1: Simple retry with timeout

```csharp
var pipeline = new ResiliencyPipelineBuilder()
    .WithRetry(Policy.Handle<IOException>()
                     .WaitAndRetryAsync(3, retryAttempt => TimeSpan.FromSeconds(Math.Pow(2, retryAttempt))))
    .WithTimeout(Policy.TimeoutAsync<HttpResponseMessage>(TimeSpan.FromSeconds(10)))
    .Build();

// Use the pipeline
HttpResponseMessage response = await pipeline.ExecuteAsync(() => httpClient.GetAsync("https://example.com/api"));
```

### Example 2: Circuit breaker, bulkhead, and fallback action

```csharp
var pipeline = new ResiliencyPipelineBuilder()
    .WithCircuitBreaker(Policy.Handle<HttpRequestException>()
                               .CircuitBreakerAsync(5, TimeSpan.FromSeconds(30)))
    .WithBulkhead(Policy.BulkheadAsync<HttpResponseMessage>(maxParallelization: 10, maxQueuingActions: 5))
    .WithFallbackAction<HttpResponseMessage>(() => new HttpResponseMessage(System.Net.HttpStatusCode.ServiceUnavailable)
                                               { ReasonPhrase = "Service temporarily unavailable" })
    .Build();

// Execute a request protected by the composed policies
HttpResponseMessage result = await pipeline.ExecuteAsync(() => flakyService.GetDataAsync());
```

## Notes

- The builder is **not thread-safe** for concurrent configuration; multiple threads should not call the `With*` methods on the same instance simultaneously.  
- Once `Build()` is called, the returned `ResiliencyPipelineService` is immutable and safe to use from any number of threads.  
- Calling `Build()` more than once yields a new service instance each time; the builder’s internal state is not reset, so subsequent builds will include the same policies as the first build.  
- If a policy type is added more than once, the later registration overwrites the earlier one (the builder keeps only the last supplied policy of each type).  
- Passing `null` to any `With*` method results in an `ArgumentNullException`; the builder does not accept null policies.  
- The fallback action generic method (`WithFallbackAction<T>`) captures the fallback value type at build time; the produced pipeline will return that type when the fallback is invoked.  
- No automatic validation of policy compatibility is performed; invalid combinations (e.g., a timeout shorter than the minimum retry delay) will only surface as exceptions when the pipeline is executed.
