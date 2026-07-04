#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===========================================================================

using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Data;

/// <summary>
/// Execution record for tracking individual operation executions.
/// </summary>
public sealed class ExecutionRecord
{
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();
    public string PolicyName { get; set; } = string.Empty;
    public string PolicyId { get; set; } = string.Empty;
    public bool IsSuccess { get; set; }
    public long ExecutionTimeMs { get; set; }
    public int AttemptCount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ErrorType { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public Dictionary<string, object> Metadata { get; set; } = new();
}

/// <summary>
/// Repository for managing execution history and metrics.
/// </summary>
public sealed class ExecutionHistoryRepository
{
    private readonly List<ExecutionRecord> _history;
    private readonly object _lockObj = new object();
    private readonly int _maxRetentionMinutes;
    private DateTime _lastCleanup = DateTime.UtcNow;

    public ExecutionHistoryRepository(int maxRetentionMinutes = 60)
    {
        _history = new List<ExecutionRecord>();
        _maxRetentionMinutes = maxRetentionMinutes;
    }

    /// <summary>
    /// Records an execution event.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when record is null.</exception>
    /// <exception cref="ValidationException">Thrown when record data is invalid.</exception>
    public void Record(ExecutionRecord record)
    {
        if (record is null)
            throw new ArgumentNullException(nameof(record));

        if (string.IsNullOrWhiteSpace(record.PolicyName))
            throw new ValidationException("Policy name is required", new Dictionary<string, string> { { nameof(record.PolicyName), "Policy name cannot be null or whitespace" } });

        if (string.IsNullOrWhiteSpace(record.PolicyId))
            throw new ValidationException("Policy ID is required", new Dictionary<string, string> { { nameof(record.PolicyId), "Policy ID cannot be null or whitespace" } });

        if (record.ExecutionTimeMs < 0)
            throw new ValidationException("Execution time cannot be negative", new Dictionary<string, string> { { nameof(record.ExecutionTimeMs), "Execution time must be non-negative" } });

        lock (_lockObj)
        {
            _history.Add(record);
            _performCleanupIfNeeded();
        }
    }

    /// <summary>
    /// Gets all execution records.
    /// </summary>
    public List<ExecutionRecord> GetAll()
    {
        lock (_lockObj)
        {
            return _history.ToList();
        }
    }

    /// <summary>
    /// Gets execution records for a specific policy.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when policyId is null.</exception>
    public List<ExecutionRecord> GetByPolicyId(string policyId)
    {
        if (string.IsNullOrEmpty(policyId))
            throw new ArgumentException("Policy ID cannot be empty", nameof(policyId));

        lock (_lockObj)
        {
            return _history.Where(r => r.PolicyId == policyId).ToList();
        }
    }

    /// <summary>
    /// Gets execution records within a time range.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when time range is invalid.</exception>
    public List<ExecutionRecord> GetByTimeRange(DateTime startTime, DateTime endTime)
    {
        if (startTime > endTime)
            throw new ValidationException("Start time must be before end time", new Dictionary<string, string> {
                { nameof(startTime), "Start time must be before end time" }
            });

        lock (_lockObj)
        {
            return _history.Where(r => r.ExecutedAt >= startTime && r.ExecutedAt <= endTime).ToList();
        }
    }

    /// <summary>
    /// Gets failed execution records.
    /// </summary>
    public List<ExecutionRecord> GetFailedExecutions()
    {
        lock (_lockObj)
        {
            return _history.Where(r => !r.IsSuccess).ToList();
        }
    }

    /// <summary>
    /// Gets successful execution records.
    /// </summary>
    public List<ExecutionRecord> GetSuccessfulExecutions()
    {
        lock (_lockObj)
        {
            return _history.Where(r => r.IsSuccess).ToList();
        }
    }

    /// <summary>
    /// Gets the latest N execution records.
    /// </summary>
    /// <exception cref="ValidationException">Thrown when count is invalid.</exception>
    public List<ExecutionRecord> GetLatest(int count)
    {
        if (count <= 0)
            throw new ValidationException("Count must be greater than 0", new Dictionary<string, string> { { nameof(count), "Count must be positive" } });

        lock (_lockObj)
        {
            return _history.OrderByDescending(r => r.ExecutedAt).Take(count).ToList();
        }
    }

    /// <summary>
    /// Calculates average execution time across all records.
    /// </summary>
    public double GetAverageExecutionTime()
    {
        lock (_lockObj)
        {
            return _history.Count == 0 ? 0 : _history.Average(r => r.ExecutionTimeMs);
        }
    }

    /// <summary>
    /// Calculates success rate.
    /// </summary>
    public double GetSuccessRate()
    {
        lock (_lockObj)
        {
            if (_history.Count == 0) return 0;

            var successCount = _history.Count(r => r.IsSuccess);
            return (successCount * 100.0) / _history.Count;
        }
    }

    /// <summary>
    /// Gets total execution count.
    /// </summary>
    public int Count()
    {
        lock (_lockObj)
        {
            return _history.Count;
        }
    }

    /// <summary>
    /// Clears all execution records.
    /// </summary>
    public void Clear()
    {
        lock (_lockObj)
        {
            _history.Clear();
        }
    }

    /// <summary>
    /// Deletes execution records older than retention period.
    /// </summary>
    /// <returns>Number of records deleted.</returns>
    public int DeleteOldRecords()
    {
        lock (_lockObj)
        {
            var cutoffTime = DateTime.UtcNow.AddMinutes(-_maxRetentionMinutes);
            var initialCount = _history.Count;

            _history.RemoveAll(r => r.ExecutedAt < cutoffTime);

            return initialCount - _history.Count;
        }
    }

    /// <summary>
    /// Gets error statistics.
    /// </summary>
    public Dictionary<string, int> GetErrorStatistics()
    {
        lock (_lockObj)
        {
            return _history
                .Where(r => !r.IsSuccess && !string.IsNullOrEmpty(r.ErrorType))
                .GroupBy(r => r.ErrorType)
                .ToDictionary(g => g.Key!, g => g.Count());
        }
    }

    /// <summary>
    /// Gets execution records grouped by policy.
    /// </summary>
    public Dictionary<string, List<ExecutionRecord>> GetByPolicy()
    {
        lock (_lockObj)
        {
            return _history.GroupBy(r => r.PolicyName).ToDictionary(g => g.Key, g => g.ToList());
        }
    }

    private void _performCleanupIfNeeded()
    {
        // Cleanup every 5 minutes to avoid locking on every record
        if ((DateTime.UtcNow - _lastCleanup).TotalMinutes >= 5)
        {
            DeleteOldRecords();
            _lastCleanup = DateTime.UtcNow;
        }
    }
}