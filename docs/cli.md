# CLI layer

The CLI layer in `src/Cli/` turns an argument array into a structured command, validates it, dispatches it to application services, and reports the outcome as a `CommandExecutionResult`.

```text
string[] args
    -> CommandParser.Parse()
    -> CommandOptions
    -> CliCommandValidator.Validate()
    -> ValidationResult
    -> CliCommandHandler.ExecuteAsync()
    -> CommandExecutionResult
```

For the individual type references, see [CommandOptions](CommandOptions.md), [CliCommandValidator](CliCommandValidator.md), and [CliCommandHandler](CliCommandHandler.md).

## End-to-end flow

1. `CommandParser` stores the supplied `string[]`. `Parse()` creates a `CommandOptions`. With no arguments, the options remain at their defaults. Otherwise, the first token becomes the lower-cased `Command`; a second token that does not start with `-` becomes the lower-cased `Subcommand`.
2. The parser consumes the remaining tokens as long or short options. Recognized value options populate typed `CommandOptions` properties. Unrecognized value options go into the case-insensitive `Arguments` dictionary. Options without values go into `Flags` exactly as written.
3. `CliCommandHandler.ExecuteAsync(options)` calls its internal `CliCommandValidator` before dispatch. The validator returns a `ValidationResult` whose `IsValid` is true only when `Errors` is empty. Warnings do not prevent execution.
4. When validation fails, `ExecuteAsync` throws `ValidationException("Command validation failed", errors)` before entering its execution `try` block. The errors are placed in a dictionary with keys `error_0`, `error_1`, and so on.
5. A valid command is routed to its handler. Exceptions thrown during routing or command work are caught and converted to a failed `CommandExecutionResult` with exit code `2`. A normal handler outcome uses exit code `0` for success or `1` for a command/subcommand failure.

Validation warnings are not included in `CommandExecutionResult` and are otherwise ignored by `CliCommandHandler`. Call `CliCommandValidator.Validate()` separately if a caller needs to display them.

## Parsing rules and options

The parser accepts these shapes:

```text
command subcommand --long-option value
command --long-option=value
command -x value
command --boolean-flag
command -x
```

Long option names and single-character short option names are lower-cased before mapping. A value is consumed only when the following token does not start with `-`. Consequently, a space-separated negative number is treated as another flag rather than as the preceding option's value; the `--option=-1` form does parse it. Failed `int.TryParse` or `TimeSpan.TryParse` conversions are silently ignored, leaving the corresponding nullable property unset.

| CLI spelling | `CommandOptions` destination | Value parsing |
| --- | --- | --- |
| `--name` | `PolicyName` | string |
| `--type` | `PolicyType` | string |
| `--max-retries`, `--maxretries` | `MaxRetries` | `int.TryParse` |
| `--threshold` | `FailureThreshold` | `int.TryParse` |
| `--parallelization`, `--max-parallel` | `MaxParallelization` | `int.TryParse` |
| `--timeout` | `Timeout` | `TimeSpan.TryParse` |
| `--duration`, `--open-duration` | `OpenDuration` | `TimeSpan.TryParse` |
| `--output`, `-o` | `OutputFile` | string |
| `--config`, `-c` | `ConfigFile` | string |
| `--verbose`, `-v` | `Verbose` | set to `true` only when parsed with a following value or `=` value |
| `--json`, `-j` | `JsonOutput` | set to `true` only when parsed with a following value or `=` value |
| any other option with a value | `Arguments[lower-cased-name]` | string |
| any option without a value | `Flags` | original token, including its dash prefix |

There are two details worth noting:

- Bare `--verbose`, `-v`, `--json`, and `-j` tokens are stored in `Flags`; they do not set `Verbose` or `JsonOutput`. With a value, the boolean property is set to `true` regardless of that value, and the key/value is also retained in `Arguments` because these names are not dedicated cases in `SetOption`.
- Multi-character short tokens such as `-abc` are always stored as flags and never split or assigned a value. `HasFlag(names)` tests each requested name against `--{name}` or `-{name[0]}` in the case-sensitive `Flags` list.

