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
    public string PolicyName { get; set; }
    public string PolicyType { get; set; }
    public DateTime OccurredAt { get; set; }

    public ResiliencyException(string message, string policyName = "", string policyType = "")
        : base(message)
    {
        PolicyName = policyName;
        PolicyType = policyType;
        OccurredAt = DateTime.UtcNow;
    }

    public ResiliencyException(string message, Exception innerException, string policyName = "", string policyType = "")
        : base(message, innerException)
    {
        PolicyName = policyName;
        PolicyType = policyType;
        OccurredAt = DateTime.UtcNow;
    }
}

/// <summary>
/// Thrown when a circuit breaker is open and rejecting requests.
/// </summary>
public class CircuitBreakerOpenException : ResiliencyException
{
    public TimeSpan TimeUntilRetry { get; set; }
    public int ConsecutiveFailures { get; set; }

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
public class BulkheadRejectedException : ResiliencyException
{
    public int CurrentExecutions { get; set; }
    public int MaxExecutions { get; set; }
    public int QueuedRequests { get; set; }

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
public class OperationTimeoutException : ResiliencyException
{
    public TimeSpan Timeout { get; set; }
    public long ActualExecutionTimeMs { get; set; }

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
public class MaxRetriesExceededException : ResiliencyException
{
    public int AttemptCount { get; set; }
    public List<Exception> AttemptExceptions { get; set; } = new();

    public MaxRetriesExceededException(string policyName, int attemptCount, List<Exception> exceptions)
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
public class FallbackFailedException : ResiliencyException
{
    public Exception PrimaryException { get; set; }
    public Exception FallbackException { get; set; }

    public FallbackFailedException(string policyName, Exception primaryEx, Exception fallbackEx)
        : base($"Both primary operation and fallback failed. Primary: {primaryEx.Message}, Fallback: {fallbackEx.Message}",
            policyName, "Fallback")
    {
        PrimaryException = primaryEx;
        FallbackException = fallbackEx;
    }
}

/// <summary>
/// Thrown when a policy configuration is invalid.
/// </summary>
public class InvalidPolicyConfigurationException : ResiliencyException
{
    public List<string> ConfigurationErrors { get; set; } = new();

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
public class PipelineExecutionException : ResiliencyException
{
    public string ExecutionId { get; set; }
    public List<string> AppliedPolicies { get; set; } = new();

    public PipelineExecutionException(string message, string executionId, List<string> appliedPolicies)
        : base(message, "", "Pipeline")
    {
        ExecutionId = executionId;
        AppliedPolicies = appliedPolicies;
    }
}
