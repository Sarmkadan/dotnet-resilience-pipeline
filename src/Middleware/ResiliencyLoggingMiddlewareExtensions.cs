#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Middleware;

/// <summary>
/// Extension methods for <see cref="ResiliencyLoggingMiddleware"/>, <see cref="LogEntry"/> and <see cref="LogSummary"/>
/// providing filtering and formatting helpers for logged resilience pipeline operations.
/// </summary>
public static class ResiliencyLoggingMiddlewareExtensions
{
    /// <summary>
    /// Retrieves log entries whose execution duration is strictly greater than the given threshold.
    /// </summary>
    /// <param name="middleware">The logging middleware to query.</param>
    /// <param name="thresholdMs">Duration threshold in milliseconds. Must be zero or greater.</param>
    /// <returns>A new list of entries with <see cref="LogEntry.DurationMs"/> greater than <paramref name="thresholdMs"/>, in insertion order.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="middleware"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="thresholdMs"/> is negative.</exception>
    public static List<LogEntry> GetLogsSlowerThan(this ResiliencyLoggingMiddleware middleware, long thresholdMs)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        if (thresholdMs < 0)
            throw new ArgumentOutOfRangeException(nameof(thresholdMs), thresholdMs, "Threshold must be zero or greater.");

        return middleware.GetLogsBetween(DateTime.MinValue, DateTime.MaxValue)
            .Where(l => l.DurationMs > thresholdMs)
            .ToList();
    }

    /// <summary>
    /// Retrieves log entries recorded within the last <paramref name="window"/> relative to the current UTC time.
    /// </summary>
    /// <param name="middleware">The logging middleware to query.</param>
    /// <param name="window">How far back to look. Must be zero or greater.</param>
    /// <returns>A new list of entries whose <see cref="LogEntry.Timestamp"/> falls within the window, in insertion order.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="middleware"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="window"/> is negative.</exception>
    public static List<LogEntry> GetRecentLogs(this ResiliencyLoggingMiddleware middleware, TimeSpan window)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        if (window < TimeSpan.Zero)
            throw new ArgumentOutOfRangeException(nameof(window), window, "Window must be zero or greater.");

        var now = DateTime.UtcNow;
        // Clamp instead of subtracting so that a very large window cannot overflow DateTime.
        var start = now - DateTime.MinValue < window ? DateTime.MinValue : now - window;

        return middleware.GetLogsBetween(start, now);
    }

    /// <summary>
    /// Formats the entry as a single human-readable line suitable for console or plain-text log output.
    /// </summary>
    /// <param name="entry">The log entry to format.</param>
    /// <returns>
    /// A line of the form <c>[timestamp] OK|FAIL policy.operation 12ms</c>, followed by the exception type and
    /// message for failed entries. Line breaks in the message are replaced with spaces so the result stays on one line.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="entry"/> is <c>null</c>.</exception>
    public static string ToLogLine(this LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        var outcome = entry.Success ? "OK" : "FAIL";
        var line = FormattableString.Invariant($"[{entry.Timestamp:O}] {outcome} {entry.PolicyName}.{entry.OperationName} {entry.DurationMs}ms");

        if (!string.IsNullOrWhiteSpace(entry.Exception))
            line += " " + entry.Exception;

        if (!string.IsNullOrWhiteSpace(entry.Message))
            line += ": " + entry.Message.ReplaceLineEndings(" ");

        return line;
    }

    /// <summary>
    /// Formats the summary as a single human-readable line suitable for console or plain-text log output.
    /// </summary>
    /// <param name="summary">The summary to format.</param>
    /// <returns>
    /// A line of the form <c>total=N ok=N failed=N successRate=X.X% avgMs=X.X</c>, followed by the time range
    /// when both oldest and newest timestamps are known.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="summary"/> is <c>null</c>.</exception>
    public static string ToLogLine(this LogSummary summary)
    {
        ArgumentNullException.ThrowIfNull(summary);

        var line = FormattableString.Invariant($"total={summary.TotalEntries} ok={summary.SuccessfulExecutions} failed={summary.FailedExecutions} successRate={summary.SuccessRate:F1}% avgMs={summary.AverageDurationMs:F1}");

        if (summary.OldestLogTime is { } oldest && summary.NewestLogTime is { } newest)
            line += FormattableString.Invariant($" range=[{oldest:O}..{newest:O}]");

        return line;
    }
}
