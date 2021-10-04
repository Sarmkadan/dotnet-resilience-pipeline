# CliCommandHandler

`CliCommandHandler` encapsulates the execution of a CLI command within the `dotnet-resilience-pipeline` framework. It provides a structured result that indicates success or failure, an optional message, any exception that occurred, and the process exit code. This type is designed to standardize command invocation and error reporting across resilience pipelines.

## API

### `CliCommandHandler`

Constructor. Initializes a new instance of `CliCommandHandler` with the command and arguments to be executed. The specific constructor parameters are not part of the documented public surface, but the type is instantiated to prepare a command for execution.

### `async Task<CommandExecutionResult> ExecuteAsync`

Executes the configured CLI command asynchronously.

- **Parameters:** None (relies on state provided at construction).
- **Returns:** `Task<CommandExecutionResult>` — a task that completes with the result of the command execution.
- **Throws:** May throw `InvalidOperationException` if the handler is not properly configured before execution. Exceptions during command execution are captured in the result rather than propagated.

### `bool Success`

Gets a value indicating whether the command executed successfully. Returns `true` when the command completed without error and with a successful exit code; otherwise `false`.

### `string Message`

Gets a human-readable message associated with the command execution result. Typically contains standard output, a summary of the outcome, or an error description when `Success` is `false`. May be empty or `null` if no message was produced.

### `Exception? Error`

Gets the exception that occurred during command execution, if any. Returns `null` when the command completed without throwing an exception. This property captures exceptions from the execution infrastructure itself, not non-zero exit codes from the underlying process.

### `int ExitCode`

Gets the exit code returned by the underlying process. A value of `0` conventionally indicates success, while non-zero values indicate failure. This property is populated regardless of whether an exception was thrown.

## Usage

### Example 1: Executing a command and checking success

```csharp
var handler = new CliCommandHandler("dotnet", "build --configuration Release");
CommandExecutionResult result = await handler.ExecuteAsync();

if (result.Success)
{
    Console.WriteLine($"Build succeeded: {result.Message}");
}
else
{
    Console.WriteLine($"Build failed with exit code {result.ExitCode}: {result.Message}");
    if (result.Error is not null)
    {
        Console.WriteLine($"Exception: {result.Error.Message}");
    }
}
```

### Example 2: Integrating with a resilience pipeline

```csharp
var pipeline = ResiliencePipelineBuilder<CommandExecutionResult>
    .Create()
    .AddRetry(new RetryStrategyOptions<CommandExecutionResult>
    {
        ShouldHandle = args => args.Outcome.Result?.Success == false,
        MaxRetryAttempts = 3,
        OnRetry = args =>
        {
            Console.WriteLine($"Retry {args.AttemptNumber}: {args.Outcome.Result?.Message}");
            return default;
        }
    })
    .Build();

var handler = new CliCommandHandler("git", "push origin main");
CommandExecutionResult result = await pipeline.ExecuteAsync(
    async _ => await handler.ExecuteAsync());

Console.WriteLine($"Final outcome: Success={result.Success}, ExitCode={result.ExitCode}");
```

## Notes

- **Result vs. Exception:** `Success` reflects the overall outcome, considering both the exit code and any captured exception. A non-zero exit code without an exception sets `Success` to `false` and populates `ExitCode`, but leaves `Error` as `null`.
- **Message content:** The `Message` property may contain stdout, stderr, or a framework-generated summary depending on the execution outcome. Do not rely on its format for programmatic decisions; use `Success` and `ExitCode` for control flow.
- **Thread safety:** `CliCommandHandler` is not guaranteed to be thread-safe. Instances should be used from a single execution context. Calling `ExecuteAsync` concurrently on the same instance may lead to undefined behavior.
- **State mutability:** The properties `Success`, `Message`, `Error`, and `ExitCode` are populated only after `ExecuteAsync` completes. Accessing them before execution yields default values and does not reflect a meaningful result.
- **Disposal:** The type does not implement `IDisposable`. Any underlying process resources are cleaned up as part of `ExecuteAsync` completion.
