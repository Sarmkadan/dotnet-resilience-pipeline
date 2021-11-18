# HttpClientFactoryExtensions

Provides convenience extension methods for `HttpClient` instances managed by a resilience pipeline, simplifying common HTTP operations such as fetching string or JSON content, posting JSON payloads, checking client availability, and retrieving status codes. These methods integrate transparently with the underlying resilience policies without requiring explicit pipeline invocation.

## API

### GetStringAsync

```csharp
public static async Task<string> GetStringAsync(this HttpClient client, string requestUri)
```

Performs an HTTP GET to the specified URI and returns the response body as a string. The operation is executed through the resilience pipeline associated with the `HttpClient`.

**Parameters:**
- `client` — the `HttpClient` instance (typically obtained from `IHttpClientFactory`).
- `requestUri` — the target URI as a string.

**Returns:** the response body content as a `string`.

**Throws:**
- `ArgumentNullException` when `client` or `requestUri` is `null`.
- `HttpRequestException` on non-success status codes or network failures.
- `TaskCanceledException` on timeout or cancellation.
- Any exception propagated by the resilience pipeline (e.g., from retry exhaustion or circuit breaker rejection).

---

### GetFromJsonAsync\<T\>

```csharp
public static async Task<T> GetFromJsonAsync<T>(this HttpClient client, string requestUri)
```

Sends an HTTP GET and deserializes the JSON response body into an instance of `T`. Uses `System.Text.Json` serialization defaults.

**Parameters:**
- `client` — the `HttpClient` instance.
- `requestUri` — the target URI as a string.

**Type Parameters:**
- `T` — the expected deserialization type.

**Returns:** an instance of `T` populated from the JSON response.

**Throws:**
- `ArgumentNullException` when `client` or `requestUri` is `null`.
- `HttpRequestException` on non-success status codes or network failures.
- `TaskCanceledException` on timeout or cancellation.
- `System.Text.Json.JsonException` when the response body is not valid JSON or cannot be mapped to `T`.
- Exceptions propagated by the resilience pipeline.

---

### PostAsJsonAsync\<TRequest\>

```csharp
public static async Task<string> PostAsJsonAsync<TRequest>(this HttpClient client, string requestUri, TRequest value)
```

Serializes `value` to JSON and sends it as an HTTP POST body to the specified URI. Returns the response body as a string.

**Parameters:**
- `client` — the `HttpClient` instance.
- `requestUri` — the target URI as a string.
- `value` — the request payload object to serialize.

**Type Parameters:**
- `TRequest` — the type of the request payload.

**Returns:** the response body content as a `string`.

**Throws:**
- `ArgumentNullException` when `client` or `requestUri` is `null`.
- `HttpRequestException` on non-success status codes or network failures.
- `TaskCanceledException` on timeout or cancellation.
- `System.Text.Json.JsonException` if `value` cannot be serialized.
- Exceptions propagated by the resilience pipeline.

---

### PostAsJsonAndGetAsync\<TRequest, TResponse\>

```csharp
public static async Task<TResponse> PostAsJsonAndGetAsync<TRequest, TResponse>(this HttpClient client, string requestUri, TRequest value)
```

Serializes `value` to JSON, sends it as an HTTP POST, and deserializes the JSON response body into an instance of `TResponse`.

**Parameters:**
- `client` — the `HttpClient` instance.
- `requestUri` — the target URI as a string.
- `value` — the request payload object to serialize.

**Type Parameters:**
- `TRequest` — the type of the request payload.
- `TResponse` — the expected deserialization type for the response.

**Returns:** an instance of `TResponse` populated from the JSON response.

**Throws:**
- `ArgumentNullException` when `client` or `requestUri` is `null`.
- `HttpRequestException` on non-success status codes or network failures.
- `TaskCanceledException` on timeout or cancellation.
- `System.Text.Json.JsonException` on serialization or deserialization failures.
- Exceptions propagated by the resilience pipeline.

---

### HasClient

```csharp
public static bool HasClient(this HttpClient client)
```

Determines whether the `HttpClient` instance is currently available for use according to the resilience pipeline state (e.g., not rejected by an open circuit breaker).

**Parameters:**
- `client` — the `HttpClient` instance.

**Returns:** `true` if the client is available; `false` if the pipeline has isolated it (e.g., circuit breaker open).

**Throws:**
- `ArgumentNullException` when `client` is `null`.

---

### GetStatusCodeAsync

```csharp
public static async Task<HttpStatusCode> GetStatusCodeAsync(this HttpClient client, string requestUri)
```

Sends an HTTP GET and returns only the response status code, discarding the body content.

**Parameters:**
- `client` — the `HttpClient` instance.
- `requestUri` — the target URI as a string.

**Returns:** the `HttpStatusCode` from the response.

**Throws:**
- `ArgumentNullException` when `client` or `requestUri` is `null`.
- `HttpRequestException` on network failures where no response is received.
- `TaskCanceledException` on timeout or cancellation.
- Exceptions propagated by the resilience pipeline.

## Usage

### Example 1: Fetching and posting with resilience

```csharp
using var client = httpClientFactory.CreateClient("my-resilient-api");

// Check availability before making calls
if (client.HasClient())
{
    // GET JSON data
    var product = await client.GetFromJsonAsync<Product>("/api/products/42");

    // POST an order and receive the created order back
    var orderRequest = new OrderRequest { ProductId = product.Id, Quantity = 3 };
    var createdOrder = await client.PostAsJsonAndGetAsync<OrderRequest, Order>("/api/orders", orderRequest);

    Console.WriteLine($"Order {createdOrder.Id} placed successfully.");
}
else
{
    Console.WriteLine("Service is currently unavailable (circuit breaker open).");
}
```

### Example 2: Health-check pattern

```csharp
using var client = httpClientFactory.CreateClient("health-check-client");

try
{
    var status = await client.GetStatusCodeAsync("/health");
    if (status == HttpStatusCode.OK)
    {
        var healthInfo = await client.GetStringAsync("/health/details");
        Console.WriteLine($"Service healthy: {healthInfo}");
    }
    else
    {
        Console.WriteLine($"Service returned {status}");
    }
}
catch (HttpRequestException) when (!client.HasClient())
{
    Console.WriteLine("Circuit breaker is open — skipping health check.");
}
```

## Notes

- All async methods execute through the resilience pipeline configured for the named or typed `HttpClient`. This means retries, timeouts, circuit breakers, and other policies are applied automatically.
- `HasClient` provides a synchronous, non-allocating check of the pipeline state. It is safe to call from any thread but reflects a point-in-time status that may change immediately after the call.
- The JSON methods use default `System.Text.Json` serialization options. For custom serialization settings, configure the `HttpClient` with a customized `JsonSerializerOptions` instance via the service collection during registration.
- `GetStatusCodeAsync` does not read or buffer the response body, making it suitable for lightweight health checks where the body is irrelevant or large.
- `PostAsJsonAsync` returns the raw response string, which may be empty for endpoints that return `204 No Content`. Callers should handle empty strings appropriately.
- Thread safety: the extension methods themselves are stateless. The underlying `HttpClient` and its associated resilience pipeline are designed for concurrent use. Multiple threads may invoke these methods on the same `HttpClient` instance safely.
- When the resilience pipeline rejects an operation (e.g., circuit breaker open), `HasClient` returns `false`, and any attempt to call the async methods will throw an exception propagated from the pipeline rather than making an HTTP request.
