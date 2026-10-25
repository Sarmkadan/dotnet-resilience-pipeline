#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Middleware;

/// <summary>
/// Extension methods for <see cref="RateLimitingMiddleware"/>, <see cref="RateLimiter"/> and
/// <see cref="RateLimitStatus"/> providing helpers for proactive throttling and back-off hints.
/// </summary>
public static class RateLimitingMiddlewareExtensions
{
    /// <summary>
    /// Determines whether either rate limit window has dropped to or below the given share of its capacity.
    /// </summary>
    /// <param name="status">The rate limit status to inspect.</param>
    /// <param name="thresholdPercent">Remaining-capacity threshold in the range 0 to 100. For example, 20 means "20% or less remaining".</param>
    /// <returns><c>true</c> if the per-second or per-minute window is at or below the threshold; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="status"/> is <c>null</c>.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="thresholdPercent"/> is NaN or outside 0 to 100.</exception>
    public static bool IsNearLimit(this RateLimitStatus status, double thresholdPercent)
    {
        ArgumentNullException.ThrowIfNull(status);
        if (double.IsNaN(thresholdPercent) || thresholdPercent < 0 || thresholdPercent > 100)
            throw new ArgumentOutOfRangeException(nameof(thresholdPercent), thresholdPercent, "Threshold must be between 0 and 100.");

        var fraction = thresholdPercent / 100.0;

        return status.RemainingTokensPerSecond <= status.RequestsPerSecond * fraction
            || status.RemainingTokensPerMinute <= status.RequestsPerMinute * fraction;
    }

    /// <summary>
    /// Estimates how long a caller should wait before the next request can be allowed.
    /// Only exhausted windows are considered; the longest wait among them is returned.
    /// </summary>
    /// <param name="status">The rate limit status to inspect.</param>
    /// <returns>
    /// The time remaining until the blocking window resets, or <see cref="TimeSpan.Zero"/> if the client is not limited
    /// or the reset time has already passed.
    /// </returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="status"/> is <c>null</c>.</exception>
    public static TimeSpan GetRetryAfter(this RateLimitStatus status)
    {
        ArgumentNullException.ThrowIfNull(status);

        var now = DateTime.UtcNow;
        var retryAfter = TimeSpan.Zero;

        if (status.RemainingTokensPerSecond <= 0)
        {
            var untilSecondReset = status.NextResetSecond - now;
            if (untilSecondReset > retryAfter)
                retryAfter = untilSecondReset;
        }

        if (status.RemainingTokensPerMinute <= 0)
        {
            var untilMinuteReset = status.NextResetMinute - now;
            if (untilMinuteReset > retryAfter)
                retryAfter = untilMinuteReset;
        }

        return retryAfter;
    }

    /// <summary>
    /// Consumes one token for <paramref name="key"/> and runs <paramref name="operation"/> only if the request is allowed.
    /// </summary>
    /// <typeparam name="T">The result type of the operation.</typeparam>
    /// <param name="middleware">The rate limiting middleware.</param>
    /// <param name="key">Client identifier to rate limit against.</param>
    /// <param name="operation">The operation to execute when the request is allowed.</param>
    /// <returns>The result of <paramref name="operation"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="middleware"/>, <paramref name="key"/> or <paramref name="operation"/> is <c>null</c>.</exception>
    /// <exception cref="ResiliencyException">Thrown if the client is rate limited. The operation is not executed.</exception>
    public static async Task<T> ExecuteIfAllowedAsync<T>(this RateLimitingMiddleware middleware, string key, Func<Task<T>> operation)
    {
        ArgumentNullException.ThrowIfNull(middleware);
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(operation);

        if (!middleware.IsRequestAllowed(key))
        {
            var retryAfter = middleware.GetStatus(key).GetRetryAfter();
            throw new ResiliencyException(
                $"Rate limit exceeded for client '{key}'. Retry after {retryAfter.TotalMilliseconds:F0} ms.",
                nameof(RateLimitingMiddleware),
                nameof(RateLimitingMiddleware));
        }

        return await operation().ConfigureAwait(false);
    }
}
