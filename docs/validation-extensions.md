# Validation extension reference

The `*Validation.cs` files under `src/` expose extension methods for checking the current state of records, policies, services, middleware, and diagnostic results. This page describes the rules implemented by those helpers; it does not imply additional domain rules beyond the checks in the source.

## Common API pattern

Most helpers provide this trio for their target type:

- `Validate(value)` returns an `IReadOnlyList<string>` of all problems the helper can collect. An empty list means the value passed every implemented check.
- `IsValid(value)` is the Boolean convenience form. It returns `true` when validation produces no problems. Depending on the helper, a `null` value either returns `false` or causes `ArgumentNullException`; see the class notes below.
- `EnsureValid(value)` returns normally when the value is valid and throws `ArgumentException` containing the collected problems when it is invalid. A `null` target ultimately causes `ArgumentNullException` in every helper.

These are state validators, not constructors or sanitizers: they do not modify the target. Some validators call methods such as `GetStatistics()`, `GetLogs()`, `GetSummary()`, or `GetRules()`, so their result reflects the state observed when validation runs.

## Rules by helper class

### `ExecutionRecordValidation`

Target: `ExecutionRecord`.

- `ExecutionId` must be nonblank and parse as a GUID.
- `PolicyName` and `PolicyId` must be nonblank.
- `ExecutionTimeMs` and `AttemptCount` must be non-negative.
- `ExecutedAt` must not be the default `DateTime` value and must not be more than five minutes later than the current UTC time.
- `Metadata` must not be `null`.
- A nonempty `ErrorMessage` may contain at most 10,000 characters; a nonempty `ErrorType` may contain at most 500.

`IsValid(null)` returns `false`; `Validate(null)` and `EnsureValid(null)` throw `ArgumentNullException`.

### `PolicyCacheServiceValidation`

Target: `PolicyCacheService`.

- `DefaultTtl` must be greater than zero.
- `MaxCacheSize` must be greater than zero.
- From `GetStatistics()`: `TotalEntries`, `ValidEntries`, and `ExpiredEntries` must be non-negative.
- `TotalEntries` must equal `ValidEntries + ExpiredEntries`.
- `HitRate` is reported when it is below 0 or above 100.
- `AverageTtl` must be non-negative.

`IsValid(null)` returns `false`; the other two methods throw `ArgumentNullException` for `null`.

Because the `HitRate` check uses only `<` and `>`, a `double.NaN` value is not rejected by this helper.

### `CliCommandHandlerValidation`

Target: `CliCommandHandler`.

There are no state rules. Every non-null handler is valid: `Validate` returns an empty array, `IsValid` checks only for non-null, and `EnsureValid` checks only for non-null.

### `CommandOptionsValidation`

Target: `CommandOptions`.

- `Arguments` and `Flags` must not be `null`. These conditions throw `ArgumentNullException` immediately instead of being returned as validation messages.
- `Command` is required and must be nonblank.
- When present, `Subcommand`, `PolicyName`, `OutputFile`, and `ConfigFile` must be nonblank.
- When present, `PolicyType` must be nonblank and must exactly match one of `circuitbreaker`, `retry`, `timeout`, `bulkhead`, or `fallback`.
- `MaxRetries`, `FailureThreshold`, and `MaxParallelization` must be non-negative.
- When present, `Timeout` and `OpenDuration` must be greater than zero.

All three methods throw `ArgumentNullException` for a null target (directly or through `Validate`).

### `TimeoutPolicyValidation`

Target: `TimeoutPolicy`.

- `Timeout` must be greater than zero.
- `TimeoutCount` and `TotalExecutions` must be non-negative.
- `AverageExecutionTimeMs` must be finite, not `NaN`, and non-negative.
- `LongestExecutionTimeMs` and `ShortestExecutionTimeMs` must be non-negative.
- `ShortestExecutionTimeMs` must not remain `long.MaxValue`, which represents the uninitialized state.
- When both shortest and longest times are positive, the shortest must not exceed the longest.
- When the average and longest times are positive, the average must not exceed 150% of the longest time.

All three methods throw `ArgumentNullException` for a null target.

### `FallbackPolicyValidation`

Target: `FallbackPolicy`.

- Inherited `Name` must be nonblank.
- `FallbackTimeout` must be greater than zero.
- If `FallbackOnAnyException` is `false`, `FallbackTriggerExceptions` must contain at least one entry.
- `FallbackTriggerExceptions` must not be `null`; each entry must be non-null and derive from `Exception`.
- `FallbackInvocationCount`, `SuccessfulFallbackCount`, and `FailedFallbackCount` must be non-negative.
- `AverageFallbackExecutionTimeMs` is reported when it is negative.

The implementation reads `FallbackTriggerExceptions.Count` before its explicit null-collection check. Consequently, a null collection currently causes `NullReferenceException` rather than a returned problem. `IsValid(null)` returns `false`; `Validate(null)` and `EnsureValid(null)` throw `ArgumentNullException`.

The average-time check does not reject `double.NaN`.

### `ResiliencyLoggingMiddlewareValidation`

