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
public class ResiliencyLoggingMiddleware
{
    private readonly ResiliencyPipelineService _pipelineService;
    private List<LogEntry> _logs = new();
    private readonly object _lockObj = new object();
    public int MaxLogEntries { get; set; } = 1000;

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
public class LogEntry
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public bool Success { get; set; }
    public long DurationMs { get; set; }
    public string? Exception { get; set; }
    public string? Message { get; set; }

    public override string ToString()
    {
        var status = Success ? "✓" : "✗";
        return $"[{Timestamp:O}] {status} {PolicyName}.{OperationName} ({DurationMs}ms)";
    }
}

/// <summary>
/// Summary statistics for logging.
/// </summary>
public class LogSummary
{
    public int TotalEntries { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public double AverageDurationMs { get; set; }
    public DateTime? OldestLogTime { get; set; }
    public DateTime? NewestLogTime { get; set; }
}
