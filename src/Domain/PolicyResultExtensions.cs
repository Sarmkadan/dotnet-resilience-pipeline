#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Runtime.ExceptionServices;

namespace DotNetResiliencePipeline.Domain;

/// <summary>
/// Provides extension methods for <see cref="PolicyResult{T}"/> and <see cref="PolicyResult"/> for reading values,
/// chaining side effects and rethrowing failures.
/// </summary>
/// <remarks>
/// The <c>WhenSuccess</c>/<c>WhenFailure</c> names are used instead of <c>OnSuccess</c>/<c>OnFailure</c> because
/// <see cref="PolicyResult{T}"/> already defines those as instance methods returning <c>void</c>; an extension with the
/// same signature would be shadowed and could not be chained.
/// </remarks>
public static class PolicyResultExtensions
{
    /// <summary>
    /// Returns the result data when the operation succeeded and carries a value; otherwise returns <paramref name="fallback"/>.
    /// </summary>
    /// <typeparam name="T">The type of the result data.</typeparam>
    /// <param name="result">The policy result to read from.</param>
    /// <param name="fallback">The value returned when the result is a failure or has no data.</param>
    /// <returns>The result data, or <paramref name="fallback"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> is null.</exception>
    public static T? GetValueOrDefault<T>(this PolicyResult<T> result, T? fallback)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.IsSuccess && result.Data is not null ? result.Data : fallback;
    }

    /// <summary>
    /// Invokes <paramref name="action"/> with the result data when the operation succeeded and carries a value,
    /// then returns the same result so calls can be chained.
    /// </summary>
    /// <typeparam name="T">The type of the result data.</typeparam>
    /// <param name="result">The policy result to inspect.</param>
    /// <param name="action">The action to run with the result data.</param>
    /// <returns>The same <paramref name="result"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> or <paramref name="action"/> is null.</exception>
    public static PolicyResult<T> WhenSuccess<T>(this PolicyResult<T> result, Action<T> action)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        if (result.IsSuccess && result.Data is not null)
        {
            action(result.Data);
        }

        return result;
    }

    /// <summary>
    /// Invokes <paramref name="action"/> with the exception when the operation failed and recorded one,
    /// then returns the same result so calls can be chained.
    /// </summary>
    /// <typeparam name="T">The type of the result data.</typeparam>
    /// <param name="result">The policy result to inspect.</param>
    /// <param name="action">The action to run with the failure exception.</param>
    /// <returns>The same <paramref name="result"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> or <paramref name="action"/> is null.</exception>
    public static PolicyResult<T> WhenFailure<T>(this PolicyResult<T> result, Action<Exception> action)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        if (!result.IsSuccess && result.Exception is not null)
        {
            action(result.Exception);
        }

        return result;
    }

    /// <summary>
    /// Throws the recorded exception when the operation failed, preserving its original stack trace.
    /// </summary>
    /// <typeparam name="T">The type of the result data.</typeparam>
    /// <param name="result">The policy result to check.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the result is a failure without a recorded exception.</exception>
    public static void ThrowIfFailed<T>(this PolicyResult<T> result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsSuccess)
        {
            RethrowFailure(result.Exception, result.PolicyName);
        }
    }

    /// <summary>
    /// Invokes <paramref name="action"/> with the exception when the operation failed and recorded one,
    /// then returns the same result so calls can be chained.
    /// </summary>
    /// <param name="result">The policy result to inspect.</param>
    /// <param name="action">The action to run with the failure exception.</param>
    /// <returns>The same <paramref name="result"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> or <paramref name="action"/> is null.</exception>
    public static PolicyResult WhenFailure(this PolicyResult result, Action<Exception> action)
    {
        ArgumentNullException.ThrowIfNull(result);
        ArgumentNullException.ThrowIfNull(action);

        if (!result.IsSuccess && result.Exception is not null)
        {
            action(result.Exception);
        }

        return result;
    }

    /// <summary>
    /// Throws the recorded exception when the operation failed, preserving its original stack trace.
    /// </summary>
    /// <param name="result">The policy result to check.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="result"/> is null.</exception>
    /// <exception cref="InvalidOperationException">Thrown if the result is a failure without a recorded exception.</exception>
    public static void ThrowIfFailed(this PolicyResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!result.IsSuccess)
        {
            RethrowFailure(result.Exception, result.PolicyName);
        }
    }

    private static void RethrowFailure(Exception? exception, string policyName)
    {
        var failure = exception ?? new InvalidOperationException($"Policy '{policyName}' failed without an exception.");

        ExceptionDispatchInfo.Capture(failure).Throw();
    }
}
