#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;

namespace DotNetResiliencePipeline.Domain;

/// <summary>
// Encapsulates the result of a resilience policy execution with status and metadata.
// </summary>
public sealed class PolicyResult<T>
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }
    /// <summary>
    /// Gets or sets the data resulting from the operation, if successful.
    /// </summary>
    public T? Data { get; set; }
    /// <summary>
    /// Gets or sets the exception that occurred, if any.
    /// </summary>
    public Exception? Exception { get; set; }
    /// <summary>
    /// Gets or sets the name of the policy that produced this result.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }
    /// <summary>
    /// Gets or sets the number of attempts made.
    /// </summary>
    public int AttemptCount { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the operation was executed (UTC).
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets a unique identifier for this execution.
    /// </summary>
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();
    /// <summary>
    /// Gets or sets additional metadata associated with the execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    /// <summary>
    /// Creates a successful result with the provided data.
    /// </summary>
    public static PolicyResult<T> Success(T data, string policyName, long executionTimeMs, int attempts = 1)
    {
        if (string.IsNullOrEmpty(policyName))
            throw new ArgumentException("Policy name cannot be null or empty.", nameof(policyName));

        return new PolicyResult<T>
        {
            IsSuccess = true,
            Data = data,
            PolicyName = policyName,
            ExecutionTimeMs = executionTimeMs,
            AttemptCount = attempts,
            Exception = null
        };
    }

    /// <summary>
    /// Creates a failure result with exception details.
    /// </summary>
    public static PolicyResult<T> Failure(Exception exception, string policyName, long executionTimeMs, int attempts = 1)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception), "Exception cannot be null.");
        if (string.IsNullOrEmpty(policyName))
            throw new ArgumentException("Policy name cannot be null or empty.", nameof(policyName));

        return new PolicyResult<T>
        {
            IsSuccess = false,
            Data = default,
            PolicyName = policyName,
            ExecutionTimeMs = executionTimeMs,
            AttemptCount = attempts,
            Exception = exception
        };
    }

    /// <summary>
    /// Creates a result from a fallback execution.
    /// </summary>
    public static PolicyResult<T> Fallback(T data, Exception fallbackException, string policyName, long executionTimeMs)
    {
        if (string.IsNullOrEmpty(policyName))
            throw new ArgumentException("Policy name cannot be null or empty.", nameof(policyName));

        return new PolicyResult<T>
        {
            IsSuccess = true,
            Data = data,
            PolicyName = policyName,
            ExecutionTimeMs = executionTimeMs,
            Exception = fallbackException,
            Metadata = new() { { "FallbackUsed", true } }
        };
    }

    /// <summary>
    /// Executes a synchronous operation with the result.
    /// </summary>
    public void OnSuccess(Action<T> action)
    {
        if (IsSuccess && Data is not null)
            action(Data);
    }

    /// <summary>
    /// Executes a synchronous operation on failure.
    /// </summary>
    public void OnFailure(Action<Exception> action)
    {
        if (!IsSuccess && Exception is not null)
            action(Exception);
    }

    /// <summary>
    /// Executes a transformation on successful data.
    /// </summary>
    public PolicyResult<TNew> Map<TNew>(Func<T, TNew> mapper)
    {
        if (!IsSuccess || Data is null)
        {
            return PolicyResult<TNew>.Failure(
                Exception ?? new InvalidOperationException("Result is not successful"),
                PolicyName,
                ExecutionTimeMs,
                AttemptCount
            );
        }

        return PolicyResult<TNew>.Success(mapper(Data), PolicyName, ExecutionTimeMs, AttemptCount);
    }
}

/// <summary>
/// Non-generic variant for void operations.
/// </summary>
public sealed class PolicyResult
{
    /// <summary>
    /// Gets or sets a value indicating whether the operation was successful.
    /// </summary>
    public bool IsSuccess { get; set; }
    /// <summary>
    /// Gets or sets the exception that occurred, if any.
    /// </summary>
    public Exception? Exception { get; set; }
    /// <summary>
    /// Gets or sets the name of the policy that produced this result.
    /// </summary>
    public string PolicyName { get; set; } = string.Empty;
    /// <summary>
    /// Gets or sets the execution time in milliseconds.
    /// </summary>
    public long ExecutionTimeMs { get; set; }
    /// <summary>
    /// Gets or sets the number of attempts made.
    /// </summary>
    public int AttemptCount { get; set; }
    /// <summary>
    /// Gets or sets the date and time when the operation was executed (UTC).
    /// </summary>
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    /// <summary>
    /// Gets or sets a unique identifier for this execution.
    /// </summary>
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();
    /// <summary>
    /// Gets or sets additional metadata associated with the execution.
    /// </summary>
    public Dictionary<string, object> Metadata { get; set; } = new();

    public static PolicyResult Success(string policyName, long executionTimeMs, int attempts = 1)
    {
        if (string.IsNullOrEmpty(policyName))
            throw new ArgumentException("Policy name cannot be null or empty.", nameof(policyName));

        return new PolicyResult
        {
            IsSuccess = true,
            PolicyName = policyName,
            ExecutionTimeMs = executionTimeMs,
            AttemptCount = attempts,
            Exception = null
        };
    }

    public static PolicyResult Failure(Exception exception, string policyName, long executionTimeMs, int attempts = 1)
    {
        if (exception == null)
            throw new ArgumentNullException(nameof(exception), "Exception cannot be null.");
        if (string.IsNullOrEmpty(policyName))
            throw new ArgumentException("Policy name cannot be null or empty.", nameof(policyName));

        return new PolicyResult
        {
            IsSuccess = false,
            PolicyName = policyName,
            ExecutionTimeMs = executionTimeMs,
            AttemptCount = attempts,
            Exception = exception
        };
    }
}