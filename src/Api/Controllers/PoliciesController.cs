#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using DotNetResiliencePipeline.Data;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Formatters;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Api.Controllers;

/// <summary>
/// REST API controller for managing resilience policies.
/// Provides endpoints for CRUD operations on policies and policy configurations.
/// </summary>
public sealed class PoliciesController
{
    private readonly ResiliencyPipelineService _pipelineService;
    private readonly PolicyRepository _policyRepository;

    /// <summary>
    /// Initializes a new instance of the <see cref="PoliciesController"/> class.
    /// </summary>
    /// <param name="pipelineService">The pipeline service used to execute and validate policies.</param>
    /// <param name="policyRepository">The repository that stores policies.</param>
    public PoliciesController(ResiliencyPipelineService pipelineService, PolicyRepository policyRepository)
    {
        _pipelineService = pipelineService;
        _policyRepository = policyRepository;
    }

    /// <summary>
    /// Returns a concise, informative representation of this controller.
    /// </summary>
    public override string ToString() =>
        $"PoliciesController {{ PipelineService = {_pipelineService}, PolicyRepository = {_policyRepository} }}";

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
        ArgumentException.ThrowIfNullOrEmpty(id);

        try
        {
            var policy = _pipelineService.GetPolicy(id);
            if (policy is null)
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
        ArgumentNullException.ThrowIfNull(request);

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

            if (policy is null)
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
        ArgumentNullException.ThrowIfNull(id);
        ArgumentNullException.ThrowIfNull(request);

        try
        {
            var policy = _pipelineService.GetPolicy(id);
            if (policy is null)
                return new ApiResponse<PolicyDto> { Success = false, Message = "Policy not found" };

            // Apply updates based on policy type
            if (policy is CircuitBreakerPolicy cb && request.CircuitBreakerConfig is not null)
            {
                cb.FailureThreshold = request.CircuitBreakerConfig.FailureThreshold;
                cb.OpenDuration = TimeSpan.FromSeconds(request.CircuitBreakerConfig.OpenDurationSeconds);
            }
            else if (policy is RetryPolicy retry && request.RetryConfig is not null)
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
        ArgumentException.ThrowIfNullOrEmpty(id);

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
    /// POST /api/policies/{name}/clone - Duplicates an existing policy under a new name.
    /// </summary>
    public async Task<ApiResponse<PolicyDto>> ClonePolicyAsync(string name, string newName)
    {
        try
        {
            // Validate newName parameter
            if (string.IsNullOrWhiteSpace(newName))
                return new ApiResponse<PolicyDto> { Success = false, Message = "newName query parameter is required" };

            // Get source policy by name
            var sourcePolicy = _policyRepository.GetByName(name);
            if (sourcePolicy is null)
                return new ApiResponse<PolicyDto> { Success = false, Message = "Source policy not found", StatusCode = 404 };

            // Check if target policy already exists
            var existingTarget = _policyRepository.GetByName(newName);
            if (existingTarget is not null)
                return new ApiResponse<PolicyDto> { Success = false, Message = "Policy with target name already exists", StatusCode = 409 };

            // Clone the policy based on its type
            ResiliencyPolicy clonedPolicy = sourcePolicy switch
            {
                CircuitBreakerPolicy cb => new CircuitBreakerPolicy(newName)
                {
                    FailureThreshold = cb.FailureThreshold,
                    OpenDuration = cb.OpenDuration,
                    IsEnabled = cb.IsEnabled,
                    Tags = new List<string>(cb.Tags),
                    Metadata = new Dictionary<string, object>(cb.Metadata)
                },
                RetryPolicy retry => new RetryPolicy(newName)
                {
                    MaxRetries = retry.MaxRetries,
                    InitialDelay = retry.InitialDelay,
                    Strategy = retry.Strategy,
                    MaxDelay = retry.MaxDelay,
                    BackoffMultiplier = retry.BackoffMultiplier,
                    UseJitter = retry.UseJitter,
                    JitterFactor = retry.JitterFactor,
                    UseDecorrelatedJitter = retry.UseDecorrelatedJitter,
                    IsEnabled = retry.IsEnabled,
                    Tags = new List<string>(retry.Tags),
                    Metadata = new Dictionary<string, object>(retry.Metadata)
                },
                TimeoutPolicy timeout => new TimeoutPolicy(newName)
                {
                    Timeout = timeout.Timeout,
                    IsEnabled = timeout.IsEnabled,
                    Tags = new List<string>(timeout.Tags),
                    Metadata = new Dictionary<string, object>(timeout.Metadata)
                },
                BulkheadPolicy bulkhead => new BulkheadPolicy(newName)
                {
                    MaxParallelization = bulkhead.MaxParallelization,
                    MaxQueueLength = bulkhead.MaxQueueLength,
                    IsEnabled = bulkhead.IsEnabled,
                    Tags = new List<string>(bulkhead.Tags),
                    Metadata = new Dictionary<string, object>(bulkhead.Metadata)
                },
                FallbackPolicy fallback => new FallbackPolicy(newName)
                {
                    IsEnabled = fallback.IsEnabled,
                    Tags = new List<string>(fallback.Tags),
                    Metadata = new Dictionary<string, object>(fallback.Metadata)
                },
                _ => throw new InvalidOperationException("Unknown policy type")
            };

            // Reset statistics for the cloned policy
            clonedPolicy.ResetStatistics();

            // Register and save the cloned policy
            _pipelineService.RegisterPolicy(clonedPolicy);
            await _policyRepository.SaveAsync(clonedPolicy);

            return new ApiResponse<PolicyDto> { Success = true, Data = MapToPolicyDto(clonedPolicy) };
        }
        catch (Exception ex)
        {
            return new ApiResponse<PolicyDto> { Success = false, Message = ex.Message };
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

    /// <summary>
    /// GET /api/policies/export/csv - Exports all policies as CSV.
    /// </summary>
    public async Task<string> ExportPoliciesAsCsvAsync()
    {
        try
        {
            var policies = _pipelineService.GetAllPolicies();
            var csvFormatter = new CsvReportFormatter();
            return csvFormatter.FormatPolicies(policies.ToList());
        }
        catch (Exception ex)
        {
            throw new Exception(ex.Message);
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
public sealed class CreatePolicyRequest
{
    /// <summary>
    /// Gets or sets the name of the policy to create.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy type, such as CircuitBreaker, Retry, Timeout, or Bulkhead.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold.
    /// </summary>
    public int? FailureThreshold { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int? MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent executions for a bulkhead.
    /// </summary>
    public int? MaxParallelization { get; set; }

    /// <summary>
    /// Gets or sets the maximum queue length for a bulkhead.
    /// </summary>
    public int? MaxQueueLength { get; set; }

    /// <summary>
    /// Gets or sets the timeout, in seconds.
    /// </summary>
    public int? TimeoutSeconds { get; set; }

    /// <summary>
    /// Gets or sets the duration, in seconds, that a circuit breaker remains open.
    /// </summary>
    public int? OpenDurationSeconds { get; set; }

    /// <summary>
    /// Gets or sets the initial retry delay, in milliseconds.
    /// </summary>
    public int? InitialDelayMs { get; set; }
}

/// <summary>
/// Request model for updating a policy.
/// </summary>
public sealed class UpdatePolicyRequest
{
    /// <summary>
    /// Gets or sets a value indicating whether the policy is enabled.
    /// </summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Gets or sets the optional circuit breaker configuration to apply.
    /// </summary>
    public CircuitBreakerConfigDto? CircuitBreakerConfig { get; set; }

    /// <summary>
    /// Gets or sets the optional retry configuration to apply.
    /// </summary>
    public RetryConfigDto? RetryConfig { get; set; }
}

/// <summary>
/// Circuit breaker configuration for updates.
/// </summary>
public sealed class CircuitBreakerConfigDto
{
    /// <summary>
    /// Gets or sets the number of consecutive failures that opens the circuit.
    /// </summary>
    public int FailureThreshold { get; set; }

    /// <summary>
    /// Gets or sets the duration, in seconds, the circuit remains open.
    /// </summary>
    public int OpenDurationSeconds { get; set; }
}

/// <summary>
/// Retry configuration for updates.
/// </summary>
public sealed class RetryConfigDto
{
    /// <summary>
    /// Gets or sets the maximum number of retry attempts.
    /// </summary>
    public int MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the initial retry delay, in milliseconds.
    /// </summary>
    public int InitialDelayMs { get; set; }
}

/// <summary>
/// Request model for policy validation.
/// </summary>
public sealed class ValidatePolicyRequest
{
    /// <summary>
    /// Gets or sets the name of the policy to validate.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy type to validate.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the circuit breaker failure threshold to validate.
    /// </summary>
    public int? FailureThreshold { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of retry attempts to validate.
    /// </summary>
    public int? MaxRetries { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of concurrent executions to validate.
    /// </summary>
    public int? MaxParallelization { get; set; }

    /// <summary>
    /// Gets or sets the timeout, in seconds, to validate.
    /// </summary>
    public int? TimeoutSeconds { get; set; }
}

/// <summary>
/// Validation result data transfer object.
/// </summary>
public sealed class ValidationResultDto
{
    /// <summary>
    /// Gets or sets a value indicating whether the policy configuration is valid.
    /// </summary>
    public bool IsValid { get; set; }

    /// <summary>
    /// Gets or sets the validation error messages, if any.
    /// </summary>
    public List<string> Errors { get; set; } = new();
}

/// <summary>
/// Policy data transfer object.
/// </summary>
public sealed class PolicyDto
{
    /// <summary>
    /// Gets or sets the identifier of the policy.
    /// </summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the name of the policy.
    /// </summary>
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets the policy type name.
    /// </summary>
    public string Type { get; set; } = string.Empty;

    /// <summary>
    /// Gets or sets a value indicating whether the policy is enabled.
    /// </summary>
    public bool IsEnabled { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the policy was created.
    /// </summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>
    /// Returns a string representation of the policy data transfer object.
    /// </summary>
    public override string ToString() =>
        $"PolicyDto {{ Id = {Id}, Name = {Name}, Type = {Type}, IsEnabled = {IsEnabled}, CreatedAt = {CreatedAt} }}";
}

/// <summary>
/// Generic API response wrapper.
/// </summary>
public sealed class ApiResponse<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the request succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the response payload, if any.
    /// </summary>
    public T? Data { get; set; }

    /// <summary>
    /// Gets or sets an optional human-readable message.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the HTTP-style status code of the response.
    /// </summary>
    public int StatusCode { get; set; } = 200;

    /// <summary>
    /// Gets or sets the UTC time at which the response was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
