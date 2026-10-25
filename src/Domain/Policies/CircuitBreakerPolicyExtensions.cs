#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using CircuitState = DotNetResiliencePipeline.Domain.Policies.CircuitBreakerPolicy.CircuitState;

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Extension methods for <see cref="CircuitBreakerPolicy"/> providing state queries
/// and fluent configuration setters.
/// </summary>
/// <remarks>
/// All extension methods validate their parameters using <see cref="ArgumentNullException.ThrowIfNull(object, string)"/>
/// and the setters follow the fluent interface pattern by returning the policy instance for method chaining.
/// State queries read the current state at the time of the call and do not change it.
/// </remarks>
public static class CircuitBreakerPolicyExtensions
{
    /// <summary>
    /// Determines whether the circuit is currently closed (normal operation).
    /// </summary>
    /// <param name="policy">The circuit breaker policy instance.</param>
    /// <returns><see langword="true"/> if the circuit is closed; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    public static bool IsClosed(this CircuitBreakerPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return policy.CurrentState == CircuitState.Closed;
    }

    /// <summary>
    /// Determines whether the circuit is currently open (requests are rejected).
    /// </summary>
    /// <param name="policy">The circuit breaker policy instance.</param>
    /// <returns><see langword="true"/> if the circuit is open; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    public static bool IsOpen(this CircuitBreakerPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return policy.CurrentState == CircuitState.Open;
    }

    /// <summary>
    /// Determines whether the circuit is currently half-open (recovery testing).
    /// </summary>
    /// <param name="policy">The circuit breaker policy instance.</param>
    /// <returns><see langword="true"/> if the circuit is half-open; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    public static bool IsHalfOpen(this CircuitBreakerPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return policy.CurrentState == CircuitState.HalfOpen;
    }

    /// <summary>
    /// Gets the time remaining until the open circuit transitions to half-open.
    /// </summary>
    /// <param name="policy">The circuit breaker policy instance.</param>
    /// <returns>
    /// The remaining time while the circuit is open, clamped to <see cref="TimeSpan.Zero"/> once the open duration has elapsed.
    /// <see cref="TimeSpan.Zero"/> when the circuit is closed or half-open, since no wait is required.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    public static TimeSpan GetTimeUntilHalfOpen(this CircuitBreakerPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return policy.TimeUntilHalfOpen ?? TimeSpan.Zero;
    }

    /// <summary>
    /// Sets the number of consecutive failures required to open the circuit.
    /// </summary>
    /// <param name="policy">The circuit breaker policy instance.</param>
    /// <param name="threshold">The consecutive failure count; must be at least 1.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="threshold"/> is less than 1.</exception>
    public static CircuitBreakerPolicy WithThreshold(this CircuitBreakerPolicy policy, int threshold)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentOutOfRangeException.ThrowIfLessThan(threshold, 1);

        policy.FailureThreshold = threshold;
        policy.ModifiedAt = DateTime.UtcNow;
        return policy;
    }

    /// <summary>
    /// Sets how long the circuit remains open before transitioning to half-open.
    /// </summary>
    /// <param name="policy">The circuit breaker policy instance.</param>
    /// <param name="duration">The open duration; must not be negative.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="duration"/> is negative.</exception>
    public static CircuitBreakerPolicy WithOpenDuration(this CircuitBreakerPolicy policy, TimeSpan duration)
    {
        ArgumentNullException.ThrowIfNull(policy);
        if (duration < TimeSpan.Zero)
        {
            throw new ArgumentOutOfRangeException(nameof(duration), duration, "Open duration must not be negative.");
        }

        policy.OpenDuration = duration;
        policy.ModifiedAt = DateTime.UtcNow;
        return policy;
    }
}
