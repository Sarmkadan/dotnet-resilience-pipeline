#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Extension methods for filtering and working with collections of resilience policies.
/// </summary>
public static class ResiliencyPolicyEnumerableExtensions
{
    /// <summary>
    /// Filters the sequence to only include policies that are currently enabled.
    /// </summary>
    /// <param name="policies">The sequence of resilience policies to filter.</param>
    /// <returns>An IEnumerable containing only the enabled policies.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policies is null.</exception>
    public static IEnumerable<ResiliencyPolicy> WhereEnabled(this IEnumerable<ResiliencyPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);
        return policies.Where(policy => policy.IsEnabled);
    }
}