# ResiliencyPipelineBuilderExtensions

Provides extension methods for configuring common resilience strategies on a `ResiliencyPipelineBuilder` instance. These methods simplify the setup of standard resilience patterns such as circuit breakers, retries, bulkheads, and timeouts with sensible defaults or minimal configuration.

## API

### `WithDefaultCircuitBreaker`

Adds a circuit breaker resilience strategy with default settings to the pipeline. The circuit breaker will track failures and open the circuit after a specified number of consecutive failures, preventing further executions until the circuit recovers.

- **Parameters**: None.
- **Return value**: The same `ResiliencyPipelineBuilder` instance to allow method chaining.
- **Exceptions**: Throws `ArgumentNullException` if the builder is `null`.

### `WithExponentialBackoffRetry`

Adds a retry resilience strategy with an exponential backoff delay between attempts to the pipeline. This is useful for transient fault handling where operations may succeed after a delay.

- **Parameters**: None.
- **Return value**: The same `ResiliencyPipelineBuilder` instance to allow method chaining.
- **Exceptions**: Throws `ArgumentNullException` if the builder is `null`.

### `WithIsolatedBulkhead`

Adds a bulkhead isolation strategy to the pipeline, limiting the number of concurrent executions to prevent resource exhaustion. This isolates the protected operation from others, ensuring it does not monopolize shared resources.

- **Parameters**: None.
- **Return value**: The same `ResiliencyPipelineBuilder` instance to allow method chaining.
- **Exceptions**: Throws `ArgumentNullException` if the builder is `null`.

### `WithDefaultTimeout`

Adds a timeout resilience strategy with a default duration to the pipeline. The operation will be canceled if it does not complete within the specified timeout period.

- **Parameters**: None.
- **Return value**: The same `ResiliencyPipelineBuilder` instance to allow method chaining.
- **Exceptions**: Throws `ArgumentNullException` if the builder is `null`.

## Usage
