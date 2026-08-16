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
        ITimeoutStrategy timeoutStrategy,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (timeoutStrategy is null)
            throw new ArgumentNullException(nameof(timeoutStrategy));

        if (!timeoutStrategy.IsValidConfiguration(out var error))
            throw new InvalidPolicyConfigurationException(timeoutStrategy.Name, error ?? "Invalid timeout configuration");

        if (!timeoutStrategy.IsEnabled)
            return await operation(cancellationToken);

        var effectiveTimeout = timeoutStrategy.GetTimeout();
        var stopwatch = Stopwatch.StartNew();
        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        try
        {
            var result = await operation(cancellationToken);
            stopwatch.Stop();

            timeoutStrategy.RecordExecutionTime(stopwatch.ElapsedMilliseconds);
            timeoutStrategy.RecordSuccess();

            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.IsCancellationRequested && !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            var timeoutMs = stopwatch.ElapsedMilliseconds;

            timeoutStrategy.RecordTimeout(timeoutMs);

            throw new OperationTimeoutException(
                timeoutStrategy.Name,
                effectiveTimeout,
                timeoutMs);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();

            // External caller cancellation - rethrow without recording as timeout
            throw;
        }
        catch (Exception)
        {
            stopwatch.Stop();
            timeoutStrategy.RecordExecutionTime(stopwatch.ElapsedMilliseconds);
            timeoutStrategy.RecordFailure();

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
    public bool HasExceededTimeout(ITimeoutStrategy timeoutStrategy, long executionTimeMs)
    {
        if (timeoutStrategy is not TimeoutPolicy timeoutPolicy)
            return false;

        return timeoutPolicy.IsTimedOutMs(executionTimeMs);
    }

    /// <summary>
    /// Gets timeout configuration in milliseconds.
    /// </summary>
    /// <remarks>
    /// Uses <see cref="TimeSpan.TotalMilliseconds"/> to return the full duration,
    /// not <see cref="TimeSpan.Milliseconds"/> which only returns the ms component (0-999).
    /// </remarks>
    public long GetTimeoutMilliseconds(ITimeoutStrategy timeoutStrategy)
    {
        return (long)(timeoutStrategy?.GetTimeout().TotalMilliseconds ?? 0);
    }
}
