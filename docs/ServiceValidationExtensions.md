# Service validation extensions

This page is a per-file reference for six `*Validation.cs` helpers: the method signatures each one exposes and the rules it enforces, as implemented in the source. The shared API pattern (`Validate` / `IsValid` / `EnsureValid`) and the detailed rule wording for the wider validator family are described in [validation-extensions.md](validation-extensions.md); this page does not repeat that material.

Each helper is a `public static class` with extension methods on its target type. None of them modify the target.

| File | Target | Notes |
|------|--------|-------|
| `src/Services/ResiliencyPipelineServiceValidation.cs` | `ResiliencyPipelineService` | Non-nullable target |
| `src/Services/FailureInjectionServiceValidation.cs` | `FailureInjectionService?` | Also validates every rule from `GetRules()` |
| `src/Caching/PolicyCacheServiceValidation.cs` | `PolicyCacheService?` | Reads `GetStatistics()` |
| `src/Middleware/ResiliencyLoggingMiddlewareValidation.cs` | `ResiliencyLoggingMiddleware` | Reads `GetLogs()` and `GetSummary()` |
| `src/Cli/CliCommandHandlerValidation.cs` | `CliCommandHandler?` | No state rules |
| `src/Cli/CommandOptionsValidation.cs` | `CommandOptions` | Name clash with an instance method; see below |

---

## `ResiliencyPipelineServiceValidation`

Namespace: `DotNetResiliencePipeline.Services`

### Signatures

```csharp
public static IReadOnlyList<string> Validate(this ResiliencyPipelineService value)
public static bool IsValid(this ResiliencyPipelineService value)
public static void EnsureValid(this ResiliencyPipelineService value)
```

### Rules

- `PipelineId` must not be null, empty, or whitespace.
- `CreatedAt` must not equal `default(DateTime)`.
- `TotalExecutions`, `SuccessfulExecutions`, and `FailedExecutions` must each be non-negative.
- When `TotalExecutions > 0`, `SuccessfulExecutions + FailedExecutions` must not exceed `TotalExecutions`.
- When `TotalExecutions > 0`, `FailedExecutions` must not exceed `TotalExecutions`. This is checked separately, so one inconsistency can produce two messages.

### Behavior

- `Validate(null)`, `IsValid(null)`, and `EnsureValid(null)` throw `ArgumentNullException`.
- `EnsureValid` throws `ArgumentException` with the message `ResiliencyPipelineService validation failed: ` followed by the problems joined with `"; "`.

---

## `FailureInjectionServiceValidation`

Namespace: `DotNetResiliencePipeline.Services`

### Signatures

```csharp
public static IReadOnlyList<string> Validate(this FailureInjectionService? value)
public static bool IsValid(this FailureInjectionService? value)
public static void EnsureValid(this FailureInjectionService? value)
```

Private helper (not callable from outside the class):

```csharp
private static void ValidateRule(InjectionRule rule, List<string> problems)
```

### Constants

| Name | Value | Use |
|------|-------|-----|
| `MaxRuleKeyLength` | `100` | Maximum `Rule.Key` length |
| `MaxExceptionMessageLength` | `500` | Maximum `Rule.ExceptionMessage` length |
| `MaxTimeoutDuration` | 24 hours | Upper bound for `Rule.TimeoutDuration` |

The one-hour upper bound for `LatencyDelay` is a literal (`3600000` ms) in `ValidateRule`, not a constant.

### Rules

Service level:

- `TotalInjections` must be non-negative.

Per rule, for each `InjectionRule` returned by `GetRules()`:

- `Key` must not be whitespace-only and must be at most 100 characters.
- `Type` must be a defined `InjectionType` value.
- `InjectionRate` must not be `NaN`, must not be infinite, and must be between 0.0 and 1.0 inclusive.
- A non-null `ExceptionMessage` must be at most 500 characters.
- A non-null `LatencyDelay` must be non-negative and at most one hour.
- A non-null `TimeoutDuration` must be non-negative and at most 24 hours.
- `InjectionsPerformed` must be non-negative.
- Type consistency:
  - `Exception` requires `ExceptionMessage` or `ExceptionFactory`.
  - `Latency` requires `LatencyDelay`.
  - `Timeout` requires `TimeoutDuration`.

Not checked: `ExceptionFactory` behavior (only the consistency rule above) and `IsEnabled` (any bool is valid).

### Behavior

- A null rule, or a null or empty `Key`, throws `ArgumentNullException` or `ArgumentException` from inside `ValidateRule`. These do not appear as entries in the returned list, so `Validate` can throw even though it normally returns problems.
- `IsValid(null)` returns `false`. `Validate(null)` and `EnsureValid(null)` throw `ArgumentNullException`.
- `EnsureValid` throws `ArgumentException` with the message `FailureInjectionService is not valid. Problems:` followed by the problems, one per line.

---

## `PolicyCacheServiceValidation`

Namespace: `DotNetResiliencePipeline.Caching`

### Signatures

```csharp
public static IReadOnlyList<string> Validate(this PolicyCacheService? value)
public static bool IsValid(this PolicyCacheService? value)
public static void EnsureValid(this PolicyCacheService? value)
```

### Rules

Configuration:

- `DefaultTtl` must be greater than zero.
- `MaxCacheSize` must be greater than zero.

From `GetStatistics()`:

