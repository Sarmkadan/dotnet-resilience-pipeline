# Policy defaults and validation limits

This reference covers the public-settable configuration properties declared by the policy classes in `src/Domain/Policies`. Runtime statistics and derived state with private setters are not configuration and are therefore omitted. TimeSpan values show both the source initializer and their resulting duration.

## Policy defaults

### `RetryPolicy`

| Property | Type | Default |
| --- | --- | --- |
| `MaxRetries` | `int` | `3` |
| `InitialDelay` | `TimeSpan` | `TimeSpan.FromMilliseconds(100)` (100 ms) |
| `Strategy` | `BackoffStrategy` | `BackoffStrategy.Exponential` |
| `MaxDelay` | `TimeSpan` | `TimeSpan.FromSeconds(30)` (30 s) |
| `BackoffMultiplier` | `double` | `2.0` |
| `UseJitter` | `bool` | `true` |
| `JitterFactor` | `double` | `1.0` |
| `UseDecorrelatedJitter` | `bool` | `false` (the default value of an uninitialized `bool`) |
| `RetryableExceptions` | `List<Type>` | `TimeoutException` and `HttpRequestException` (the constructor replaces the empty-list property initializer) |

### `CircuitBreakerPolicy`

| Property | Type | Default |
| --- | --- | --- |
| `FailureThreshold` | `int` | `5` |
| `OpenDuration` | `TimeSpan` | `TimeSpan.FromSeconds(30)` (30 s) |
| `SuccessThresholdInHalfOpen` | `int` | `3` (from the backing-field initializer) |

### `TimeoutPolicy`

| Property | Type | Default |
| --- | --- | --- |
| `Timeout` | `TimeSpan` | `TimeSpan.FromSeconds(10)` (10 s) |

### `AdaptiveTimeoutPolicy`

| Property | Type | Default |
| --- | --- | --- |
| `InitialTimeout` | `TimeSpan` | `TimeSpan.FromSeconds(10)` (10 s) |
| `MinTimeout` | `TimeSpan` | `TimeSpan.FromMilliseconds(200)` (200 ms) |
| `MaxTimeout` | `TimeSpan` | `TimeSpan.FromSeconds(60)` (60 s) |
| `TargetPercentile` | `double` | `95.0` |
| `HeadroomFactor` | `double` | `1.2` |
| `WindowSize` | `int` | `100` |
| `MinSampleSize` | `int` | `10` |
| `AdjustmentInterval` | `TimeSpan` | `TimeSpan.FromSeconds(30)` (30 s) |

`CurrentTimeout` is runtime state rather than a configurable property. Its constructor-established initial value is `InitialTimeout` (10 s with the defaults above).

### `BulkheadPolicy`

| Property | Type | Default |
| --- | --- | --- |
| `MaxParallelization` | `int` | `10` |
| `MaxQueueLength` | `int` | `50` |
| `MaxQueueWaitTimeout` | `TimeSpan` | `TimeSpan.FromSeconds(30)` (30 s) |

### `FallbackPolicy`

| Property | Type | Default |
| --- | --- | --- |
| `FallbackTriggerExceptions` | `List<Type>` | Empty list (`new()`) |
| `FallbackOnAnyException` | `bool` | `true` |
| `FallbackTimeout` | `TimeSpan` | `TimeSpan.FromSeconds(5)` (5 s) |

## Options validator limits

`DotnetResiliencePipelineOptionsValidator` validates the configuration-option representations below. All stated endpoints are inclusive.

| Options property | Accepted value | Corresponding policy property |
| --- | --- | --- |
| `CircuitBreaker.FailureThreshold` | 1–1,000 | `CircuitBreakerPolicy.FailureThreshold` |
| `CircuitBreaker.OpenDurationSeconds` | 1–3,600 seconds | `CircuitBreakerPolicy.OpenDuration` |
| `CircuitBreaker.SuccessThresholdInHalfOpen` | 1–100 | `CircuitBreakerPolicy.SuccessThresholdInHalfOpen` |
| `Retry.MaxRetries` | 0–20 | `RetryPolicy.MaxRetries` |
| `Retry.InitialDelayMs` | 1–10,000 ms | `RetryPolicy.InitialDelay` |
| `Retry.MaxDelayMs` | 1–300,000 ms | `RetryPolicy.MaxDelay` |
| `Retry.BackoffMultiplier` | 1.0–10.0 | `RetryPolicy.BackoffMultiplier` |
| `Retry.JitterFactor` | 0.0–1.0 | `RetryPolicy.JitterFactor` |
| `Timeout.TimeoutSeconds` | 1–300 seconds | `TimeoutPolicy.Timeout` |
| `Bulkhead.MaxParallelization` | 1–1,000 | `BulkheadPolicy.MaxParallelization` |
| `Bulkhead.MaxQueueLength` | 0–10,000 | `BulkheadPolicy.MaxQueueLength` |
| `Fallback.FallbackTimeoutSeconds` | 1–60 seconds | `FallbackPolicy.FallbackTimeout` |

In addition to the individual ranges, `Retry.MaxDelayMs` must be greater than or equal to `Retry.InitialDelayMs`.

The validator does not impose limits on `Retry.Strategy`, `Retry.UseJitter`, or `Fallback.FallbackOnAnyException`. It has no `AdaptiveTimeoutPolicy` options section, and it does not validate `RetryPolicy.UseDecorrelatedJitter`, `RetryPolicy.RetryableExceptions`, `BulkheadPolicy.MaxQueueWaitTimeout`, or `FallbackPolicy.FallbackTriggerExceptions`.
