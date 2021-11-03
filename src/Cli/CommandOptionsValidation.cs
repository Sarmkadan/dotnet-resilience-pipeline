#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotNetResiliencePipeline.Cli;

/// <summary>
/// Provides validation helpers for <see cref="CommandOptions"/> instances.
/// </summary>
public static class CommandOptionsValidation
{
    /// <summary>
    /// Validates the specified <see cref="CommandOptions"/> instance.
    /// </summary>
    /// <param name="value">The command options to validate.</param>
    /// <returns>A list of validation error messages; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this CommandOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Command
        if (string.IsNullOrWhiteSpace(value.Command))
        {
            errors.Add("Command is required and cannot be null or whitespace.");
        }

        // Validate Subcommand
        if (value.Subcommand is not null && string.IsNullOrWhiteSpace(value.Subcommand))
        {
            errors.Add("Subcommand cannot be empty or whitespace.");
        }

        // Validate Arguments dictionary
        if (value.Arguments is null)
        {
            errors.Add("Arguments dictionary cannot be null.");
        }

        // Validate Flags list
        if (value.Flags is null)
        {
            errors.Add("Flags list cannot be null.");
        }

        // Validate PolicyName
        if (value.PolicyName is not null && string.IsNullOrWhiteSpace(value.PolicyName))
        {
            errors.Add("PolicyName cannot be empty or whitespace.");
        }

        // Validate PolicyType
        if (value.PolicyType is not null && string.IsNullOrWhiteSpace(value.PolicyType))
        {
            errors.Add("PolicyType cannot be empty or whitespace.");
        }
        else if (value.PolicyType is not null && !IsValidPolicyType(value.PolicyType))
        {
            errors.Add($"Invalid policy type: '{value.PolicyType}'. Valid types are: circuitbreaker, retry, timeout, bulkhead, fallback.");
        }

        // Validate MaxRetries
        if (value.MaxRetries < 0)
        {
            errors.Add("MaxRetries cannot be negative.");
        }

        // Validate FailureThreshold
        if (value.FailureThreshold < 0)
        {
            errors.Add("FailureThreshold cannot be negative.");
        }

        // Validate MaxParallelization
        if (value.MaxParallelization < 0)
        {
            errors.Add("MaxParallelization cannot be negative.");
        }

        // Validate Timeout
        if (value.Timeout is not null && value.Timeout <= TimeSpan.Zero)
        {
            errors.Add("Timeout must be a positive time span.");
        }

        // Validate OpenDuration
        if (value.OpenDuration is not null && value.OpenDuration <= TimeSpan.Zero)
        {
            errors.Add("OpenDuration must be a positive time span.");
        }

        // Validate OutputFile
        if (value.OutputFile is not null && string.IsNullOrWhiteSpace(value.OutputFile))
        {
            errors.Add("OutputFile cannot be empty or whitespace.");
        }

        // Validate ConfigFile
        if (value.ConfigFile is not null && string.IsNullOrWhiteSpace(value.ConfigFile))
        {
            errors.Add("ConfigFile cannot be empty or whitespace.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="CommandOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The command options to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this CommandOptions value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="CommandOptions"/> instance is valid.
    /// </summary>
    /// <param name="value">The command options to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="value"/> is invalid. The exception message contains all validation errors.</exception>
    public static void EnsureValid(this CommandOptions value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            "CommandOptions validation failed:\n" + string.Join("\n", errors),
            nameof(value));
    }

    private static bool IsValidPolicyType(string type)
    {
        return type is "circuitbreaker" or "retry" or "timeout" or "bulkhead" or "fallback";
    }
}