The built-in help text also advertises `--help`/`-h`. The parser treats either spelling as a bare flag, but neither the validator nor handler gives that flag special behavior; use the `help` command to obtain help.

## Commands exactly as implemented

The validator accepts only `policy`, `pipeline`, `metrics`, `health`, and `help`. Command and subcommand matching is effectively case-insensitive for parser-produced options because the parser lower-cases both tokens.

| Accepted command | Implemented behavior |
| --- | --- |
| `help` | Returns `CommandParser.GetHelpText()` with success and exit code `0`. A subcommand, if supplied, is ignored. |
| `policy create` | Requires `--name` and `--type` in the handler. Creates and registers a policy, then saves it through `PolicyRepository`. Supported types and defaults are listed below. |
| `policy list` | Lists all policies registered in `ResiliencyPipelineService`; `--name` is not required. |
| `policy get` | Requires `--name`; returns the matching registered policy or a caught execution failure if it is not found. |
| `policy delete` | Requires `--name`; removes the matching registered policy from the pipeline service. It does not delete from `PolicyRepository`. |
| `policy validate` | Requires `--name`; reports the matching registered policy as valid. |
| `policy` with no subcommand | Passes validation, then returns failure with exit code `1` and `Subcommand required: create, list, get, delete, or validate`. |
| `pipeline` | Returns pipeline ID, total executions, and success rate. Any subcommand is ignored. |
| `metrics` | Returns successful executions, failed executions, and success rate. Any subcommand is ignored. |
| `health` | Returns `✓ Pipeline is healthy`. Any subcommand is ignored. |

For `policy create`, the handler constructs:

| `--type` | Applied options and defaults |
| --- | --- |
| `circuitbreaker` | `FailureThreshold = --threshold` or `5`; `OpenDuration = --duration`/`--open-duration` or 30 seconds |
| `retry` | `MaxRetries = --max-retries`/`--maxretries` or `3`; `InitialDelay = 100` milliseconds |
| `timeout` | `Timeout = --timeout` or 10 seconds |
| `bulkhead` | `MaxParallelization = --parallelization`/`--max-parallel` or `10`; `MaxQueueLength = 50` |
| `fallback` | No additional CLI configuration |

`CommandOptions` also has its own `Validate()` method, but the end-to-end handler does not call it; it uses `CliCommandValidator` instead.

### Handler routes currently blocked by validation

`CliCommandHandler` contains cases for `dashboard`, `inject`, and `export`, but `CliCommandValidator` does not include those names in its valid-command allowlist. Passing any of them through `ExecuteAsync` therefore throws `ValidationException` before dispatch, so they are not usable end to end as currently implemented.

Their unreachable handler implementations are:

- `dashboard [--name value] [--reset]`: lists circuit breakers, shows one breaker, or resets a named breaker.
- `inject --rule value [--type exception|latency|timeout] [--rate number]`: returns a failure-injection rule summary; it does not register the rule. Additionally, the parser maps `--type` to `PolicyType`, while this handler reads `Arguments["type"]`.
- `export [--format json|csv|prometheus] [--output file]`: exports metrics to standard result text or writes the output file. Any format other than `csv` or `prometheus` falls back to JSON.

## `CliCommandValidator` errors and warnings

Each call clears the validator's previous errors and warnings. Checks are independent, so one option can produce more than one message. Messages below are reproduced exactly; text in angle brackets is substituted at runtime.

### Errors

