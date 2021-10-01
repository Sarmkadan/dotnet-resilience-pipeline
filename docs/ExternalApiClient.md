# ExternalApiClient

`ExternalApiClient` is a resilient HTTP client wrapper designed for interacting with external REST APIs. It provides typed `GET` and `POST` operations, automatic retry logic with configurable delays, and a unified response envelope (`ApiResponse<T>`) that encapsulates success state, payload, diagnostic messages, and response headers. The client manages a registry of named API endpoints, supports multiple authentication schemes (API key or bearer token), and exposes a connection-testing utility.

## API

### Constructors

#### `public ExternalApiClient`

Initializes a new instance of the client. The constructor accepts no arguments; all configuration is performed via the public properties and the `RegisterApi` method after instantiation.

### Methods

#### `public void RegisterApi`

Registers a named API endpoint with the client. The exact signature parameters are not listed in the provided members, but the method stores the API definition internally so that subsequent calls to `GetAsync<T>` and `PostAsync<T>` can reference it by name. Throws if the API name is null, empty, or already registered.

#### `public async Task<ApiResponse<T>> GetAsync<T>`

Sends an HTTP `GET` request to a previously registered API and returns a typed response envelope. The type parameter `T` determines the deserialization target for the response body. The method respects the configured `Timeout`, `DefaultHeaders`, `MaxRetries`, and `RetryDelay`. Throws `InvalidOperationException` if the specified API name has not been registered. Throws `TaskCanceledException` when the cumulative timeout (including retries) is exceeded. Network-level exceptions are surfaced after all retries are exhausted.

#### `public async Task<ApiResponse<T>> PostAsync<T>`

Sends an HTTP `POST` request to a previously registered API with a serialized request body and returns a typed response envelope. Behavior regarding retries, timeout, and headers mirrors `GetAsync<T>`. Throws under the same conditions as `GetAsync<T>`, with the additional possibility of `HttpRequestException` if the request body cannot be serialized.

#### `public async Task<bool> TestConnectionAsync`

Performs a lightweight connectivity check against the configured `BaseUrl`. Returns `true` if the endpoint responds within the configured `Timeout` (typically via an HTTP `HEAD` or a minimal `GET`); returns `false` if the request fails or times out. Does not throw—failures are always represented by a `false` return value.

#### `public List<string> GetRegisteredApis`

Returns a list of all API names currently registered with the client. The returned list is a snapshot; subsequent registrations do not modify the returned instance.

### Properties

#### `public string BaseUrl`

Gets or sets the base URL for all API calls (e.g., `"https://api.example.com/v1"`). Must be an absolute URI. Setting this property after APIs have been registered does not retroactively update their full URLs; it applies only to subsequent requests.

#### `public string? ApiKey`

Gets or sets an optional API key. When non-null, the client includes it in requests according to the configured authentication scheme (typically as a query parameter or a custom header). Set to `null` to disable API key authentication.

#### `public string? BearerToken`

Gets or sets an optional bearer token. When non-null, the client adds an `Authorization: Bearer <value>` header to every request. Set to `null` to disable bearer authentication. If both `ApiKey` and `BearerToken` are set, the client’s precedence rules determine which is applied (bearer tokens typically take priority).

#### `public TimeSpan Timeout`

Gets or sets the per-request timeout. This applies to each individual HTTP attempt, not the cumulative retry duration. Default value is implementation-specific (commonly 30 or 100 seconds). Setting a value of `TimeSpan.Zero` or negative throws `ArgumentOutOfRangeException`.

#### `public Dictionary<string, string> DefaultHeaders`

Gets or sets a dictionary of headers included in every request. Modifications to the dictionary affect subsequent requests immediately. Header names must conform to HTTP header naming rules; invalid names cause `HttpRequestException` at request time.

#### `public int MaxRetries`

Gets or sets the maximum number of retry attempts for failed requests. A value of `0` means no retries. Retries are triggered by transient failures (HTTP 5xx, network errors, timeout). Setting a negative value throws `ArgumentOutOfRangeException`.

#### `public TimeSpan RetryDelay`

Gets or sets the fixed delay between retry attempts. Does not apply exponential backoff unless implemented internally. Setting a negative value throws `ArgumentOutOfRangeException`.

