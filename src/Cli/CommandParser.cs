// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.RegularExpressions;

namespace DotNetResiliencePipeline.Cli;

/// <summary>
/// Parses command-line arguments into structured CommandOptions.
/// Handles both short (-) and long (--) flags with value assignments.
/// </summary>
public class CommandParser
{
    private readonly string[] _args;

    public CommandParser(string[] args)
    {
        _args = args;
    }

    /// <summary>
    /// Parses raw command-line arguments into CommandOptions.
    /// Supports formats: cmd --flag value, --flag=value, -f value
    /// </summary>
    public CommandOptions Parse()
    {
        var options = new CommandOptions();

        if (_args.Length == 0)
            return options;

        // First argument is the command
        options.Command = _args[0].ToLowerInvariant();

        // If there's a second argument and it doesn't start with dash, it's a subcommand
        if (_args.Length > 1 && !_args[1].StartsWith("-"))
            options.Subcommand = _args[1].ToLowerInvariant();

        int startIndex = options.Subcommand != null ? 2 : 1;

        // Parse remaining arguments
        for (int i = startIndex; i < _args.Length; i++)
        {
            string arg = _args[i];

            if (arg.StartsWith("--"))
            {
                ParseLongFlag(arg, options, ref i);
            }
            else if (arg.StartsWith("-") && arg.Length > 1)
            {
                ParseShortFlag(arg, options, ref i);
            }
        }

        return options;
    }

    private void ParseLongFlag(string arg, CommandOptions options, ref int index)
    {
        // Handle --flag=value format
        if (arg.Contains("="))
        {
            var parts = arg[2..].Split('=', 2);
            string flagName = parts[0];
            string value = parts[1];

            SetOption(flagName, value, options);
        }
        else
        {
            string flagName = arg[2..];

            // Check if next argument is a value (doesn't start with dash)
            if (index + 1 < _args.Length && !_args[index + 1].StartsWith("-"))
            {
                index++;
                SetOption(flagName, _args[index], options);
            }
            else
            {
                options.Flags.Add(arg);
            }
        }
    }

    private void ParseShortFlag(string arg, CommandOptions options, ref int index)
    {
        string flagName = arg[1..];

        // Single character flags
        if (flagName.Length == 1)
        {
            // Check if there's a value following
            if (index + 1 < _args.Length && !_args[index + 1].StartsWith("-"))
            {
                index++;
                SetOption(flagName, _args[index], options);
            }
            else
            {
                options.Flags.Add(arg);
            }
        }
        else
        {
            // Multi-character short flag
            options.Flags.Add(arg);
        }
    }

    private static void SetOption(string key, string value, CommandOptions options)
    {
        key = key.ToLowerInvariant();

        // Map CLI arguments to CommandOptions properties
        switch (key)
        {
            case "name":
                options.PolicyName = value;
                break;
            case "type":
                options.PolicyType = value;
                break;
            case "max-retries":
            case "maxretries":
                if (int.TryParse(value, out int retries))
                    options.MaxRetries = retries;
                break;
            case "threshold":
                if (int.TryParse(value, out int threshold))
                    options.FailureThreshold = threshold;
                break;
            case "parallelization":
            case "max-parallel":
                if (int.TryParse(value, out int parallel))
                    options.MaxParallelization = parallel;
                break;
            case "timeout":
                if (TimeSpan.TryParse(value, out var timeout))
                    options.Timeout = timeout;
                break;
            case "duration":
            case "open-duration":
                if (TimeSpan.TryParse(value, out var duration))
                    options.OpenDuration = duration;
                break;
            case "output":
            case "o":
                options.OutputFile = value;
                break;
            case "config":
            case "c":
                options.ConfigFile = value;
                break;
            default:
                options.Arguments[key] = value;
                break;
        }

        // Handle verbose and json flags
        if (key == "verbose" || key == "v")
            options.Verbose = true;

        if (key == "json" || key == "j")
            options.JsonOutput = true;
    }

    /// <summary>
    /// Displays help information for available commands.
    /// </summary>
    public static string GetHelpText()
    {
        return @"
DotNet Resilience Pipeline - CLI Interface
============================================

USAGE: dotnet run -- <command> [subcommand] [options]

COMMANDS:
  policy      - Manage resilience policies
  pipeline    - Manage the pipeline
  metrics     - Display metrics and statistics
  health      - Check pipeline health status
  help        - Show this help message

POLICY COMMANDS:
  policy create    - Create a new policy
  policy list      - List all policies
  policy get       - Get policy details
  policy delete    - Delete a policy
  policy validate  - Validate a policy configuration

OPTIONS:
  --name              Policy name
  --type              Policy type (circuitbreaker, retry, timeout, bulkhead, fallback)
  --max-retries       Maximum retry attempts
  --threshold         Failure threshold for circuit breaker
  --parallelization   Max parallel executions
  --timeout           Timeout duration (e.g., 00:00:10)
  --duration          Open duration for circuit breaker
  --config -c         Configuration file path
  --output -o         Output file path
  --verbose -v        Enable verbose output
  --json -j           Output in JSON format
  --help -h           Show help information

EXAMPLES:
  dotnet run -- policy create --name payment --type circuitbreaker --threshold 5
  dotnet run -- metrics list --json --output stats.json
  dotnet run -- health check --verbose
  dotnet run -- policy list --config resilience.json
";
    }
}
