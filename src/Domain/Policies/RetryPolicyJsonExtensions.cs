#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Linq;
using System.Text.Json;

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Provides JSON serialization extensions for <see cref="RetryPolicy"/> instances.
/// </summary>
public static class RetryPolicyJsonExtensions
{
    /// <summary>
    /// Serializes a retry policy to a JSON string using an anonymous projection.
    /// </summary>
    /// <param name="policy">The retry policy to serialize.</param>
    /// <returns>A JSON string representation of the retry policy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="policy"/> is null.</exception>
    public static string ToJson(this RetryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var projection = new
        {
            policy.Id,
            policy.Name,
            policy.IsEnabled,
            policy.MaxRetries,
            policy.InitialDelay,
            policy.MaxDelay,
            Strategy = policy.Strategy.ToString(),
            policy.BackoffMultiplier,
            policy.UseJitter,
            policy.JitterFactor,
            policy.UseDecorrelatedJitter,
            RetryableExceptions = policy.RetryableExceptions.Select(type => type.Name),
            policy.TotalExecutions,
            policy.SuccessfulExecutions,
            policy.FailedExecutions,
            policy.TotalRetryAttempts
        };

        return JsonSerializer.Serialize(projection, new JsonSerializerOptions(JsonSerializerDefaults.Web));
    }
}
