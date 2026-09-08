#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Provides JSON serialization extensions for <see cref="AdaptiveTimeoutPolicy"/> instances.
/// </summary>
public static class AdaptiveTimeoutPolicyJsonExtensions
{
    /// <summary>
    /// Serializes an <see cref="AdaptiveTimeoutPolicy"/> instance to a JSON string.
    /// </summary>
    /// <param name="policy">The adaptive timeout policy to serialize.</param>
    /// <returns>A JSON string containing the policy configuration and counters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
    public static string ToJson(this AdaptiveTimeoutPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return JsonSerializer.Serialize(new
        {
            policy.Id,
            policy.Name,
            policy.IsEnabled,
            policy.InitialTimeout,
            policy.MinTimeout,
            policy.MaxTimeout,
            policy.CurrentTimeout,
            policy.TargetPercentile,
            policy.WindowSize,
            policy.MinSampleSize,
            policy.TotalExecutions,
            policy.SuccessfulExecutions,
            policy.FailedExecutions,
            policy.TotalAdjustments,
            policy.TimeoutCount
        });
    }
}
