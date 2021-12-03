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
    private const string NullControllerMessage = "Controller cannot be null.";
    private const string NullNameMessage = "Name cannot be null or whitespace.";
    private const string NullTypeMessage = "Type cannot be null or whitespace.";
    private const string NullConfigureMessage = "Configure action cannot be null.";
    private const string NullIdMessage = "ID cannot be null or whitespace.";
    private const string NullPolicyMessage = "Policy cannot be null.";

    /// <summary>
    /// Creates a new policy with the specified configuration and returns the created policy DTO.
    /// </summary>
    /// <param name="controller">The policies controller instance.</param>
    /// <param name="name">The name of the policy.</param>
    /// <param name="type">The type of policy (circuitbreaker, retry, timeout, bulkhead, fallback).</param>
    /// <param name="configure">Action to configure the policy-specific settings.</param>
    /// <returns>ApiResponse containing the created policy DTO.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="controller"/> or <paramref name="configure"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/> or <paramref name="type"/> is null or whitespace.</exception>
    public static async Task<ApiResponse<PolicyDto>> CreatePolicyAsync(
        this PoliciesController controller,
        string name,
        string type,
        Action<CreatePolicyRequest> configure)
    {
        ArgumentNullException.ThrowIfNull(controller, NullControllerMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(name, NullNameMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(type, NullTypeMessage);
        ArgumentNullException.ThrowIfNull(configure, NullConfigureMessage);

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
    /// <param name="controller">The policies controller instance.</param>
    /// <returns>List of PolicyDto objects.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="controller"/> is null.</exception>
    public static async Task<List<PolicyDto>> GetAllPoliciesListAsync(this PoliciesController controller)
    {
        ArgumentNullException.ThrowIfNull(controller, NullControllerMessage);

        var response = await controller.GetAllPoliciesAsync();
        return response.Success && response.Data is not null
            ? response.Data
            : [];
    }

    /// <summary>
    /// Gets a policy by ID and deserializes it to the specified type.
    /// </summary>
    /// <typeparam name="T">The policy type to deserialize to.</typeparam>
    /// <param name="controller">The policies controller instance.</param>
    /// <param name="id">The policy ID.</param>
    /// <returns>PolicyDto if found, otherwise null.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="controller"/> or <paramref name="id"/> is null.</exception>
    public static async Task<PolicyDto?> GetPolicyAsync<T>(this PoliciesController controller, string id) where T : ResiliencyPolicy
    {
        ArgumentNullException.ThrowIfNull(controller, NullControllerMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(id, NullIdMessage);

        var response = await controller.GetPolicyAsync(id);
        return response.Success && response.Data is not null
            ? response.Data
            : null;
    }

    /// <summary>
    /// Validates a policy configuration and returns detailed validation errors.
    /// </summary>
    /// <param name="controller">The policies controller instance.</param>
    /// <param name="name">The policy name.</param>
    /// <param name="type">The policy type.</param>
    /// <param name="failureThreshold">Optional failure threshold.</param>
    /// <param name="maxRetries">Optional max retries.</param>
    /// <param name="maxParallelization">Optional max parallelization.</param>
    /// <param name="timeoutSeconds">Optional timeout in seconds.</param>
    /// <returns>ValidationResultDto with detailed validation information.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="controller"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if <paramref name="name"/> or <paramref name="type"/> is null or whitespace.</exception>
    public static async Task<ValidationResultDto> ValidatePolicyConfigurationAsync(
        this PoliciesController controller,
        string name,
        string type,
        int? failureThreshold = null,
        int? maxRetries = null,
        int? maxParallelization = null,
        int? timeoutSeconds = null)
    {
        ArgumentNullException.ThrowIfNull(controller, NullControllerMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(name, NullNameMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(type, NullTypeMessage);

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
        return response.Success && response.Data is not null
            ? response.Data
            : new ValidationResultDto { IsValid = false, Errors = ["Validation request failed"] };
    }

    /// <summary>
    /// Serializes a policy to JSON with camelCase naming convention.
    /// </summary>
    /// <param name="policy">The policy to deserialize.</param>
    /// <returns>JSON string representation of the policy.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    public static string ToJson(this PolicyDto policy) => JsonSerializer.Serialize(
        policy,
        new JsonSerializerOptions
        {
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            WriteIndented = true
        });

    /// <summary>
    /// Checks if a policy with the specified name exists.
    /// </summary>
    /// <param name="controller">The policies controller instance.</param>
    /// <param name="name">The policy name to check.</param>
    /// <returns>True if policy exists, false otherwise.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="controller"/> or <paramref name="name"/> is null.</exception>
    public static async Task<bool> PolicyExistsAsync(this PoliciesController controller, string name)
    {
        ArgumentNullException.ThrowIfNull(controller, NullControllerMessage);
        ArgumentException.ThrowIfNullOrWhiteSpace(name, NullNameMessage);

        var policies = await controller.GetAllPoliciesListAsync();
        return policies.Any(p => string.Equals(p.Name, name, StringComparison.OrdinalIgnoreCase));
    }
}