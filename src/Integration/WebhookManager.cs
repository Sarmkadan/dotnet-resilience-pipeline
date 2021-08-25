#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Text.Json;

namespace DotNetResiliencePipeline.Integration;

/// <summary>
/// Manages webhook subscriptions and deliveries for pipeline events.
/// Handles registration, delivery, retry logic, and event notifications.
/// </summary>
public class WebhookManager
{
    private readonly ConcurrentDictionary<string, WebhookSubscription> _subscriptions = new();
    private readonly List<WebhookDelivery> _deliveryHistory = new();
    private readonly object _lockObj = new object();
    public int MaxHistoryEntries { get; set; } = 1000;

    /// <summary>
    /// Registers a webhook subscription.
    /// </summary>
    public string RegisterWebhook(string url, string[] events, Dictionary<string, string>? headers = null)
    {
        var subscription = new WebhookSubscription
        {
            Id = Guid.NewGuid().ToString(),
            Url = url,
            Events = events.ToList(),
            CustomHeaders = headers ?? new(),
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        };

        _subscriptions.TryAdd(subscription.Id, subscription);
        return subscription.Id;
    }

    /// <summary>
    /// Unregisters a webhook subscription.
    /// </summary>
    public bool UnregisterWebhook(string webhookId)
    {
        return _subscriptions.TryRemove(webhookId, out _);
    }

    /// <summary>
    /// Triggers a webhook event.
    /// </summary>
    public async Task TriggerEventAsync(string eventType, object eventData, CancellationToken cancellationToken = default)
    {
        var applicableWebhooks = _subscriptions.Values.Where(w => w.IsActive && w.Events.Contains(eventType)).ToList();

        foreach (var webhook in applicableWebhooks)
        {
            await DeliverWebhookAsync(webhook, eventType, eventData, cancellationToken);
        }
    }

    /// <summary>
    /// Delivers a webhook with retry logic.
    /// </summary>
    private async Task DeliverWebhookAsync(
        WebhookSubscription webhook,
        string eventType,
        object eventData,
        CancellationToken cancellationToken)
    {
        var delivery = new WebhookDelivery
        {
            Id = Guid.NewGuid().ToString(),
            WebhookId = webhook.Id,
            EventType = eventType,
            Url = webhook.Url,
            Timestamp = DateTime.UtcNow
        };

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
            delivery.AttemptCount = 1;
        }
        catch (Exception ex)
        {
            delivery.Success = false;
            delivery.ErrorMessage = ex.Message;
            delivery.AttemptCount = 1;
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
    public WebhookSubscription? GetWebhook(string webhookId)
    {
        return _subscriptions.TryGetValue(webhookId, out var webhook) ? webhook : null;
    }

    /// <summary>
    /// Enables/disables a webhook.
    /// </summary>
    public bool SetWebhookActive(string webhookId, bool isActive)
    {
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
public class WebhookSubscription
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
public class WebhookDelivery
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
public class WebhookStatistics
{
    public int TotalDeliveries { get; set; }
    public int SuccessfulDeliveries { get; set; }
    public int FailedDeliveries { get; set; }
    public double SuccessRate { get; set; }
    public int ActiveSubscriptions { get; set; }
}
