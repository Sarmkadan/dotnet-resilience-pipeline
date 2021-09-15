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
    public string? WebhookId { get; set; }
    public string? WebhookUrl { get; set; }

    public WebhookException(string message, string? webhookId = null, string? webhookUrl = null)
        : base(message)
    {
        WebhookId = webhookId;
        WebhookUrl = webhookUrl;
    }

    public WebhookException(string message, Exception innerException, string? webhookId = null, string? webhookUrl = null)
        : base(message, innerException)
    {
        WebhookId = webhookId;
        WebhookUrl = webhookUrl;
    }
}

/// <summary>
/// Thrown when webhook delivery fails after all retry attempts.
/// </summary>
public sealed class WebhookDeliveryFailedException : WebhookException
{
    public int AttemptCount { get; set; }
    public string? EventType { get; set; }

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
    public WebhookRegistrationException(string message, string? webhookUrl = null)
        : base(message, null, webhookUrl)
    {
    }

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
    public InvalidWebhookException(string message, string webhookId, string webhookUrl)
        : base(message, webhookId, webhookUrl)
    {
    }
}