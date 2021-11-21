# PoliciesControllerExtensions

Extension methods for working with resilience policies in API controllers. Provides common operations for creating, retrieving, validating, and serializing policy configurations.

## API

### `CreatePolicyAsync`

Creates a new resilience policy based on the provided configuration.

- **Parameters**
  - `controller` (`ControllerBase`): The controller instance.
  - `policyDto` (`PolicyDto`): The policy configuration to create.
  - `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

- **Return value**
  - `Task<ApiResponse<PolicyDto>>`: An API response containing the created policy or validation errors.

- **Exceptions**
  - Throws `ArgumentNullException` if `controller` or `policyDto` is `null`.

### `GetAllPoliciesListAsync`

Retrieves all configured resilience policies as a list.

- **Parameters**
  - `controller` (`ControllerBase`): The controller instance.
  - `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

- **Return value**
  - `Task<List<PolicyDto>>`: A list of all configured policies.

- **Exceptions**
  - Throws `ArgumentNullException` if `controller` is `null`.

### `GetPolicyAsync<T>`

Retrieves a specific policy by its type.

- **Parameters**
  - `controller` (`ControllerBase`): The controller instance.
  - `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

- **Type Parameters**
  - `T`: The type of the policy to retrieve.

- **Return value**
  - `Task<PolicyDto?>`: The policy DTO if found; otherwise, `null`.

- **Exceptions**
  - Throws `ArgumentNullException` if `controller` is `null`.

### `ValidatePolicyConfigurationAsync`

Validates a policy configuration for correctness and completeness.

- **Parameters**
  - `controller` (`ControllerBase`): The controller instance.
  - `policyDto` (`PolicyDto`): The policy configuration to validate.
  - `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

- **Return value**
  - `Task<ValidationResultDto>`: A result indicating whether validation succeeded and any error messages.

- **Exceptions**
  - Throws `ArgumentNullException` if `controller` or `policyDto` is `null`.

### `ToJson`

Serializes a policy DTO to a JSON string.

- **Parameters**
  - `policyDto` (`PolicyDto`): The policy to serialize.

- **Return value**
  - `string`: A JSON representation of the policy.

- **Exceptions**
  - Throws `ArgumentNullException` if `policyDto` is `null`.

### `PolicyExistsAsync`

Checks whether a policy with the specified name exists.

- **Parameters**
  - `controller` (`ControllerBase`): The controller instance.
  - `policyName` (`string`): The name of the policy to check.
  - `cancellationToken` (`CancellationToken`, optional): A token to monitor for cancellation requests.

- **Return value**
  - `Task<bool>`: `true` if the policy exists; otherwise, `false`.

- **Exceptions**
  - Throws `ArgumentNullException` if `controller` or `policyName` is `null`.

## Usage

### Creating and Retrieving a Policy
