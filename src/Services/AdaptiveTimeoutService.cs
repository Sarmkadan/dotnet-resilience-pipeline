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
    private readonly TimeoutService _timeoutService;

    /// <summary>
    /// Initializes the service with the required logger.
    /// </summary>
    public AdaptiveTimeoutService(ILogger<AdaptiveTimeoutService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _timeoutService = new TimeoutService();
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

        // Log start of execution
        _logger.LogInformation("Starting execution of adaptive timeout policy {PolicyName}", policy.Name);

        // Use the common TimeoutService with the adaptive timeout policy
        var result = await _timeoutService.ExecuteAsync(policy, operation, cancellationToken);

        // Log additional adaptive-specific information
        _logger.LogDebug(
            "AdaptiveTimeout '{PolicyName}' completed in {{ElapsedMs}}ms (limit={{TimeoutMs}}ms)",
            policy.Name, _timeoutService.GetTimeoutMilliseconds(policy));

        // Log end of execution
        _logger.LogInformation("Finished execution of adaptive timeout policy {PolicyName}", policy.Name);

        return result;
    }

    /// <summary>
    /// Returns the current effective timeout for the given policy.
    /// </summary>
    public TimeSpan GetCurrentTimeout(AdaptiveTimeoutPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        _logger.LogInformation("Getting current timeout for policy {PolicyName}", policy.Name);
        var timeout = policy.CurrentTimeout;
        _logger.LogInformation("Returning current timeout {Timeout} for policy {PolicyName}", timeout, policy.Name);
        return timeout;
    }

    /// <summary>
    /// Returns a key-value summary of the policy's current adaptation state, suitable for logging or dashboards.
    /// </summary>
    public Dictionary<string, object> GetAdaptationSummary(AdaptiveTimeoutPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        _logger.LogInformation("Getting adaptation summary for policy {PolicyName}", policy.Name);
        var summary = new Dictionary<string, object>
        {
            { "PolicyName", policy.Name },
            { "CurrentTimeoutMs", policy.CurrentTimeout.TotalMilliseconds },
            { "InitialTimeoutMs", policy.InitialTimeout.TotalMilliseconds },
            { "TargetPercentile", policy.TargetPercentile },
            { "TotalAdjustments", policy.TotalAdjustments },
            { "LastAdjustmentAt", policy.LastAdjustmentAt },
            { "TimeoutCount", policy.TimeoutCount },
            { "TimeoutPercentage", policy.GetTimeoutPercentage() },
            { "P95ExecutionTimeMs", policy.GetPercentileExecutionTime(95) },
            { "SuccessRate", policy.GetSuccessRate() },
            { "TotalExecutions", policy.TotalExecutions }
        };
        _logger.LogInformation("Returning adaptation summary for policy {PolicyName}", policy.Name);
        return summary;
    }
}