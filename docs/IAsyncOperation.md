# IAsyncOperation

`IAsyncOperation` represents an asynchronous operation that can be executed with resilience policies such as retry, timeout, or circuit breaker. It is designed to encapsulate asynchronous work while allowing policy-driven fault handling and recovery strategies.

## API

### `CircuitBreakerService_ExecuteAsync_WhenCircuitIsOpen_NeverInvokesOperation`

Ensures that when the circuit breaker is in an open state, the encapsulated operation is never invoked, preventing further failures until the circuit recovers.

- **Parameters**:
  - `operation`: The asynchronous operation to execute.
  - `circuitBreaker`: The circuit breaker policy determining whether the operation should be executed.
- **Return value**: A `Task` representing the completion of the operation or the circuit breaker's rejection.
- **Exceptions**: Throws `InvalidOperationException` if the circuit breaker is in an invalid state.

### `PolicyValidationHelper_ValidatePolicy_CircuitBreakerWithZeroFailureThreshold_ReturnsError`

Validates a circuit breaker policy and returns an error if the failure threshold is set to zero, which would prevent the circuit from ever opening.

- **Parameters**:
  - `policy`: The circuit breaker policy to validate.
- **Return value**: `void`.
- **Exceptions**: Throws `ArgumentException` with a descriptive message if the failure threshold is zero.

### `PolicyValidationHelper_ValidatePolicy_RetryWithNegativeMaxRetries_ReturnsError`

Validates a retry policy and returns an error if the maximum retry count is negative, which would result in invalid retry behavior.

- **Parameters**:
  - `policy`: The retry policy to validate.
- **Return value**: `void`.
- **Exceptions**: Throws `ArgumentException` with a descriptive message if the maximum retry count is negative.

### `PolicyValidationHelper_SuggestOptimizations_FixedStrategyRetry_RecommendsExponentialBackoff`

Suggests optimizations for a fixed-interval retry strategy by recommending the use of exponential backoff for improved resilience and performance.

- **Parameters**:
  - `policy`: The retry policy to analyze.
- **Return value**: `void`.
- **Exceptions**: None.

## Usage

### Example 1: Executing an operation with a circuit breaker
