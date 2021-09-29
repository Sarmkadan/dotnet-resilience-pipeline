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
    /// <param name="configureOptions">Action to configure JSON serializer options.</param>
    /// <returns>Configured JsonPolicySerializer instance.</returns>
    public static JsonPolicySerializer WithOptions(this JsonPolicySerializer serializer, Action<JsonSerializerOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(configureOptions);

        var newOptions = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };

        configureOptions(newOptions);

        // Create a new serializer with the configured options
        // Note: This creates a new instance since the original is sealed with private options
        var newSerializer = new JsonPolicySerializer();

        // Use reflection to set the private _options field
        var optionsField = typeof(JsonPolicySerializer).GetField("_options", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        optionsField?.SetValue(newSerializer, newOptions);

        return newSerializer;
    }

    /// <summary>
    /// Deserializes a JSON string to a policy with error handling.
    /// </summary>
    /// <param name="json">JSON string to deserialize.</param>
    /// <param name="policy">Output policy if successful, null otherwise.</param>
    /// <returns>True if deserialization succeeded, false otherwise.</returns>
    public static bool TryDeserialize(this JsonPolicySerializer serializer, string json, out ResiliencyPolicy? policy)
    {
        try
        {
            policy = serializer.Deserialize(json);
            return policy != null;
        }
        catch
        {
            policy = null;
            return false;
        }
    }

    /// <summary>
    /// Serializes a policy to indented JSON string.
    /// </summary>
    /// <param name="policy">Policy to serialize.</param>
    /// <param name="indent">Whether to indent the JSON output.</param>
    /// <returns>JSON string representation of the policy.</returns>
    public static string Serialize(this JsonPolicySerializer serializer, ResiliencyPolicy policy, bool indent)
    {
        if (!indent)
        {
            var compactOptions = new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
            };

            var dto = serializer.MapToDto(policy);
            return JsonSerializer.Serialize(dto, compactOptions);
        }

        return serializer.Serialize(policy);
    }

    /// <summary>
    /// Serializes multiple policies to a JSON array with custom formatting.
    /// </summary>
    /// <param name="policies">Policies to serialize.</param>
    /// <param name="includeMetadata">Whether to include metadata like CreatedAt.</param>
    /// <returns>JSON string array representation of the policies.</returns>
    public static string SerializeMultiple(this JsonPolicySerializer serializer, IEnumerable<ResiliencyPolicy> policies, bool includeMetadata)
    {
        if (!includeMetadata)
        {
            // Create simplified DTOs without metadata
            var simplifiedPolicies = new List<object>();
            foreach (var policy in policies)
            {
                var policyObj = new System.Collections.Generic.Dictionary<string, object?>
                {
                    ["id"] = policy.Id,
                    ["name"] = policy.Name,
                    ["type"] = policy.GetType().Name,
                    ["isEnabled"] = policy.IsEnabled
                };

                if (policy is CircuitBreakerPolicy cb)
                {
                    policyObj["failureThreshold"] = cb.FailureThreshold;
                    policyObj["openDurationSeconds"] = (int)cb.OpenDuration.TotalSeconds;
                    policyObj["successThreshold"] = cb.SuccessThresholdInHalfOpen;
                }
                else if (policy is RetryPolicy retry)
                {
                    policyObj["maxRetries"] = retry.MaxRetries;
                    policyObj["initialDelayMs"] = (int)retry.InitialDelay.TotalMilliseconds;
                    policyObj["strategy"] = retry.Strategy.ToString();
                    policyObj["backoffMultiplier"] = retry.BackoffMultiplier;
                }
                else if (policy is TimeoutPolicy timeout)
                {
                    policyObj["timeoutSeconds"] = (int)timeout.Timeout.TotalSeconds;
                }
                else if (policy is BulkheadPolicy bulkhead)
                {
                    policyObj["maxParallelization"] = bulkhead.MaxParallelization;
                    policyObj["maxQueueLength"] = bulkhead.MaxQueueLength;
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

    private static PolicyJson MapToDto(this JsonPolicySerializer serializer, ResiliencyPolicy policy)
    {
        // Use reflection to access the private MapToDto method
        var method = typeof(JsonPolicySerializer).GetMethod("MapToDto", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        return (PolicyJson)method?.Invoke(serializer, new object[] { policy })!;
    }
}