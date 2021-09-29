#nullable enable

using DotNetResiliencePipeline.Api.Controllers;
using DotNetResiliencePipeline.Domain.Policies;
using System.Text.Json;

namespace DotNetResiliencePipeline.Api.Controllers;

/// <summary>
/// Extension methods for <see cref="PoliciesController"/> that provide additional functionality
/// for working with resilience policies.
/// </summary>
public static class PoliciesControllerExtensions
{
    /// <summary>
    /// Creates a new policy with the specified configuration and returns the created policy DTO.
    /// </summary>
    /// <param name="controller">The policies controller instance</param>
    /// <param name="name">The name of the policy</param>
    /// <param name="type">The type of policy (circuitbreaker, retry, timeout, bulkhead, fallback)</param>
    /// <param name="configure">Action to configure the policy-specific settings</param>
    /// <returns>ApiResponse containing the created policy DTO</returns>
    public static async Task<ApiResponse<PolicyDto>> CreatePolicyAsync(
        this PoliciesController controller,
        string name,
        string type,
        Action<CreatePolicyRequest> configure)
    {
        var request = new CreatePolicyRequest
        {
            Name = name,
            Type = type
        };

        configure(request);

        return await controller.CreatePolicyAsync(request);
    }

    /// <summary>
    /// Gets all policies as a strongly-typed list.
    /// </summary>
    /// <param name="controller">The policies controller instance</param>
    /// <returns>List of PolicyDto objects</returns>
    public static async Task<List<PolicyDto>> GetAllPoliciesListAsync(this PoliciesController controller)
    {
        var response = await controller.GetAllPoliciesAsync();
        return response.Success && response.Data != null
            ? response.Data
            : new List<PolicyDto>();
    }

    /// <summary>
    /// Gets a policy by ID and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The policy type to deserialize to</typeparam>
    /// <param name="controller">The policies controller instance</param>
    /// <param name="id">The policy ID</param>
    /// <returns>Optional containing the deserialized policy, or None if not found</returns>
    public static async Task<PolicyDto?> GetPolicyAsync<T>(this PoliciesController controller, string id) where T : ResiliencyPolicy
    {
        var response = await controller.GetPolicyAsync(id);
        return response.Success && response.Data != null
            ? response.Data
            : null;
    }

    /// <summary>
    /// Validates a policy configuration and returns detailed validation errors.
    /// </summary>
    /// <param name="controller">The policies controller instance</param>
    /// <param name="name">The policy name</param>
    /// <param name="type">The policy type</param>
    /// <param name="failureThreshold">Optional failure threshold</param>
    /// <param name="maxRetries">Optional max retries</param>
    /// <param name="maxParallelization">Optional max parallelization</param>
    /// <param name="timeoutSeconds">Optional timeout in seconds</param>
    /// <returns>ValidationResultDto with detailed validation information</returns>
    public static async Task<ValidationResultDto> ValidatePolicyConfigurationAsync(
        this PoliciesController controller,
        string name,
        string type,
        int? failureThreshold = null,
        int? maxRetries = null,
        int? maxParallelization = null,
        int? timeoutSeconds = null)
    {
        var request = new ValidatePolicyRequest
        {
            Name = name,
            Type = type,
            FailureThreshold = failureThreshold,
            MaxRetries = maxRetries,
            MaxParallelization = maxParallelization,
            TimeoutSeconds = timeoutSeconds
        };

        var response = await controller.ValidatePolicyAsync(request);
        return response.Success && response.Data != null
            ? response.Data
            : new ValidationResultDto { IsValid = false, Errors = new List<string> { "Validation request failed" } };
    }

    /// <summary>
    /// Serializes a policy to JSON with camelCase naming convention.
    /// </summary>
    /// <param name="policy">The policy to serialize</param>
    /// <returns>JSON string representation of the policy</returns>
    public static string ToJson(this PolicyDto policy)
    {
        return JsonSerializer.Serialize(policy, new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });
    }

    /// <summary>
    /// Checks if a policy with the specified name exists.
    /// </summary>
    /// <param name="controller">The policies controller instance</param>
    /// <param name="name">The policy name to check</param>
    /// <returns>True if policy exists, false otherwise</returns>
    public static async Task<bool> PolicyExistsAsync(this PoliciesController controller, string name)
    {
        var policies = await controller.GetAllPoliciesListAsync();
        return policies.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}