#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using Microsoft.Extensions.Logging;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Service that executes operations under an <see cref="AdaptiveTimeoutPolicy"/>, automatically
/// adjusting the timeout ceiling based on observed response-time percentiles.
/// </summary>
public sealed class AdaptiveTimeoutService
{
    private readonly ILogger<AdaptiveTimeoutService> _logger;

    /// <summary>
    /// Initializes the service with the required logger.
    /// </summary>
    public AdaptiveTimeoutService(ILogger<AdaptiveTimeoutService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
    }

    /// <summary>
    /// Executes an operation subject to the current adaptive timeout of the policy.
    /// </summary>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="policy">Adaptive timeout policy governing this execution.</param>
    /// <param name="operation">Async operation to execute.</param>
    /// <param name="cancellationToken">Optional caller-supplied cancellation token.</param>
    /// <returns>The operation result.</returns>
    /// <exception cref="OperationTimeoutException">Thrown when the policy timeout elapses before the operation completes.</exception>
    public async Task<T> ExecuteAsync<T>(
        AdaptiveTimeoutPolicy policy,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        if (!policy.IsValidConfiguration(out var error))
            throw new InvalidPolicyConfigurationException(policy.Name, error ?? "Invalid adaptive timeout configuration");

        if (!policy.IsEnabled)
            return await operation(cancellationToken);

        var effectiveTimeout = policy.CurrentTimeout;

        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await operation(linkedCts.Token);
            stopwatch.Stop();

            policy.RecordExecutionTime(stopwatch.ElapsedMilliseconds);
            policy.RecordSuccess();

            _logger.LogDebug(
                "AdaptiveTimeout '{PolicyName}' completed in {ElapsedMs}ms (limit={TimeoutMs}ms)",
                policy.Name, stopwatch.ElapsedMilliseconds, effectiveTimeout.TotalMilliseconds);

            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested
                                                 && !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            policy.RecordTimeout(elapsed);

            _logger.LogWarning(
                "AdaptiveTimeout '{PolicyName}' timed out after {ElapsedMs}ms (limit={TimeoutMs}ms)",
                policy.Name, elapsed, effectiveTimeout.TotalMilliseconds);

            throw new OperationTimeoutException(policy.Name, effectiveTimeout, elapsed);
        }
        catch (Exception ex)
        {
            stopwatch.Stop();

            policy.RecordExecutionTime(stopwatch.ElapsedMilliseconds);
            policy.RecordFailure();

            _logger.LogError(ex,
                "AdaptiveTimeout '{PolicyName}' execution failed after {ElapsedMs}ms",
                policy.Name, stopwatch.ElapsedMilliseconds);

            throw;
        }
    }

    /// <summary>
    /// Returns the current effective timeout for the given policy.
    /// </summary>
    public TimeSpan GetCurrentTimeout(AdaptiveTimeoutPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        return policy.CurrentTimeout;
    }

    /// <summary>
    /// Returns a key-value summary of the policy's current adaptation state, suitable for logging or dashboards.
    /// </summary>
    public Dictionary<string, object> GetAdaptationSummary(AdaptiveTimeoutPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        return new Dictionary<string, object>
        {
            { "PolicyName",          policy.Name },
            { "CurrentTimeoutMs",    policy.CurrentTimeout.TotalMilliseconds },
            { "InitialTimeoutMs",    policy.InitialTimeout.TotalMilliseconds },
            { "TargetPercentile",    policy.TargetPercentile },
            { "TotalAdjustments",    policy.TotalAdjustments },
            { "LastAdjustmentAt",    policy.LastAdjustmentAt },
            { "TimeoutCount",        policy.TimeoutCount },
            { "TimeoutPercentage",   policy.GetTimeoutPercentage() },
            { "P95ExecutionTimeMs",  policy.GetPercentileExecutionTime(95) },
            { "SuccessRate",         policy.GetSuccessRate() },
            { "TotalExecutions",     policy.TotalExecutions }
        };
    }
}
