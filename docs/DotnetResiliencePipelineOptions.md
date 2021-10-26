# DotnetResiliencePipelineOptions

Configuration options for building a resilience pipeline in .NET applications. This class aggregates settings for common resilience strategies (circuit breaker, retry, timeout, bulkhead, and fallback) and provides validation and policy construction capabilities.

## API

### Properties

#### `CircuitBreakerOptions CircuitBreaker`
Gets or sets the circuit breaker configuration options. The circuit breaker monitors failures and opens the circuit after a specified threshold, preventing further calls until recovery conditions are met.

#### `RetryOptions Retry`
Gets or sets the retry configuration options. Retry attempts to execute the operation again after a failure, with configurable delays and backoff strategies.

#### `TimeoutOptions Timeout`
Gets or sets the timeout configuration options. Timeout enforces a maximum duration for the operation to complete before aborting.

#### `BulkheadOptions Bulkhead`
Gets or sets the bulkhead configuration options. Bulkhead limits concurrent executions to prevent resource exhaustion.

#### `FallbackOptions Fallback`
Gets or sets the fallback configuration options. Fallback provides an alternative outcome when the primary operation fails.

#### `bool Validate`
Gets or sets a value indicating whether to validate the configuration during policy construction. Throws if invalid when `true`.

#### `int FailureThreshold`
Gets or sets the number of failures required to open the circuit breaker. Must be a positive integer.

#### `int OpenDurationSeconds`
Gets or sets the duration (in seconds) the circuit breaker remains open before transitioning to half-open. Must be a non-negative integer.

#### `int SuccessThresholdInHalfOpen`
Gets or sets the number of successful calls required in half-open state to close the circuit breaker. Must be a positive integer.

### Methods

#### `CircuitBreakerPolicy ToPolicy()`
Constructs a circuit breaker policy from the current configuration. Throws if `Validate` is `true` and configuration is invalid.

#### `int MaxRetries`
Gets or sets the maximum number of retry attempts. Must be a non-negative integer.

#### `int InitialDelayMs`
Gets or sets the initial delay (in milliseconds) before the first retry. Must be a non-negative integer.

#### `RetryPolicy.BackoffStrategy Strategy`
Gets or sets the backoff strategy for retry delays. Supports linear, exponential, or custom strategies.

#### `int MaxDelayMs`
Gets or sets the maximum delay (in milliseconds) for retry backoff. Must be a non-negative integer.

#### `double BackoffMultiplier`
Gets or sets the multiplier for exponential backoff. Must be a positive number.

#### `bool UseJitter`
Gets or sets whether to add jitter to retry delays to avoid thundering herds. Defaults to `false`.

#### `double JitterFactor`
Gets or sets the jitter factor (0.0 to 1.0) applied to retry delays when `UseJitter` is `true`.

#### `RetryPolicy ToPolicy()`
Constructs a retry policy from the current configuration. Throws if `Validate` is `true` and configuration is invalid.

#### `int TimeoutSeconds`
Gets or sets the timeout duration (in seconds). Must be a positive integer.

#### `TimeoutPolicy ToPolicy()`
Constructs a timeout policy from the current configuration. Throws if `Validate` is `true` and configuration is invalid.

## Usage

### Example 1: Basic Resilience Pipeline
