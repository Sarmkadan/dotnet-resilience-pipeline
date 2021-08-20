// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Cli;

/// <summary>
/// Command-line options and arguments parser for CLI interface.
/// Supports policy creation, monitoring, and configuration operations.
/// </summary>
public class CommandOptions
{
    public string Command { get; set; } = string.Empty;
    public string? Subcommand { get; set; }
    public Dictionary<string, string> Arguments { get; set; } = new(StringComparer.OrdinalIgnoreCase);
    public List<string> Flags { get; set; } = new();

    // Policy configuration options
    public string? PolicyName { get; set; }
    public string? PolicyType { get; set; }
    public int? MaxRetries { get; set; }
    public int? FailureThreshold { get; set; }
    public int? MaxParallelization { get; set; }
    public TimeSpan? Timeout { get; set; }
    public TimeSpan? OpenDuration { get; set; }
    public bool Verbose { get; set; }
    public bool JsonOutput { get; set; }
    public string? OutputFile { get; set; }
    public string? ConfigFile { get; set; }

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

        if (PolicyType != null && !IsValidPolicyType(PolicyType))
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
