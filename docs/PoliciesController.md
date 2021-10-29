# PoliciesController

The `PoliciesController` is an ASP.NET Core controller responsible for managing resilience policies through a RESTful API. It provides endpoints to create, read, update, delete, and validate resilience policies, enabling dynamic configuration of circuit breakers, retries, and other resilience strategies at runtime.

## API

### `PoliciesController`
The controller class that exposes endpoints for managing resilience policies.

### `public async Task<ApiResponse<List<PolicyDto>>> GetAllPoliciesAsync()`
Retrieves all configured resilience policies.

- **Returns**: An `ApiResponse<List<PolicyDto>>` containing a list of all policies.
- **Throws**: May throw if the underlying policy store is unavailable or encounters an error.

### `public async Task<ApiResponse<PolicyDto>> GetPolicyAsync(string name)`
Retrieves a specific policy by its name.

- **Parameters**:
  - `name` (string): The unique name of the policy to retrieve.
- **Returns**: An `ApiResponse<PolicyDto>>` containing the requested policy or a not-found response.
- **Throws**: May throw if the policy does not exist or the store is unavailable.

### `public async Task<ApiResponse<PolicyDto>> CreatePolicyAsync(PolicyDto policyDto)`
Creates a new resilience policy.

- **Parameters**:
  - `policyDto` (PolicyDto): The policy definition to create.
- **Returns**: An `ApiResponse<PolicyDto>>` containing the created policy.
- **Throws**: May throw if validation fails, the policy name already exists, or the store is unavailable.

### `public async Task<ApiResponse<PolicyDto>> UpdatePolicyAsync(string name, PolicyDto policyDto)`
Updates an existing resilience policy.

- **Parameters**:
  - `name` (string): The name of the policy to update.
  - `policyDto` (PolicyDto): The updated policy definition.
- **Returns**: An `ApiResponse<PolicyDto>>` containing the updated policy.
- **Throws**: May throw if the policy does not exist, validation fails, or the store is unavailable.

### `public async Task<ApiResponse<bool>> DeletePolicyAsync(string name)`
Deletes an existing resilience policy.

- **Parameters**:
  - `name` (string): The name of the policy to delete.
- **Returns**: An `ApiResponse<bool>>` indicating success (`true`) or failure (`false`).
- **Throws**: May throw if the policy does not exist or the store is unavailable.

### `public async Task<ApiResponse<ValidationResultDto>> ValidatePolicyAsync(PolicyDto policyDto)`
Validates a policy definition without persisting it.

- **Parameters**:
  - `policyDto` (PolicyDto): The policy definition to validate.
- **Returns**: An `ApiResponse<ValidationResultDto>>` containing validation results (e.g., errors, warnings).
- **Throws**: May throw if the validation logic encounters an unrecoverable error.

### `public string Name`
Gets or sets the name of the policy. Used as a unique identifier.

### `public string Type`
Gets or sets the type of resilience policy (e.g., "CircuitBreaker", "Retry").

### `public int? FailureThreshold`
Gets or sets the number of failures required before the policy activates (e.g., for circuit breakers).

### `public int? MaxRetries`
Gets or sets the maximum number of retry attempts for transient failures.

### `public int? MaxParallelization`
Gets or sets the maximum number of parallel executions allowed by the policy.

### `public int? MaxQueueLength`
Gets or sets the maximum queue length for bulkhead policies.

### `public int? TimeoutSeconds`
Gets or sets the timeout duration in seconds for operations governed by the policy.

### `public int? OpenDurationSeconds`
Gets or sets the duration in seconds that a circuit remains open before attempting to close.

### `public int? InitialDelayMs`
Gets or sets the initial delay in milliseconds before the first retry attempt.

### `public bool IsEnabled`
Gets or sets whether the policy is currently active.

### `public CircuitBreakerConfigDto? CircuitBreakerConfig`
Gets or sets circuit breaker-specific configuration (e.g., failure thresholds, open durations).

### `public RetryConfigDto? RetryConfig`
Gets or sets retry-specific configuration (e.g., max retries, backoff strategies).

### `public int FailureThreshold`
Gets or sets the current failure threshold used by the policy (runtime value).

## Usage

### Example 1: Creating and Retrieving a Policy
