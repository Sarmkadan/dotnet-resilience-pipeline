#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Provides JSON serialization extensions for <see cref="TimeoutPolicy"/> instances.
/// </summary>
public static class TimeoutPolicyJsonExtensions
{
    /// <summary>
    /// Serializes a timeout policy to a JSON string using an anonymous projection.
    /// </summary>
    /// <param name="policy">The timeout policy to serialize.</param>
    /// <returns>A JSON string containing the policy configuration and statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
    public static string ToJson(this TimeoutPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var projection = new
        {
            policy.Id,
            policy.Name,
            policy.IsEnabled,
            policy.Timeout,
            policy.TotalExecutions,
            policy.SuccessfulExecutions,
            policy.FailedExecutions,
            policy.TimeoutCount,
            policy.AverageExecutionTimeMs,
            policy.LongestExecutionTimeMs,
            policy.ShortestExecutionTimeMs
        };

        return JsonSerializer.Serialize(projection, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
