#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Provides validation helpers for <see cref="FailureInjectionService"/> instances.
/// </summary>
public static class FailureInjectionServiceValidation
{
    /// <summary>
    /// Validates the specified <see cref="FailureInjectionService"/> instance.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <returns>A list of human-readable validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this FailureInjectionService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate rules
        var rules = value.GetRules();
        foreach (var rule in rules)
        {
            ValidateRule(rule, problems);
        }

        // Validate total injections count
        if (value.TotalInjections < 0)
        {
            problems.Add($"TotalInjections must be non-negative, but was {value.TotalInjections}.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="FailureInjectionService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to check.</param>
    /// <returns><c>true</c> if valid; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this FailureInjectionService? value)
    {
        return value?.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="FailureInjectionService"/> instance is valid.
    /// </summary>
    /// <param name="value">The service instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is not valid, containing a list of problems.</exception>
    public static void EnsureValid(this FailureInjectionService? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"FailureInjectionService is not valid. Problems:\n{string.Join("\n", problems)}");
        }
    }

    private static void ValidateRule(InjectionRule rule, List<string> problems)
    {
        ArgumentNullException.ThrowIfNull(rule);
        ArgumentException.ThrowIfNullOrEmpty(rule.Key, nameof(rule.Key));

        // Validate Key
        if (string.IsNullOrWhiteSpace(rule.Key))
        {
            problems.Add($"Rule.Key cannot be null or whitespace.");
        }
        else if (rule.Key.Length > 100)
        {
            problems.Add($"Rule.Key '{rule.Key}' exceeds maximum length of 100 characters.");
        }

        // Validate Type
        if (!Enum.IsDefined(typeof(InjectionType), rule.Type))
        {
            problems.Add($"Rule.Type has invalid value {(int)rule.Type}.");
        }

        // Validate IsEnabled
        // No validation needed - boolean can always be true or false

        // Validate InjectionRate
        if (double.IsNaN(rule.InjectionRate))
        {
            problems.Add($"Rule.InjectionRate cannot be NaN.");
        }
        else if (double.IsInfinity(rule.InjectionRate))
        {
            problems.Add($"Rule.InjectionRate cannot be infinite.");
        }
        else if (rule.InjectionRate < 0.0 || rule.InjectionRate > 1.0)
        {
            problems.Add($"Rule.InjectionRate must be between 0.0 and 1.0 (inclusive), but was {rule.InjectionRate.ToString(CultureInfo.InvariantCulture)}.");
        }

        // Validate ExceptionMessage
        if (rule.ExceptionMessage is not null && rule.ExceptionMessage.Length > 500)
        {
            problems.Add($"Rule.ExceptionMessage for rule '{rule.Key}' exceeds maximum length of 500 characters.");
        }

        // Validate ExceptionFactory
        // ExceptionFactory is a delegate - cannot validate its behavior, only that it's not null

        // Validate LatencyDelay
        if (rule.LatencyDelay.HasValue)
        {
            if (rule.LatencyDelay.Value < TimeSpan.Zero)
            {
                problems.Add($"Rule.LatencyDelay for rule '{rule.Key}' cannot be negative, but was {rule.LatencyDelay}.");
            }
            else if (rule.LatencyDelay.Value.TotalMilliseconds > 3600000) // 1 hour
            {
                problems.Add($"Rule.LatencyDelay for rule '{rule.Key}' exceeds reasonable maximum of 1 hour, but was {rule.LatencyDelay}.");
            }
        }

        // Validate TimeoutDuration
        if (rule.TimeoutDuration.HasValue)
        {
            if (rule.TimeoutDuration.Value < TimeSpan.Zero)
            {
                problems.Add($"Rule.TimeoutDuration for rule '{rule.Key}' cannot be negative, but was {rule.TimeoutDuration}.");
            }
            else if (rule.TimeoutDuration.Value.TotalMilliseconds > 86400000) // 24 hours
            {
                problems.Add($"Rule.TimeoutDuration for rule '{rule.Key}' exceeds reasonable maximum of 24 hours, but was {rule.TimeoutDuration}.");
            }
        }

        // Validate InjectionsPerformed
        if (rule.InjectionsPerformed < 0)
        {
            problems.Add($"Rule.InjectionsPerformed for rule '{rule.Key}' must be non-negative, but was {rule.InjectionsPerformed}.");
        }

        // Validate consistency between InjectionType and related properties
        switch (rule.Type)
        {
            case InjectionType.Exception:
                if (rule.ExceptionMessage is null && rule.ExceptionFactory is null)
                {
                    problems.Add($"Rule '{rule.Key}' has InjectionType.Exception but neither ExceptionMessage nor ExceptionFactory is set.");
                }
                break;

            case InjectionType.Latency:
                if (!rule.LatencyDelay.HasValue)
                {
                    problems.Add($"Rule '{rule.Key}' has InjectionType.Latency but LatencyDelay is not set.");
                }
                break;

            case InjectionType.Timeout:
                if (!rule.TimeoutDuration.HasValue)
                {
                    problems.Add($"Rule '{rule.Key}' has InjectionType.Timeout but TimeoutDuration is not set.");
                }
                break;
        }
    }
}