Target: `ResiliencyLoggingMiddleware`.

- `MaxLogEntries` must be greater than zero.
- Every item returned by `GetLogs()` must have a nonblank `Id`, `PolicyName`, and `OperationName`.
- Every log entry must have a non-negative `DurationMs` and a non-default `Timestamp`.
- In the result of `GetSummary()`, `TotalEntries`, `SuccessfulExecutions`, `FailedExecutions`, and `AverageDurationMs` must be non-negative.
- Summary `SuccessRate` is reported when it is below 0 or above 100.
- When summary `TotalEntries` is positive, `OldestLogTime` and `NewestLogTime` must not be default `DateTime` values.

`IsValid(null)` returns `false`; the other methods throw `ArgumentNullException` for `null`.

The summary floating-point comparisons do not reject `double.NaN` for `SuccessRate` or `AverageDurationMs`.

### `ResiliencyPipelineServiceValidation`

Target: `ResiliencyPipelineService`.

- `PipelineId` must be nonblank.
- `CreatedAt` must not be the default `DateTime` value.
- `TotalExecutions`, `SuccessfulExecutions`, and `FailedExecutions` must be non-negative.
- When `TotalExecutions` is positive, `SuccessfulExecutions + FailedExecutions` must not exceed it.
- When `TotalExecutions` is positive, `FailedExecutions` must not exceed it. This is checked separately even though the preceding sum check may also report the inconsistency.

All three methods throw `ArgumentNullException` for a null target (directly or through `Validate`).

### `FailureInjectionServiceValidation`

Targets: `FailureInjectionService` and every `InjectionRule` returned by `GetRules()`.

- Service `TotalInjections` must be non-negative.
- A rule and its `Key` must not be null, and `Key` must not be empty. These cases throw immediately rather than being returned as problems.
- A whitespace-only key is invalid; a nonblank key may contain at most 100 characters.
- `Type` must be a defined `InjectionType` enum value.
- `InjectionRate` must be finite, not `NaN`, and from 0.0 through 1.0, inclusive.
- A non-null `ExceptionMessage` may contain at most 500 characters.
- When present, `LatencyDelay` must be non-negative and at most one hour.
- When present, `TimeoutDuration` must be non-negative and at most 24 hours.
- `InjectionsPerformed` must be non-negative.
- An `Exception` rule must supply either `ExceptionMessage` or `ExceptionFactory`; a `Latency` rule must supply `LatencyDelay`; a `Timeout` rule must supply `TimeoutDuration`.

No behavior check is performed on `ExceptionFactory`, and `IsEnabled` needs no validation. `IsValid(null)` returns `false`; `Validate(null)` and `EnsureValid(null)` throw `ArgumentNullException`.

### `CircuitBreakerDiagnosticsValidation`

This class overloads the trio for three target types.

For `CircuitBreakerDiagnosticReport`:

- `PolicyId` must not be null or empty. Failure throws `ArgumentException` immediately; whitespace-only text is accepted.
- `PolicyName` must be nonblank. A null or empty name throws `ArgumentException` immediately, while whitespace-only text is returned as a validation problem.
- `CurrentState` must be a defined `CircuitBreakerPolicy.CircuitState` value.
- `FailureThreshold` and `SuccessThreshold` must be greater than zero.
- `OpenDuration` must be greater than zero.
- `GeneratedAt` must not be the default value and must not be more than one minute later than the current UTC time.
- `Issues` and `Recommendations` must not be `null`.

For `CircuitBreakerEffectiveness`:

- `PolicyName` has the same immediate null/empty behavior and collected whitespace-only error described above.
- `TotalExecutions` and `FailedExecutions` must be non-negative, and failures must not exceed total executions.
- `FailureRate` is reported when it is below 0 or above 100.
- `CurrentState` must be a defined circuit state.
- `EffectivenessRating` must exactly equal `Excellent`, `Good`, `Fair`, or `Poor`.

The `FailureRate` comparison does not reject `double.NaN`.

For `CircuitBreakerConfiguration`:

- `SuggestedFailureThreshold` and `SuggestedSuccessThreshold` must be greater than zero.
- `SuggestedOpenDuration` must be greater than zero.

For all three overload sets, null targets cause `ArgumentNullException` through `Validate`, including calls made by `IsValid` and `EnsureValid`.

## Usage

Import the namespace containing the target's validation extensions, then choose the form that fits the caller:

```csharp
using System;
using System.Collections.Generic;
using DotNetResiliencePipeline.Data;

static void ProcessRecord(ExecutionRecord record)
{
    // Inspect every reported issue without throwing.
    IReadOnlyList<string> problems = record.Validate();
    if (problems.Count > 0)
    {
        Console.Error.WriteLine(string.Join(Environment.NewLine, problems));
        return;
    }

    // Boolean checks are convenient for guards and predicates.
    if (!record.IsValid())
    {
        return;
    }

    // Boundary code can fail fast; this throws ArgumentException if invalid.
    record.EnsureValid();

    // Continue with the validated record.
}
```
