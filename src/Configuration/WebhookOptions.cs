#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Configuration;

/// <summary>
/// Configuration options for webhook delivery retries and timeouts.
/// </summary>
public sealed class WebhookOptions
{
    /// <summary>
    /// Gets or sets the maximum number of delivery attempts per webhook, including the first one.
    /// Default: 3
    /// </summary>
    public int MaxAttempts { get; set; } = 3;

    /// <summary>
    /// Gets or sets the timeout of a single delivery request in seconds.
    /// Default: 10
    /// </summary>
    public int DeliveryTimeoutSeconds { get; set; } = 10;

    /// <summary>
    /// Gets or sets the base of the exponential backoff. The delay before the next attempt is
    /// <c>BackoffBase ^ attemptNumber</c> seconds.
    /// Default: 2
    /// </summary>
    public int BackoffBase { get; set; } = 2;
}
