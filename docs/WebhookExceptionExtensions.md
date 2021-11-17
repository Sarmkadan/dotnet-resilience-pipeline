# WebhookExceptionExtensions

Provides extension methods for exceptions occurring within webhook processing pipelines. This utility class enables callers to classify webhook-related failures into distinct categories—delivery failures, registration errors, and invalid webhook configurations—and to extract a consolidated error summary for logging or diagnostics.

## API

### IsDeliveryFailure

```csharp
public static bool IsDeliveryFailure(this Exception exception)
```

Determines whether the exception represents a failure to deliver a webhook payload to its intended endpoint.

**Parameters:**
- `exception` — The exception instance to evaluate. Must not be `null`.

**Return Value:**
`true` if the exception indicates a delivery failure (e.g., network errors, timeouts, non-2xx responses from the target endpoint); otherwise `false`.

**Throws:**
- `ArgumentNullException` when `exception` is `null`.

---

### IsRegistrationError

```csharp
public static bool IsRegistrationError(this Exception exception)
```

Determines whether the exception represents a failure during webhook registration with a provider.

**Parameters:**
- `exception` — The exception instance to evaluate. Must not be `null`.

**Return Value:**
`true` if the exception indicates a registration error (e.g., authentication failures, malformed registration requests, provider-side rejection); otherwise `false`.

**Throws:**
- `ArgumentNullException` when `exception` is `null`.

---

### IsInvalidWebhook

```csharp
public static bool IsInvalidWebhook(this Exception exception)
```

Determines whether the exception stems from an invalid webhook definition or configuration.

**Parameters:**
- `exception` — The exception instance to evaluate. Must not be `null`.

**Return Value:**
`true` if the exception indicates an invalid webhook (e.g., missing required fields, malformed URLs, unsupported event types); otherwise `false`.

**Throws:**
- `ArgumentNullException` when `exception` is `null`.

---

### GetErrorSummary

```csharp
public static string GetErrorSummary(this Exception exception)
```

Produces a human-readable summary of the exception suitable for logging, diagnostics, or user-facing error messages.

**Parameters:**
- `exception` — The exception instance from which to extract the summary. Must not be `null`.

**Return Value:**
A `string` containing a concise description of the error, potentially including the exception type, message, and relevant contextual details derived from the exception’s properties or inner exceptions.

**Throws:**
- `ArgumentNullException` when `exception` is `null`.

## Usage

### Example 1: Classifying an Exception Before Retry Decision

```csharp
async Task ProcessWebhookAsync(WebhookMessage message, CancellationToken ct)
{
    try
    {
        await deliveryPipeline.SendAsync(message, ct);
    }
    catch (Exception ex) when (ex.IsDeliveryFailure())
    {
        logger.LogWarning("Delivery failed; scheduling retry. Summary: {Summary}", ex.GetErrorSummary());
        await retryQueue.EnqueueAsync(message, ct);
    }
    catch (Exception ex) when (ex.IsRegistrationError() || ex.IsInvalidWebhook())
    {
        logger.LogError("Fatal webhook error. Summary: {Summary}", ex.GetErrorSummary());
        await adminNotifier.AlertAsync(message.WebhookId, ex, ct);
    }
}
```

### Example 2: Aggregating Diagnostics Across Multiple Webhooks

```csharp
IReadOnlyList<Exception> CollectFailures(IEnumerable<WebhookDispatchResult> results)
{
    var failures = new List<Exception>();

    foreach (var result in results)
    {
        if (result.Exception is not null)
        {
            failures.Add(result.Exception);
        }
    }

    foreach (var ex in failures)
    {
        string category = ex switch
        {
            _ when ex.IsDeliveryFailure() => "DELIVERY",
            _ when ex.IsRegistrationError() => "REGISTRATION",
            _ when ex.IsInvalidWebhook() => "INVALID",
            _ => "UNKNOWN"
        };

        diagnosticsSink.Record(category, ex.GetErrorSummary());
    }

    return failures;
}
```

## Notes

- All methods throw `ArgumentNullException` if the `exception` argument is `null`. Callers should guard against `null` before invocation where the exception source is uncertain.
- The classification methods (`IsDeliveryFailure`, `IsRegistrationError`, `IsInvalidWebhook`) are mutually exclusive for a given exception instance in typical implementations; an exception should satisfy at most one predicate. However, the exact behavior depends on the internal classification logic, which may inspect exception types, HTTP status codes, or custom exception properties.
- `GetErrorSummary` may return different levels of detail depending on the exception type. The returned string is not guaranteed to be stable across versions and should be used for human consumption rather than programmatic comparison.
- These methods are extension methods on `Exception` and are stateless; they are safe to call concurrently from multiple threads without external synchronization.
- Inner exceptions are typically considered during classification and summary generation. An `AggregateException` wrapping multiple failures may yield a summary that concatenates or selects the most severe inner error.
