using System;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Exceptions;

public static class WebhookExceptionExtensions
{
    public static bool IsDeliveryFailure(this WebhookException exception)
    {
        return exception is WebhookDeliveryFailedException;
    }

    public static bool IsRegistrationError(this WebhookException exception)
    {
        return exception is WebhookRegistrationException;
    }

    public static bool IsInvalidWebhook(this WebhookException exception)
    {
        return exception is InvalidWebhookException;
    }

    public static string GetErrorSummary(this WebhookException exception)
    {
        return $"Webhook {exception.WebhookId} failed: {exception.Message}";
    }
}
