#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Exceptions;

/// <summary>
/// Base exception for webhook-related failures.
/// </summary>
public class WebhookException : ResiliencyException
{
    /// <summary>
    /// Gets or sets the webhook identifier.
    /// </summary>
    public string? WebhookId { get; set; }
    /// <summary>
    /// Gets or sets the webhook URL.
    /// </summary>
    public string? WebhookUrl { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="webhookId">The webhook identifier.</param>
    /// <param name="webhookUrl">The webhook URL.</param>
    public WebhookException(string message, string? webhookId = null, string? webhookUrl = null)
        : base(message)
    {
        WebhookId = webhookId;
        WebhookUrl = webhookUrl;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <param name="webhookId">The webhook identifier.</param>
    /// <param name="webhookUrl">The webhook URL.</param>
    public WebhookException(string message, Exception innerException, string? webhookId = null, string? webhookUrl = null)
        : base(message, innerException)
    {
        WebhookId = webhookId;
        WebhookUrl = webhookUrl;
    }

    public override string ToString() =>
        $"WebhookException {{ WebhookId = {WebhookId}, WebhookUrl = {WebhookUrl}, AttemptCount = {(this as WebhookDeliveryFailedException)?.AttemptCount}, EventType = {(this as WebhookDeliveryFailedException)?.EventType} }}";
}

/// <summary>
/// Thrown when webhook delivery fails after all retry attempts.
/// </summary>
public sealed class WebhookDeliveryFailedException : WebhookException
{
    /// <summary>
    /// Gets or sets the number of delivery attempts made.
    /// </summary>
    public int AttemptCount { get; set; }
    /// <summary>
    /// Gets or sets the event type that triggered the webhook delivery.
    /// </summary>
    public string? EventType { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookDeliveryFailedException"/> class with the specified parameters.
    /// </summary>
    /// <param name="webhookId">The webhook identifier.</param>
    /// <param name="webhookUrl">The webhook URL.</param>
    /// <param name="eventType">The event type that triggered the webhook delivery.</param>
    /// <param name="attemptCount">The number of delivery attempts made.</param>
    /// <param name="innerException">The exception that is the cause of the current exception.</param>
    public WebhookDeliveryFailedException(string webhookId, string webhookUrl, string eventType, int attemptCount, Exception innerException)
        : base($"Webhook delivery failed after {attemptCount} attempt(s) for event '{eventType}' to {webhookUrl}",
              innerException, webhookId, webhookUrl)
    {
        EventType = eventType;
        AttemptCount = attemptCount;
    }
}

/// <summary>
/// Thrown when webhook registration fails.
/// </summary>
public sealed class WebhookRegistrationException : WebhookException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookRegistrationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="webhookUrl">The webhook URL.</param>
    public WebhookRegistrationException(string message, string? webhookUrl = null)
        : base(message, null, webhookUrl)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="WebhookRegistrationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <param name="webhookUrl">The webhook URL.</param>
    public WebhookRegistrationException(string message, Exception innerException, string? webhookUrl = null)
        : base(message, innerException, null, webhookUrl)
    {
    }
}

/// <summary>
/// Thrown when webhook subscription is invalid.
/// </summary>
public sealed class InvalidWebhookException : WebhookException
{
    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidWebhookException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="webhookId">The webhook identifier.</param>
    /// <param name="webhookUrl">The webhook URL.</param>
    public InvalidWebhookException(string message, string webhookId, string webhookUrl)
        : base(message, webhookId, webhookUrl)
    {
    }
}
