# WebhookManagerExtensions

Provides convenience extension methods for `WebhookManager` instances, simplifying common operations such as checking webhook registration, filtering webhooks by event type, and retrieving delivery history for specific webhooks within a time window.

## API

### HasWebhookRegistered

```csharp
public static bool HasWebhookRegistered(this WebhookManager manager, string webhookId)
```

Checks if a webhook with the specified ID is currently registered.

**Parameters:**
- `manager` — the `WebhookManager` instance.
- `webhookId` — the ID of the webhook to check.

**Returns:** `true` if the webhook exists and is registered; otherwise `false`.

**Throws:**
- `ArgumentNullException` when `manager` or `webhookId` is `null`.

---

### GetWebhooksByEvent

```csharp
public static IReadOnlyList<WebhookSubscription> GetWebhooksByEvent(this WebhookManager manager, string eventType)
```

Retrieves all webhooks that match the specified event type.

**Parameters:**
- `manager` — the `WebhookManager` instance.
- `eventType` — the event type to filter webhooks by.

**Returns:** An `IReadOnlyList` of `WebhookSubscription` objects matching the event type.

**Throws:**
- `ArgumentNullException` when `manager` or `eventType` is `null`.

---

### GetDeliveryHistoryForWebhook

```csharp
public static IReadOnlyList<WebhookDelivery> GetDeliveryHistoryForWebhook(
    this WebhookManager manager,
    string webhookId,
    DateTime startTime,
    DateTime endTime)
```

Retrieves delivery history for a specific webhook within a time window.

**Parameters:**
- `manager` — the `WebhookManager` instance.
- `webhookId` — the ID of the webhook to query.
- `startTime` — the start of the time window (inclusive).
- `endTime` — the end of the time window (exclusive).

**Returns:** An `IReadOnlyList` of `WebhookDelivery` objects within the specified time range.

**Throws:**
- `ArgumentNullException` when `manager` or `webhookId` is `null`.
- `ArgumentException` when `startTime` is after `endTime`.

## Usage

### Example 1: Checking webhook registration and filtering by event

```csharp
var webhookManager = new WebhookManager();
// ... register webhooks ...

if (webhookManager.HasWebhookRegistered("webhook-123"))
{
    Console.WriteLine("Webhook is registered.");
}

// Get all webhooks for the "order.created" event
var orderWebhooks = webhookManager.GetWebhooksByEvent("order.created");
foreach (var webhook in orderWebhooks)
{
    Console.WriteLine($"Webhook ID: {webhook.Id}, URL: {webhook.Url}");
}
```

### Example 2: Retrieving delivery history for a specific time range

```csharp
var webhookManager = new WebhookManager();
// ... register webhooks and simulate deliveries ...

var yesterday = DateTime.UtcNow.AddDays(-1);
var now = DateTime.UtcNow;

var deliveries = webhookManager.GetDeliveryHistoryForWebhook(
    "webhook-123",
    yesterday,
    now);

Console.WriteLine($"Found {deliveries.Count} deliveries in the last 24 hours.");
foreach (var delivery in deliveries)
{
    Console.WriteLine($"Delivery ID: {delivery.Id}, Status: {delivery.Status}, Timestamp: {delivery.Timestamp}");
}
```

## Notes

- All methods perform null checks on their parameters and throw `ArgumentNullException` when required arguments are `null`.
- `GetDeliveryHistoryForWebhook` validates that `startTime` is before `endTime` and throws an `ArgumentException` if this condition is not met.
- The returned collections are read-only wrappers around internal lists to prevent modification of the webhook manager's internal state.
- These extension methods do not modify the state of the `WebhookManager`; they only query existing data.
- Thread safety: The extension methods themselves are stateless and thread-safe. However, the underlying `WebhookManager` instance must be thread-safe if accessed concurrently from multiple threads.