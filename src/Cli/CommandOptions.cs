#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Cli;

/// <summary>
/// Command-line options and arguments parser for CLI interface.
/// Supports policy creation, monitoring, and configuration operations.
/// </summary>
public sealed class CommandOptions
{
    /// <summary>
    /// Gets or sets the top-level command name (for example, <c>policy</c> or <c>export</c>).
    /// </summary>
    public string Command { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the optional subcommand that follows the top-level command.
    /// </summary>
    public string? Subcommand { get; set; }

    /// <summary>
    /// Gets or sets the named arguments parsed from <c>--key value</c> pairs, keyed case-insensitively.
    /// </summary>
    public Dictionary<string, string> Arguments { get; set; } = new(StringComparer.OrdinalIgnoreCase);

    /// <summary>
    /// Gets or sets the raw flags (for example, <c>--reset</c>) that were present on the command line.
    /// </summary>
    public List<string> Flags { get; set; } = new();

    // Policy configuration options

    /// <summary>
    /// Gets or sets the name of the policy the command operates on.
    /// </summary>
    public string? PolicyName { get; set; }

    /// <summary>
    /// Gets or sets the policy type (<c>circuitbreaker</c>, <c>retry</c>, <c>timeout</c>, <c>bulkhead</c>, or <c>fallback</c>).
    /// </summary>
    public string? PolicyType { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int? MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failures that opens a circuit breaker.
    /// </summary>
    public int? FailureThreshold { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent executions allowed by a bulkhead.
    /// </summary>
    public int? MaxParallelization { get; set; }

    /// <summary>
    /// Gets or sets the timeout applied to an operation.
    /// </summary>
    public TimeSpan? Timeout { get; set; }

    /// <summary>
    /// Gets or sets how long a circuit breaker stays open before it allows a trial call.
    /// </summary>
    public TimeSpan? OpenDuration { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether verbose output was requested.
    /// </summary>
    public bool Verbose { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether output should be formatted as JSON.
    /// </summary>
    public bool JsonOutput { get; set; }

    /// <summary>
    /// Gets or sets the path of the file that command output should be written to.
    /// </summary>
    public string? OutputFile { get; set; }

    /// <summary>
    /// Gets or sets the path of the configuration file to load.
    /// </summary>
    public string? ConfigFile { get; set; }

    /// <summary>
    /// Returns a string representation of the command options.
    /// </summary>
    public override string ToString() => $"CommandOptions {{ Command = {Command}, Subcommand = {Subcommand}, Arguments = {string.Join(", ", Arguments.Select(kvp => $"{kvp.Key}:{kvp.Value}"))}, Flags = {string.Join(", ", Flags)}, PolicyName = {PolicyName}, PolicyType = {PolicyType} }}";

    /// <summary>
    /// Gets a flag value by name, checking multiple variations.
    /// </summary>
    public bool HasFlag(params string[] names)
    {
        return names.Any(name => Flags.Contains($"--{name}") || Flags.Contains($"-{name[0]}"));
    }

    /// <summary>
    /// Gets an argument value by key with fallback support.
    /// </summary>
    public string? GetArgument(string key, string? defaultValue = null)
    {
        return Arguments.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Validates that required options are set.
    /// </summary>
    public List<string> Validate()
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(Command))
            errors.Add("Command is required");

        if (Command == "policy" && string.IsNullOrWhiteSpace(PolicyName))
            errors.Add("Policy name is required for policy operations");

        if (PolicyType is not null && !IsValidPolicyType(PolicyType))
            errors.Add($"Invalid policy type: {PolicyType}");

        if (MaxRetries < 0)
            errors.Add("MaxRetries cannot be negative");

        if (FailureThreshold < 0)
            errors.Add("FailureThreshold cannot be negative");

        return errors;
    }

    private static bool IsValidPolicyType(string type)
    {
        return type switch
        {
            "circuitbreaker" or "retry" or "timeout" or "bulkhead" or "fallback" => true,
            _ => false
        };
    }
}
