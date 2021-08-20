// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Data;

namespace DotNetResiliencePipeline.Api.Controllers;

/// <summary>
/// REST API controller for managing resilience policies.
/// Provides endpoints for CRUD operations on policies and policy configurations.
/// </summary>
public class PoliciesController
{
    private readonly ResiliencyPipelineService _pipelineService;
    private readonly PolicyRepository _policyRepository;

    public PoliciesController(ResiliencyPipelineService pipelineService, PolicyRepository policyRepository)
    {
        _pipelineService = pipelineService;
        _policyRepository = policyRepository;
    }

    /// <summary>
    /// GET /api/policies - Retrieves all registered policies.
    /// </summary>
    public async Task<ApiResponse<List<PolicyDto>>> GetAllPoliciesAsync()
    {
        try
        {
            var policies = _pipelineService.GetAllPolicies();
            var dtos = policies.Select(MapToPolicyDto).ToList();
            return new ApiResponse<List<PolicyDto>> { Success = true, Data = dtos };
        }
        catch (Exception ex)
        {
            return new ApiResponse<List<PolicyDto>> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// GET /api/policies/{id} - Retrieves a specific policy by ID.
    /// </summary>
    public async Task<ApiResponse<PolicyDto>> GetPolicyAsync(string id)
    {
        try
        {
            var policy = _pipelineService.GetPolicy(id);
            if (policy == null)
                return new ApiResponse<PolicyDto> { Success = false, Message = "Policy not found" };

            return new ApiResponse<PolicyDto> { Success = true, Data = MapToPolicyDto(policy) };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PolicyDto> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// POST /api/policies - Creates a new policy.
    /// </summary>
    public async Task<ApiResponse<PolicyDto>> CreatePolicyAsync(CreatePolicyRequest request)
    {
        try
        {
            // Validate request
            var errors = ValidateCreateRequest(request);
            if (errors.Count > 0)
                return new ApiResponse<PolicyDto> { Success = false, Message = string.Join(", ", errors) };

            ResiliencyPolicy policy = request.Type.ToLowerInvariant() switch
            {
                "circuitbreaker" => new CircuitBreakerPolicy(request.Name)
                {
                    FailureThreshold = request.FailureThreshold ?? 5,
                    OpenDuration = TimeSpan.FromSeconds(request.OpenDurationSeconds ?? 30)
                },
                "retry" => new RetryPolicy(request.Name)
                {
                    MaxRetries = request.MaxRetries ?? 3,
                    InitialDelay = TimeSpan.FromMilliseconds(request.InitialDelayMs ?? 100)
                },
                "timeout" => new TimeoutPolicy(request.Name)
                {
                    Timeout = TimeSpan.FromSeconds(request.TimeoutSeconds ?? 10)
                },
                "bulkhead" => new BulkheadPolicy(request.Name)
                {
                    MaxParallelization = request.MaxParallelization ?? 10,
                    MaxQueueLength = request.MaxQueueLength ?? 50
                },
                "fallback" => new FallbackPolicy(request.Name),
                _ => null!
            };

            if (policy == null)
                return new ApiResponse<PolicyDto> { Success = false, Message = "Invalid policy type" };

            _pipelineService.RegisterPolicy(policy);
            await _policyRepository.SaveAsync(policy);

            return new ApiResponse<PolicyDto> { Success = true, Data = MapToPolicyDto(policy) };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PolicyDto> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// PUT /api/policies/{id} - Updates an existing policy.
    /// </summary>
    public async Task<ApiResponse<PolicyDto>> UpdatePolicyAsync(string id, UpdatePolicyRequest request)
    {
        try
        {
            var policy = _pipelineService.GetPolicy(id);
            if (policy == null)
                return new ApiResponse<PolicyDto> { Success = false, Message = "Policy not found" };

            // Apply updates based on policy type
            if (policy is CircuitBreakerPolicy cb && request.CircuitBreakerConfig != null)
            {
                cb.FailureThreshold = request.CircuitBreakerConfig.FailureThreshold;
                cb.OpenDuration = TimeSpan.FromSeconds(request.CircuitBreakerConfig.OpenDurationSeconds);
            }
            else if (policy is RetryPolicy retry && request.RetryConfig != null)
            {
                retry.MaxRetries = request.RetryConfig.MaxRetries;
                retry.InitialDelay = TimeSpan.FromMilliseconds(request.RetryConfig.InitialDelayMs);
            }

            policy.IsEnabled = request.IsEnabled;

            await _policyRepository.SaveAsync(policy);

            return new ApiResponse<PolicyDto> { Success = true, Data = MapToPolicyDto(policy) };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PolicyDto> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// DELETE /api/policies/{id} - Deletes a policy.
    /// </summary>
    public async Task<ApiResponse<bool>> DeletePolicyAsync(string id)
    {
        try
        {
            if (_pipelineService.RemovePolicy(id))
                return new ApiResponse<bool> { Success = true, Data = true };

            return new ApiResponse<bool> { Success = false, Message = "Failed to delete policy" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<bool> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// POST /api/policies/validate - Validates a policy configuration.
    /// </summary>
    public async Task<ApiResponse<ValidationResultDto>> ValidatePolicyAsync(ValidatePolicyRequest request)
    {
        try
        {
            var errors = ValidateCreateRequest(new CreatePolicyRequest
            {
                Name = request.Name,
                Type = request.Type,
                FailureThreshold = request.FailureThreshold,
                MaxRetries = request.MaxRetries,
                MaxParallelization = request.MaxParallelization,
                TimeoutSeconds = request.TimeoutSeconds
            });

            return new ApiResponse<ValidationResultDto>
            {
                Success = errors.Count == 0,
                Data = new ValidationResultDto { IsValid = errors.Count == 0, Errors = errors }
            };
        }
        catch (Exception ex)
        {
            return new ApiResponse<ValidationResultDto> { Success = false, Message = ex.Message };
        }
    }

    private static List<string> ValidateCreateRequest(CreatePolicyRequest request)
    {
        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(request.Name))
            errors.Add("Name is required");

        if (string.IsNullOrWhiteSpace(request.Type))
            errors.Add("Type is required");

        if (!new[] { "circuitbreaker", "retry", "timeout", "bulkhead", "fallback" }
            .Contains(request.Type?.ToLowerInvariant()))
            errors.Add("Invalid policy type");

        if (request.FailureThreshold < 0)
            errors.Add("FailureThreshold cannot be negative");

        if (request.MaxRetries < 0)
            errors.Add("MaxRetries cannot be negative");

        return errors;
    }

    private static PolicyDto MapToPolicyDto(ResiliencyPolicy policy)
    {
        return new PolicyDto
        {
            Id = policy.Id,
            Name = policy.Name,
            Type = policy.GetType().Name,
            IsEnabled = policy.IsEnabled,
            CreatedAt = DateTime.UtcNow
        };
    }
}

/// <summary>
/// Request model for creating a new policy.
/// </summary>
public class CreatePolicyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? FailureThreshold { get; set; }
    public int? MaxRetries { get; set; }
    public int? MaxParallelization { get; set; }
    public int? MaxQueueLength { get; set; }
    public int? TimeoutSeconds { get; set; }
    public int? OpenDurationSeconds { get; set; }
    public int? InitialDelayMs { get; set; }
}

/// <summary>
/// Request model for updating a policy.
/// </summary>
public class UpdatePolicyRequest
{
    public bool IsEnabled { get; set; } = true;
    public CircuitBreakerConfigDto? CircuitBreakerConfig { get; set; }
    public RetryConfigDto? RetryConfig { get; set; }
}

/// <summary>
/// Circuit breaker configuration for updates.
/// </summary>
public class CircuitBreakerConfigDto
{
    public int FailureThreshold { get; set; }
    public int OpenDurationSeconds { get; set; }
}

/// <summary>
/// Retry configuration for updates.
/// </summary>
public class RetryConfigDto
{
    public int MaxRetries { get; set; }
    public int InitialDelayMs { get; set; }
}

/// <summary>
/// Request model for policy validation.
/// </summary>
public class ValidatePolicyRequest
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public int? FailureThreshold { get; set; }
    public int? MaxRetries { get; set; }
    public int? MaxParallelization { get; set; }
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// Validation result data transfer object.
/// </summary>
public class ValidationResultDto
{
    public bool IsValid { get; set; }
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Policy data transfer object.
/// </summary>
public class PolicyDto
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}

/// <summary>
/// Generic API response wrapper.
/// </summary>
public class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
