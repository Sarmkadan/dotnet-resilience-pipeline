using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetResiliencePipeline.Integration
{
    /// <summary>
    /// Extension methods for <see cref="WebhookManager"/> operations.
    /// </summary>
    public static class WebhookManagerExtensions
    {
        /// <summary>
        /// Checks if a webhook with the specified ID is currently registered.
        /// </summary>
        /// <param name="manager">The webhook manager instance.</param>
        /// <param name="webhookId">The ID of the webhook to check.</param>
        /// <returns>True if the webhook exists and is registered.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> or <paramref name="webhookId"/> is null.</exception>
        public static bool HasWebhookRegistered(this WebhookManager manager, string webhookId)
            => manager.GetWebhook(webhookId) is not null;

        /// <summary>
        /// Retrieves all webhooks that match the specified event type.
        /// </summary>
        /// <param name="manager">The webhook manager instance.</param>
        /// <param name="eventType">The event type to filter webhooks by.</param>
        /// <returns>An IReadOnlyList of matching webhook subscriptions.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> or <paramref name="eventType"/> is null.</exception>
        public static IReadOnlyList<WebhookSubscription> GetWebhooksByEvent(this WebhookManager manager, string eventType)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(eventType);

            return manager.GetAllWebhooks()
                .Where(sub => sub.Events.Contains(eventType))
                .ToList()
                .AsReadOnly();
        }

        /// <summary>
        /// Retrieves delivery history for a specific webhook within a time window.
        /// </summary>
        /// <param name="manager">The webhook manager instance.</param>
        /// <param name="webhookId">The ID of the webhook to query.</param>
        /// <param name="startTime">The start of the time window (inclusive).</param>
        /// <param name="endTime">The end of the time window (exclusive).</param>
        /// <returns>An IReadOnlyList of deliveries within the specified time range.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="manager"/> or <paramref name="webhookId"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="startTime"/> is after <paramref name="endTime"/>.</exception>
        public static IReadOnlyList<WebhookDelivery> GetDeliveryHistoryForWebhook(
            this WebhookManager manager,
            string webhookId,
            DateTime startTime,
            DateTime endTime)
        {
            ArgumentNullException.ThrowIfNull(manager);
            ArgumentNullException.ThrowIfNull(webhookId);

            if (startTime >= endTime)
                throw new ArgumentException("Start time must be before end time.", nameof(startTime));

            return manager.GetDeliveryHistory()
                .Where(delivery => delivery.WebhookId == webhookId &&
                                   delivery.Timestamp >= startTime &&
                                   delivery.Timestamp < endTime)
                .ToList()
                .AsReadOnly();
        }
    }
}