- `TotalEntries`, `ValidEntries`, and `ExpiredEntries` must be non-negative.
- `TotalEntries` must equal `ValidEntries + ExpiredEntries`.
- `HitRate` must be between 0 and 100. The check uses `<` and `>`, so `NaN` passes.
- `AverageTtl` must be non-negative.

### Behavior

- `IsValid(null)` returns `false`. `Validate(null)` and `EnsureValid(null)` throw `ArgumentNullException`.
- `EnsureValid` throws `ArgumentException` with the message `PolicyCacheService is invalid. Problems:` followed by each problem on its own line, prefixed with ` - ` and separated by `Environment.NewLine`.

---

## `ResiliencyLoggingMiddlewareValidation`

Namespace: `DotNetResiliencePipeline.Middleware`

### Signatures

```csharp
public static IReadOnlyList<string> Validate(this ResiliencyLoggingMiddleware value)
public static bool IsValid(this ResiliencyLoggingMiddleware value)
public static void EnsureValid(this ResiliencyLoggingMiddleware value)
```

### Rules

Configuration:

- `MaxLogEntries` must be positive.

Per log entry, from `GetLogs()`. Each rule adds a single message if any entry violates it:

- `Id`, `PolicyName`, and `OperationName` must not be null, empty, or whitespace.
- `DurationMs` must not be negative.
- `Timestamp` must not equal `default(DateTime)`.

From `GetSummary()`:

- `TotalEntries`, `SuccessfulExecutions`, `FailedExecutions`, and `AverageDurationMs` must be non-negative.
- `SuccessRate` must be between 0 and 100.
- When `TotalEntries > 0`, `OldestLogTime` and `NewestLogTime` must not equal `default(DateTime)`.

### Behavior

- `IsValid(null)` returns `false` because the body checks `value is not null`. `Validate(null)` and `EnsureValid(null)` throw `ArgumentNullException`.
- `EnsureValid` throws `ArgumentException` with the message `ResiliencyLoggingMiddleware is invalid. Problems:` followed by the problems, one per line.
- Because `GetLogs()` and `GetSummary()` are called during validation, the result reflects the log state at the moment of the call.

---

## `CliCommandHandlerValidation`

Namespace: `DotNetResiliencePipeline.Cli`

### Signatures

```csharp
public static IReadOnlyList<string> Validate(this CliCommandHandler? value)
public static bool IsValid(this CliCommandHandler? value)
public static void EnsureValid(this CliCommandHandler? value)
```

### Rules

None. A non-null handler is always valid.

### Behavior

- `Validate` returns a new empty `List<string>`.
- `IsValid` returns `value is not null`.
- `EnsureValid` throws only `ArgumentNullException`, for a null handler.

---

## `CommandOptionsValidation`

Namespace: `DotNetResiliencePipeline.Cli`

### Signatures

```csharp
public static IReadOnlyList<string> Validate(this CommandOptions value)
public static bool IsValid(this CommandOptions value)
public static void EnsureValid(this CommandOptions value)
```

Private helper:

```csharp
private static bool IsValidPolicyType(string type)
```

`IsValidPolicyType` accepts exactly `circuitbreaker`, `retry`, `timeout`, `bulkhead`, or `fallback`. The comparison is case-sensitive.

### Rules

Null checks (these throw immediately and are not returned as messages):

- `Arguments` and `Flags` must not be null.

Problem messages:

- `Command` must not be null, empty, or whitespace (`Command is required and cannot be null or whitespace.`).
- When `Subcommand` is set, it must not be whitespace-only.
- When `Command == "policy"` and `PolicyName` is blank, the message is `Policy name is required for policy operations`. The check is case-sensitive, and this message has no trailing period.
- When `PolicyName` is set but blank (and `Command` is not `policy`), it is invalid.
- When `PolicyType` is set, it must not be whitespace-only and must be one of the allowed names above.
- `MaxRetries`, `FailureThreshold`, and `MaxParallelization` must be non-negative when set.
- When set, `Timeout` and `OpenDuration` must be greater than zero.
- When set, `OutputFile` and `ConfigFile` must not be whitespace-only.

### Behavior

- `Validate(null)` and `EnsureValid(null)` throw `ArgumentNullException`. `IsValid(null)` throws `NullReferenceException`; see the note below.
- `EnsureValid` throws `ArgumentException` (with `paramName` `value`) and the message `CommandOptions validation failed:` followed by the problems, one per line.

> **Name clash with `CommandOptions.Validate()`.** `CommandOptions` declares its own instance method `public List<string> Validate()` (`src/Cli/CommandOptions.cs:118`). C# binds `options.Validate()` to the instance method, not to this extension, so the extension is never reached through member-call syntax. The instance method applies a smaller rule set (it has no whitespace checks for `Subcommand`, `PolicyName`, `OutputFile`, or `ConfigFile`, and no checks for `PolicyType` blankness, `MaxParallelization`, `Timeout`, or `OpenDuration`). To run the full rule set, call the extension statically:
>
> ```csharp
> IReadOnlyList<string> problems = CommandOptionsValidation.Validate(options);
> ```
>
> Because `IsValid` and `EnsureValid` have no instance counterparts, `options.IsValid()` and `options.EnsureValid()` do reach the extension. For the same reason, `IsValid` calls `value.Validate()` through member syntax, which binds to the instance method. A null receiver there therefore throws `NullReferenceException`, not `ArgumentNullException`. The per-type behavior described above has not been run; this note is derived from the C# member-binding rules and the source.
