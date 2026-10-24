#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Middleware;

/// <summary>
/// Middleware that logs all resilience pipeline operations with detailed metrics.
/// Tracks execution time, success/failure, and policy types involved.
/// </summary>
public sealed class ResiliencyLoggingMiddleware
{
    private readonly ResiliencyPipelineService _pipelineService;
    private List<LogEntry> _logs = new();
    private readonly object _lockObj = new object();
    /// <summary>
    /// Maximum number of log entries retained in memory.
    /// </summary>
    public int MaxLogEntries { get; set; } = 1000;

    /// <summary>
    /// Initializes a new logging middleware that wraps the given pipeline service.
    /// </summary>
    /// <param name="pipelineService">The pipeline service whose executions are logged.</param>
    public ResiliencyLoggingMiddleware(ResiliencyPipelineService pipelineService)
    {
        _pipelineService = pipelineService;
    }

    /// <summary>
    /// Logs an operation execution with full context.
    /// </summary>
    public void LogExecution(string policyName, string operationName, bool success, long durationMs, Exception? exception = null)
    {
        var entry = new LogEntry
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            PolicyName = policyName,
            OperationName = operationName,
            Success = success,
            DurationMs = durationMs,
            Exception = exception?.GetType().Name,
            Message = exception?.Message
        };

        lock (_lockObj)
        {
            _logs.Add(entry);

            // Keep logs bounded
            if (_logs.Count > MaxLogEntries)
                _logs = _logs.Skip(_logs.Count - MaxLogEntries).ToList();
        }
    }

    /// <summary>
    /// Retrieves all logged entries.
    /// </summary>
    public List<LogEntry> GetLogs() => new(_logs);

    /// <summary>
    /// Retrieves logs filtered by policy name.
    /// </summary>
    public List<LogEntry> GetLogsByPolicy(string policyName)
    {
        lock (_lockObj)
        {
            return _logs.Where(l => l.PolicyName == policyName).ToList();
        }
    }

    /// <summary>
    /// Retrieves logs within a time range.
    /// </summary>
    public List<LogEntry> GetLogsBetween(DateTime startTime, DateTime endTime)
    {
        lock (_lockObj)
        {
            return _logs.Where(l => l.Timestamp >= startTime && l.Timestamp <= endTime).ToList();
        }
    }

    /// <summary>
    /// Retrieves only failed execution logs.
    /// </summary>
    public List<LogEntry> GetFailedLogs()
    {
        lock (_lockObj)
        {
            return _logs.Where(l => !l.Success).ToList();
        }
    }

    /// <summary>
    /// Clears all logged entries.
    /// </summary>
    public void Clear()
    {
        lock (_lockObj)
        {
            _logs.Clear();
        }
    }

    /// <summary>
    /// Generates a summary report of logging statistics.
    /// </summary>
    public LogSummary GetSummary()
    {
        lock (_lockObj)
        {
            var totalLogs = _logs.Count;
            var successCount = _logs.Count(l => l.Success);
            var failureCount = _logs.Count(l => !l.Success);
            var avgDuration = _logs.Count > 0 ? _logs.Average(l => l.DurationMs) : 0;

            return new LogSummary
            {
                TotalEntries = totalLogs,
                SuccessfulExecutions = successCount,
                FailedExecutions = failureCount,
                SuccessRate = totalLogs > 0 ? (successCount * 100.0) / totalLogs : 0,
                AverageDurationMs = avgDuration,
                OldestLogTime = _logs.FirstOrDefault()?.Timestamp,
                NewestLogTime = _logs.LastOrDefault()?.Timestamp
            };
        }
    }
}

/// <summary>
/// Individual log entry for an operation.
/// </summary>
public sealed class LogEntry
{
    /// <summary>
    /// Unique identifier of this log entry.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// UTC time at which the operation was logged.
    /// </summary>
    public DateTime Timestamp { get; set; }

    /// <summary>
    /// Name of the policy that executed the operation.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Name of the logged operation.
    /// </summary>
    public string OperationName { get; set; } = string.Empty;

    /// <summary>
    /// Whether the operation succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Execution duration in milliseconds.
    /// </summary>
    public long DurationMs { get; set; }

    /// <summary>
    /// Exception type name if the operation failed, otherwise <see langword="null"/>.
    /// </summary>
    public string? Exception { get; set; }

    /// <summary>
    /// Additional message describing the entry, if any.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Returns a one-line summary of the entry: timestamp, outcome, policy, operation and duration.
    /// </summary>
    public override string ToString()
    {
        var status = Success ? "✓" : "✗";
        return $"[{Timestamp:O}] {status} {PolicyName}.{OperationName} ({DurationMs}ms)";
    }
}

/// <summary>
/// Summary statistics for logging.
/// </summary>
public sealed class LogSummary
{
    /// <summary>
    /// Total number of log entries.
    /// </summary>
    public int TotalEntries { get; set; }

    /// <summary>
    /// Number of successful executions among the entries.
    /// </summary>
    public int SuccessfulExecutions { get; set; }

    /// <summary>
    /// Number of failed executions among the entries.
    /// </summary>
    public int FailedExecutions { get; set; }

    /// <summary>
    /// Success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Average execution duration in milliseconds.
    /// </summary>
    public double AverageDurationMs { get; set; }

    /// <summary>
    /// Timestamp of the oldest entry, or <see langword="null"/> if there are none.
    /// </summary>
    public DateTime? OldestLogTime { get; set; }

    /// <summary>
    /// Timestamp of the newest entry, or <see langword="null"/> if there are none.
    /// </summary>
    public DateTime? NewestLogTime { get; set; }
    /// <summary>
    /// Returns a string representation of the LogSummary.
    /// </summary>
    public override string ToString()
    {
        return $"LogSummary {{ TotalEntries = {TotalEntries}, SuccessfulExecutions = {SuccessfulExecutions}, FailedExecutions = {FailedExecutions}, SuccessRate = {SuccessRate}, AverageDurationMs = {AverageDurationMs}, OldestLogTime = {OldestLogTime}, NewestLogTime = {NewestLogTime} }}";
    }
}
