#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Provides validation helpers for <see cref="FallbackPolicy"/> instances.
/// </summary>
public static class FallbackPolicyValidation
{
    /// <summary>
    /// Validates the specified fallback policy.
    /// </summary>
    /// <param name="value">The fallback policy to validate.</param>
    /// <returns>A list of validation problems; empty if the policy is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this FallbackPolicy? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate name (inherited from ResiliencyPolicy)
        if (string.IsNullOrWhiteSpace(value.Name))
        {
            problems.Add("Name cannot be null, empty, or whitespace.");
        }

        // Validate fallback timeout
        if (value.FallbackTimeout <= TimeSpan.Zero)
        {
            problems.Add("FallbackTimeout must be a positive time span.");
        }

        // Validate fallback trigger exceptions when FallbackOnAnyException is false
        if (!value.FallbackOnAnyException && value.FallbackTriggerExceptions.Count == 0)
        {
            problems.Add("Must have fallback trigger exceptions when FallbackOnAnyException is false.");
        }

        // Validate fallback trigger exceptions collection
        foreach (var exceptionType in value.FallbackTriggerExceptions)
        {
            if (exceptionType is null)
            {
                problems.Add("FallbackTriggerExceptions collection contains a null element.");
                continue;
            }

            if (!typeof(Exception).IsAssignableFrom(exceptionType))
            {
                problems.Add($"FallbackTriggerExceptions contains invalid type '{exceptionType.Name}' which is not an Exception.");
            }
        }

        // Validate statistics counters (should not be negative)
        if (value.FallbackInvocationCount < 0)
        {
            problems.Add("FallbackInvocationCount cannot be negative.");
        }

        if (value.SuccessfulFallbackCount < 0)
        {
            problems.Add("SuccessfulFallbackCount cannot be negative.");
        }

        if (value.FailedFallbackCount < 0)
        {
            problems.Add("FailedFallbackCount cannot be negative.");
        }

        // Validate average execution time (should not be negative)
        if (value.AverageFallbackExecutionTimeMs < 0)
        {
            problems.Add("AverageFallbackExecutionTimeMs cannot be negative.");
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified fallback policy is valid.
    /// </summary>
    /// <param name="value">The fallback policy to check.</param>
    /// <returns>True if the policy is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this FallbackPolicy? value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified fallback policy is valid.
    /// </summary>
    /// <param name="value">The fallback policy to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the policy is not valid, containing a list of validation problems.</exception>
    public static void EnsureValid(this FallbackPolicy? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();

        if (problems.Count > 0)
        {
            throw new ArgumentException(
                $"FallbackPolicy validation failed:{Environment.NewLine}{string.Join(Environment.NewLine, problems)}");
        }
    }
}