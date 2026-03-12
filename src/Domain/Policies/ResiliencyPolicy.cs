#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Base class for all resilience policies defining common properties and behavior.
/// </summary>
public abstract class ResiliencyPolicy
{
    /// <summary>
    /// Unique identifier for this policy instance.
    /// </summary>
    public string Id { get; } = Guid.NewGuid().ToString();

    /// <summary>
    /// Friendly name for the policy.
    /// </summary>
    public string Name { get; set; }

    /// <summary>
    /// Whether this policy is currently enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Policy creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; } = DateTime.UtcNow;

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    public DateTime ModifiedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Total executions tracked by this policy.
    /// </summary>
    public long TotalExecutions { get; protected set; }

    /// <summary>
    /// Total successful executions.
    /// </summary>
    public long SuccessfulExecutions { get; protected set; }

    /// <summary>
    /// Total failed executions.
    /// </summary>
    public long FailedExecutions { get; protected set; }

    /// <summary>
    /// Tags for categorizing or filtering policies.
    /// </summary>
    public List<string> Tags { get; set; } = new();

    /// <summary>
    /// Custom metadata associated with this policy.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    protected ResiliencyPolicy(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Policy name cannot be empty", nameof(name));

        Name = name;
    }

    /// <summary>
    /// Records a successful execution.
    /// </summary>
    public virtual void RecordSuccess()
    {
        TotalExecutions++;
        SuccessfulExecutions++;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed execution.
    /// </summary>
    public virtual void RecordFailure()
    {
        TotalExecutions++;
        FailedExecutions++;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Calculates the success rate as a percentage (0-100).
    /// </summary>
    public double GetSuccessRate()
    {
        if (TotalExecutions == 0)
            return 0;

        return (SuccessfulExecutions * 100.0) / TotalExecutions;
    }

    /// <summary>
    /// Resets all execution statistics.
    /// </summary>
    public virtual void ResetStatistics()
    {
        TotalExecutions = 0;
        SuccessfulExecutions = 0;
        FailedExecutions = 0;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets a snapshot of the current policy state.
    /// </summary>
    public virtual PolicySnapshot GetSnapshot()
    {
        return new PolicySnapshot
        {
            PolicyId = Id,
            PolicyName = Name,
            PolicyType = this.GetType().Name,
            IsEnabled = IsEnabled,
            TotalExecutions = TotalExecutions,
            SuccessfulExecutions = SuccessfulExecutions,
            FailedExecutions = FailedExecutions,
            SuccessRate = GetSuccessRate(),
            SnapshotTime = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Snapshot of a policy's current state for reporting and monitoring.
/// </summary>
public sealed class PolicySnapshot
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public string PolicyType { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public long TotalExecutions { get; set; }
    public long SuccessfulExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public double SuccessRate { get; set; }
    public DateTime SnapshotTime { get; set; }
}