| Condition | Error text |
| --- | --- |
| `Command` is not one of `policy`, `pipeline`, `metrics`, `health`, `help` (including an empty command) | `Invalid command: <command>` |
| A non-empty `policy` subcommand is not one of `create`, `list`, `get`, `delete`, `validate` | `Invalid policy subcommand: <subcommand>` |
| `PolicyName` has fewer than 2 characters | `Policy name must be at least 2 characters` |
| `PolicyName` has more than 100 characters | `Policy name must not exceed 100 characters` |
| `PolicyName` contains a character outside ASCII letters, digits, `_`, `.`, and `-` | `Policy name contains invalid characters. Use alphanumeric, dash, and underscore only` |
| `PolicyType`, compared case-insensitively, is not `circuitbreaker`, `retry`, `timeout`, `bulkhead`, or `fallback` | `Invalid policy type: <value>` |
| `MaxRetries < 0` | `MaxRetries cannot be negative` |
| `FailureThreshold < 1` | `FailureThreshold must be at least 1` |
| `MaxParallelization < 1` | `MaxParallelization must be at least 1` |
| `Timeout < 1` millisecond | `Timeout must be at least 1 millisecond` |
| `OpenDuration < 1` second | `OpenDuration must be at least 1 second` |
| `OutputFile` has a non-empty directory component and that directory does not exist | `Output directory does not exist: <directory>` |
| Inspecting the output path throws | `Invalid output file path: <exception message>` |
| `ConfigFile` does not exist | `Configuration file not found: <path>` |

The policy-name error mentions alphanumeric, dash, and underscore, although the actual regular expression also accepts a dot. The validator does not require a policy name or policy type; command-specific checks in the policy handler enforce those where needed.

### Warnings

| Condition | Warning text |
| --- | --- |
| `MaxRetries > 100` | `MaxRetries is very high (>100). Consider reducing for production.` |
| `FailureThreshold > 1000` | `FailureThreshold is very high (>1000). Consider reducing.` |
| `MaxParallelization > 10000` | `MaxParallelization is very high (>10000). Consider reducing for resource constraints.` |
| `Timeout > 1` hour | `Timeout exceeds 1 hour. Consider reducing.` |
| `OpenDuration > 1` hour | `OpenDuration exceeds 1 hour. Might be too long for recovery.` |

`ValidationResult.ToString()` renders either `❌ Validation Failed:` plus `ERROR:` lines or `✓ Validation Passed`, followed by a warnings section when warnings exist.

## Execution results and exception boundary

`CommandExecutionResult` has four mutable properties:

- `Success`: whether the handler reports success.
- `Message`: human-readable output; defaults to an empty string.
- `Error`: the caught exception, or `null` on normal results.
- `ExitCode`: `0` for successful built-in handlers, `1` for normal unknown command/subcommand results, and `2` when an exception is caught during dispatch or execution.

Pre-dispatch validation is outside the handler's `try`/`catch`, so invalid commands throw rather than return a result. Command-specific `ValidationException`s, repository failures, file-write failures, and other exceptions raised after dispatch begins are caught and returned with `Success = false`, a message beginning `Command execution failed:`, the exception in `Error`, and exit code `2`.

## Wiring example

The service and repository types used below all provide parameterless construction (or optional constructor parameters). The caller owns presentation and process-exit handling.

```csharp
using DotNetResiliencePipeline.Cli;
using DotNetResiliencePipeline.Data;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;

var parser = new CommandParser(args);
CommandOptions options = parser.Parse();

// Validate separately if warnings must be shown before execution.
var validation = new CliCommandValidator().Validate(options);
foreach (var warning in validation.Warnings)
    Console.Error.WriteLine($"WARNING: {warning}");

var pipelineService = new ResiliencyPipelineService();
var policyRepository = new PolicyRepository();
var historyRepository = new ExecutionHistoryRepository();
var handler = new CliCommandHandler(
    pipelineService,
    policyRepository,
    historyRepository);

try
{
    CommandExecutionResult result = await handler.ExecuteAsync(options);
    Console.WriteLine(result.Message);
    Environment.ExitCode = result.ExitCode;
}
catch (ValidationException ex)
{
    Console.Error.WriteLine(ex.Message);
    Environment.ExitCode = 1;
}
```

The handler owns another `CliCommandValidator`, so the explicit validation in the snippet is optional and execution validates the options again.
