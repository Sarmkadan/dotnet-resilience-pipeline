#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Provides JSON serialization extensions for <see cref="CircuitBreakerPolicy"/> instances.
/// </summary>
public static class CircuitBreakerPolicyJsonExtensions
{
    /// <summary>
    /// Serializes a circuit breaker policy to a JSON string using an anonymous projection.
    /// </summary>
    /// <param name="policy">The circuit breaker policy to serialize.</param>
    /// <returns>A JSON string containing the policy configuration, state, and counters.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
    public static string ToJson(this CircuitBreakerPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var projection = new
        {
            policy.Id,
            policy.Name,
            policy.IsEnabled,
            policy.FailureThreshold,
            policy.OpenDuration,
            State = policy.CurrentState.ToString(),
            policy.TotalExecutions,
            policy.SuccessfulExecutions,
            policy.FailedExecutions,
            policy.ConsecutiveFailures,
            policy.CircuitBreakerTrips
        };

        return JsonSerializer.Serialize(projection, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
