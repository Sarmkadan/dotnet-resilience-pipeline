#nullable enable

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Defines a strategy for determining timeout values for operations.
/// </summary>
public interface ITimeoutStrategy
{
    /// <summary>
    /// Unique identifier for this strategy instance.
    /// </summary>
    string Id { get; set; }

    /// <summary>
    /// Friendly name for the strategy.
    /// </summary>
    string Name { get; set; }

    /// <summary>
    /// Whether this strategy is currently enabled.
    /// </summary>
    bool IsEnabled { get; set; }

    /// <summary>
    /// Policy creation timestamp.
    /// </summary>
    DateTime CreatedAt { get; }

    /// <summary>
    /// Last modification timestamp.
    /// </summary>
    DateTime ModifiedAt { get; set; }

    /// <summary>
    /// Total executions tracked by this strategy.
    /// </summary>
    long TotalExecutions { get; }

    /// <summary>
    /// Total successful executions.
    /// </summary>
    long SuccessfulExecutions { get; }

    /// <summary>
    /// Total failed executions.
    /// </summary>
    long FailedExecutions { get; }

    /// <summary>
    /// Gets the timeout value that should be applied to the next execution.
    /// </summary>
    TimeSpan GetTimeout();

    /// <summary>
    /// Records an execution time for statistical tracking and potential adaptation.
    /// </summary>
    /// <param name="executionTimeMs">Execution time in milliseconds.</param>
    void RecordExecutionTime(long executionTimeMs);

    /// <summary>
    /// Records a successful execution.
    /// </summary>
    void RecordSuccess();

    /// <summary>
    /// Records a failed execution.
    /// </summary>
    void RecordFailure();

    /// <summary>
    /// Records a timeout event that occurred during execution.
    /// </summary>
    /// <param name="executionTimeMs">Execution time in milliseconds before timeout.</param>
    void RecordTimeout(long executionTimeMs);

    /// <summary>
    /// Gets the percentage of operations that timed out.
    /// </summary>
    double GetTimeoutPercentage();

    /// <summary>
    /// Calculates the success rate as a percentage (0-100).
    /// </summary>
    double GetSuccessRate();

    /// <summary>
    /// Validates the timeout strategy configuration.
    /// </summary>
    /// <param name="error">Error message if validation fails, otherwise null.</param>
    /// <returns>True if valid, false otherwise.</returns>
    bool IsValidConfiguration(out string? error);

    /// <summary>
    /// Resets all execution statistics.
    /// </summary>
    void ResetStatistics();
}
