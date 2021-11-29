using System;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Exceptions;

/// <summary>
/// Provides extension methods for <see cref="WebhookException"/> and its derived types.
/// </summary>
public static class WebhookExceptionExtensions
{
    /// <summary>
    /// Determines whether the specified exception represents a webhook delivery failure.
    /// </summary>
    /// <param name="exception">The exception to check. Cannot be null.</param>
    /// <returns><see langword="true"/> if the exception is a <see cref="WebhookDeliveryFailedException"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static bool IsDeliveryFailure(this WebhookException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is WebhookDeliveryFailedException;
    }

    /// <summary>
    /// Determines whether the specified exception represents a webhook registration error.
    /// </summary>
    /// <param name="exception">The exception to check. Cannot be null.</param>
    /// <returns><see langword="true"/> if the exception is a <see cref="WebhookRegistrationException"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static bool IsRegistrationError(this WebhookException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is WebhookRegistrationException;
    }

    /// <summary>
    /// Determines whether the specified exception represents an invalid webhook.
    /// </summary>
    /// <param name="exception">The exception to check. Cannot be null.</param>
    /// <returns><see langword="true"/> if the exception is an <see cref="InvalidWebhookException"/>; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static bool IsInvalidWebhook(this WebhookException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return exception is InvalidWebhookException;
    }

    /// <summary>
    /// Gets a human-readable summary of the webhook failure.
    /// </summary>
    /// <param name="exception">The exception containing webhook failure details. Cannot be null.</param>
    /// <returns>A formatted string containing the webhook ID and error message.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="exception"/> is null.</exception>
    public static string GetErrorSummary(this WebhookException exception)
    {
        ArgumentNullException.ThrowIfNull(exception);
        return $"Webhook {exception.WebhookId ?? "unknown"} failed: {exception.Message}";
    }
}
