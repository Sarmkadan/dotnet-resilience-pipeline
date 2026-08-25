#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Monitors performance metrics for policy executions.
/// Tracks execution times, throughput, and identifies performance degradation.
/// </summary>
public sealed class PerformanceMonitor
{
    private readonly Dictionary<string, PerformanceMetrics> _metrics = new();
    private readonly object _lockObj = new object();

    /// <summary>
    /// Records an execution with its duration.
    /// </summary>
    public void RecordExecution(string policyName, long durationMs, bool success)
    {
        // Ignore negative or zero measurements due to potential clock skew
        if (durationMs <= 0)
            return;

        lock (_lockObj)
        {
            if (!_metrics.ContainsKey(policyName))
                _metrics[policyName] = new PerformanceMetrics { PolicyName = policyName };

            var metrics = _metrics[policyName];
            metrics.TotalExecutions++;
            metrics.TotalDurationMs += durationMs;

            if (success)
                metrics.SuccessfulExecutions++;
            else
                metrics.FailedExecutions++;

            // Track percentiles
            metrics.AllDurations.Add(durationMs);
            if (metrics.AllDurations.Count > 1000)
                metrics.AllDurations.RemoveAt(0); // Keep bounded
        }
    }

    /// <summary>
    /// Gets performance metrics for a policy.
    /// </summary>
    public PerformanceMetrics GetMetrics(string policyName)
    {
        lock (_lockObj)
        {
            if (_metrics.TryGetValue(policyName, out var metrics))
                return new PerformanceMetrics
                {
                    PolicyName = metrics.PolicyName,
                    TotalExecutions = metrics.TotalExecutions,
                    TotalDurationMs = metrics.TotalDurationMs,
                    SuccessfulExecutions = metrics.SuccessfulExecutions,
                    FailedExecutions = metrics.FailedExecutions
                };

            return new PerformanceMetrics { PolicyName = policyName };
        }
    }

    /// <summary>
    /// Gets all performance metrics.
    /// </summary>
    public List<PerformanceMetrics> GetAllMetrics()
    {
        lock (_lockObj)
        {
            return _metrics.Values.ToList();
        }
    }

    /// <summary>
    /// Identifies slow-running policies.
    /// </summary>
    public List<PerformanceIssue> IdentifyPerformanceIssues(long slowThresholdMs = 1000)
    {
        lock (_lockObj)
        {
            var issues = new List<PerformanceIssue>();

            foreach (var metrics in _metrics.Values)
            {
                if (metrics.AverageDurationMs > slowThresholdMs)
                {
                    issues.Add(new PerformanceIssue
                    {
                        PolicyName = metrics.PolicyName,
                        IssueType = "SlowExecution",
                        AverageDurationMs = metrics.AverageDurationMs,
                        Severity = metrics.AverageDurationMs > slowThresholdMs * 5 ? "Critical" : "Warning"
                    });
                }

                if (metrics.FailureRate > 0.5) // More than 50% failure
                {
                    issues.Add(new PerformanceIssue
                    {
                        PolicyName = metrics.PolicyName,
                        IssueType = "HighFailureRate",
                        FailureRate = metrics.FailureRate,
                        Severity = "Warning"
                    });
                }
            }

            return issues;
        }
    }

    /// <summary>
    /// Clears all performance metrics.
    /// </summary>
    public void Clear()
    {
        lock (_lockObj)
        {
            _metrics.Clear();
        }
    }

    /// <summary>
    /// Gets comparative performance metrics between policies.
    /// </summary>
    public List<PerformanceComparison> ComparePerformance()
    {
        lock (_lockObj)
        {
            if (_metrics.Count == 0)
                return new();

            var slowest = _metrics.Values.OrderByDescending(m => m.AverageDurationMs).First();

            return _metrics.Values.Select(m => new PerformanceComparison
            {
                PolicyName = m.PolicyName,
                AverageDurationMs = m.AverageDurationMs,
                PercentageOfSlowest = slowest.AverageDurationMs > 0 ? (m.AverageDurationMs * 100.0) / slowest.AverageDurationMs : 0,
                SuccessRate = m.SuccessRate
            }).OrderByDescending(c => c.AverageDurationMs).ToList();

        }
    }

    /// <summary>
    /// Returns a concise, informative representation of the monitor,
    /// including each tracked policy and its key performance metrics.
    /// </summary>
    public override string ToString()
    {
        lock (_lockObj)
        {
            if (_metrics.Count == 0)
                return "PerformanceMonitor { PoliciesTracked = 0 }";

            var details = string.Join("; ", _metrics.Values.Select(m =>
                $"(PolicyName = {m.PolicyName}, TotalExecutions = {m.TotalExecutions}, TotalDurationMs = {m.TotalDurationMs}, SuccessfulExecutions = {m.SuccessfulExecutions}, FailedExecutions = {m.FailedExecutions}, AllDurations.Count = {m.AllDurations.Count})"));

            return $"PerformanceMonitor {{ PoliciesTracked = {_metrics.Count}, Metrics = [{details}] }}";
        }
    }
}

/// <summary>
/// Performance metrics for a policy.
/// </summary>
public sealed class PerformanceMetrics
{
    public string PolicyName { get; set; } = string.Empty;
    public long TotalExecutions { get; set; }
    public long TotalDurationMs { get; set; }
    public long SuccessfulExecutions { get; set; }
    public long FailedExecutions { get; set; }

    public double AverageDurationMs =>
        TotalExecutions > 0 ? (double)TotalDurationMs / TotalExecutions : 0;

    public double SuccessRate =>
        TotalExecutions > 0 ? (SuccessfulExecutions * 100.0) / TotalExecutions : 0;

    public double FailureRate =>
        TotalExecutions > 0 ? (FailedExecutions * 100.0) / TotalExecutions : 0;

    public double ThroughputPerSecond =>
        TotalDurationMs > 0 ? (TotalExecutions * 1000.0) / TotalDurationMs : 0;

    public List<long> AllDurations { get; set; } = new();

    public long P50 => CalculatePercentile(50);
    public long P90 => CalculatePercentile(90);
    public long P95 => CalculatePercentile(95);
    public long P99 => CalculatePercentile(99);

    private long CalculatePercentile(int percentile)
    {
        if (AllDurations.Count == 0)
            return 0;

        var sorted = AllDurations.OrderBy(x => x).ToList();
        int index = (int)((percentile / 100.0) * sorted.Count);
        return sorted[Math.Min(index, sorted.Count - 1)];
    }
}

/// <summary>
/// Performance issue identified by the monitor.
/// </summary>
public sealed class PerformanceIssue
{
    public string PolicyName { get; set; } = string.Empty;
    public string IssueType { get; set; } = string.Empty;
    public double AverageDurationMs { get; set; }
    public double FailureRate { get; set; }
    public string Severity { get; set; } = string.Empty;
}

/// <summary>
/// Comparative performance metrics between policies.
/// </summary>
public sealed class PerformanceComparison
{
    public string PolicyName { get; set; } = string.Empty;
    public double AverageDurationMs { get; set; }
    public double PercentageOfSlowest { get; set; }
    public double SuccessRate { get; set; }
}
