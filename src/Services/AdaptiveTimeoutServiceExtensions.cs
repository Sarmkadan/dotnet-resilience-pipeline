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
/// Extension methods for <see cref="AdaptiveTimeoutService"/> that provide additional functionality
/// for working with adaptive timeout policies.
/// </summary>
public static class AdaptiveTimeoutServiceExtensions
{
    /// <summary>
    /// Executes an operation with a custom timeout multiplier applied to the adaptive timeout.
    /// </summary>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="service">The adaptive timeout service instance.</param>
    /// <param name="policy">Adaptive timeout policy governing this execution.</param>
    /// <param name="operation">Async operation to execute.</param>
    /// <param name="timeoutMultiplier">Multiplier to apply to the current adaptive timeout (e.g., 1.5 for 50% longer).</param>
    /// <param name="cancellationToken">Optional caller-supplied cancellation token.</param>
    /// <returns>The operation result.</returns>
    /// <exception cref="OperationTimeoutException">Thrown when the policy timeout elapses before the operation completes.</exception>
    public static async Task<T> ExecuteAsync<T>(
        this AdaptiveTimeoutService service,
        AdaptiveTimeoutPolicy policy,
        Func<CancellationToken, Task<T>> operation,
        double timeoutMultiplier,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        if (timeoutMultiplier <= 0)
            throw new ArgumentOutOfRangeException(nameof(timeoutMultiplier), "Timeout multiplier must be positive");

        var customTimeout = TimeSpan.FromTicks((long)(service.GetCurrentTimeout(policy).Ticks * timeoutMultiplier));

        using var timeoutCts = new CancellationTokenSource(customTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await operation(linkedCts.Token);
            stopwatch.Stop();

            policy.RecordExecutionTime(stopwatch.ElapsedMilliseconds);
            policy.RecordSuccess();

            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested
                                               && !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            policy.RecordTimeout(elapsed);
            throw new OperationTimeoutException(policy.Name, customTimeout, elapsed);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            policy.RecordExecutionTime(stopwatch.ElapsedMilliseconds);
            policy.RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Executes an operation with a minimum timeout guarantee, ensuring the timeout never falls below a specified value.
    /// </summary>
    /// <typeparam name="T">Return type of the operation.</typeparam>
    /// <param name="service">The adaptive timeout service instance.</param>
    /// <param name="policy">Adaptive timeout policy governing this execution.</param>
    /// <param name="operation">Async operation to execute.</param>
    /// <param name="minimumTimeout">Minimum timeout duration to enforce.</param>
    /// <param name="cancellationToken">Optional caller-supplied cancellation token.</param>
    /// <returns>The operation result.</returns>
    /// <exception cref="OperationTimeoutException">Thrown when the policy timeout elapses before the operation completes.</exception>
    public static async Task<T> ExecuteWithMinimumTimeoutAsync<T>(
        this AdaptiveTimeoutService service,
        AdaptiveTimeoutPolicy policy,
        Func<CancellationToken, Task<T>> operation,
        TimeSpan minimumTimeout,
        CancellationToken cancellationToken = default)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (operation is null)
            throw new ArgumentNullException(nameof(operation));

        var currentTimeout = service.GetCurrentTimeout(policy);
        var effectiveTimeout = currentTimeout < minimumTimeout ? minimumTimeout : currentTimeout;

        using var timeoutCts = new CancellationTokenSource(effectiveTimeout);
        using var linkedCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken, timeoutCts.Token);

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await operation(linkedCts.Token);
            stopwatch.Stop();

            policy.RecordExecutionTime(stopwatch.ElapsedMilliseconds);
            policy.RecordSuccess();

            return result;
        }
        catch (OperationCanceledException) when (timeoutCts.Token.IsCancellationRequested
                                               && !cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            var elapsed = stopwatch.ElapsedMilliseconds;

            policy.RecordTimeout(elapsed);
            throw new OperationTimeoutException(policy.Name, effectiveTimeout, elapsed);
        }
        catch (Exception)
        {
            stopwatch.Stop();
            policy.RecordExecutionTime(stopwatch.ElapsedMilliseconds);
            policy.RecordFailure();
            throw;
        }
    }

    /// <summary>
    /// Gets the current timeout and formats it as a human-readable string with units.
    /// </summary>
    /// <param name="service">The adaptive timeout service instance.</param>
    /// <param name="policy">Adaptive timeout policy to query.</param>
    /// <returns>Formatted timeout string (e.g., "1.5s", "500ms", "2.3s").</returns>
    public static string GetCurrentTimeoutString(this AdaptiveTimeoutService service, AdaptiveTimeoutPolicy policy)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        var timeout = service.GetCurrentTimeout(policy);
        return FormatTimeout(timeout);
    }

    /// <summary>
    /// Gets a simplified adaptation summary containing only the most critical metrics for quick decision making.
    /// </summary>
    /// <param name="service">The adaptive timeout service instance.</param>
    /// <param name="policy">Adaptive timeout policy to query.</param>
    /// <returns>Dictionary with key metrics: PolicyName, CurrentTimeoutMs, SuccessRate, TimeoutPercentage.</returns>
    public static Dictionary<string, object> GetCriticalAdaptationSummary(this AdaptiveTimeoutService service, AdaptiveTimeoutPolicy policy)
    {
        if (service is null)
            throw new ArgumentNullException(nameof(service));

        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        var fullSummary = service.GetAdaptationSummary(policy);

        return new Dictionary<string, object>(StringComparer.Ordinal)
        {
            { "PolicyName", fullSummary["PolicyName"] },
            { "CurrentTimeoutMs", fullSummary["CurrentTimeoutMs"] },
            { "SuccessRate", fullSummary["SuccessRate"] },
            { "TimeoutPercentage", fullSummary["TimeoutPercentage"] }
        };
    }

    /// <summary>
    /// Formats a TimeSpan as a human-readable timeout string.
    /// </summary>
    private static string FormatTimeout(TimeSpan timeout)
    {
        if (timeout.TotalMilliseconds < 1)
            return "0ms";

        if (timeout.TotalMilliseconds < 1000)
            return $"{timeout.TotalMilliseconds:F0}ms";

        if (timeout.TotalSeconds < 60)
            return $"{timeout.TotalSeconds:F1}s";

        return $"{timeout.TotalMinutes:F1}m";
    }
}