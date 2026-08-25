#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Timeout policy that automatically adjusts its timeout ceiling based on observed
/// response-time percentiles within a sliding window of recent executions.
/// </summary>
public sealed class AdaptiveTimeoutPolicy : ResiliencyPolicy, ITimeoutStrategy
{
    private readonly Queue<long> _responseWindow = new();
    private readonly object _lock = new();

    /// <summary>
    /// Timeout applied before enough observations have accumulated in the window.
    /// </summary>
    public TimeSpan InitialTimeout { get; set; } = TimeSpan.FromSeconds(10);

    /// <summary>
    /// Minimum allowed timeout value; acts as a safety floor.
    /// </summary>
    public TimeSpan MinTimeout { get; set; } = TimeSpan.FromMilliseconds(200);

    /// <summary>
    /// Maximum allowed timeout value; prevents unbounded growth.
    /// </summary>
    public TimeSpan MaxTimeout { get; set; } = TimeSpan.FromSeconds(60);

    /// <summary>
    /// Effective timeout applied to the next execution, updated automatically by the adaptation algorithm.
    /// </summary>
    public TimeSpan CurrentTimeout { get; private set; }

    /// <summary>
    /// Response-time percentile used to derive the new timeout (e.g. 95.0 for P95).
    /// </summary>
    public double TargetPercentile { get; set; } = 95.0;

    /// <summary>
    /// Multiplier applied above the target percentile to add headroom (e.g. 1.2 for 20% headroom).
    /// </summary>
    public double HeadroomFactor { get; set; } = 1.2;

    /// <summary>
    /// Maximum number of recent observations retained in the sliding window.
    /// </summary>
    public int WindowSize { get; set; } = 100;

    /// <summary>
    /// Minimum number of window observations required before the timeout may adapt.
    /// </summary>
    public int MinSampleSize { get; set; } = 10;

