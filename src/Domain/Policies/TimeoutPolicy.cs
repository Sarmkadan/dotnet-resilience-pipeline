// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Timeout policy that enforces maximum execution time for operations.
/// </summary>
public class TimeoutPolicy : ResiliencyPolicy
{
    /// <summary>
    /// Maximum allowed execution duration.
    /// </summary>
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Total number of timeout events recorded.
    /// </summary>
    public long TimeoutCount { get; private set; }

    /// <summary>
    /// Average execution time of non-timed-out operations in milliseconds.
    /// </summary>
    public double AverageExecutionTimeMs { get; private set; }

    /// <summary>
    /// Longest execution time recorded (in milliseconds).
    /// </summary>
    public long LongestExecutionTimeMs { get; private set; }

    /// <summary>
    /// Shortest execution time recorded (in milliseconds).
    /// </summary>
    public long ShortestExecutionTimeMs { get; private set; } = long.MaxValue;

    private List<long> _executionTimes = new();

    public TimeoutPolicy(string name) : base(name)
    {
    }

    /// <summary>
    /// Checks if the given execution time exceeds the timeout.
    /// </summary>
    public bool IsTimedOut(TimeSpan executionTime)
    {
        return executionTime > Timeout;
    }

    /// <summary>
    /// Checks if the given execution time in milliseconds exceeds the timeout.
    /// </summary>
    public bool IsTimedOutMs(long executionTimeMs)
    {
        return executionTimeMs > Timeout.TotalMilliseconds;
    }

    /// <summary>
    /// Records an execution time, updating statistics.
    /// </summary>
    public void RecordExecutionTime(long executionTimeMs)
    {
        if (executionTimeMs < 0)
            throw new ArgumentException("Execution time cannot be negative", nameof(executionTimeMs));

        _executionTimes.Add(executionTimeMs);
        UpdateStatistics(executionTimeMs);
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a timeout event.
    /// </summary>
    public void RecordTimeout(long executionTimeMs)
    {
        TimeoutCount++;
        RecordFailure();
        RecordExecutionTime(executionTimeMs);
        Metadata["LastTimeoutAt"] = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the percentage of operations that timed out.
    /// </summary>
    public double GetTimeoutPercentage()
    {
        if (TotalExecutions == 0)
            return 0;

        return (TimeoutCount * 100.0) / TotalExecutions;
    }

    /// <summary>
    /// Gets the 95th percentile execution time in milliseconds.
    /// </summary>
    public long GetPercentile95ExecutionTime()
    {
        if (_executionTimes.Count == 0)
            return 0;

        var sorted = _executionTimes.OrderBy(t => t).ToList();
        int index = (int)Math.Ceiling(sorted.Count * 0.95) - 1;
        return sorted[Math.Max(0, index)];
    }

    /// <summary>
    /// Gets the 99th percentile execution time in milliseconds.
    /// </summary>
    public long GetPercentile99ExecutionTime()
    {
        if (_executionTimes.Count == 0)
            return 0;

        var sorted = _executionTimes.OrderBy(t => t).ToList();
        int index = (int)Math.Ceiling(sorted.Count * 0.99) - 1;
        return sorted[Math.Max(0, index)];
    }

    /// <summary>
    /// Validates timeout configuration.
    /// </summary>
    public bool IsValidConfiguration(out string? error)
    {
        if (Timeout <= TimeSpan.Zero)
        {
            error = "Timeout must be positive";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Resets all statistics.
    /// </summary>
    public override void ResetStatistics()
    {
        base.ResetStatistics();
        TimeoutCount = 0;
        _executionTimes.Clear();
        AverageExecutionTimeMs = 0;
        LongestExecutionTimeMs = 0;
        ShortestExecutionTimeMs = long.MaxValue;
    }

    private void UpdateStatistics(long executionTimeMs)
    {
        if (executionTimeMs > LongestExecutionTimeMs)
            LongestExecutionTimeMs = executionTimeMs;

        if (executionTimeMs < ShortestExecutionTimeMs)
            ShortestExecutionTimeMs = executionTimeMs;

        AverageExecutionTimeMs = _executionTimes.Average();
    }

    /// <summary>
    /// Gets detailed timeout policy snapshot.
    /// </summary>
    public override PolicySnapshot GetSnapshot()
    {
        var baseSnapshot = base.GetSnapshot();
        baseSnapshot.Metadata = new Dictionary<string, object>
        {
            { "TimeoutMs", Timeout.TotalMilliseconds },
            { "TimeoutCount", TimeoutCount },
            { "TimeoutPercentage", GetTimeoutPercentage() },
            { "AverageExecutionTimeMs", AverageExecutionTimeMs },
            { "P95ExecutionTimeMs", GetPercentile95ExecutionTime() },
            { "P99ExecutionTimeMs", GetPercentile99ExecutionTime() },
            { "LongestExecutionTimeMs", LongestExecutionTimeMs }
        };
        return baseSnapshot;
    }
}
