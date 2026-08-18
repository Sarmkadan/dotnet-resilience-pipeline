#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;

namespace DotNetResiliencePipeline.Domain;

/// <summary>
// Encapsulates the result of a resilience policy execution with status and metadata.
/// </summary>
public sealed class PolicyResult<T>
{
    public bool IsSuccess { get; set; }
    public T? Data { get; set; }
    public Exception? Exception { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public long ExecutionTimeMs { get; set; }
    public int AttemptCount { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();
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
    public bool IsSuccess { get; set; }
    public Exception? Exception { get; set; }
    public string PolicyName { get; set; } = string.Empty;
    public long ExecutionTimeMs { get; set; }
    public int AttemptCount { get; set; }
    public DateTime ExecutedAt { get; set; } = DateTime.UtcNow;
    public string ExecutionId { get; set; } = Guid.NewGuid().ToString();
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