# CommandParser

`CommandParser` converts a command-line token array into a [`CommandOptions`](CommandOptions.md) instance. It identifies the command and optional subcommand, maps recognized value options to strongly typed properties, and preserves unrecognized options or standalone flags for later processing.

Parsing and validation are separate operations. Use [`CliCommandValidator`](CliCommandValidator.md) to validate the returned options and [`CliCommandHandler`](CliCommandHandler.md) to execute them.

## Public API

### `CommandParser(string[] args)`

Creates a parser for an already-tokenized command line. The array is retained and read when `Parse()` is called.

### `CommandOptions Parse()`

Parses the supplied tokens and returns a new `CommandOptions` object. With an empty array, it returns an object whose properties retain their defaults. Otherwise, the first token becomes `Command`. If the second token does not start with `-`, it becomes `Subcommand`. Both values are converted to lowercase using invariant casing.

### `static string GetHelpText()`

Returns the built-in CLI help text, including its command summary, displayed options, and examples. It does not inspect arguments or return a `CommandOptions` instance.

## Accepted argument syntax

The parser expects this token order:

```text
<command> [subcommand] [options]
```

It recognizes these option forms:

```text
--name value
--name=value
-o value
--standalone-flag
-v
```

- A long option begins with `--`. It takes a value from text after the first `=`, or from the next token when that token does not start with `-`.
- A one-character short option begins with `-` and may take its value from the next token when that token does not start with `-`.
- A long option without a value is added verbatim to `CommandOptions.Flags`.
- A one-character short option without a value is also added verbatim to `Flags`.
- A multi-character single-dash token such as `-verbose` is always added to `Flags`; it never consumes a value.
- Positional tokens after the optional subcommand are ignored unless consumed as an option value.
- A value beginning with `-` cannot be supplied as a separate token. Use the long `--name=value` form when such a value is required.

Option names are normalized to lowercase before mapping. The parsing code recognizes exactly the following value-option names:

| Accepted name | `CommandOptions` destination | Conversion |
| --- | --- | --- |
| `--name` | `PolicyName` | String |
| `--type` | `PolicyType` | String |
| `--max-retries`, `--maxretries` | `MaxRetries` | `int.TryParse` |
| `--threshold` | `FailureThreshold` | `int.TryParse` |
| `--parallelization`, `--max-parallel` | `MaxParallelization` | `int.TryParse` |
| `--timeout` | `Timeout` | `TimeSpan.TryParse` |
| `--duration`, `--open-duration` | `OpenDuration` | `TimeSpan.TryParse` |
| `--output`, `--o`, `-o` | `OutputFile` | String |
| `--config`, `--c`, `-c` | `ConfigFile` | String |

The `--o` and `--c` forms work because long names are mapped by their text; the conventional short forms are `-o` and `-c`. Other single-character options with values are accepted but, unless described below, are placed in `Arguments` rather than mapped to a dedicated property.

The names `verbose`/`v` and `json`/`j` have value-dependent behavior:

- When parsed with a value, such as `--verbose=true`, `-v true`, `--json=true`, or `-j true`, the key and value are added to `Arguments` and the corresponding `Verbose` or `JsonOutput` property is set to `true`. The value itself is not parsed as a Boolean, so any value enables the property.
- When written as standalone flags (`--verbose`, `-v`, `--json`, or `-j`), they are added only to `Flags`; `Parse()` does not set `Verbose` or `JsonOutput`.

Any other long option with a value, or any unrecognized one-character short option with a value, is stored in `Arguments` under the normalized option name. Unknown standalone options remain in `Flags`. Repeated mapped options overwrite the earlier property value; repeated `Arguments` keys overwrite the earlier dictionary value; repeated standalone flags remain as duplicate list entries.

## How `CommandOptions` is filled

For this input:

```text
policy create --name payments --type retry --max-retries 4 --timeout=00:00:10 --tag production -o policies.json --dry-run
```

the parser produces the equivalent of:

```csharp
new CommandOptions
{
    Command = "policy",
    Subcommand = "create",
    PolicyName = "payments",
    PolicyType = "retry",
    MaxRetries = 4,
    Timeout = TimeSpan.FromSeconds(10),
    OutputFile = "policies.json",
    Arguments = { ["tag"] = "production" },
    Flags = { "--dry-run" }
};
```

Properties not populated by an argument retain the defaults defined by `CommandOptions`.

## Error handling and validation

`Parse()` does not report malformed or unsupported input and does not perform semantic validation:

- Invalid integers and `TimeSpan` values fail their `TryParse` calls silently, leaving the corresponding nullable property unchanged.
- Unknown value options are retained in `Arguments`, while unknown standalone options are retained in `Flags`.
- Tokens that are neither options nor the command/optional subcommand are ignored.
- No exception is intentionally raised for bad option syntax. Passing `null` to the constructor is not guarded, however, and a later `Parse()` call will fail when it accesses the array.

After parsing, call `CliCommandValidator.Validate(options)` to obtain validation errors and warnings. `CliCommandHandler.ExecuteAsync(options)` performs that validation before routing a command and throws a `ValidationException` when validation fails.

## Usage

```csharp
using DotNetResiliencePipeline.Cli;

string[] args =
[
    "policy", "create",
    "--name", "payments",
    "--type", "retry",
    "--max-retries=4"
];

var options = new CommandParser(args).Parse();
var validation = new CliCommandValidator().Validate(options);

if (!validation.IsValid)
{
    foreach (string error in validation.Errors)
        Console.Error.WriteLine(error);
}
```
