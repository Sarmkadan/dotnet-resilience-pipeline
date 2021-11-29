#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;

namespace DotNetResiliencePipeline.Exceptions;

/// <summary>
/// Extension methods for <see cref="ResiliencyException"/> and its derived types.
/// Provides utility methods for formatting, analyzing, and working with resilience exceptions.
/// </summary>
/// <exception cref="ArgumentNullException">Thrown when the <paramref name="exception"/> parameter is <see langword="null"/>.</exception>
public static class ResiliencyExceptionExtensions
{
    /// <summary>
    /// Creates a detailed error message that includes all available exception information.
    /// </summary>
    /// <param name="exception">The resilience exception to format.</param>
    /// <returns>A formatted error message with policy details, timestamps, and exception information.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="exception"/> parameter is <see langword="null"/>.</exception>
    public static string ToDetailedErrorMessage(this ResiliencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        var builder = new StringBuilder();
        builder.AppendLine("=== Resilience Pipeline Error Report ===");
        builder.AppendLine();

        // Basic exception info
        builder.AppendLine($"Exception Type: {exception.GetType().Name}");
        builder.AppendLine($"Message: {exception.Message}");
        builder.AppendLine($"Occurred At: {exception.OccurredAt:yyyy-MM-dd HH:mm:ss.fff} UTC");

        // Policy information
        if (!string.IsNullOrWhiteSpace(exception.PolicyName))
        {
            builder.AppendLine($"Policy Name: {exception.PolicyName}");
        }

        if (!string.IsNullOrWhiteSpace(exception.PolicyType))
        {
            builder.AppendLine($"Policy Type: {exception.PolicyType}");
        }

        builder.AppendLine();

        // Type-specific information
        switch (exception)
        {
            case CircuitBreakerOpenException cbEx:
                builder.AppendLine("=== Circuit Breaker Details ===");
                builder.AppendLine($"Time Until Retry: {cbEx.TimeUntilRetry.TotalSeconds:F2} seconds");
                builder.AppendLine($"Consecutive Failures: {cbEx.ConsecutiveFailures}");
                builder.AppendLine($"Retry Status: {(cbEx.TimeUntilRetry.TotalSeconds > 0 ? "Open" : "Half-Open")}");
                break;

            case BulkheadRejectedException bhEx:
                builder.AppendLine("=== Bulkhead Details ===");
                builder.AppendLine($"Current Executions: {bhEx.CurrentExecutions}/{bhEx.MaxExecutions}");
                builder.AppendLine($"Queued Requests: {bhEx.QueuedRequests}");
                builder.AppendLine($"Utilization: {(double)bhEx.CurrentExecutions / bhEx.MaxExecutions:P0}");
                break;

            case OperationTimeoutException toEx:
                builder.AppendLine("=== Timeout Details ===");
                builder.AppendLine($"Configured Timeout: {toEx.Timeout.TotalSeconds:F2} seconds");
                builder.AppendLine($"Actual Execution Time: {toEx.ActualExecutionTimeMs}ms");
                builder.AppendLine($"Timeout Exceeded By: {toEx.ActualExecutionTimeMs - (long)toEx.Timeout.TotalMilliseconds}ms");
                break;

            case MaxRetriesExceededException mrEx:
                builder.AppendLine("=== Retry Details ===");
                builder.AppendLine($"Total Attempts: {mrEx.AttemptCount}");
                if (mrEx.AttemptExceptions?.Count > 0)
                {
                    builder.AppendLine($"Failed Attempts: {mrEx.AttemptExceptions.Count}");
                    for (int i = 0; i < Math.Min(mrEx.AttemptExceptions.Count, 5); i++)
                    {
                        var ex = mrEx.AttemptExceptions[i];
                        builder.AppendLine($" Attempt {i + 1}: {ex.GetType().Name} - {ex.Message}");
                    }
                    if (mrEx.AttemptExceptions.Count > 5)
                    {
                        builder.AppendLine($" ... and {mrEx.AttemptExceptions.Count - 5} more exceptions");
                    }
                }
                break;

            case FallbackFailedException ffEx:
                builder.AppendLine("=== Fallback Details ===");
                builder.AppendLine($"Primary Exception: {ffEx.PrimaryException?.GetType().Name ?? "None"}");
                builder.AppendLine($"Fallback Exception: {ffEx.FallbackException?.GetType().Name ?? "None"}");
                break;

            case PipelineExecutionException peEx:
                builder.AppendLine("=== Pipeline Details ===");
                builder.AppendLine($"Execution ID: {peEx.ExecutionId}");
                if (peEx.AppliedPolicies?.Count > 0)
                {
                    builder.AppendLine("Applied Policies:");
                    foreach (var policy in peEx.AppliedPolicies)
                    {
                        builder.AppendLine($" - {policy}");
                    }
                }
                break;
        }

        builder.AppendLine();
        builder.AppendLine("=== End Report ===");

        return builder.ToString();
    }

    /// <summary>
    /// Determines if the exception represents a retryable failure.
    /// </summary>
    /// <param name="exception">The resilience exception to check.</param>
    /// <returns>True if the exception indicates a retryable failure; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="exception"/> parameter is <see langword="null"/>.</exception>
    public static bool IsRetryable(this ResiliencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            CircuitBreakerOpenException => false,                      // Circuit breaker is open - not retryable until timeout passes
            BulkheadRejectedException => true,                        // Bulkhead rejected - retryable after waiting
            OperationTimeoutException => true,                         // Timeout - retryable
            MaxRetriesExceededException => false,                      // Max retries exceeded - not retryable
            InvalidPolicyConfigurationException => false,                // Configuration errors are not retryable
            PipelineExecutionException => true,                        // Pipeline execution errors - retryable if not a terminal state
            FallbackFailedException ffEx => ffEx.PrimaryException is not CircuitBreakerOpenException, // Fallback failed - depends on the underlying exception
            _ => true                                                // Default: retryable
        };
    }

    /// <summary>
    /// Gets a user-friendly display name for the exception type.
    /// </summary>
    /// <param name="exception">The resilience exception.</param>
    /// <returns>A friendly name describing the exception type.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="exception"/> parameter is <see langword="null"/>.</exception>
    public static string GetFriendlyName(this ResiliencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            CircuitBreakerOpenException => "Circuit Breaker Open",
            BulkheadRejectedException => "Bulkhead Rejected",
            OperationTimeoutException => "Timeout Exceeded",
            MaxRetriesExceededException => "Max Retries Exceeded",
            FallbackFailedException => "Fallback Failed",
            InvalidPolicyConfigurationException => "Invalid Configuration",
            PipelineExecutionException => "Pipeline Execution Failed",
            _ => exception.GetType().Name.Replace("Exception", string.Empty)
        };
    }

    /// <summary>
    /// Gets a severity level for the exception based on its type and impact.
    /// </summary>
    /// <param name="exception">The resilience exception.</param>
    /// <returns>A severity level: Critical, High, Medium, or Low.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="exception"/> parameter is <see langword="null"/>.</exception>
    public static string GetSeverityLevel(this ResiliencyException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        return exception switch
        {
            // Critical: Circuit breaker open, configuration errors
            CircuitBreakerOpenException or InvalidPolicyConfigurationException => "Critical",

            // High: Max retries exceeded, fallback failed
            MaxRetriesExceededException or FallbackFailedException => "High",

            // Medium: Timeout, Bulkhead rejected
            OperationTimeoutException or BulkheadRejectedException => "Medium",

            // Default for base exception and others
            _ => "Low"
        };
    }
}