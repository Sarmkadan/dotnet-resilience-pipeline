#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Convenience extension methods over <see cref="ThrottlingHelper"/> and <see cref="ThrottleStatistics"/>.
/// All methods delegate to the existing public API and add argument guards.
/// </summary>
public static class ThrottlingHelperExtensions
{
    private const int NoRequests = 0;
    private const int DefaultRequestCost = 1;

    /// <summary>
    /// Checks whether a request for the given policy would be throttled.
    /// </summary>
    /// <remarks>
    /// This consumes tokens when the request is allowed, exactly like <see cref="ThrottlingHelper.ShouldThrottle"/>.
    /// Use it to decide whether to proceed, not to peek at the current state.
    /// </remarks>
    /// <param name="throttler">The throttling helper to query.</param>
    /// <param name="policyName">The policy whose throttle should be checked.</param>
    /// <param name="cost">The number of tokens the request consumes. Must be positive.</param>
    /// <returns><c>true</c> if the request should be throttled; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="throttler"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is null or empty.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="cost"/> is zero or negative.</exception>
    public static bool IsThrottled(
        this ThrottlingHelper throttler,
        string policyName,
        int cost = DefaultRequestCost)
    {
        ArgumentNullException.ThrowIfNull(throttler);
        ArgumentException.ThrowIfNullOrEmpty(policyName);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(cost);

        return throttler.ShouldThrottle(policyName, cost);
    }

    /// <summary>
    /// Gets the fraction of requests that were throttled, in the range 0 to 1.
    /// </summary>
    /// <remarks>
    /// Unlike <see cref="ThrottleStatistics.ThrottleRate"/>, which is a percentage (0-100),
    /// this value is a ratio. Returns 0 when no requests have been recorded yet.
    /// </remarks>
    /// <param name="statistics">The statistics snapshot to evaluate.</param>
    /// <returns>The ratio of throttled requests to total requests.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="statistics"/> is null.</exception>
    public static double GetRejectionRate(this ThrottleStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        if (statistics.TotalRequests <= NoRequests)
            return 0.0;

        return (double)statistics.ThrottledRequests / statistics.TotalRequests;
    }

    /// <summary>
    /// Executes an operation under throttling protection and returns its result.
    /// </summary>
    /// <remarks>
    /// Thin wrapper over <see cref="ThrottlingExtensions.ExecuteWithThrottlingAsync{T}(ThrottlingHelper, string, Func{Task{T}}, TimeSpan?)"/>.
    /// If the request is still throttled after the maximum number of attempts, an <see cref="InvalidOperationException"/> is thrown.
    /// </remarks>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="throttler">The throttling helper that enforces the policy's rate limit.</param>
    /// <param name="policyName">The policy name used to look up the throttle.</param>
    /// <param name="operation">The operation to run once the request is allowed.</param>
    /// <param name="retryDelay">Delay between throttled attempts. Defaults to the underlying helper's default when null.</param>
    /// <returns>The result of <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="throttler"/> or <paramref name="operation"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is null or empty.</exception>
    /// <exception cref="InvalidOperationException">The request remained throttled after all attempts.</exception>
    public static Task<T> ExecuteThrottledAsync<T>(
        this ThrottlingHelper throttler,
        string policyName,
        Func<Task<T>> operation,
        TimeSpan? retryDelay = null)
    {
        ArgumentNullException.ThrowIfNull(throttler);
        ArgumentException.ThrowIfNullOrEmpty(policyName);
        ArgumentNullException.ThrowIfNull(operation);

        return throttler.ExecuteWithThrottlingAsync(policyName, operation, retryDelay);
    }
}
