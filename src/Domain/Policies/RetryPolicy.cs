#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Retry policy with exponential backoff and jitter for transient failure handling.
/// </summary>
public sealed class RetryPolicy : ResiliencyPolicy
{
    public enum BackoffStrategy
    {
        Fixed,
        Linear,
        Exponential
    }

    /// <summary>
    /// Maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; } = 3;

    /// <summary>
    /// Initial delay between retries.
    /// </summary>
    public TimeSpan InitialDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    /// <summary>
    /// Backoff strategy for calculating delays.
    /// </summary>
    public BackoffStrategy Strategy { get; set; } = BackoffStrategy.Exponential;

    /// <summary>
    /// Maximum delay between retries.
    /// </summary>
    public TimeSpan MaxDelay { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// Multiplier for exponential/linear backoff.
    /// </summary>
    public double BackoffMultiplier { get; set; } = 2.0;

    /// <summary>
    /// Whether to add random jitter to delays.
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Types of exceptions that trigger a retry.
    /// </summary>
    public List<Type> RetryableExceptions { get; set; } = new();

    /// <summary>
    /// Total number of retry attempts executed.
    /// </summary>
    public long TotalRetryAttempts { get; private set; }

    public RetryPolicy(string name) : base(name)
    {
        RetryableExceptions = new List<Type> { typeof(TimeoutException), typeof(HttpRequestException) };
    }

    /// <summary>
    /// Determines if an exception should trigger a retry.
    /// </summary>
    public bool IsRetryable(Exception exception)
    {
        if (exception is null)
            return false;

        if (RetryableExceptions.Count == 0)
            return true;

        return RetryableExceptions.Any(type => type.IsInstanceOfType(exception));
    }

    /// <summary>
    /// Calculates the delay before the next retry attempt.
    /// </summary>
    public TimeSpan CalculateDelay(int attemptNumber)
    {
        if (attemptNumber < 0 || attemptNumber >= MaxRetries)
            throw new ArgumentOutOfRangeException(nameof(attemptNumber));

        TimeSpan delay = Strategy switch
        {
            BackoffStrategy.Fixed => InitialDelay,
            BackoffStrategy.Linear => TimeSpan.FromMilliseconds(
                InitialDelay.TotalMilliseconds * (attemptNumber + 1)),
            BackoffStrategy.Exponential => TimeSpan.FromMilliseconds(
                InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attemptNumber)),
            _ => InitialDelay
        };

        // Cap at max delay
        if (delay > MaxDelay)
            delay = MaxDelay;

        // Add jitter
        if (UseJitter)
        {
            var random = new Random();
            var jitterFactor = random.NextDouble() * 0.1; // 0-10% jitter
            var jitterMs = delay.TotalMilliseconds * jitterFactor;
            delay = TimeSpan.FromMilliseconds(delay.TotalMilliseconds + jitterMs);
        }

        return delay;
    }

    /// <summary>
    /// Records a retry attempt.
    /// </summary>
    public void RecordRetryAttempt()
    {
        TotalRetryAttempts++;
        ModifiedAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Gets the next delay in milliseconds for the specified attempt.
    /// </summary>
    public long GetNextDelayMs(int attemptNumber)
    {
        var delay = CalculateDelay(attemptNumber);
        return (long)delay.TotalMilliseconds;
    }

    /// <summary>
    /// Validates retry configuration.
    /// </summary>
    public bool IsValidConfiguration(out string? error)
    {
        if (MaxRetries <= 0)
        {
            error = "MaxRetries must be greater than 0";
            return false;
        }

        if (InitialDelay <= TimeSpan.Zero)
        {
            error = "InitialDelay must be positive";
            return false;
        }

        if (MaxDelay < InitialDelay)
        {
            error = "MaxDelay must be greater than or equal to InitialDelay";
            return false;
        }

        if (BackoffMultiplier <= 0)
        {
            error = "BackoffMultiplier must be positive";
            return false;
        }

        error = null;
        return true;
    }

    /// <summary>
    /// Gets detailed retry policy snapshot.
    /// </summary>
    public override PolicySnapshot GetSnapshot()
    {
        var baseSnapshot = base.GetSnapshot();
        baseSnapshot.Metadata = new Dictionary<string, object>
        {
            { "MaxRetries", MaxRetries },
            { "InitialDelay", InitialDelay.TotalMilliseconds },
            { "Strategy", Strategy },
            { "TotalRetryAttempts", TotalRetryAttempts },
            { "BackoffMultiplier", BackoffMultiplier }
        };
        return baseSnapshot;
    }
}
