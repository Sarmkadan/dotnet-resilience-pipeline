# Resiliency Exceptions

This document describes the derived exception types for the `dotnet-resilience-pipeline` library. For the base exception class and its common properties, see [ResiliencyException](ResiliencyException.md).

## CircuitBreakerOpenException

Thrown when a circuit breaker is open and rejecting requests.

### Properties

- **`public TimeSpan TimeUntilRetry`**  
  Gets the time span until the circuit breaker will transition to half-open state.

- **`public int ConsecutiveFailures`**  
  Gets the number of consecutive failures that caused the circuit breaker to open.

### Constructors

- **`public CircuitBreakerOpenException(string policyName, TimeSpan timeUntilRetry, int consecutiveFailures)`**  
  Initializes a new instance of the `CircuitBreakerOpenException` class.  
  *Parameters*:  
  - `policyName`: The name of the policy that threw this exception.  
  - `timeUntilRetry`: The time span until the circuit breaker will transition to half-open state.  
  - `consecutiveFailures`: The number of consecutive failures that caused the circuit breaker to open.

## BulkheadRejectedException

Thrown when the bulkhead limit is exceeded.

### Properties

- **`public int CurrentExecutions`**  
  Gets the current number of executions in the bulkhead.

- **`public int MaxExecutions`**  
  Gets the maximum number of executions allowed in the bulkhead.

- **`public int QueuedRequests`**  
  Gets the number of requests currently queued waiting for execution.

### Constructors

- **`public BulkheadRejectedException(string policyName, int currentExecutions, int maxExecutions, int queuedRequests)`**  
  Initializes a new instance of the `BulkheadRejectedException` class.  
  *Parameters*:  
  - `policyName`: The name of the policy that threw this exception.  
  - `currentExecutions`: The current number of executions in the bulkhead.  
  - `maxExecutions`: The maximum number of executions allowed in the bulkhead.  
  - `queuedRequests`: The number of requests currently queued waiting for execution.

## OperationTimeoutException

Thrown when an operation exceeds its timeout.

### Properties

- **`public TimeSpan Timeout`**  
  Gets the timeout value that was exceeded.

- **`public long ActualExecutionTimeMs`**  
  Gets the actual execution time in milliseconds when the timeout occurred.

### Constructors

- **`public OperationTimeoutException(string policyName, TimeSpan timeout, long actualTimeMs)`**  
  Initializes a new instance of the `OperationTimeoutException` class.  
  *Parameters*:  
  - `policyName`: The name of the policy that threw this exception.  
  - `timeout`: The timeout value that was exceeded.  
  - `actualTimeMs`: The actual execution time in milliseconds when the timeout occurred.

## MaxRetriesExceededException

Thrown when all retry attempts have been exhausted.

### Properties

- **`public int AttemptCount`**  
  Gets the number of retry attempts that were made before failing.

- **`public List<Exception>? AttemptExceptions`**  
  Gets the list of exceptions that occurred during each retry attempt.

### Constructors

- **`public MaxRetriesExceededException(string policyName, int attemptCount, List<Exception>? exceptions)`**  
  Initializes a new instance of the `MaxRetriesExceededException` class.  
  *Parameters*:  
  - `policyName`: The name of the policy that threw this exception.  
  - `attemptCount`: The number of retry attempts that were made before failing.  
  - `exceptions`: The list of exceptions that occurred during each retry attempt.

## FallbackFailedException

Thrown when fallback execution fails.

### Properties

- **`public Exception? PrimaryException`**  
  Gets the exception that occurred during the primary operation.

- **`public Exception? FallbackException`**  
  Gets the exception that occurred during the fallback operation.

### Constructors

- **`public FallbackFailedException(string policyName, Exception? primaryEx, Exception? fallbackEx)`**  
  Initializes a new instance of the `FallbackFailedException` class.  
  *Parameters*:  
  - `policyName`: The name of the policy that threw this exception.  
  - `primaryEx`: The exception that occurred during the primary operation.  
  - `fallbackEx`: The exception that occurred during the fallback operation.

## InvalidPolicyConfigurationException

Thrown when a policy configuration is invalid.

### Properties

- **`public List<string>? ConfigurationErrors`**  
  Gets the list of configuration errors that caused this exception.

### Constructors

- **`public InvalidPolicyConfigurationException(string policyName, string message, List<string>? errors = null)`**  
  Initializes a new instance of the `InvalidPolicyConfigurationException` class.  
  *Parameters*:  
  - `policyName`: The name of the policy that threw this exception.  
  - `message`: The error message that explains the reason for the exception.  
  - `errors`: The list of configuration errors that caused this exception.

## PipelineExecutionException

Thrown when pipeline execution encounters an unrecoverable error.

### Properties

- **`public string? ExecutionId`**  
  Gets the unique identifier for this pipeline execution.

- **`public List<string>? AppliedPolicies`**  
  Gets the list of policies that were applied during execution.

### Constructors

- **`public PipelineExecutionException(string message, string executionId, List<string>? appliedPolicies)`**  
  Initializes a new instance of the `PipelineExecutionException` class with the specified message, execution ID, and applied policies.  
  *Parameters*:  
  - `message`: The message that describes the error.  
  - `executionId`: The unique identifier for this pipeline execution.  
  - `appliedPolicies`: The list of policies that were applied during execution.

- **`public PipelineExecutionException(string message, Exception innerException, string executionId, List<string>? appliedPolicies)`**  
  Initializes a new instance of the `PipelineExecutionException` class with the specified message, inner exception, execution ID, and applied policies.  
  *Parameters*:  
  - `message`: The message that describes the error.  
  - `innerException`: The exception that is the cause of the current exception.  
  - `executionId`: The unique identifier for this pipeline execution.  
  - `appliedPolicies`: The list of policies that were applied during execution.