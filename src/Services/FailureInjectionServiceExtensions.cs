#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetResiliencePipeline.Services
{
    /// <summary>
    /// Extension methods for <see cref="FailureInjectionService"/> and <see cref="InjectionRule"/>.
    /// </summary>
    public static class FailureInjectionServiceExtensions
    {
        /// <summary>
        /// Disables all injection rules in the specified service.
        /// </summary>
        /// <param name="service">The failure injection service.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> is null.</exception>
        public static void DisableAll(this FailureInjectionService service)
        {
            if (service is null)
                throw new ArgumentNullException(nameof(service));

            service.DisableAll();
        }

        /// <summary>
        /// Enables only the injection rule with the specified key and disables all others.
        /// </summary>
        /// <param name="service">The failure injection service.</param>
        /// <param name="key">The key of the rule to enable.</param>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown if <paramref name="key"/> is null or empty.</exception>
        public static void EnableOnly(this FailureInjectionService service, string key)
        {
            if (service is null)
                throw new ArgumentNullException(nameof(service));

            if (string.IsNullOrWhiteSpace(key))
                throw new ArgumentException("Rule key cannot be null or empty.", nameof(key));

            service.DisableAll();

            var rule = service.GetRules()
                              .FirstOrDefault(r => string.Equals(r.Key, key, StringComparison.OrdinalIgnoreCase));

            if (rule != null)
            {
                rule.IsEnabled = true;
            }
        }

        /// <summary>
        /// Gets the keys of all active injection rules in the specified service.
        /// A rule is considered active if it is enabled and within its time window (if configured).
        /// </summary>
        /// <param name="service">The failure injection service.</param>
        /// <returns>A collection of active rule keys.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> is null.</exception>
        public static IEnumerable<string> GetActiveRuleKeys(this FailureInjectionService service)
        {
            if (service is null)
                throw new ArgumentNullException(nameof(service));

            return service.GetRules()
                          .Where(r => r.IsEnabled && FailureInjectionService.IsActiveAt(r, DateTimeOffset.Now))
                          .Select(r => r.Key);
        }

        /// <summary>
        /// Determines whether the specified injection rule is active at the current time.
        /// </summary>
        /// <param name="rule">The injection rule to check.</param>
        /// <returns>True if the rule is active now; otherwise, false.</returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="rule"/> is null.</exception>
        public static bool IsActiveNow(this InjectionRule rule)
        {
            if (rule is null)
                throw new ArgumentNullException(nameof(rule));

            return FailureInjectionService.IsActiveAt(rule, DateTimeOffset.Now);
        }
    }
}