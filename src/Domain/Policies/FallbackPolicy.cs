// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Fallback policy that provides alternative execution paths when primary operations fail.
/// </summary>
public class FallbackPolicy : ResiliencyPolicy
{
    /// <summary>
    /// Number of times fallback was invoked.
    /// </summary>
    public long FallbackInvocationCount { get; private set; }

    /// <summary>
    /// Number of successful fallback executions.
    /// </summary>
    public long SuccessfulFallbackCount { get; private set; }

    /// <summary>
    /// Number of failed fallback executions.
    /// </summary>
    public long FailedFallbackCount { get; private set; }

    /// <summary>
    /// Types of exceptions that trigger fallback.
    /// </summary>
    public List<Type> FallbackTriggerExceptions { get; set; } = new();

    /// <summary>
    /// Whether to fallback on any exception or only specified types.
    /// </summary>
    public bool FallbackOnAnyException { get; set; } = true;

    /// <summary>
    /// Timeout for fallback execution.
    /// </summary>
    public TimeSpan FallbackTimeout { get; set; } = TimeSpan.FromSeconds(5);

    /// <summary>
    /// Average fallback execution time in milliseconds.
    /// </summary>
    public double AverageFallbackExecutionTimeMs { get; private set; }

    private List<long> _fallbackExecutionTimes = new();
    private readonly object _lockObj = new object();

    public FallbackPolicy(string name) : base(name)
    {
    }

    /// <summary>
    /// Determines if an exception should trigger fallback execution.
    /// </summary>
    public bool ShouldTriggerFallback(Exception exception)
    {
        if (exception == null)
            return false;

        if (FallbackOnAnyException)
            return true;

        return FallbackTriggerExceptions.Any(type => type.IsInstanceOfType(exception));
    }

    /// <summary>
    /// Records a successful fallback execution.
    /// </summary>
    public void RecordSuccessfulFallback(long executionTimeMs)
    {
        if (executionTimeMs < 0)
            throw new ArgumentException("Execution time cannot be negative", nameof(executionTimeMs));

        lock (_lockObj)
        {
            FallbackInvocationCount++;
            SuccessfulFallbackCount++;
            _fallbackExecutionTimes.Add(executionTimeMs);
            UpdateFallbackStatistics();
        }

        RecordSuccess();
        Metadata["LastSuccessfulFallbackAt"] = DateTime.UtcNow;
    }

    /// <summary>
    /// Records a failed fallback execution.
    /// </summary>
    public void RecordFailedFallback(Exception fallbackException, long executionTimeMs)
    {
        if (executionTimeMs < 0)
            throw new ArgumentException("Execution time cannot be negative", nameof(executionTimeMs));

        lock (_lockObj)
        {
            FallbackInvocationCount++;
            FailedFallbackCount++;
            _fallbackExecutionTimes.Add(executionTimeMs);
            UpdateFallbackStatistics();
        }

        RecordFailure();
        Metadata["LastFailedFallbackAt"] = DateTime.UtcNow;
        Metadata["LastFallbackException"] = fallbackException.GetType().Name;
    }

    /// <summary>
    /// Gets the success rate of fallback executions.
    /// </summary>
    public double GetFallbackSuccessRate()
    {
        if (FallbackInvocationCount == 0)
            return 0;

        return (SuccessfulFallbackCount * 100.0) / FallbackInvocationCount;
    }

    /// <summary>
    /// Gets the percentage of times fallback was needed.
    /// </summary>
    public double GetFallbackInvocationPercentage()
    {
        if (TotalExecutions == 0)
            return 0;

        return (FallbackInvocationCount * 100.0) / TotalExecutions;
    }

    /// <summary>
    /// Adds an exception type that should trigger fallback.
    /// </summary>
    public void AddFallbackTrigger(Type exceptionType)
    {
        if (exceptionType == null)
            throw new ArgumentNullException(nameof(exceptionType));

        if (!typeof(Exception).IsAssignableFrom(exceptionType))
            throw new ArgumentException($"{exceptionType.Name} is not an Exception type", nameof(exceptionType));

        if (!FallbackTriggerExceptions.Contains(exceptionType))
        {
            FallbackTriggerExceptions.Add(exceptionType);
        }
    }

    /// <summary>
    /// Removes an exception type from fallback triggers.
    /// </summary>
    public void RemoveFallbackTrigger(Type exceptionType)
    {
        FallbackTriggerExceptions.Remove(exceptionType);
    }

    /// <summary>
    /// Validates fallback configuration.
    /// </summary>
    public bool IsValidConfiguration(out string? error)
    {
        if (FallbackTimeout <= TimeSpan.Zero)
        {
            error = "FallbackTimeout must be positive";
            return false;
        }

        if (!FallbackOnAnyException && FallbackTriggerExceptions.Count == 0)
        {
            error = "Must have fallback trigger exceptions when FallbackOnAnyException is false";
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
        lock (_lockObj)
        {
            base.ResetStatistics();
            FallbackInvocationCount = 0;
            SuccessfulFallbackCount = 0;
            FailedFallbackCount = 0;
            _fallbackExecutionTimes.Clear();
            AverageFallbackExecutionTimeMs = 0;
        }
    }

    private void UpdateFallbackStatistics()
    {
        if (_fallbackExecutionTimes.Count > 0)
            AverageFallbackExecutionTimeMs = _fallbackExecutionTimes.Average();
    }

    /// <summary>
    /// Gets detailed fallback policy snapshot.
    /// </summary>
    public override PolicySnapshot GetSnapshot()
    {
        var baseSnapshot = base.GetSnapshot();
        baseSnapshot.Metadata = new Dictionary<string, object>
        {
            { "FallbackInvocationCount", FallbackInvocationCount },
            { "SuccessfulFallbackCount", SuccessfulFallbackCount },
            { "FailedFallbackCount", FailedFallbackCount },
            { "FallbackSuccessRate", GetFallbackSuccessRate() },
            { "FallbackInvocationPercentage", GetFallbackInvocationPercentage() },
            { "AverageFallbackExecutionTimeMs", AverageFallbackExecutionTimeMs },
            { "FallbackTimeoutMs", FallbackTimeout.TotalMilliseconds }
        };
        return baseSnapshot;
    }
}
