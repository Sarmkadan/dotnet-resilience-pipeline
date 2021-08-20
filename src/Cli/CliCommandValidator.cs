// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Cli;

/// <summary>
/// Validates CLI commands and their arguments before execution.
/// Ensures all required parameters are present and values are within acceptable ranges.
/// </summary>
public class CliCommandValidator
{
    private readonly List<string> _errors = new();
    private readonly List<string> _warnings = new();

    /// <summary>
    /// Validates a complete command with all its options.
    /// </summary>
    public ValidationResult Validate(CommandOptions options)
    {
        _errors.Clear();
        _warnings.Clear();

        ValidateCommand(options);
        ValidateOptions(options);

        return new ValidationResult
        {
            IsValid = _errors.Count == 0,
            Errors = _errors.ToList(),
            Warnings = _warnings.ToList()
        };
    }

    private void ValidateCommand(CommandOptions options)
    {
        var validCommands = new[] { "policy", "pipeline", "metrics", "health", "help" };

        if (!validCommands.Contains(options.Command))
            _errors.Add($"Invalid command: {options.Command}");

        // Validate subcommands based on command type
        if (options.Command == "policy")
        {
            var validSubcommands = new[] { "create", "list", "get", "delete", "validate" };
            if (!string.IsNullOrEmpty(options.Subcommand) && !validSubcommands.Contains(options.Subcommand))
                _errors.Add($"Invalid policy subcommand: {options.Subcommand}");
        }
    }

    private void ValidateOptions(CommandOptions options)
    {
        // Validate policy name
        if (!string.IsNullOrEmpty(options.PolicyName))
        {
            if (options.PolicyName.Length < 2)
                _errors.Add("Policy name must be at least 2 characters");

            if (options.PolicyName.Length > 100)
                _errors.Add("Policy name must not exceed 100 characters");

            if (!IsValidIdentifier(options.PolicyName))
                _errors.Add("Policy name contains invalid characters. Use alphanumeric, dash, and underscore only");
        }

        // Validate policy type
        if (!string.IsNullOrEmpty(options.PolicyType))
        {
            var validTypes = new[] { "circuitbreaker", "retry", "timeout", "bulkhead", "fallback" };
            if (!validTypes.Contains(options.PolicyType.ToLowerInvariant()))
                _errors.Add($"Invalid policy type: {options.PolicyType}");
        }

        // Validate numeric ranges
        if (options.MaxRetries.HasValue && options.MaxRetries < 0)
            _errors.Add("MaxRetries cannot be negative");

        if (options.MaxRetries.HasValue && options.MaxRetries > 100)
            _warnings.Add("MaxRetries is very high (>100). Consider reducing for production.");

        if (options.FailureThreshold.HasValue && options.FailureThreshold < 1)
            _errors.Add("FailureThreshold must be at least 1");

        if (options.FailureThreshold.HasValue && options.FailureThreshold > 1000)
            _warnings.Add("FailureThreshold is very high (>1000). Consider reducing.");

        if (options.MaxParallelization.HasValue && options.MaxParallelization < 1)
            _errors.Add("MaxParallelization must be at least 1");

        if (options.MaxParallelization.HasValue && options.MaxParallelization > 10000)
            _warnings.Add("MaxParallelization is very high (>10000). Consider reducing for resource constraints.");

        // Validate timeouts
        if (options.Timeout.HasValue)
        {
            if (options.Timeout.Value < TimeSpan.FromMilliseconds(1))
                _errors.Add("Timeout must be at least 1 millisecond");

            if (options.Timeout.Value > TimeSpan.FromHours(1))
                _warnings.Add("Timeout exceeds 1 hour. Consider reducing.");
        }

        if (options.OpenDuration.HasValue)
        {
            if (options.OpenDuration.Value < TimeSpan.FromSeconds(1))
                _errors.Add("OpenDuration must be at least 1 second");

            if (options.OpenDuration.Value > TimeSpan.FromHours(1))
                _warnings.Add("OpenDuration exceeds 1 hour. Might be too long for recovery.");
        }

        // Validate file paths
        if (!string.IsNullOrEmpty(options.OutputFile))
        {
            try
            {
                var path = Path.GetDirectoryName(options.OutputFile);
                if (!string.IsNullOrEmpty(path) && !Directory.Exists(path))
                    _errors.Add($"Output directory does not exist: {path}");
            }
            catch (Exception ex)
            {
                _errors.Add($"Invalid output file path: {ex.Message}");
            }
        }

        if (!string.IsNullOrEmpty(options.ConfigFile))
        {
            if (!File.Exists(options.ConfigFile))
                _errors.Add($"Configuration file not found: {options.ConfigFile}");
        }
    }

    private static bool IsValidIdentifier(string name)
    {
        // Allow alphanumeric, dash, underscore, and dot
        return System.Text.RegularExpressions.Regex.IsMatch(
            name,
            @"^[a-zA-Z0-9_.-]+$");
    }
}

/// <summary>
/// Result of command validation with errors and warnings.
/// </summary>
public class ValidationResult
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();

    public override string ToString()
    {
        var output = new System.Text.StringBuilder();

        if (!IsValid)
        {
            output.AppendLine("❌ Validation Failed:");
            foreach (var error in Errors)
                output.AppendLine($"  ERROR: {error}");
        }
        else
        {
            output.AppendLine("✓ Validation Passed");
        }

        if (Warnings.Count > 0)
        {
            output.AppendLine("\n⚠ Warnings:");
            foreach (var warning in Warnings)
                output.AppendLine($"  WARNING: {warning}");
        }

        return output.ToString();
    }
}