### Nested Type: `ApiResponse<T>`

#### `public bool Success`

Indicates whether the HTTP request completed with a success status code (2xx) and the body was successfully deserialized.

#### `public T? Data`

The deserialized response body when `Success` is `true`; otherwise `default(T)`.

#### `public string? Message`

A human-readable diagnostic message. On success, typically `null` or an empty string. On failure, contains error details such as the HTTP status code, reason phrase, or deserialization error.

#### `public Dictionary<string, string>? Headers`

The response headers from the final HTTP attempt. `null` if no response was received (e.g., network failure before a connection was established).

## Usage

### Example 1: Basic GET with Retry Configuration

```csharp
var client = new ExternalApiClient
{
    BaseUrl = "https://jsonplaceholder.typicode.com",
    Timeout = TimeSpan.FromSeconds(10),
    MaxRetries = 3,
    RetryDelay = TimeSpan.FromSeconds(1)
};

client.RegisterApi("GetPost", HttpMethod.Get, "/posts/{id}");

ApiResponse<Post> response = await client.GetAsync<Post>("GetPost", new { id = 1 });

if (response.Success)
{
    Console.WriteLine($"Title: {response.Data.Title}");
}
else
{
    Console.WriteLine($"Failed: {response.Message}");
}
```

### Example 2: POST with Authentication and Custom Headers

```csharp
var client = new ExternalApiClient
{
    BaseUrl = "https://api.example.com/v2",
    BearerToken = "eyJhbGciOi...",
    DefaultHeaders = new Dictionary<string, string>
    {
        ["X-Client-Id"] = "dotnet-resilience-pipeline",
        ["Accept"] = "application/json"
    },
    MaxRetries = 2,
    RetryDelay = TimeSpan.FromMilliseconds(500)
};

client.RegisterApi("CreateOrder", HttpMethod.Post, "/orders");

var payload = new { Item = "Widget", Quantity = 5 };
ApiResponse<OrderConfirmation> response = await client.PostAsync<OrderConfirmation>("CreateOrder", payload);

if (response.Success)
{
    Console.WriteLine($"Order ID: {response.Data.OrderId}");
    Console.WriteLine($"Trace-Id header: {response.Headers?.GetValueOrDefault("X-Trace-Id")}");
}
else
{
    Console.WriteLine($"Error: {response.Message}");
}
```

## Notes

- **Retry semantics**: Retries are performed on a fixed-delay schedule. The total elapsed time for a request can reach `(MaxRetries + 1) * Timeout + MaxRetries * RetryDelay`. Callers should set `Timeout` and `RetryDelay` with this cumulative ceiling in mind.
- **Authentication conflicts**: When both `ApiKey` and `BearerToken` are set, the client applies only one. The precedence is implementation-defined; consult the source or configuration to confirm which takes priority. To avoid ambiguity, set one to `null` when using the other.
- **Thread safety**: All public instance members are designed for use from a single thread at a time. Concurrent calls to `GetAsync<T>`, `PostAsync<T>`, or `TestConnectionAsync` while mutating `BaseUrl`, `DefaultHeaders`, `MaxRetries`, or `RetryDelay` may result in inconsistent request configurations. Synchronize externally if concurrent use is required.
- **`ApiResponse<T>.Headers` nullability**: The `Headers` dictionary is `null` when no HTTP response is received (e.g., DNS resolution failure, connection refused). Always perform a null check before accessing it.
- **`ApiResponse<T>.Data` on failure**: When `Success` is `false`, `Data` is `default(T)`. For reference types, this is `null`. Do not assume `Data` is non-null based on a prior success.
- **`TestConnectionAsync` behavior**: This method does not trigger retries and does not throw. It is suitable for health checks and circuit-breaker probes but does not validate authentication or specific API availability—only that the `BaseUrl` host is reachable.
- **`RegisterApi` idempotency**: Registering the same API name twice throws. Check `GetRegisteredApis` before registering if dynamic registration from multiple code paths is possible.
- **Timeout granularity**: `Timeout` applies per HTTP attempt. A single retry cycle consists of one attempt, a `RetryDelay` pause, and the next attempt—each subject to its own `Timeout`. A slow response that just exceeds `Timeout` on every attempt will exhaust all retries and ultimately throw `TaskCanceledException`.
