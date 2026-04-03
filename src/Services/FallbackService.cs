#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using DotNetResiliencePipeline.Domain;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Service handling fallback policy execution for graceful degradation.
/// </summary>
public sealed class FallbackService
{
    /// <summary>
    /// Executes an operation with fallback support.
    /// </summary>
    public async Task<PolicyResult<T>> ExecuteAsync<T>(
        FallbackPolicy policy,
        Exception primaryException,
        long primaryExecutionTimeMs,
        CancellationToken cancellationToken)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (!policy.IsValidConfiguration(out var error))
            throw new InvalidPolicyConfigurationException(policy.Name, error ?? "Invalid fallback configuration");

        if (!policy.IsEnabled || !policy.ShouldTriggerFallback(primaryException))
        {
            return PolicyResult<T>.Failure(primaryException, policy.Name, primaryExecutionTimeMs);
        }

        var fallbackAction = policy.GetFallbackAction();
        if (fallbackAction is null)
        {
            // If no fallback action is set, just re-throw the primary exception or indicate no fallback configured
            throw primaryException;
        }

        var fallbackStopwatch = Stopwatch.StartNew();
        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(policy.FallbackTimeout);

        try
        {
            // Execute the stored fallback action
            object? rawFallbackResult = await fallbackAction(cts.Token);
            T fallbackResult = (T)rawFallbackResult!; // Cast to the expected return type

            policy.RecordSuccessfulFallback(fallbackStopwatch.ElapsedMilliseconds);

            return PolicyResult<T>.Fallback(fallbackResult, primaryException, policy.Name, primaryExecutionTimeMs);
        }
        catch (Exception fallbackException)
        {
            fallbackStopwatch.Stop();
            policy.RecordFailedFallback(fallbackException, fallbackStopwatch.ElapsedMilliseconds);

            throw new FallbackFailedException(policy.Name, primaryException, fallbackException);
        }
        finally
        {
            cts?.Dispose();
        }
    }

    /// <summary>
    /// Determines if fallback should be triggered for an exception.
    /// </summary>
    public bool ShouldTriggerFallback(FallbackPolicy policy, Exception exception)
    {
        return policy?.ShouldTriggerFallback(exception) ?? false;
    }

    /// <summary>
    /// Gets the success rate of fallback executions.
    /// </summary>
    public double GetFallbackSuccessRate(FallbackPolicy policy)
    {
        return policy?.GetFallbackSuccessRate() ?? 0;
    }

    /// <summary>
    /// Adds an exception type that triggers fallback.
    /// </summary>
    public void AddFallbackTrigger(FallbackPolicy policy, Type exceptionType)
    {
        policy?.AddFallbackTrigger(exceptionType);
    }

    /// <summary>
    /// Removes an exception type from fallback triggers.
    /// </summary>
    public void RemoveFallbackTrigger(FallbackPolicy policy, Type exceptionType)
    {
        policy?.RemoveFallbackTrigger(exceptionType);
    }
}
