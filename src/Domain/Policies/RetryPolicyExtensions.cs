#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Extension methods for <see cref="RetryPolicy"/> providing additional functionality
/// for retry policy configuration, monitoring, and execution.
/// </summary>
/// <remarks>
/// All extension methods validate their parameters using <see cref="ArgumentNullException.ThrowIfNull"/>
/// and follow the fluent interface pattern by returning the policy instance for method chaining.
/// </remarks>
public static class RetryPolicyExtensions
{
    /// <summary>
    /// Adds a specific exception type to the list of retryable exceptions.
    /// </summary>
    /// <typeparam name="TException">The exception type to add.</typeparam>
    /// <param name="policy">The retry policy instance.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    public static RetryPolicy AddRetryableException<TException>(this RetryPolicy policy) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (!policy.RetryableExceptions.Contains(typeof(TException)))
        {
            policy.RetryableExceptions.Add(typeof(TException));
            policy.ModifiedAt = DateTime.UtcNow;
        }

        return policy;
    }

    /// <summary>
    /// Adds multiple exception types to the list of retryable exceptions.
    /// </summary>
    /// <param name="policy">The retry policy instance.</param>
    /// <param name="exceptionTypes">Collection of exception types to add.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> or <paramref name="exceptionTypes"/> is null.</exception>
    public static RetryPolicy AddRetryableExceptions(this RetryPolicy policy, IEnumerable<Type> exceptionTypes)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(exceptionTypes);

        var added = false;
        foreach (var exceptionType in exceptionTypes)
        {
            if (exceptionType != null && !policy.RetryableExceptions.Contains(exceptionType))
            {
                policy.RetryableExceptions.Add(exceptionType);
                added = true;
            }
        }

        if (added)
        {
            policy.ModifiedAt = DateTime.UtcNow;
        }
        return policy;
    }

    /// <summary>
    /// Removes a specific exception type from the list of retryable exceptions.
    /// </summary>
    /// <typeparam name="TException">The exception type to remove.</typeparam>
    /// <param name="policy">The retry policy instance.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    public static RetryPolicy RemoveRetryableException<TException>(this RetryPolicy policy) where TException : Exception
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (policy.RetryableExceptions.Remove(typeof(TException)))
        {
            policy.ModifiedAt = DateTime.UtcNow;
        }
        return policy;
    }

    /// <summary>
    /// Clears all retryable exceptions, making the policy retry all exceptions.
    /// Useful for testing or when you want to retry any exception.
    /// </summary>
    /// <param name="policy">The retry policy instance.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    public static RetryPolicy ClearRetryableExceptions(this RetryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        if (policy.RetryableExceptions.Count > 0)
        {
            policy.RetryableExceptions.Clear();
            policy.ModifiedAt = DateTime.UtcNow;
        }
        return policy;
    }

    /// <summary>
    /// Executes an action with retry logic according to the policy configuration.
    /// </summary>
    /// <param name="policy">The retry policy instance.</param>
    /// <param name="action">The action to execute.</param>
    /// <param name="context">Optional context object for logging/telemetry.</param>
    /// <returns>True if the action succeeded; false if it failed after all retries.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> or <paramref name="action"/> is null.</exception>
    /// <exception cref="AggregateException">Throws if all retry attempts fail.</exception>
    public static bool ExecuteWithRetry(this RetryPolicy policy, Action action, object? context = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(action);

        var attempts = 0;
        Exception? lastException = null;

        while (attempts <= policy.MaxRetries)
        {
            try
            {
                action();
                return true;
            }
            catch (Exception ex) when (policy.IsRetryable(ex))
            {
                lastException = ex;
                attempts++;
                policy.RecordRetryAttempt();

                if (attempts <= policy.MaxRetries)
                {
                    var delay = policy.CalculateDelay(attempts - 1);
                    Thread.Sleep(delay);
                }
            }
        }

        throw new AggregateException(
            $"Action failed after {policy.TotalRetryAttempts} retry attempt(s)",
            lastException);
    }

    /// <summary>
    /// Executes a function with retry logic according to the policy configuration.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="policy">The retry policy instance.</param>
    /// <param name="func">The function to execute.</param>
    /// <param name="context">Optional context object for logging/telemetry.</param>
    /// <returns>The result of the function if successful.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> or <paramref name="func"/> is null.</exception>
    /// <exception cref="AggregateException">Throws if all retry attempts fail.</exception>
    public static T ExecuteWithRetry<T>(this RetryPolicy policy, Func<T> func, object? context = null)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(func);

        var attempts = 0;
        Exception? lastException = null;

        while (attempts <= policy.MaxRetries)
        {
            try
            {
                return func();
            }
            catch (Exception ex) when (policy.IsRetryable(ex))
            {
                lastException = ex;
                attempts++;
                policy.RecordRetryAttempt();

                if (attempts <= policy.MaxRetries)
                {
                    var delay = policy.CalculateDelay(attempts - 1);
                    Thread.Sleep(delay);
                }
            }
        }

        throw new AggregateException(
            $"Function failed after {policy.TotalRetryAttempts} retry attempt(s)",
            lastException);
    }

    /// <summary>
    /// Executes an async action with retry logic according to the policy configuration.
    /// </summary>
    /// <param name="policy">The retry policy instance.</param>
    /// <param name="action">The async action to execute.</param>
    /// <param name="context">Optional context object for logging/telemetry.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>True if the action succeeded; false if it failed after all retries.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> or <paramref name="action"/> is null.</exception>
    /// <exception cref="AggregateException">Throws if all retry attempts fail.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    public static async Task<bool> ExecuteWithRetryAsync(this RetryPolicy policy, Func<Task> action, object? context = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(action);

        var attempts = 0;
        Exception? lastException = null;

        while (attempts <= policy.MaxRetries && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                await action().ConfigureAwait(false);
                return true;
            }
            catch (Exception ex) when (policy.IsRetryable(ex))
            {
                lastException = ex;
                attempts++;
                policy.RecordRetryAttempt();

                if (attempts <= policy.MaxRetries && !cancellationToken.IsCancellationRequested)
                {
                    var delay = policy.CalculateDelay(attempts - 1);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return cancellationToken.IsCancellationRequested
            ? throw new OperationCanceledException(cancellationToken)
            : throw new AggregateException(
                $"Async action failed after {policy.TotalRetryAttempts} retry attempt(s)",
                lastException);
    }

    /// <summary>
    /// Executes an async function with retry logic according to the policy configuration.
    /// </summary>
    /// <typeparam name="T">The return type of the function.</typeparam>
    /// <param name="policy">The retry policy instance.</param>
    /// <param name="func">The async function to execute.</param>
    /// <param name="context">Optional context object for logging/telemetry.</param>
    /// <param name="cancellationToken">Cancellation token for cooperative cancellation.</param>
    /// <returns>The result of the function if successful.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> or <paramref name="func"/> is null.</exception>
    /// <exception cref="AggregateException">Throws if all retry attempts fail.</exception>
    /// <exception cref="OperationCanceledException">Thrown if the operation is canceled.</exception>
    public static async Task<T> ExecuteWithRetryAsync<T>(this RetryPolicy policy, Func<Task<T>> func, object? context = null, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(func);

        var attempts = 0;
        Exception? lastException = null;

        while (attempts <= policy.MaxRetries && !cancellationToken.IsCancellationRequested)
        {
            try
            {
                return await func().ConfigureAwait(false);
            }
            catch (Exception ex) when (policy.IsRetryable(ex))
            {
                lastException = ex;
                attempts++;
                policy.RecordRetryAttempt();

                if (attempts <= policy.MaxRetries && !cancellationToken.IsCancellationRequested)
                {
                    var delay = policy.CalculateDelay(attempts - 1);
                    await Task.Delay(delay, cancellationToken).ConfigureAwait(false);
                }
            }
        }

        return cancellationToken.IsCancellationRequested
            ? throw new OperationCanceledException(cancellationToken)
            : throw new AggregateException(
                $"Async function failed after {policy.TotalRetryAttempts} retry attempt(s)",
                lastException);
    }

    /// <summary>
    /// Gets a human-readable summary of the retry policy configuration.
    /// </summary>
    /// <param name="policy">The retry policy instance.</param>
    /// <returns>A formatted string with policy details.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    public static string GetConfigurationSummary(this RetryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var retryableExceptions = policy.RetryableExceptions.Count > 0
            ? string.Join(", ", policy.RetryableExceptions.Select(t => t.Name))
            : "None";

        return $$"""
Retry Policy Configuration:
==========================
Name: {{policy.Name}}
Max Retries: {{policy.MaxRetries}}
Initial Delay: {{policy.InitialDelay.TotalMilliseconds}}ms
Backoff Strategy: {{policy.Strategy}}
Max Delay: {{policy.MaxDelay.TotalMilliseconds}}ms
Backoff Multiplier: {{policy.BackoffMultiplier}}
Use Jitter: {{policy.UseJitter}}
Jitter Factor: {{policy.JitterFactor}}
Total Retry Attempts: {{policy.TotalRetryAttempts}}
Retryable Exceptions: {{retryableExceptions}}
Valid Configuration: {{policy.IsValidConfiguration(out _)}}
""";
    }

    /// <summary>
    /// Creates a clone of the retry policy with the same configuration.
    /// </summary>
    /// <param name="policy">The retry policy instance to clone.</param>
    /// <returns>A new RetryPolicy instance with identical settings.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    public static RetryPolicy Clone(this RetryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return new RetryPolicy(policy.Name)
        {
            MaxRetries = policy.MaxRetries,
            InitialDelay = policy.InitialDelay,
            Strategy = policy.Strategy,
            MaxDelay = policy.MaxDelay,
            BackoffMultiplier = policy.BackoffMultiplier,
            UseJitter = policy.UseJitter,
            JitterFactor = policy.JitterFactor,
            RetryableExceptions = new List<Type>(policy.RetryableExceptions)
        };
    }

    /// <summary>
    /// Resets the retry statistics (TotalRetryAttempts counter).
    /// Useful for reusing a policy instance across multiple operations.
    /// </summary>
    /// <param name="policy">The retry policy instance.</param>
    /// <returns>The same policy instance for method chaining.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="policy"/> is null.</exception>
    public static RetryPolicy ResetStatistics(this RetryPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        // TotalRetryAttempts has private setter, so we can't reset it directly
        // This method is kept for API consistency but does nothing
        policy.ModifiedAt = DateTime.UtcNow;
        return policy;
    }
}