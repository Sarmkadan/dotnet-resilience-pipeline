#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text;

namespace DotNetResiliencePipeline.Middleware;

/// <summary>
/// Extension methods for <see cref="ErrorHandlingMiddleware"/> providing additional functionality
/// for error analysis, filtering, and reporting.
/// </summary>
public static class ErrorHandlingMiddlewareExtensions
{
    /// <summary>
    /// Filters error contexts by exception type.
    /// </summary>
    /// <param name="middleware">The middleware instance.</param>
    /// <param name="exceptionType">Exception type to filter by (e.g., "TimeoutException").</param>
    /// <returns>Filtered list of error contexts.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="middleware"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="exceptionType"/> is null or whitespace.</exception>
    public static List<ErrorContext> GetErrorsByType(this ErrorHandlingMiddleware middleware, string exceptionType)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        ArgumentException.ThrowIfNullOrWhiteSpace(exceptionType);

        return middleware.GetErrorContexts()
            .Where(c => c.ExceptionType.Equals(exceptionType, StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>
    /// Filters error contexts by recoverability status.
    /// </summary>
    /// <param name="middleware">The middleware instance.</param>
    /// <param name="recoverableOnly">True to get only recoverable errors, false for non-recoverable.</param>
    /// <returns>Filtered list of error contexts.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="middleware"/> is null.</exception>
    public static List<ErrorContext> GetErrorsByRecoverability(this ErrorHandlingMiddleware middleware, bool recoverableOnly = true)
    {
        ArgumentNullException.ThrowIfNull(middleware);

        return middleware.GetErrorContexts()
            .Where(c => c.IsRecoverable == recoverableOnly)
            .ToList();
    }

    /// <summary>
    /// Gets error contexts for a specific operation name.
    /// </summary>
    /// <param name="middleware">The middleware instance.</param>
    /// <param name="operationName">Operation name to filter by.</param>
    /// <returns>Filtered list of error contexts.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="middleware"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="operationName"/> is null or whitespace.</exception>
    public static List<ErrorContext> GetErrorsForOperation(this ErrorHandlingMiddleware middleware, string operationName)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        ArgumentException.ThrowIfNullOrWhiteSpace(operationName);

        return middleware.GetErrorContexts()
            .Where(c => c.OperationName.Equals(operationName, StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>
    /// Generates a formatted error report string containing statistics and common errors.
    /// </summary>
    /// <param name="middleware">The middleware instance.</param>
    /// <param name="includeContexts">Whether to include detailed error contexts in the report.</param>
    /// <returns>Formatted error report string.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="middleware"/> is null.</exception>
    public static string GenerateErrorReport(this ErrorHandlingMiddleware middleware, bool includeContexts = false)
    {
        ArgumentNullException.ThrowIfNull(middleware);

        var sb = new StringBuilder();
        sb.AppendLine("=== Error Handling Middleware Report ===");
        sb.AppendLine($"Generated: {DateTime.UtcNow:O}");
        sb.AppendLine();

        var stats = middleware.GetErrorStatistics();
        sb.AppendLine("=== Error Statistics ===");
        sb.AppendLine($"Total unique error types: {stats.Count}");
        sb.AppendLine($"Total error occurrences: {stats.Sum(s => s.Value.Count)}");
        sb.AppendLine();

        sb.AppendLine("=== Most Common Errors ===");
        var commonErrors = middleware.GetMostCommonErrors(15);
        foreach (var (error, count) in commonErrors)
        {
            var errorStats = stats[error];
            sb.AppendLine($"- {error}: {count} occurrences (Last: {errorStats.LastOccurrence:O}, Frequency: {errorStats.Frequency:P1})");
        }
        sb.AppendLine();

        if (includeContexts && middleware.GetErrorContexts().Count > 0)
        {
            sb.AppendLine("=== Recent Error Contexts ===");
            foreach (var context in middleware.GetErrorContexts().OrderByDescending(c => c.Timestamp).Take(20))
            {
                sb.AppendLine(context.ToString());
                sb.AppendLine($" Policy: {context.PolicyName}");
                sb.AppendLine($" Operation: {context.OperationName}");
                sb.AppendLine($" Recoverable: {context.IsRecoverable}");
                sb.AppendLine($" Recommendation: {context.RecoveryRecommendation}");
                sb.AppendLine();
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Checks if a specific error type has occurred within a time window.
    /// </summary>
    /// <param name="middleware">The middleware instance.</param>
    /// <param name="exceptionType">Exception type to check.</param>
    /// <param name="timeWindowMinutes">Time window in minutes to check within.</param>
    /// <returns>True if error occurred within the time window.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="middleware"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="exceptionType"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeWindowMinutes"/> is not positive.</exception>
    public static bool HasErrorOccurredRecently(this ErrorHandlingMiddleware middleware, string exceptionType, int timeWindowMinutes = 60)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        ArgumentException.ThrowIfNullOrWhiteSpace(exceptionType);
        if (timeWindowMinutes <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(timeWindowMinutes), "Time window must be positive");
        }

        var cutoff = DateTime.UtcNow.AddMinutes(-timeWindowMinutes);
        return middleware.GetErrorContexts()
            .Any(c => c.ExceptionType.Equals(exceptionType, StringComparison.Ordinal) && c.Timestamp >= cutoff);
    }

    /// <summary>
    /// Gets the total count of all error occurrences across all error types.
    /// </summary>
    /// <param name="middleware">The middleware instance.</param>
    /// <returns>Total error count.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="middleware"/> is null.</exception>
    public static int GetTotalErrorCount(this ErrorHandlingMiddleware middleware)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        return middleware.GetErrorStatistics().Sum(s => s.Value.Count);
    }
}