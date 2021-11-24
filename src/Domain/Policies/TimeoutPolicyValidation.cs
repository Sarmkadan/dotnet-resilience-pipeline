#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Provides validation helpers for <see cref="TimeoutPolicy"/> instances.
/// </summary>
public static class TimeoutPolicyValidation
{
    /// <summary>
    /// Validates a <see cref="TimeoutPolicy"/> instance and returns a list of human-readable validation errors.
    /// </summary>
    /// <param name="value">The policy instance to validate.</param>
    /// <returns>An empty list if valid; otherwise, a list of validation error messages.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this TimeoutPolicy value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Timeout
        if (value.Timeout <= TimeSpan.Zero)
        {
            errors.Add("Timeout must be a positive time span.");
        }

        // Validate TimeoutCount
        if (value.TimeoutCount < 0)
        {
            errors.Add("TimeoutCount cannot be negative.");
        }

        // Validate AverageExecutionTimeMs
        if (double.IsNaN(value.AverageExecutionTimeMs) || double.IsInfinity(value.AverageExecutionTimeMs))
        {
            errors.Add("AverageExecutionTimeMs must be a valid number.");
        }
        else if (value.AverageExecutionTimeMs < 0)
        {
            errors.Add("AverageExecutionTimeMs cannot be negative.");
        }

        // Validate LongestExecutionTimeMs
        if (value.LongestExecutionTimeMs < 0)
        {
            errors.Add("LongestExecutionTimeMs cannot be negative.");
        }

        // Validate ShortestExecutionTimeMs
        if (value.ShortestExecutionTimeMs < 0)
        {
            errors.Add("ShortestExecutionTimeMs cannot be negative.");
        }
        else if (value.ShortestExecutionTimeMs == long.MaxValue)
        {
            // This indicates no execution times have been recorded yet
            errors.Add("ShortestExecutionTimeMs has not been initialized with actual execution times.");
        }

        // Validate that ShortestExecutionTimeMs <= LongestExecutionTimeMs when both are set
        if (value.ShortestExecutionTimeMs > 0 && value.LongestExecutionTimeMs > 0
            && value.ShortestExecutionTimeMs > value.LongestExecutionTimeMs)
        {
            errors.Add("ShortestExecutionTimeMs cannot be greater than LongestExecutionTimeMs.");
        }

        // Validate that AverageExecutionTimeMs is within bounds of min/max execution times
        if (value.AverageExecutionTimeMs > 0 && value.LongestExecutionTimeMs > 0)
        {
            if (value.AverageExecutionTimeMs > value.LongestExecutionTimeMs * 1.5)
            {
                errors.Add("AverageExecutionTimeMs appears inconsistent with recorded execution times (too high compared to LongestExecutionTimeMs).");
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="TimeoutPolicy"/> instance is valid.
    /// </summary>
    /// <param name="value">The policy instance to check.</param>
    /// <returns><see langword="true"/> if the policy is valid; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this TimeoutPolicy value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="TimeoutPolicy"/> instance is valid.
    /// </summary>
    /// <param name="value">The policy instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the policy is invalid, containing all validation errors.</exception>
    public static void EnsureValid(this TimeoutPolicy value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"TimeoutPolicy validation failed:{Environment.NewLine}- {
                    string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }
}