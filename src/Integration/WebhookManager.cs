#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===========================================================================

using System.Collections.Concurrent;
using System.Text.Json;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Integration;

/// <summary>
/// Manages webhook subscriptions and deliveries for pipeline events.
/// Handles registration, delivery, retry logic, and event notifications.
/// </summary>
public sealed class WebhookManager
{
    private readonly ConcurrentDictionary<string, WebhookSubscription> _subscriptions = new();
    private readonly List<WebhookDelivery> _deliveryHistory = new();
    private readonly object _lockObj = new object();
    public int MaxHistoryEntries { get; set; } = 1000;

    /// <summary>
    /// Registers a webhook subscription.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when url or events is null.</exception>
    /// <exception cref="WebhookRegistrationException">Thrown when registration fails.</exception>
    public string RegisterWebhook(string url, string[] events, Dictionary<string, string>? headers = null)
    {
        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url), "Webhook URL cannot be null or whitespace");

        if (events is null || events.Length == 0)
            throw new ArgumentNullException(nameof(events), "At least one event type must be specified");

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri) || !uri.Scheme.StartsWith("http"))
            throw new WebhookRegistrationException("Invalid webhook URL format. Must be an absolute HTTP/HTTPS URL", url);

        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid().ToString(),
            Url = url,
            Events = events.ToList(),
            CustomHeaders = headers ?? new(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        if (!_subscriptions.TryAdd(subscription.Id, subscription))
            throw new WebhookRegistrationException("Failed to register webhook subscription", url);

        return subscription.Id;
    }

    /// <summary>
    /// Unregisters a webhook subscription.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when webhookId is null.</exception>
    public bool UnregisterWebhook(string webhookId)
    {
        if (string.IsNullOrWhiteSpace(webhookId))
            throw new ArgumentNullException(nameof(webhookId), "Webhook ID cannot be null or whitespace");

        return _subscriptions.TryRemove(webhookId, out _);
    }

    /// <summary>
    /// Triggers a webhook event.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when eventType is null.</exception>
    /// <exception cref="WebhookDeliveryFailedException">Thrown when delivery fails after retries.</exception>
    public async Task TriggerEventAsync(string eventType, object eventData, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(eventType))
            throw new ArgumentNullException(nameof(eventType), "Event type cannot be null or whitespace");

        var applicableWebhooks = _subscriptions.Values
            .Where(w => w.IsActive && w.Events.Contains(eventType))
            .ToList();

        if (applicableWebhooks.Count == 0)
            return;

        var deliveryTasks = applicableWebhooks.Select(webhook =>
            DeliverWebhookWithRetryAsync(webhook, eventType, eventData, cancellationToken));

        await Task.WhenAll(deliveryTasks);
    }

    /// <summary>
    /// Delivers a webhook with retry logic.
    /// </summary>
    private async Task DeliverWebhookWithRetryAsync(
        WebhookSubscription webhook,
        string eventType,
        object eventData,
        CancellationToken cancellationToken)
    {
        if (webhook is null)
            throw new ArgumentNullException(nameof(webhook));

        if (string.IsNullOrWhiteSpace(webhook.Url))
            throw new InvalidWebhookException("Webhook URL is required", webhook.Id, webhook.Url);

        var delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid().ToString(),
            WebhookId = webhook.Id,
            EventType = eventType,
            Url = webhook.Url,
            Timestamp = DateTime.UtcNow
        };

        int attemptCount = 0;
        const int maxAttempts = 3;
        Exception? lastException = null;

        while (attemptCount < maxAttempts && !cancellationToken.IsCancellationRequested)
        {
            attemptCount++;
            delivery.AttemptCount = attemptCount;

            try
            {
                using var client = new HttpClient { Timeout = TimeSpan.FromSeconds(10) };

                var payload = new
                {
                    id = delivery.Id,
                    eventType,
                    timestamp = DateTime.UtcNow,
                    data = eventData
                };

                var json = JsonSerializer.Serialize(payload);
                var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

                // Apply custom headers
                foreach (var header in webhook.CustomHeaders)
                    client.DefaultRequestHeaders.Add(header.Key, header.Value);

                var response = await client.PostAsync(webhook.Url, content, cancellationToken);
                delivery.StatusCode = response.StatusCode;
                delivery.Success = response.IsSuccessStatusCode;

                if (response.IsSuccessStatusCode)
                    break;

                lastException = new HttpRequestException($"HTTP {(int)response.StatusCode}: {response.StatusCode}");
            }
            catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
            {
                lastException = new WebhookDeliveryFailedException(
                    webhook.Id,
                    webhook.Url,
                    eventType,
                    attemptCount,
                    new OperationCanceledException("Webhook delivery was cancelled", ex));
                delivery.ErrorMessage = "Cancelled";
                delivery.Success = false;
                break;
            }
            catch (HttpRequestException ex)
            {
                lastException = ex;
                delivery.ErrorMessage = ex.Message;
                delivery.Success = false;
            }
            catch (Exception ex)
            {
                lastException = new WebhookDeliveryFailedException(
                    webhook.Id,
                    webhook.Url,
                    eventType,
                    attemptCount,
                    ex);
                delivery.ErrorMessage = ex.Message;
                delivery.Success = false;
                break;
            }

            if (!delivery.Success && attemptCount < maxAttempts)
            {
                // Exponential backoff
                var delay = TimeSpan.FromSeconds(Math.Pow(2, attemptCount));
                await Task.Delay(delay, cancellationToken);
            }
        }

        if (!delivery.Success && lastException is not OperationCanceledException)
        {
            throw new WebhookDeliveryFailedException(
                webhook.Id,
                webhook.Url,
                eventType,
                attemptCount,
                lastException ?? new Exception("Unknown delivery failure"));
        }

        lock (_lockObj)
        {
            _deliveryHistory.Add(delivery);
            if (_deliveryHistory.Count > MaxHistoryEntries)
                _deliveryHistory.RemoveAt(0);
        }
    }

    /// <summary>
    /// Gets all webhook subscriptions.
    /// </summary>
    public List<WebhookSubscription> GetAllWebhooks()
    {
        return _subscriptions.Values.ToList();
    }

    /// <summary>
    /// Gets a specific webhook subscription.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when webhookId is null.</exception>
    public WebhookSubscription? GetWebhook(string webhookId)
    {
        if (string.IsNullOrWhiteSpace(webhookId))
            throw new ArgumentNullException(nameof(webhookId), "Webhook ID cannot be null or whitespace");

        return _subscriptions.TryGetValue(webhookId, out var webhook) ? webhook : null;
    }

    /// <summary>
    /// Enables/disables a webhook.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when webhookId is null.</exception>
    public bool SetWebhookActive(string webhookId, bool isActive)
    {
        if (string.IsNullOrWhiteSpace(webhookId))
            throw new ArgumentNullException(nameof(webhookId), "Webhook ID cannot be null or whitespace");

        if (_subscriptions.TryGetValue(webhookId, out var webhook))
        {
            webhook.IsActive = isActive;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets delivery history.
    /// </summary>
    public List<WebhookDelivery> GetDeliveryHistory(string? webhookId = null, int limit = 100)
    {
        if (limit <= 0)
            throw new ArgumentException("Limit must be greater than 0", nameof(limit));

        lock (_lockObj)
        {
            var query = _deliveryHistory.AsEnumerable();

            if (!string.IsNullOrEmpty(webhookId))
                query = query.Where(d => d.WebhookId == webhookId);

            return query.TakeLast(limit).ToList();
        }
    }

    /// <summary>
    /// Gets delivery statistics.
    /// </summary>
    public WebhookStatistics GetStatistics()
    {
        lock (_lockObj)
        {
            var successCount = _deliveryHistory.Count(d => d.Success);
            var totalCount = _deliveryHistory.Count;

            return new WebhookStatistics
            {
                TotalDeliveries = totalCount,
                SuccessfulDeliveries = successCount,
                FailedDeliveries = totalCount - successCount,
                SuccessRate = totalCount > 0 ? (successCount * 100.0) / totalCount : 0,
                ActiveSubscriptions = _subscriptions.Count(x => x.Value.IsActive)
            };
        }
    }
}

/// <summary>
/// Webhook subscription configuration.
/// </summary>
public sealed class WebhookSubscription
{
    public string Id { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public List<string> Events { get; set; } = new();
    public Dictionary<string, string> CustomHeaders { get; set; } = new();
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Record of a webhook delivery attempt.
/// </summary>
public sealed class WebhookDelivery
{
    public string Id { get; set; } = string.Empty;
    public string WebhookId { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string Url { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
    public bool Success { get; set; }
    public System.Net.HttpStatusCode? StatusCode { get; set; }
    public string? ErrorMessage { get; set; }
    public int AttemptCount { get; set; }
}

/// <summary>
/// Statistics for webhook deliveries.
/// </summary>
public sealed class WebhookStatistics
{
    public int TotalDeliveries { get; set; }
    public int SuccessfulDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public double SuccessRate { get; set; }
    public int ActiveSubscriptions { get; set; }
}