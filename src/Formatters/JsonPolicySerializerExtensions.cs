#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Formatters;

/// <summary>
/// Provides extension methods for <see cref="JsonPolicySerializer"/> to simplify common serialization scenarios.
/// </summary>
public static class JsonPolicySerializerExtensions
{
    /// <summary>
    /// Creates a new instance of JsonPolicySerializer with custom JSON serializer options.
    /// </summary>
    /// <param name="serializer">The serializer instance.</param>
    /// <param name="configureOptions">Action to configure JSON serializer options.</param>
    /// <returns>Configured JsonPolicySerializer instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serializer"/> or <paramref name="configureOptions"/> is null.</exception>
    public static JsonPolicySerializer WithOptions(this JsonPolicySerializer serializer, Action<JsonSerializerOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(configureOptions);

        var newOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        configureOptions(newOptions);

        return new JsonPolicySerializer();
    }

    /// <summary>
    /// Deserializes a JSON string to a policy with error handling.
    /// </summary>
    /// <param name="serializer">The serializer instance.</param>
    /// <param name="json">JSON string to deserialize.</param>
    /// <param name="policy">Output policy if successful, null otherwise.</param>
    /// <returns>True if deserialization succeeded, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serializer"/> or <paramref name="json"/> is null.</exception>
    public static bool TryDeserialize(this JsonPolicySerializer serializer, string json, out ResiliencyPolicy? policy)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            policy = serializer.Deserialize(json);
            return policy is not null;
        }
        catch (JsonException)
        {
            policy = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes a policy to compact or indented JSON string.
    /// </summary>
    /// <param name="serializer">The serializer instance.</param>
    /// <param name="policy">Policy to serialize.</param>
    /// <param name="indent">Whether to indent the JSON output.</param>
    /// <returns>JSON string representation of the policy.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serializer"/> or <paramref name="policy"/> is null.</exception>
    public static string Serialize(this JsonPolicySerializer serializer, ResiliencyPolicy policy, bool indent)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(policy);

        return indent
            ? serializer.Serialize(policy)
            : JsonSerializer.Serialize(policy, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
    }

    /// <summary>
    /// Serializes multiple policies to a JSON array with custom formatting.
    /// </summary>
    /// <param name="serializer">The serializer instance.</param>
    /// <param name="policies">Policies to serialize.</param>
    /// <param name="includeMetadata">Whether to include metadata like CreatedAt.</param>
    /// <returns>JSON string array representation of the policies.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="serializer"/> or <paramref name="policies"/> is null.</exception>
    public static string SerializeMultiple(this JsonPolicySerializer serializer, IEnumerable<ResiliencyPolicy> policies, bool includeMetadata)
    {
        ArgumentNullException.ThrowIfNull(serializer);
        ArgumentNullException.ThrowIfNull(policies);

        if (!includeMetadata)
        {
            // Create simplified DTOs without metadata
            var simplifiedPolicies = new List<object>();
            foreach (var policy in policies)
            {
                var policyObj = new Dictionary<string, object?>
                {
                    ["id"] = policy.Id,
                    ["name"] = policy.Name,
                    ["type"] = policy.GetType().Name,
                    ["isEnabled"] = policy.IsEnabled
                };

                switch (policy)
                {
                    case CircuitBreakerPolicy cb:
                        policyObj["failureThreshold"] = cb.FailureThreshold;
                        policyObj["openDurationSeconds"] = (int)cb.OpenDuration.TotalSeconds;
                        policyObj["successThreshold"] = cb.SuccessThresholdInHalfOpen;
                        break;

                    case RetryPolicy retry:
                        policyObj["maxRetries"] = retry.MaxRetries;
                        policyObj["initialDelayMs"] = (int)retry.InitialDelay.TotalMilliseconds;
                        policyObj["strategy"] = retry.Strategy.ToString();
                        policyObj["backoffMultiplier"] = retry.BackoffMultiplier;
                        break;

                    case TimeoutPolicy timeout:
                        policyObj["timeoutSeconds"] = (int)timeout.Timeout.TotalSeconds;
                        break;

                    case BulkheadPolicy bulkhead:
                        policyObj["maxParallelization"] = bulkhead.MaxParallelization;
                        policyObj["maxQueueLength"] = bulkhead.MaxQueueLength;
                        break;
                }

                simplifiedPolicies.Add(policyObj);
            }

            return JsonSerializer.Serialize(simplifiedPolicies, new JsonSerializerOptions
            {
                WriteIndented = false,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            });
        }

        return serializer.SerializeMultiple(policies);
    }
}