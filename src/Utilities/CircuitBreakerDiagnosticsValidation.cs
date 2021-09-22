#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Validation utilities for circuit breaker diagnostic types.
/// Provides validation, ensuring data integrity and correctness.
/// </summary>
public static class CircuitBreakerDiagnosticsValidation
{
    /// <summary>
    /// Validates a circuit breaker diagnostic report.
    /// </summary>
    /// <param name="value">The diagnostic report to validate</param>
    /// <returns>List of validation errors (empty if valid)</returns>
    public static IReadOnlyList<string> Validate(this CircuitBreakerDiagnosticReport value)
    {
        if (value is null)
        {
            return new[] { "Diagnostic report cannot be null" };
        }

        var errors = new List<string>();

        // Validate PolicyId
        if (string.IsNullOrWhiteSpace(value.PolicyId))
        {
            errors.Add("PolicyId cannot be null or whitespace");
        }

        // Validate PolicyName
        if (string.IsNullOrWhiteSpace(value.PolicyName))
        {
            errors.Add("PolicyName cannot be null or whitespace");
        }

        // Validate CurrentState
        if (!Enum.IsDefined(typeof(CircuitBreakerPolicy.CircuitState), value.CurrentState))
        {
            errors.Add("CurrentState has an invalid value");
        }

        // Validate FailureThreshold
        if (value.FailureThreshold <= 0)
        {
            errors.Add("FailureThreshold must be greater than 0");
        }

        // Validate OpenDuration
        if (value.OpenDuration <= TimeSpan.Zero)
        {
            errors.Add("OpenDuration must be greater than zero");
        }

        // Validate SuccessThreshold
        if (value.SuccessThreshold <= 0)
        {
            errors.Add("SuccessThreshold must be greater than 0");
        }

        // Validate GeneratedAt
        if (value.GeneratedAt == default)
        {
            errors.Add("GeneratedAt cannot be default(DateTime)");
        }
        else if (value.GeneratedAt > DateTime.UtcNow.AddMinutes(1))
        {
            errors.Add("GeneratedAt cannot be in the future");
        }

        // Validate Issues collection
        if (value.Issues is null)
        {
            errors.Add("Issues collection cannot be null");
        }

        // Validate Recommendations collection
        if (value.Recommendations is null)
        {
            errors.Add("Recommendations collection cannot be null");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates a circuit breaker effectiveness analysis.
    /// </summary>
    /// <param name="value">The effectiveness analysis to validate</param>
    /// <returns>List of validation errors (empty if valid)</returns>
    public static IReadOnlyList<string> Validate(this CircuitBreakerEffectiveness value)
    {
        if (value is null)
        {
            return new[] { "Effectiveness analysis cannot be null" };
        }

        var errors = new List<string>();

        // Validate PolicyName
        if (string.IsNullOrWhiteSpace(value.PolicyName))
        {
            errors.Add("PolicyName cannot be null or whitespace");
        }

        // Validate TotalExecutions
        if (value.TotalExecutions < 0)
        {
            errors.Add("TotalExecutions cannot be negative");
        }

        // Validate FailedExecutions
        if (value.FailedExecutions < 0)
        {
            errors.Add("FailedExecutions cannot be negative");
        }

        // Validate FailedExecutions <= TotalExecutions
        if (value.FailedExecutions > value.TotalExecutions)
        {
            errors.Add("FailedExecutions cannot exceed TotalExecutions");
        }

        // Validate FailureRate
        if (value.FailureRate < 0 || value.FailureRate > 100)
        {
            errors.Add("FailureRate must be between 0 and 100");
        }

        // Validate CurrentState
        if (!Enum.IsDefined(typeof(CircuitBreakerPolicy.CircuitState), value.CurrentState))
        {
            errors.Add("CurrentState has an invalid value");
        }

        // Validate EffectivenessRating
        if (string.IsNullOrWhiteSpace(value.EffectivenessRating))
        {
            errors.Add("EffectivenessRating cannot be null or whitespace");
        }
        else if (value.EffectivenessRating != "Excellent" &&
                 value.EffectivenessRating != "Good" &&
                 value.EffectivenessRating != "Fair" &&
                 value.EffectivenessRating != "Poor")
        {
            errors.Add("EffectivenessRating must be one of: Excellent, Good, Fair, Poor");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Validates a circuit breaker configuration suggestion.
    /// </summary>
    /// <param name="value">The configuration to validate</param>
    /// <returns>List of validation errors (empty if valid)</returns>
    public static IReadOnlyList<string> Validate(this CircuitBreakerConfiguration value)
    {
        if (value is null)
        {
            return new[] { "Configuration cannot be null" };
        }

        var errors = new List<string>();

        // Validate PolicyName
        if (string.IsNullOrWhiteSpace(value.PolicyName))
        {
            errors.Add("PolicyName cannot be null or whitespace");
        }

        // Validate SuggestedFailureThreshold
        if (value.SuggestedFailureThreshold <= 0)
        {
            errors.Add("SuggestedFailureThreshold must be greater than 0");
        }

        // Validate SuggestedOpenDuration
        if (value.SuggestedOpenDuration <= TimeSpan.Zero)
        {
            errors.Add("SuggestedOpenDuration must be greater than zero");
        }

        // Validate SuggestedSuccessThreshold
        if (value.SuggestedSuccessThreshold <= 0)
        {
            errors.Add("SuggestedSuccessThreshold must be greater than 0");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Checks if a diagnostic report is valid.
    /// </summary>
    /// <param name="value">The diagnostic report to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(this CircuitBreakerDiagnosticReport value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Checks if an effectiveness analysis is valid.
    /// </summary>
    /// <param name="value">The effectiveness analysis to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(this CircuitBreakerEffectiveness value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Checks if a configuration suggestion is valid.
    /// </summary>
    /// <param name="value">The configuration to check</param>
    /// <returns>True if valid, false otherwise</returns>
    public static bool IsValid(this CircuitBreakerConfiguration value)
    {
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that a diagnostic report is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The diagnostic report to validate</param>
    /// <exception cref="ArgumentException">Thrown if the report is invalid</exception>
    public static void EnsureValid(this CircuitBreakerDiagnosticReport value)
    {
        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException($"Circuit breaker diagnostic report is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <summary>
    /// Ensures that an effectiveness analysis is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The effectiveness analysis to validate</param>
    /// <exception cref="ArgumentException">Thrown if the analysis is invalid</exception>
    public static void EnsureValid(this CircuitBreakerEffectiveness value)
    {
        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException($"Circuit breaker effectiveness analysis is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }

    /// <summary>
    /// Ensures that a configuration suggestion is valid, throwing an exception if not.
    /// </summary>
    /// <param name="value">The configuration to validate</param>
    /// <exception cref="ArgumentException">Thrown if the configuration is invalid</exception>
    public static void EnsureValid(this CircuitBreakerConfiguration value)
    {
        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException($"Circuit breaker configuration is invalid:{Environment.NewLine}{string.Join(Environment.NewLine, errors)}");
        }
    }
}