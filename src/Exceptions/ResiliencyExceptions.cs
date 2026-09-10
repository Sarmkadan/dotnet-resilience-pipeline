#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Exceptions;

/// <summary>
/// Base exception for all resilience pipeline failures.
/// </summary>
public class ResiliencyException : Exception
{
    /// <summary>
    /// Gets or sets the name of the policy that threw this exception.
    /// </summary>
    public string? PolicyName { get; set; }

    /// <summary>
    /// Gets or sets the type of the policy that threw this exception.
    /// </summary>
    public string? PolicyType { get; set; }

    /// <summary>
    /// Gets or sets the date and time (in UTC) when the exception occurred.
    /// </summary>
    public DateTime OccurredAt { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencyException"/> class with a specified error message and optional policy information.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="policyName">The name of the policy that threw this exception.</param>
    /// <param name="policyType">The type of the policy that threw this exception.</param>
    public ResiliencyException(string? message, string? policyName = null, string? policyType = null)
        : base(message)
    {
        PolicyName = policyName;
        PolicyType = policyType;
        OccurredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ResiliencyException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <param name="policyName">The name of the policy that threw this exception.</param>
    /// <param name="policyType">The type of the policy that threw this exception.</param>
    public ResiliencyException(string? message, Exception? innerException, string? policyName = null, string? policyType = null)
        : base(message, innerException)
    {
        PolicyName = policyName;
        PolicyType = policyType;
        OccurredAt = DateTime.UtcNow;
    }

    /// <summary>
    /// Returns a concise, informative representation of this exception,
    /// including policy details and strategy-specific state when available.
    /// </summary>
    public override string ToString()
    {
        var timeUntilRetry = (this as CircuitBreakerOpenException)?.TimeUntilRetry.ToString() ?? "N/A";
        var consecutiveFailures = (this as CircuitBreakerOpenException)?.ConsecutiveFailures.ToString() ?? "N/A";
        var currentExecutions = (this as BulkheadRejectedException)?.CurrentExecutions.ToString() ?? "N/A";

        return $"ResiliencyException {{ PolicyName = {PolicyName ?? "N/A"}, PolicyType = {PolicyType ?? "N/A"}, OccurredAt = {OccurredAt:O}, TimeUntilRetry = {timeUntilRetry}, ConsecutiveFailures = {consecutiveFailures}, CurrentExecutions = {currentExecutions} }}";
    }
}

/// <summary>
/// Thrown when a circuit breaker is open and rejecting requests.
/// </summary>
public sealed class CircuitBreakerOpenException : ResiliencyException
{
    /// <summary>
    /// Gets or sets the time span until the circuit breaker will transition to half-open state.
    /// </summary>
    public TimeSpan TimeUntilRetry { get; set; }

    /// <summary>
    /// Gets or sets the number of consecutive failures that caused the circuit breaker to open.
    /// </summary>
    public int ConsecutiveFailures { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="CircuitBreakerOpenException"/> class.
    /// </summary>
    /// <param name="policyName">The name of the policy that threw this exception.</param>
    /// <param name="timeUntilRetry">The time span until the circuit breaker will transition to half-open state.</param>
    /// <param name="consecutiveFailures">The number of consecutive failures that caused the circuit breaker to open.</param>
    public CircuitBreakerOpenException(string policyName, TimeSpan timeUntilRetry, int consecutiveFailures)
        : base($"Circuit breaker '{policyName}' is open. Retry after {timeUntilRetry.TotalSeconds:F2} seconds.",
            policyName, "CircuitBreaker")
    {
        TimeUntilRetry = timeUntilRetry;
        ConsecutiveFailures = consecutiveFailures;
    }
}

/// <summary>
/// Thrown when the bulkhead limit is exceeded.
/// </summary>
public sealed class BulkheadRejectedException : ResiliencyException
{
    /// <summary>
    /// Gets or sets the current number of executions in the bulkhead.
    /// </summary>
    public int CurrentExecutions { get; set; }

    /// <summary>
    /// Gets or sets the maximum number of executions allowed in the bulkhead.
    /// </summary>
    public int MaxExecutions { get; set; }

    /// <summary>
    /// Gets or sets the number of requests currently queued waiting for execution.
    /// </summary>
    public int QueuedRequests { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="BulkheadRejectedException"/> class.
    /// </summary>
    /// <param name="policyName">The name of the policy that threw this exception.</param>
    /// <param name="currentExecutions">The current number of executions in the bulkhead.</param>
    /// <param name="maxExecutions">The maximum number of executions allowed in the bulkhead.</param>
    /// <param name="queuedRequests">The number of requests currently queued waiting for execution.</param>
    public BulkheadRejectedException(string policyName, int currentExecutions, int maxExecutions, int queuedRequests)
        : base($"Bulkhead '{policyName}' is saturated ({currentExecutions}/{maxExecutions} slots in use, {queuedRequests} queued).",
            policyName, "Bulkhead")
    {
        CurrentExecutions = currentExecutions;
        MaxExecutions = maxExecutions;
        QueuedRequests = queuedRequests;
    }
}

/// <summary>
/// Thrown when an operation exceeds its timeout.
/// </summary>
public sealed class OperationTimeoutException : ResiliencyException
{
    /// <summary>
    /// Gets or sets the timeout value that was exceeded.
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Gets or sets the actual execution time in milliseconds when the timeout occurred.
    /// </summary>
    public long ActualExecutionTimeMs { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="OperationTimeoutException"/> class.
    /// </summary>
    /// <param name="policyName">The name of the policy that threw this exception.</param>
    /// <param name="timeout">The timeout value that was exceeded.</param>
    /// <param name="actualTimeMs">The actual execution time in milliseconds when the timeout occurred.</param>
    public OperationTimeoutException(string policyName, TimeSpan timeout, long actualTimeMs)
        : base($"Operation exceeded timeout of {timeout.TotalSeconds:F2} seconds ({actualTimeMs}ms).",
            policyName, "Timeout")
    {
        Timeout = timeout;
        ActualExecutionTimeMs = actualTimeMs;
    }
}

/// <summary>
/// Thrown when all retry attempts have been exhausted.
/// </summary>
public sealed class MaxRetriesExceededException : ResiliencyException
{
    /// <summary>
    /// Gets or sets the number of retry attempts that were made before failing.
    /// </summary>
    public int AttemptCount { get; set; }

    /// <summary>
    /// Gets or sets the list of exceptions that occurred during each retry attempt.
    /// </summary>
    public List<Exception>? AttemptExceptions { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="MaxRetriesExceededException"/> class.
    /// </summary>
    /// <param name="policyName">The name of the policy that threw this exception.</param>
    /// <param name="attemptCount">The number of retry attempts that were made before failing.</param>
    /// <param name="exceptions">The list of exceptions that occurred during each retry attempt.</param>
    public MaxRetriesExceededException(string policyName, int attemptCount, List<Exception>? exceptions)
        : base($"All {attemptCount} retry attempts failed.",
            policyName, "Retry")
    {
        AttemptCount = attemptCount;
        AttemptExceptions = exceptions;
    }
}

/// <summary>
/// Thrown when fallback execution fails.
/// </summary>
public sealed class FallbackFailedException : ResiliencyException
{
    /// <summary>
    /// Gets or sets the exception that occurred during the primary operation.
    /// </summary>
    public Exception? PrimaryException { get; set; }

    /// <summary>
    /// Gets or sets the exception that occurred during the fallback operation.
    /// </summary>
    public Exception? FallbackException { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="FallbackFailedException"/> class.
    /// </summary>
    /// <param name="policyName">The name of the policy that threw this exception.</param>
    /// <param name="primaryEx">The exception that occurred during the primary operation.</param>
    /// <param name="fallbackEx">The exception that occurred during the fallback operation.</param>
    public FallbackFailedException(string policyName, Exception? primaryEx, Exception? fallbackEx)
        : base($"Both primary operation and fallback failed. Primary: {primaryEx?.Message}, Fallback: {fallbackEx?.Message}",
            policyName, "Fallback")
    {
        PrimaryException = primaryEx;
        FallbackException = fallbackEx;
    }
}

/// <summary>
/// Thrown when a policy configuration is invalid.
/// </summary>
public sealed class InvalidPolicyConfigurationException : ResiliencyException
{
    /// <summary>
    /// Gets or sets the list of configuration errors that caused this exception.
    /// </summary>
    public List<string>? ConfigurationErrors { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidPolicyConfigurationException"/> class.
    /// </summary>
    /// <param name="policyName">The name of the policy that threw this exception.</param>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="errors">The list of configuration errors that caused this exception.</param>
    public InvalidPolicyConfigurationException(string policyName, string message, List<string>? errors = null)
        : base(message, policyName, "Configuration")
    {
        if (errors is not null)
            ConfigurationErrors = errors;
    }
}

/// <summary>
/// Thrown when pipeline execution encounters an unrecoverable error.
/// </summary>
public sealed class PipelineExecutionException : ResiliencyException
{
    /// <summary>
    /// Gets or sets the unique identifier for this pipeline execution.
    /// </summary>
    public string? ExecutionId { get; set; }

    /// <summary>
    /// Gets or sets the list of policies that were applied during execution.
    /// </summary>
    public List<string>? AppliedPolicies { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineExecutionException"/> class with the specified message, execution ID, and applied policies.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="executionId">The unique identifier for this pipeline execution.</param>
    /// <param name="appliedPolicies">The list of policies that were applied during execution.</param>
    public PipelineExecutionException(string message, string executionId, List<string>? appliedPolicies)
        : base(message, appliedPolicies?.Any() == true ? appliedPolicies.First() : null, "Pipeline")
    {
        ExecutionId = executionId;
        AppliedPolicies = appliedPolicies;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineExecutionException"/> class with the specified message, inner exception, execution ID, and applied policies.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    /// <param name="executionId">The unique identifier for this pipeline execution.</param>
    /// <param name="appliedPolicies">The list of policies that were applied during execution.</param>
    public PipelineExecutionException(string message, Exception innerException, string executionId, List<string>? appliedPolicies)
        : base(message, innerException, appliedPolicies?.Any() == true ? appliedPolicies.First() : null, "Pipeline")
    {
        ExecutionId = executionId;
        AppliedPolicies = appliedPolicies;
    }
}
