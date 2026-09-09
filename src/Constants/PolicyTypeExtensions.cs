#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Constants;

/// <summary>
/// Provides extension methods for <see cref="PolicyType"/> values.
/// </summary>
public static class PolicyTypeExtensions
{
    /// <summary>
    /// Converts a policy type to its human-readable display name.
    /// </summary>
    /// <param name="policyType">The policy type to convert.</param>
    /// <returns>The human-readable display name for the policy type.</returns>
    public static string ToDisplayName(this PolicyType policyType)
    {
        return policyType switch
        {
            PolicyType.CircuitBreaker => "Circuit Breaker",
            PolicyType.Bulkhead => "Bulkhead",
            PolicyType.Retry => "Retry",
            PolicyType.Timeout => "Timeout",
            PolicyType.Fallback => "Fallback",
            _ => policyType.ToString()
        };
    }
}
