#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using System.Globalization;

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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policyName"/> is null.</exception>
    public void RecordExecution(string policyName, long durationMs, bool success)
    {
        ArgumentNullException.ThrowIfNull(policyName);
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policyName"/> is null.</exception>
    public PerformanceMetrics GetMetrics(string policyName)
    {
        ArgumentNullException.ThrowIfNull(policyName);
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
    /// <summary>
    /// Gets or sets the name of the policy these metrics describe.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the total number of executions recorded.
    /// </summary>
    public long TotalExecutions { get; set; }

    /// <summary>
    /// Gets or sets the sum of all execution durations, in milliseconds.
    /// </summary>
    public long TotalDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the number of successful executions.
    /// </summary>
    public long SuccessfulExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of failed executions.
    /// </summary>
    public long FailedExecutions { get; set; }

    /// <summary>
    /// Gets the average execution duration, in milliseconds.
    /// </summary>
    public double AverageDurationMs =>
        TotalExecutions > 0 ? (double)TotalDurationMs / TotalExecutions : 0;

    /// <summary>
    /// Gets the success rate, as a percentage.
    /// </summary>
    public double SuccessRate =>
        TotalExecutions > 0 ? (SuccessfulExecutions * 100.0) / TotalExecutions : 0;

    /// <summary>
    /// Gets the failure rate, as a percentage.
    /// </summary>
    public double FailureRate =>
        TotalExecutions > 0 ? (FailedExecutions * 100.0) / TotalExecutions : 0;

    /// <summary>
    /// Gets the throughput, in executions per second.
    /// </summary>
    public double ThroughputPerSecond =>
        TotalDurationMs > 0 ? (TotalExecutions * 1000.0) / TotalDurationMs : 0;

    /// <summary>
    /// Gets or sets every recorded execution duration, in milliseconds, used for percentile calculations.
    /// </summary>
    public List<long> AllDurations { get; set; } = new();

    /// <summary>
    /// Gets the 50th percentile (median) latency, in milliseconds, computed from <see cref="AllDurations"/>.
    /// </summary>
    public long P50 => CalculatePercentile(50);

    /// <summary>
    /// Gets the 90th percentile latency, in milliseconds, computed from <see cref="AllDurations"/>.
    /// </summary>
    public long P90 => CalculatePercentile(90);

    /// <summary>
    /// Gets the 95th percentile latency, in milliseconds, computed from <see cref="AllDurations"/>.
    /// </summary>
    public long P95 => CalculatePercentile(95);

    /// <summary>
    /// Gets the 99th percentile latency, in milliseconds, computed from <see cref="AllDurations"/>.
    /// </summary>
    public long P99 => CalculatePercentile(99);

    private long CalculatePercentile(int percentile)
    {
        if (AllDurations.Count == 0)
            return 0;

        var sorted = AllDurations.OrderBy(x => x).ToList();
        int index = (int)((percentile / 100.0) * sorted.Count);
        return sorted[Math.Min(index, sorted.Count - 1)];
    }

    /// <summary>
    /// Returns a summary of the metrics: policy name, execution counts, rates, latency and percentiles.
    /// </summary>
    /// <returns>A string in the format PerformanceMetrics { PolicyName = ..., TotalExecutions = ..., ... }.</returns>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"PerformanceMetrics {{ PolicyName = {PolicyName}, TotalExecutions = {TotalExecutions}, SuccessfulExecutions = {SuccessfulExecutions}, FailedExecutions = {FailedExecutions}, SuccessRate = {SuccessRate:F2}%, FailureRate = {FailureRate:F2}%, AverageDurationMs = {AverageDurationMs:F2}, TotalDurationMs = {TotalDurationMs}, ThroughputPerSecond = {ThroughputPerSecond:F2}, P50 = {P50}, P90 = {P90}, P95 = {P95}, P99 = {P99} }}");
}

/// <summary>
/// Performance issue identified by the monitor.
/// </summary>
public sealed class PerformanceIssue
{
    /// <summary>
    /// Gets or sets the name of the policy with the issue.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the category of the issue (for example, slow execution or high failure rate).
    /// </summary>
    public string IssueType { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the average execution duration of the policy, in milliseconds.
    /// </summary>
    public double AverageDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the failure rate of the policy, as a percentage.
    /// </summary>
    public double FailureRate { get; set; }

    /// <summary>
    /// Gets or sets the severity of the issue.
    /// </summary>
    public string Severity { get; set; } = string.Empty;

    /// <summary>
    /// Returns a summary of the issue: policy name, issue type, severity and the measured value.
    /// </summary>
    /// <returns>A string in the format PerformanceIssue { PolicyName = ..., IssueType = ..., ... }.</returns>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"PerformanceIssue {{ PolicyName = {PolicyName}, IssueType = {IssueType}, Severity = {Severity}, AverageDurationMs = {AverageDurationMs:F2}, FailureRate = {FailureRate:F2}% }}");
}

/// <summary>
/// Comparative performance metrics between policies.
/// </summary>
public sealed class PerformanceComparison
{
    /// <summary>
    /// Gets or sets the name of the policy being compared.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the average execution duration of the policy, in milliseconds.
    /// </summary>
    public double AverageDurationMs { get; set; }

    /// <summary>
    /// Gets or sets the policy's average duration as a percentage of the slowest policy's duration.
    /// </summary>
    public double PercentageOfSlowest { get; set; }

    /// <summary>
    /// Gets or sets the success rate of the policy, as a percentage.
    /// </summary>
    public double SuccessRate { get; set; }

    /// <summary>
    /// Returns a summary of the comparison: policy name, average duration, share of the slowest policy and success rate.
    /// </summary>
    /// <returns>A string in the format PerformanceComparison { PolicyName = ..., AverageDurationMs = ..., ... }.</returns>
    public override string ToString() =>
        string.Create(CultureInfo.InvariantCulture, $"PerformanceComparison {{ PolicyName = {PolicyName}, AverageDurationMs = {AverageDurationMs:F2}, PercentageOfSlowest = {PercentageOfSlowest:F2}%, SuccessRate = {SuccessRate:F2}% }}");
}
