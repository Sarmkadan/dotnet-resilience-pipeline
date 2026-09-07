# DotnetResiliencePipelineOptionsValidator

`DotnetResiliencePipelineOptionsValidator` implements
`IValidateOptions<DotnetResiliencePipelineOptions>` and integrates validation of
[`DotnetResiliencePipelineOptions`](DotnetResiliencePipelineOptions.md) with the
.NET options pipeline.

## Validation behavior

`Validate` first rejects a `null` options instance. It then runs
`Validator.TryValidateObject` against the top-level options object with
`validateAllProperties: true`. If data-annotation validation fails, all non-null
annotation error messages are joined with a single space and returned as one
failure.

The validator next checks the nested sections in this order: circuit breaker,
retry, timeout, bulkhead, and fallback. It returns immediately on the first
failure, so a validation result contains only the first failed nested rule.
Bounds shown below are inclusive.

### Circuit breaker

| Option | Valid range | Failure message |
| --- | --- | --- |
| `CircuitBreaker.FailureThreshold` | 1 through 1000 | `CircuitBreaker.FailureThreshold must be between 1 and 1000` |
| `CircuitBreaker.OpenDurationSeconds` | 1 through 3600 | `CircuitBreaker.OpenDurationSeconds must be between 1 and 3600` |
| `CircuitBreaker.SuccessThresholdInHalfOpen` | 1 through 100 | `CircuitBreaker.SuccessThresholdInHalfOpen must be between 1 and 100` |

### Retry

| Option | Valid range or relationship | Failure message |
| --- | --- | --- |
| `Retry.MaxRetries` | 0 through 20 | `Retry.MaxRetries must be between 0 and 20` |
| `Retry.InitialDelayMs` | 1 through 10000 | `Retry.InitialDelayMs must be between 1 and 10000` |
| `Retry.MaxDelayMs` | 1 through 300000 | `Retry.MaxDelayMs must be between 1 and 300000` |
| `Retry.MaxDelayMs` relative to `Retry.InitialDelayMs` | Greater than or equal to `Retry.InitialDelayMs` | `Retry.MaxDelayMs must be greater than or equal to Retry.InitialDelayMs` |
| `Retry.BackoffMultiplier` | 1.0 through 10.0 | `Retry.BackoffMultiplier must be between 1.0 and 10.0` |
| `Retry.JitterFactor` | 0.0 through 1.0 | `Retry.JitterFactor must be between 0.0 and 1.0` |

`Retry.Strategy` and `Retry.UseJitter` have no rules in this validator. The
`JitterFactor` range is enforced regardless of the value of `UseJitter`.

### Timeout

| Option | Valid range | Failure message |
| --- | --- | --- |
| `Timeout.TimeoutSeconds` | 1 through 300 | `Timeout.TimeoutSeconds must be between 1 and 300` |

### Bulkhead

| Option | Valid range | Failure message |
| --- | --- | --- |
| `Bulkhead.MaxParallelization` | 1 through 1000 | `Bulkhead.MaxParallelization must be between 1 and 1000` |
| `Bulkhead.MaxQueueLength` | 0 through 10000 | `Bulkhead.MaxQueueLength must be between 0 and 10000` |

### Fallback

| Option | Valid range | Failure message |
| --- | --- | --- |
| `Fallback.FallbackTimeoutSeconds` | 1 through 60 | `Fallback.FallbackTimeoutSeconds must be between 1 and 60` |

`Fallback.FallbackOnAnyException` has no rule in this validator.

### Other cases

- A `null` options instance fails with `Configuration options cannot be null`.
- `TimeWindow` is not checked by this validator.
- The options name passed to `Validate` does not alter validation; the same
  rules apply to default and named options.
- When every check succeeds, the validator returns
  `ValidateOptionsResult.Success`.

## Registration

The library's `AddResiliencePipelineWithOptions` extension configures the
options and registers the validator as a singleton:

```csharp
services.AddResiliencePipelineWithOptions(options =>
{
    options.Retry.MaxRetries = 5;
    options.Timeout.TimeoutSeconds = 30;
});
```

For direct use of the .NET options pipeline, register the options and validator
explicitly:

```csharp
using DotNetResiliencePipeline.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

services.Configure<DotnetResiliencePipelineOptions>(configurationSection);
services.AddSingleton<
    IValidateOptions<DotnetResiliencePipelineOptions>,
    DotnetResiliencePipelineOptionsValidator>();
```

The validator runs when `IOptions<DotnetResiliencePipelineOptions>.Value` is
resolved. If startup-time validation is required, add an options-builder
registration with `ValidateOnStart()` in addition to registering the validator.
