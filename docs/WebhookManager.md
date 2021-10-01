# WebhookManager

The `WebhookManager` class provides functionality for managing webhook subscriptions and event delivery in a .NET application. It allows registering, unregistering, and triggering webhooks, as well as retrieving subscription details, delivery history, and statistics. This component is designed to facilitate real-time event notifications to external endpoints with configurable retry and history tracking capabilities.

## API

### `public int MaxHistoryEntries`
Gets or sets the maximum number of delivery history entries to retain per webhook. When the limit is reached, older entries are automatically removed. Default value is implementation-specific.

**Throws:**
- `ArgumentOutOfRangeException` if set to a value less than 1.

---

### `public string RegisterWebhook(string url, List<string> events, Dictionary<string, string>? customHeaders = null, bool isActive = true)`
Registers a new webhook subscription.

**Parameters:**
- `url` (string): The target URL to which events will be delivered.
- `events` (List\<string>): The list of event types the webhook should subscribe to.
- `customHeaders` (Dictionary\<string, string>?, optional): Additional headers to include in webhook requests.
- `isActive` (bool, optional): Whether the webhook is active upon registration (default: `true`).

**Returns:**
- (string): The unique identifier (`Id`) of the newly registered webhook.

**Throws:**
- `ArgumentNullException` if `url` or `events` is `null`.
- `ArgumentException` if `url` is empty or invalid, or if `events` is empty.
- `InvalidOperationException` if the webhook cannot be registered due to internal constraints.

---

### `public bool UnregisterWebhook(string webhookId)`
Unregisters an existing webhook subscription.

**Parameters:**
- `webhookId` (string): The unique identifier (`Id`) of the webhook to unregister.

**Returns:**
- (bool): `true` if the webhook was successfully unregistered; `false` if the webhook was not found.

**Throws:**
- `ArgumentNullException` if `webhookId` is `null`.

---

### `public async Task TriggerEventAsync(string eventType, object payload, Dictionary<string, string>? customHeaders = null)`
Triggers delivery of an event to all subscribed webhooks.

**Parameters:**
- `eventType` (string): The type of event being triggered.
- `payload` (object): The data payload to include in the webhook request.
- `customHeaders` (Dictionary\<string, string>?, optional): Additional headers to include in the request, merged with subscription headers.

**Returns:**
- (Task): A task representing the asynchronous operation.

**Throws:**
- `ArgumentNullException` if `eventType` or `payload` is `null`.
- `InvalidOperationException` if the event cannot be delivered due to internal errors.

---

### `public List<WebhookSubscription> GetAllWebhooks()`
Retrieves all registered webhook subscriptions.

**Returns:**
- (List\<WebhookSubscription>): A list of `WebhookSubscription` objects representing all active and inactive webhooks.

**Throws:**
- None.

---

### `public WebhookSubscription? GetWebhook(string webhookId)`
Retrieves a specific webhook subscription by its identifier.

**Parameters:**
- `webhookId` (string): The unique identifier (`Id`) of the webhook to retrieve.

**Returns:**
- (WebhookSubscription?): The `WebhookSubscription` object if found; otherwise, `null`.

**Throws:**
- `ArgumentNullException` if `webhookId` is `null`.

---

### `public bool SetWebhookActive(string webhookId, bool isActive)`
Activates or deactivates a webhook subscription.

**Parameters:**
- `webhookId` (string): The unique identifier (`Id`) of the webhook to update.
- `isActive` (bool): Whether the webhook should be active (`true`) or inactive (`false`).

**Returns:**
- (bool): `true` if the webhook's status was successfully updated; `false` if the webhook was not found.

**Throws:**
- `ArgumentNullException` if `webhookId` is `null`.

---

### `public List<WebhookDelivery> GetDeliveryHistory(string webhookId)`
Retrieves the delivery history for a specific webhook.

**Parameters:**
- `webhookId` (string): The unique identifier (`Id`) of the webhook.

**Returns:**
- (List\<WebhookDelivery>): A list of `WebhookDelivery` objects representing delivery attempts for the webhook.

**Throws:**
- `ArgumentNullException` if `webhookId` is `null`.
- `KeyNotFoundException` if the webhook does not exist.

---

### `public WebhookStatistics GetStatistics()`
Retrieves aggregated statistics for all webhook activity, including total deliveries, successes, failures, and average latency.

**Returns:**
- (WebhookStatistics): A `WebhookStatistics` object containing the computed metrics.

**Throws:**
- None.

---

### `public string Id` (WebhookSubscription)
Gets the unique identifier of the webhook subscription.

---

### `public string Url` (WebhookSubscription)
Gets the target URL of the webhook subscription.

---

### `public List<string> Events` (WebhookSubscription)
Gets the list of event types the webhook is subscribed to.

---

### `public Dictionary<string, string> CustomHeaders` (WebhookSubscription)
Gets the custom headers configured for the webhook subscription.

---

### `public DateTime CreatedAt` (WebhookSubscription)
Gets the timestamp when the webhook subscription was created.

---

### `public bool IsActive` (WebhookSubscription)
Gets whether the webhook subscription is currently active.

---

### `public string WebhookId` (WebhookDelivery)
Gets the unique identifier of the webhook associated with the delivery attempt.

---

### `public string EventType` (WebhookDelivery)
Gets the type of event that was delivered.

---

### `public string Url` (WebhookDelivery)
Gets the target URL of the delivery attempt.

---

### `public DateTime Timestamp` (WebhookDelivery)
Gets the timestamp when the delivery attempt occurred.

## Usage

### Example 1: Registering and Triggering a Webhook
