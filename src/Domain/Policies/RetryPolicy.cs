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
    Exponential, // Multiplies BaseDelay by BackoffMultiplier
    ExponentialWithJitter, // AWS 'full jitter' algorithm
    DecorrelatedJitter // Decorrelated jitter algorithm: delay = min(maxDelay, random(baseDelay, previousDelay*3))
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
    /// Whether to apply jitter to exponential backoff strategies.
    /// Set to false for deterministic delays (useful in tests).
    /// </summary>
    public bool UseJitter { get; set; } = true;

    /// <summary>
    /// Random jitter factor applied to exponential backoff (0.0 = no jitter, 1.0 = full jitter).
    /// </summary>
    public double JitterFactor { get; set; } = 1.0;

/// <summary>
/// Whether to use decorrelated jitter backoff strategy instead of exponential backoff.
/// When true, uses decorrelated jitter algorithm: delay = min(maxDelay, random(baseDelay, previousDelay*3)).
/// </summary>
public bool UseDecorrelatedJitter { get; set; }


    private static readonly Random _random = new();
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

        TimeSpan delay;

        switch (Strategy)
        {
            case BackoffStrategy.Fixed:
                delay = InitialDelay;
                break;
            case BackoffStrategy.Linear:
                delay = TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * (attemptNumber + 1));
                break;
            case BackoffStrategy.Exponential:
                delay = TimeSpan.FromMilliseconds(InitialDelay.TotalMilliseconds * Math.Pow(BackoffMultiplier, attemptNumber));
                break;
            case BackoffStrategy.ExponentialWithJitter:
                // AWS 'full jitter' algorithm: sleep = random_between(0, min(cap, base * 2 ** attempt))
                double cap = MaxDelay.TotalMilliseconds;
                double baseDelayMs = InitialDelay.TotalMilliseconds;
                double exponentialBackoff = baseDelayMs * Math.Pow(2, attemptNumber);
                double maxJitteredDelay = Math.Min(cap, exponentialBackoff);

                // Apply JitterFactor: scale the random range
                // If JitterFactor is 0, no jitter (random range is 0 to 0).
                // If JitterFactor is 1 (full jitter), random range is 0 to maxJitteredDelay.
                delay = TimeSpan.FromMilliseconds(
                    UseJitter
                        ? _random.NextDouble() * maxJitteredDelay * JitterFactor
                        : maxJitteredDelay);
                break;
            default:
                delay = InitialDelay;
                break;
        }

        // Cap at max delay if not already handled by ExponentialWithJitter
        if (Strategy != BackoffStrategy.ExponentialWithJitter && delay > MaxDelay)
            delay = MaxDelay;

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
            { "BaseDelayMs", InitialDelay.TotalMilliseconds },
            { "Strategy", Strategy },
            { "TotalRetryAttempts", TotalRetryAttempts },
            { "BackoffMultiplier", BackoffMultiplier },
            { "JitterFactor", JitterFactor }
        };
        return baseSnapshot;
    }
}
