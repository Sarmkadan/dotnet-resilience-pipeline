#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Convenience extension methods for <see cref="ResiliencyPipelineService"/> that wrap common
/// read and execute patterns without changing the orchestrator itself.
/// </summary>
/// <remarks>
/// These methods only call public members of <see cref="ResiliencyPipelineService"/>, so they inherit its
/// thread-safety guarantees. They do not add state of their own.
/// </remarks>
public static class ResiliencyPipelineServiceExtensions
{
    /// <summary>
    /// Executes an operation through the pipeline and returns <paramref name="fallback"/> instead of throwing
    /// or returning a failed result when the execution does not succeed.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="service">The pipeline that executes the operation.</param>
    /// <param name="operation">The operation to execute. Receives the cancellation token.</param>
    /// <param name="fallback">The value returned when the execution fails or is rejected by a policy.</param>
    /// <param name="cancellationToken">
    /// Cancels the execution. If cancellation was requested, the failure is not masked by
    /// <paramref name="fallback"/>; an <see cref="OperationCanceledException"/> is thrown instead.
    /// </param>
    /// <param name="circuitBreaker">Optional circuit breaker policy applied to this call.</param>
    /// <param name="retry">Optional retry policy applied to this call.</param>
    /// <param name="timeout">Optional timeout strategy applied to this call.</param>
    /// <param name="bulkhead">Optional bulkhead policy applied to this call.</param>
    /// <returns>The operation result on success, otherwise <paramref name="fallback"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="operation"/> is <c>null</c>.</exception>
    /// <exception cref="OperationCanceledException">Thrown if cancellation was requested and the execution did not succeed.</exception>
    /// <remarks>
    /// Resiliency exceptions (for example an open circuit) are not converted to <paramref name="fallback"/>;
    /// they propagate exactly as <see cref="ResiliencyPipelineService.ExecuteAsync{T}"/> throws them.
    /// </remarks>
    public static async Task<T> ExecuteOrDefaultAsync<T>(
        this ResiliencyPipelineService service,
        Func<CancellationToken, Task<T>> operation,
        T fallback,
        CancellationToken cancellationToken = default,
        CircuitBreakerPolicy? circuitBreaker = null,
        RetryPolicy? retry = null,
        ITimeoutStrategy? timeout = null,
        BulkheadPolicy? bulkhead = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(operation);

        var result = await service.ExecuteAsync(
            operation,
            cancellationToken,
            circuitBreaker: circuitBreaker,
            retry: retry,
            timeout: timeout,
            bulkhead: bulkhead);

        if (result.IsSuccess)
        {
            return result.Data!;
        }

        cancellationToken.ThrowIfCancellationRequested();
        return fallback;
    }

    /// <summary>
    /// Gets the pipeline success rate as a percentage between 0 and 100.
    /// </summary>
    /// <param name="service">The pipeline to inspect.</param>
    /// <returns>
    /// The percentage of successful executions, or <c>0</c> when the pipeline has not executed anything yet.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> is <c>null</c>.</exception>
    /// <remarks>
    /// Values are read from <see cref="ResiliencyPipelineService.GetStatistics"/> in one locked snapshot, so the
    /// success count and total count are consistent with each other. The call builds a snapshot of every
    /// registered policy; use <see cref="ResiliencyPipelineService.SuccessfulExecutions"/> and
    /// <see cref="ResiliencyPipelineService.TotalExecutions"/> directly when only the raw counters are needed.
    /// </remarks>
    public static double GetSuccessRate(this ResiliencyPipelineService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        return service.GetStatistics().SuccessRate;
    }

    /// <summary>
    /// Determines whether a policy with the given name is registered on the pipeline.
    /// </summary>
    /// <param name="service">The pipeline to search.</param>
    /// <param name="policyName">The policy name to look for. Comparison is ordinal and case-sensitive.</param>
    /// <returns><c>true</c> if at least one registered policy has the name; otherwise, <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="policyName"/> is <c>null</c>.</exception>
    public static bool HasPolicy(this ResiliencyPipelineService service, string policyName)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(policyName);

        return service.GetPolicyByName(policyName) is not null;
    }

    /// <summary>
    /// Lists the names of all policies registered on the pipeline.
    /// </summary>
    /// <param name="service">The pipeline to inspect.</param>
    /// <returns>
    /// A read-only snapshot of the policy names. The list is empty when no policies are registered.
    /// Names are not de-duplicated, so two policies that share a name appear twice.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> is <c>null</c>.</exception>
    public static IReadOnlyList<string> GetPolicyNames(this ResiliencyPipelineService service)
    {
        ArgumentNullException.ThrowIfNull(service);

        return service.GetAllPolicies()
            .Select(p => p.Name)
            .ToList()
            .AsReadOnly();
    }
}