    /// <summary>
    /// Minimum time between consecutive timeout adjustments.
    /// </summary>
    public TimeSpan AdjustmentInterval { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Total number of times the timeout value has been adjusted since creation or last reset.
    /// </summary>
    public int TotalAdjustments { get; private set; }

    /// <summary>
    /// Timestamp of the most recent timeout adjustment.
    /// </summary>
    public DateTime LastAdjustmentAt { get; private set; } = DateTime.MinValue;

    /// <summary>
    /// Total number of operations that exceeded the current timeout.
    /// </summary>
    public long TimeoutCount { get; private set; }

    /// <summary>
    /// Initializes a new adaptive timeout policy with the given name.
    /// </summary>
    public AdaptiveTimeoutPolicy(string name) : base(name)
    {
        CurrentTimeout = InitialTimeout;
    }

    /// <summary>
    /// Returns a concise, informative string representation of this policy.
    /// </summary>
    public override string ToString() =>
        $"AdaptiveTimeoutPolicy {{ InitialTimeout = {InitialTimeout}, MinTimeout = {MinTimeout}, MaxTimeout = {MaxTimeout}, TargetPercentile = {TargetPercentile}, HeadroomFactor = {HeadroomFactor}, WindowSize = {WindowSize} }}";

    /// <summary>
    /// Records an observed execution time and triggers timeout adaptation when the adjustment interval elapses.
    /// </summary>
    public void RecordExecutionTime(long executionTimeMs)
    {
        if (executionTimeMs < 0)
            throw new ArgumentException("Execution time cannot be negative", nameof(executionTimeMs));

        lock (_lock)
        {
            _responseWindow.Enqueue(executionTimeMs);

            if (_responseWindow.Count > WindowSize)
                _responseWindow.Dequeue();

            TryAdaptTimeout();
        }

        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a timeout event and includes the elapsed time in the adaptation window.
    /// </summary>
    public void RecordTimeout(long executionTimeMs)
    {
        TimeoutCount++;
        RecordFailure();
        RecordExecutionTime(executionTimeMs);
        Metadata["LastTimeoutAt"] = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns the percentage of operations that exceeded the timeout.
    /// </summary>
    public double GetTimeoutPercentage()
    {
        if (TotalExecutions == 0)
            return 0;

        return (TimeoutCount * 100.0) / TotalExecutions;
    }

    /// <summary>
    /// Calculates the given response-time percentile from the current observation window.
    /// </summary>
    /// <param name="percentile">Value between 0 and 100 (e.g. 95.0 for P95).</param>
    /// <returns>Percentile execution time in milliseconds, or 0 if the window is empty.</returns>
    public long GetPercentileExecutionTime(double percentile)
    {
        if (percentile < 0 || percentile > 100)
            throw new ArgumentOutOfRangeException(nameof(percentile), "Percentile must be between 0 and 100");

        lock (_lock)
        {
            if (_responseWindow.Count == 0)
                return 0;

            var sorted = _responseWindow.OrderBy(t => t).ToList();
            int index = (int)Math.Ceiling(sorted.Count * (percentile / 100.0)) - 1;
            return sorted[Math.Max(0, index)];
        }
    }

    /// <summary>
    /// Validates that the policy configuration is consistent and ready to be applied.
    /// </summary>
    /// <param name="error">Describes the first validation failure, or null on success.</param>
    public bool IsValidConfiguration(out string? error)
    {
        var errors = new List<string>();

        if (InitialTimeout <= TimeSpan.Zero)
            errors.Add("InitialTimeout must be positive");

        if (MinTimeout <= TimeSpan.Zero)
            errors.Add("MinTimeout must be positive");

        if (MaxTimeout <= TimeSpan.Zero)
            errors.Add("MaxTimeout must be positive");

        if (MinTimeout > MaxTimeout)
            errors.Add("MinTimeout cannot exceed MaxTimeout");

        if (InitialTimeout < MinTimeout || InitialTimeout > MaxTimeout)
            errors.Add("InitialTimeout must be within [MinTimeout, MaxTimeout]");

        if (TargetPercentile <= 0 || TargetPercentile > 100)
            errors.Add("TargetPercentile must be between 0 (exclusive) and 100 (inclusive)");

        if (HeadroomFactor < 1.0)
            errors.Add("HeadroomFactor must be >= 1.0");

        if (WindowSize < 1)
            errors.Add("WindowSize must be at least 1");

        if (MinSampleSize < 1)
            errors.Add("MinSampleSize must be at least 1");

        if (MinSampleSize > WindowSize)
            errors.Add("MinSampleSize cannot exceed WindowSize");

        if (AdjustmentInterval <= TimeSpan.Zero)
            errors.Add("AdjustmentInterval must be positive");

        if (errors.Count > 0)
        {
            error = string.Join("; ", errors);
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Gets the timeout value that should be applied to the next execution.
    /// </summary>
    TimeSpan ITimeoutStrategy.GetTimeout()
    {
        return CurrentTimeout;
    }

    /// <summary>
    /// Records an execution time for statistical tracking and potential adaptation.
    /// </summary>
    /// <param name="executionTimeMs">Execution time in milliseconds.</param>
    void ITimeoutStrategy.RecordExecutionTime(long executionTimeMs)
    {
        RecordExecutionTime(executionTimeMs);
    }

    /// <summary>
    /// Records a timeout event that occurred during execution.
    /// </summary>
    /// <param name="executionTimeMs">Execution time in milliseconds before timeout.</param>
    void ITimeoutStrategy.RecordTimeout(long executionTimeMs)
    {
        RecordTimeout(executionTimeMs);
    }

    /// <summary>
    /// Gets the percentage of operations that timed out.
    /// </summary>
    double ITimeoutStrategy.GetTimeoutPercentage()
    {
        return GetTimeoutPercentage();
    }

    /// <summary>
    /// Resets all execution statistics and reverts <see cref="CurrentTimeout"/> to <see cref="InitialTimeout"/>.
    /// </summary>
    public override void ResetStatistics()
    {
        base.ResetStatistics();

        lock (_lock)
        {
            _responseWindow.Clear();
            CurrentTimeout = InitialTimeout;
        }

        TimeoutCount = 0;
        TotalAdjustments = 0;
        LastAdjustmentAt = DateTime.MinValue;
    }

    /// <summary>
    /// Gets a detailed snapshot including adaptive-timeout-specific metrics.
    /// </summary>
    public override PolicySnapshot GetSnapshot()
    {
        var baseSnapshot = base.GetSnapshot();
        int windowCount;
        lock (_lock) { windowCount = _responseWindow.Count; }

        baseSnapshot.Metadata = new Dictionary<string, object>
        {
            { "CurrentTimeoutMs",   CurrentTimeout.TotalMilliseconds },
            { "InitialTimeoutMs",   InitialTimeout.TotalMilliseconds },
            { "MinTimeoutMs",       MinTimeout.TotalMilliseconds },
            { "MaxTimeoutMs",       MaxTimeout.TotalMilliseconds },
            { "TargetPercentile",   TargetPercentile },
            { "HeadroomFactor",     HeadroomFactor },
            { "TotalAdjustments",   TotalAdjustments },
            { "LastAdjustmentAt",   LastAdjustmentAt },
            { "WindowSampleCount",  windowCount },
            { "TimeoutCount",       TimeoutCount },
            { "TimeoutPercentage",  GetTimeoutPercentage() },
            { "P50ExecutionTimeMs", GetPercentileExecutionTime(50) },
            { "P95ExecutionTimeMs", GetPercentileExecutionTime(95) },
            { "P99ExecutionTimeMs", GetPercentileExecutionTime(99) }
        };
        return baseSnapshot;
    }

    // Called while holding _lock; must not re-acquire it.
    private void TryAdaptTimeout()
    {
        if (_responseWindow.Count < MinSampleSize)
            return;

        if (DateTime.UtcNow - LastAdjustmentAt < AdjustmentInterval)
            return;

        var sorted = _responseWindow.OrderBy(t => t).ToList();
        int index = (int)Math.Ceiling(sorted.Count * (TargetPercentile / 100.0)) - 1;
        long percentileMs = sorted[Math.Max(0, index)];

        var proposed = TimeSpan.FromMilliseconds(percentileMs * HeadroomFactor);
        var clamped = Clamp(proposed, MinTimeout, MaxTimeout);

        if (clamped == CurrentTimeout)
            return;

        CurrentTimeout = clamped;
        TotalAdjustments++;
        LastAdjustmentAt = DateTime.UtcNow;
        Metadata["LastAdaptedTimeoutMs"] = clamped.TotalMilliseconds;
    }

    private static TimeSpan Clamp(TimeSpan value, TimeSpan min, TimeSpan max)
    {
        if (value < min) return min;
        if (value > max) return max;
        return value;
    }
}
