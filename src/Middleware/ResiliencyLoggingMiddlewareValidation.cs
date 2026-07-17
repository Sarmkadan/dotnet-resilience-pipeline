#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Globalization;

namespace DotNetResiliencePipeline.Middleware;

/// <summary>
/// Provides validation helpers for <see cref="ResiliencyLoggingMiddleware"/> instances.
/// Validates configuration, state, and business rules to ensure middleware is used correctly.
/// </summary>
public static class ResiliencyLoggingMiddlewareValidation
{
    /// <summary>
    /// Validates the given middleware instance and returns a list of human-readable problems.
    /// Returns an empty list if the middleware is valid.
    /// </summary>
    /// <param name="value">The middleware instance to validate.</param>
    /// <returns>A list of validation problems; empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ResiliencyLoggingMiddleware value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // Validate MaxLogEntries
        if (value.MaxLogEntries <= 0)
        {
            problems.Add($"MaxLogEntries must be positive, but was {value.MaxLogEntries}.");
        }

        // Validate Id (from LogEntry)
        if (value.GetLogs().Any(e => string.IsNullOrWhiteSpace(e.Id)))
        {
            problems.Add("One or more log entries have null, empty, or whitespace Id.");
        }

        // Validate PolicyName (from LogEntry)
        if (value.GetLogs().Any(e => string.IsNullOrWhiteSpace(e.PolicyName)))
        {
            problems.Add("One or more log entries have null, empty, or whitespace PolicyName.");
        }

        // Validate OperationName (from LogEntry)
        if (value.GetLogs().Any(e => string.IsNullOrWhiteSpace(e.OperationName)))
        {
            problems.Add("One or more log entries have null, empty, or whitespace OperationName.");
        }

        // Validate DurationMs (from LogEntry) - should be non-negative
        if (value.GetLogs().Any(e => e.DurationMs < 0))
        {
            problems.Add("One or more log entries have negative DurationMs.");
        }

        // Validate Timestamp (from LogEntry) - should not be default(DateTime)
        if (value.GetLogs().Any(e => e.Timestamp == default))
        {
            problems.Add("One or more log entries have default DateTime Timestamp.");
        }

        // Validate Success (from LogEntry) - no specific validation needed beyond boolean

        // Validate TotalEntries (from LogSummary)
        var summary = value.GetSummary();
        if (summary.TotalEntries < 0)
        {
            problems.Add($"LogSummary.TotalEntries cannot be negative, but was {summary.TotalEntries}.");
        }

        // Validate SuccessfulExecutions (from LogSummary)
        if (summary.SuccessfulExecutions < 0)
        {
            problems.Add($"LogSummary.SuccessfulExecutions cannot be negative, but was {summary.SuccessfulExecutions}.");
        }

        // Validate FailedExecutions (from LogSummary)
        if (summary.FailedExecutions < 0)
        {
            problems.Add($"LogSummary.FailedExecutions cannot be negative, but was {summary.FailedExecutions}.");
        }

        // Validate SuccessRate (from LogSummary) - should be between 0 and 100
        if (summary.SuccessRate < 0 || summary.SuccessRate > 100)
        {
            problems.Add($"LogSummary.SuccessRate must be between 0 and 100, but was {summary.SuccessRate.ToString(CultureInfo.InvariantCulture)}.");
        }

        // Validate AverageDurationMs (from LogSummary) - should be non-negative
        if (summary.AverageDurationMs < 0)
        {
            problems.Add($"LogSummary.AverageDurationMs cannot be negative, but was {summary.AverageDurationMs.ToString(CultureInfo.InvariantCulture)}.");
        }

        // Validate OldestLogTime/NewestLogTime (from LogSummary) - should not be default if TotalEntries > 0
        if (summary.TotalEntries > 0)
        {
            if (summary.OldestLogTime == default)
            {
                problems.Add("LogSummary.OldestLogTime is default DateTime despite having log entries.");
            }

            if (summary.NewestLogTime == default)
            {
                problems.Add("LogSummary.NewestLogTime is default DateTime despite having log entries.");
            }
        }

        return problems.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the given middleware instance is valid.
    /// </summary>
    /// <param name="value">The middleware instance to check.</param>
    /// <returns>True if valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static bool IsValid(this ResiliencyLoggingMiddleware value)
        => value is not null && value.Validate().Count == 0;

    /// <summary>
    /// Ensures that the given middleware instance is valid, throwing an <see cref="ArgumentException"/>
    /// with a detailed message listing all validation problems if it is not.
    /// </summary>
    /// <param name="value">The middleware instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the middleware is invalid, containing a list of problems.</exception>
    public static void EnsureValid(this ResiliencyLoggingMiddleware value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = value.Validate();
        if (problems.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"ResiliencyLoggingMiddleware is invalid. Problems:\n{string.Join("\n", problems)}");
    }
}