using System;
using System.Collections.Generic;

namespace DotNetResiliencePipeline.Domain.Policies
{
    /// <summary>
    /// Provides extension methods for <see cref="FallbackPolicy"/> to enhance fallback policy configuration and monitoring.
    /// </summary>
    public static class FallbackPolicyExtensions
    {
        /// <summary>
        /// Adds multiple exception types to the list of triggers for the fallback policy.
        /// </summary>
        /// <param name="policy">The fallback policy to configure.</param>
        /// <param name="exceptionTypes">The collection of exception types to add.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> or <paramref name="exceptionTypes"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="exceptionTypes"/> is empty.</exception>
        public static void AddFallbackTriggers(this FallbackPolicy policy, IEnumerable<Type> exceptionTypes)
        {
            ArgumentNullException.ThrowIfNull(policy);
            ArgumentNullException.ThrowIfNull(exceptionTypes);

            if (!exceptionTypes.Any())
            {
                throw new ArgumentException("Exception types collection cannot be empty.", nameof(exceptionTypes));
            }

            foreach (var exceptionType in exceptionTypes)
            {
                policy.AddFallbackTrigger(exceptionType);
            }
        }

        /// <summary>
        /// Determines if the fallback policy is operating within a healthy success rate threshold.
        /// </summary>
        /// <param name="policy">The fallback policy to check.</param>
        /// <param name="minSuccessRate">The minimum acceptable success rate (0.0 to 1.0). Default is 0.8.</param>
        /// <returns>True if the policy is healthy or has not been invoked yet; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
        public static bool IsFallbackHealthy(this FallbackPolicy policy, double minSuccessRate = 0.8)
        {
            ArgumentNullException.ThrowIfNull(policy);

            // If the fallback hasn't been invoked, we consider it healthy by default.
            if (policy.FallbackInvocationCount == 0)
            {
                return true;
            }

            return policy.GetFallbackSuccessRate() >= minSuccessRate;
        }

        /// <summary>
        /// Generates a human-readable summary of the fallback policy's execution statistics.
        /// </summary>
        /// <param name="policy">The fallback policy to summarize.</param>
        /// <returns>A string containing the key statistics.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
        public static string GetExecutionSummary(this FallbackPolicy policy)
        {
            ArgumentNullException.ThrowIfNull(policy);

            return $"Fallback Statistics: " +
                   $"Invocations: {policy.FallbackInvocationCount}, " +
                   $"Successes: {policy.SuccessfulFallbackCount}, " +
                   $"Failures: {policy.FailedFallbackCount}, " +
                   $"Success Rate: {policy.GetFallbackSuccessRate():P2}, " +
                   $"Avg Execution Time: {policy.AverageFallbackExecutionTimeMs:F2}ms";
        }
    }
}
