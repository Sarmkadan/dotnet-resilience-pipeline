#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;

namespace DotNetResiliencePipeline.Domain.Policies
{
    /// <summary>
    /// Extension methods for <see cref="AdaptiveTimeoutPolicy"/>.
    /// </summary>
    public static class AdaptiveTimeoutPolicyExtensions
    {
        /// <summary>
        /// Determines whether the policy has collected enough samples in the sliding window.
        /// </summary>
        /// <param name="policy">The adaptive timeout policy.</param>
        /// <param name="min">The minimum number of samples required.</param>
        /// <returns>True if the policy has at least <paramref name="min"/> samples; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="min"/> is less than 1.</exception>
        public static bool HasEnoughSamples(this AdaptiveTimeoutPolicy policy, int min)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (min < 1)
                throw new ArgumentOutOfRangeException(nameof(min), "Minimum sample size must be at least 1.");

            var snapshot = policy.GetSnapshot();
            if (snapshot.Metadata.TryGetValue("WindowSampleCount", out var obj) && obj is int count)
                return count >= min;

            return false;
        }

        /// <summary>
        /// Gets the 99th percentile execution time from the sliding window.
        /// </summary>
        /// <param name="policy">The adaptive timeout policy.</param>
        /// <returns>The 99th percentile execution time in milliseconds, or 0 if the window is empty.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
        public static long GetP99(this AdaptiveTimeoutPolicy policy)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));

            return policy.GetPercentileExecutionTime(99.0);
        }

        /// <summary>
        /// Determines whether the timeout value is stable, meaning it has not been adjusted for a period of time.
        /// </summary>
        /// <param name="policy">The adaptive timeout policy.</param>
        /// <param name="tolerance">
        /// The tolerance multiplier for the adjustment interval. The timeout is considered stable if the time since the last adjustment
        /// is at least <paramref name="tolerance"/> times the adjustment interval.
        /// </param>
        /// <returns>True if the timeout is stable; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
        /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="tolerance"/> is negative.</exception>
        public static bool IsTimeoutStable(this AdaptiveTimeoutPolicy policy, double tolerance)
        {
            if (policy == null)
                throw new ArgumentNullException(nameof(policy));
            if (tolerance < 0)
                throw new ArgumentOutOfRangeException(nameof(tolerance), "Tolerance must be non-negative.");

            // If the policy has never been adjusted, consider it stable.
            if (policy.LastAdjustmentAt == DateTime.MinValue)
                return true;

            var timeSinceLastAdjustment = DateTime.UtcNow - policy.LastAdjustmentAt;
            var stabilityInterval = TimeSpan.FromTicks((long)(policy.AdjustmentInterval.Ticks * tolerance));
            return timeSinceLastAdjustment >= stabilityInterval;
        }
    }
}