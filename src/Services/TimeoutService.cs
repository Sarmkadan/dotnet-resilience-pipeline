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
public class TimeoutService
{
    /// <summary>
    /// Executes an operation with a timeout constraint.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        TimeoutPolicy policy,
        Func<CancellationToken, Task<T>> operation)
    {
        if (policy == null)
            throw new ArgumentNullException(nameof(policy));

        if (!policy.IsValidConfiguration(out var error))
            throw new InvalidPolicyConfigurationException(policy.Name, error ?? "Invalid timeout configuration");

        if (!policy.IsEnabled)
            return await operation(CancellationToken.None);

        var stopwatch = Stopwatch.StartNew();
        var cts = new CancellationTokenSource(policy.Timeout);

        try
        {
            var result = await operation(cts.Token);
            stopwatch.Stop();

            policy.RecordExecutionTime(stopwatch.ElapsedMilliseconds);
            policy.RecordSuccess();

            return result;
        }
        catch (OperationCanceledException ex) when (cts.Token.IsCancellationRequested)
        {
            stopwatch.Stop();
            var timeoutMs = stopwatch.ElapsedMilliseconds;

            policy.RecordTimeout(timeoutMs);

            throw new OperationTimeoutException(
                policy.Name,
                policy.Timeout,
                timeoutMs);
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
            cts?.Dispose();
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
    public long GetTimeoutMilliseconds(TimeoutPolicy policy)
    {
        return policy?.Timeout.Milliseconds ?? 0;
    }
}
