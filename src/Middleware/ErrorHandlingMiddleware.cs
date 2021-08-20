// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Middleware;

/// <summary>
/// Middleware for centralized error handling and error recovery strategies.
/// Implements error classification, tracking, and recovery recommendations.
/// </summary>
public class ErrorHandlingMiddleware
{
    private readonly ConcurrentDictionary<string, ErrorStatistics> _errorStats = new();
    private List<ErrorContext> _errorContexts = new();
    private readonly object _lockObj = new object();
    public int MaxContexts { get; set; } = 500;

    /// <summary>
    /// Handles an exception with classification and logging.
    /// </summary>
    public ErrorContext HandleException(Exception ex, string policyName, string operationName)
    {
        var context = new ErrorContext
        {
            Id = Guid.NewGuid().ToString(),
            Timestamp = DateTime.UtcNow,
            ExceptionType = ex.GetType().Name,
            ExceptionMessage = ex.Message,
            PolicyName = policyName,
            OperationName = operationName,
            IsRecoverable = IsRecoverable(ex),
            RecoveryRecommendation = GetRecoveryRecommendation(ex, policyName)
        };

        // Update statistics
        var key = $"{policyName}:{ex.GetType().Name}";
        _errorStats.AddOrUpdate(key,
            new ErrorStatistics { Count = 1, LastOccurrence = DateTime.UtcNow },
            (k, v) => { v.Count++; v.LastOccurrence = DateTime.UtcNow; return v; });

        // Store context
        lock (_lockObj)
        {
            _errorContexts.Add(context);
            if (_errorContexts.Count > MaxContexts)
                _errorContexts = _errorContexts.Skip(_errorContexts.Count - MaxContexts).ToList();
        }

        return context;
    }

    /// <summary>
    /// Determines if an exception is recoverable.
    /// </summary>
    private static bool IsRecoverable(Exception ex)
    {
        return ex switch
        {
            TimeoutException => true,
            InvalidOperationException => false,
            CircuitBreakerOpenException => true,
            BulkheadRejectedException => true,
            OperationTimeoutException => true,
            _ => true // Default to recoverable
        };
    }

    /// <summary>
    /// Generates recovery recommendation based on exception type and policy.
    /// </summary>
    private static string GetRecoveryRecommendation(Exception ex, string policyName)
    {
        return ex switch
        {
            CircuitBreakerOpenException =>
                "Circuit is open. Wait for it to transition to half-open state or reduce failure rate.",

            OperationTimeoutException =>
                "Operation timed out. Consider increasing timeout duration or optimizing the operation.",

            BulkheadRejectedException =>
                "Bulkhead capacity exceeded. Increase max parallelization or reduce concurrent load.",

            MaxRetriesExceededException =>
                "All retry attempts exhausted. Check service health and consider backoff strategy.",

            TimeoutException =>
                "Timeout occurred. Check network connectivity and service availability.",

            _ => "Check logs for details and verify policy configuration."
        };
    }

    /// <summary>
    /// Gets all recorded error contexts.
    /// </summary>
    public List<ErrorContext> GetErrorContexts() => new(_errorContexts);

    /// <summary>
    /// Gets error statistics for all tracked errors.
    /// </summary>
    public Dictionary<string, ErrorStatistics> GetErrorStatistics() =>
        new(_errorStats);

    /// <summary>
    /// Gets errors for a specific policy.
    /// </summary>
    public List<ErrorContext> GetErrorsForPolicy(string policyName)
    {
        lock (_lockObj)
        {
            return _errorContexts.Where(c => c.PolicyName == policyName).ToList();
        }
    }

    /// <summary>
    /// Gets most common errors.
    /// </summary>
    public List<(string Error, int Count)> GetMostCommonErrors(int top = 10)
    {
        return _errorStats
            .OrderByDescending(x => x.Value.Count)
            .Take(top)
            .Select(x => (x.Key, x.Value.Count))
            .ToList();
    }

    /// <summary>
    /// Clears all error tracking data.
    /// </summary>
    public void Clear()
    {
        lock (_lockObj)
        {
            _errorContexts.Clear();
            _errorStats.Clear();
        }
    }
}

/// <summary>
/// Context information for a handled error.
/// </summary>
public class ErrorContext
{
    public string Id { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string OperationName { get; set; } = string.Empty;
    public bool IsRecoverable { get; set; }
    public string RecoveryRecommendation { get; set; } = string.Empty;

    public override string ToString()
    {
        return $"[{Timestamp:O}] {ExceptionType}: {ExceptionMessage} (Policy: {PolicyName})";
    }
}

/// <summary>
/// Statistics for a specific error type.
/// </summary>
public class ErrorStatistics
{
    public int Count { get; set; }
    public DateTime LastOccurrence { get; set; }
    public double Frequency => Count > 0 ? 100.0 / Count : 0;
}
