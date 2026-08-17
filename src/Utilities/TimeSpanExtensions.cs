#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Extension and helper methods for <see cref="TimeSpan"/> used across the resilience pipeline.
/// </summary>
public static class TimeSpanExtensions
{
    /// <summary>
    /// Applies random jitter to the duration.
    /// </summary>
    /// <param name="duration">The base duration to jitter.</param>
    /// <param name="factor">Jitter factor in the range [0.0, 1.0]. 0.0 returns the duration unchanged,
    /// 1.0 returns a random value between zero and the duration (full jitter).</param>
    /// <returns>A jittered duration.</returns>
    public static TimeSpan WithJitter(this TimeSpan duration, double factor)
    {
        if (duration <= TimeSpan.Zero)
            return TimeSpan.Zero;

        if (factor <= 0.0)
            return duration;

        var jitterFactor = factor > 1.0 ? 1.0 : factor;
        var jitteredMs = Random.Shared.NextDouble() * duration.TotalMilliseconds * jitterFactor;
        return TimeSpan.FromMilliseconds(jitteredMs);
    }

    /// <summary>
    /// Returns the smaller of this duration and <paramref name="other"/>.
    /// </summary>
    public static TimeSpan Min(this TimeSpan duration, TimeSpan other)
        => duration <= other ? duration : other;

    /// <summary>
    /// Returns the larger of this duration and <paramref name="other"/>.
    /// </summary>
    public static TimeSpan Max(this TimeSpan duration, TimeSpan other)
        => duration >= other ? duration : other;

    /// <summary>
    /// Formats the duration as a compact human-readable string, e.g. "1m 30s", "45s", "2h 5m".
    /// </summary>
    /// <param name="duration">The duration to format.</param>
    /// <returns>A human-readable representation of the duration.</returns>
    public static string ToHumanString(this TimeSpan duration)
    {
        if (duration < TimeSpan.Zero)
            return "-" + (-duration).ToHumanString();

        if (duration < TimeSpan.FromSeconds(1))
            return $"{Math.Max(0, (int)duration.TotalMilliseconds)}ms";

        var parts = new List<string>();

        if (duration.TotalDays >= 1)
            parts.Add($"{duration.Days}d");

        if (duration.Hours > 0)
            parts.Add($"{duration.Hours}h");

        if (duration.Minutes > 0)
            parts.Add($"{duration.Minutes}m");

        if (duration.Seconds > 0)
            parts.Add($"{duration.Seconds}s");

        return parts.Count > 0 ? string.Join(" ", parts) : "0s";
    }
}