#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Extension methods for <see cref="TimeoutPolicy"/> providing fluent configuration
/// and timeout budget helpers.
/// </summary>
/// <remarks>
/// All extension methods validate their parameters using <see cref="ArgumentNullException.ThrowIfNull(object, string)"/>
/// and <see cref="ArgumentOutOfRangeException"/>. Setters follow the fluent interface pattern by returning
/// the policy instance for method chaining.
/// </remarks>
public static class TimeoutPolicyExtensions
{
    /// <summary>
    /// Sets the maximum allowed execution duration of the policy.
    /// </summary>
    /// <param name="policy">The timeout policy instance.</param>
    /// <param name="timeout">The new timeout. Must be greater than <see cref="TimeSpan.Zero"/>.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="timeout"/> is not positive.</exception>
    public static TimeoutPolicy WithTimeout(this TimeoutPolicy policy, TimeSpan timeout)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (timeout <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(timeout), timeout, "Timeout must be a positive time span.");
        }

        policy.Timeout = timeout;
        policy.ModifiedAt = DateTime.UtcNow;
        return policy;
    }

    /// <summary>
    /// Sets the maximum allowed execution duration of the policy, expressed in milliseconds.
    /// </summary>
    /// <param name="policy">The timeout policy instance.</param>
    /// <param name="milliseconds">The new timeout in milliseconds. Must be greater than zero.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="milliseconds"/> is not positive.</exception>
    public static TimeoutPolicy WithTimeoutMilliseconds(this TimeoutPolicy policy, int milliseconds)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (milliseconds <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(milliseconds), milliseconds, "Timeout in milliseconds must be greater than zero.");
        }

        return policy.WithTimeout(TimeSpan.FromMilliseconds(milliseconds));
    }

    /// <summary>
    /// Determines whether the policy's timeout is at or below the given threshold,
    /// meaning operations are cut off early relative to that threshold.
    /// </summary>
    /// <param name="policy">The timeout policy instance.</param>
    /// <param name="threshold">The duration to compare the timeout against. Must be greater than <see cref="TimeSpan.Zero"/>.</param>
    /// <returns><see langword="true"/> if <see cref="TimeoutPolicy.Timeout"/> is less than or equal to <paramref name="threshold"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="threshold"/> is not positive.</exception>
    public static bool IsAggressive(this TimeoutPolicy policy, TimeSpan threshold)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (threshold <= TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(threshold), threshold, "Threshold must be a positive time span.");
        }

        return policy.Timeout <= threshold;
    }

    /// <summary>
    /// Calculates how much of the policy's timeout budget remains for an operation started at the given time.
    /// </summary>
    /// <param name="policy">The timeout policy instance.</param>
    /// <param name="startedAt">The UTC time at which the operation started.</param>
    /// <returns>
    /// The remaining time before the timeout elapses, or <see cref="TimeSpan.Zero"/> if the budget is exhausted.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="startedAt"/> is in the future relative to UTC now.</exception>
    /// <remarks>
    /// <paramref name="startedAt"/> must be expressed in UTC, consistent with the timestamps used elsewhere on
    /// <see cref="ResiliencyPolicy"/>.
    /// </remarks>
    public static TimeSpan GetTimeoutBudgetRemaining(this TimeoutPolicy policy, DateTime startedAt)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var now = DateTime.UtcNow;
        if (startedAt > now)
        {
            throw new ArgumentException("Start time cannot be in the future.", nameof(startedAt));
        }

        var remaining = policy.Timeout - (now - startedAt);
        return remaining > TimeSpan.Zero ? remaining : TimeSpan.Zero;
    }
}
