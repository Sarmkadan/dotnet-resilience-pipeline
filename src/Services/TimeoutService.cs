#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Service handling timeout policy execution with configurable limits.
/// </summary>
public sealed class TimeoutService
{
    /// <summary>
    /// Executes an operation with a timeout constraint.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        TimeoutPolicy policy,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (!policy.IsValidConfiguration(out var error))
            throw new InvalidPolicyConfigurationException(policy.Name, error ?? "Invalid timeout configuration");

        if (!policy.IsEnabled)
            return await operation(cancellationToken);

        var stopwatch = Stopwatch.StartNew();
        using var timeoutCts = new CancellationTokenSource(policy.Timeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var result = await operation(linkedCts.Token);
            stopwatch.Stop();

            policy.RecordExecutionTime(stopwatch.ElapsedMilliseconds);
            policy.RecordSuccess();

            return result;
        }
        catch (OperationCanceledException ex) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            var timeoutMs = stopwatch.ElapsedMilliseconds;

            policy.RecordTimeout(timeoutMs);

            throw new OperationTimeoutException(
                policy.Name,
                policy.Timeout,
                timeoutMs);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            // External caller cancellation - rethrow without recording as timeout
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            policy.RecordExecutionTime(stopwatch.ElapsedMilliseconds);
            policy.RecordFailure();

            throw;
        }
        finally
        {
            stopwatch.Stop();
        }
    }

    /// <summary>
    /// Validates if an execution time exceeds the timeout.
    /// </summary>
    public bool HasExceededTimeout(TimeoutPolicy policy, long executionTimeMs)
    {
        return policy?.IsTimedOutMs(executionTimeMs) ?? false;
    }

    /// <summary>
    /// Gets timeout configuration in milliseconds.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="TimeSpan.TotalMilliseconds"/> to return the full duration,
    /// not <see cref="TimeSpan.Milliseconds"/> which only returns the ms component (0-999).
    /// </remarks>
    public long GetTimeoutMilliseconds(TimeoutPolicy policy)
    {
        return (long)(policy?.Timeout.TotalMilliseconds ?? 0);
    }
}