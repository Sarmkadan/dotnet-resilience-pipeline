# HttpClientFactory

The `HttpClientFactory` type provides a lightweight wrapper around `System.Net.Http.HttpClient` that adds resilient request handling (retry, timeout, circuit‑breaker) and exposes helper methods for common GET and POST operations. It also manages a collection of named `HttpClient` instances, allowing callers to retrieve, create, or remove clients as needed while ensuring proper disposal of resources.

## API

### `public HttpClientFactory()`
Creates a new instance of the factory with default resilience settings. No parameters are required. Throws no exceptions under normal conditions.

### `public HttpClient CreateClient()`
Creates and returns a new `HttpClient` configured with the factory’s resilience pipeline. The caller owns the returned client and should dispose of it when finished (or rely on the factory’s `Dispose` method if the client was registered internally). Throws `ObjectDisposedException` if the factory has already been disposed.

### `public HttpClient? GetClient()`
Attempts to retrieve an existing `HttpClient` managed by the factory. Returns the client if one is available; otherwise returns `null`. Throws `ObjectDisposedException` if the factory is disposed.

### `public bool RemoveClient()`
Attempts to remove the managed `HttpClient` from the factory’s internal collection. Returns `true` if a client was removed, `false` if none existed. Throws `ObjectDisposedException` if the factory is disposed.

### `public async Task<ResilientHttpResponse> GetAsync(string requestUri)`
Sends an asynchronous GET request to the specified `requestUri` using the factory’s resilient pipeline.  
- **Parameters**: `requestUri` – the target URI (must not be `null`).  
- **Return**: A `ResilientHttpResponse` containing the outcome of the request.  
- **Throws**:  
  - `ArgumentNullException` if `requestUri` is `null`.  
  - `ObjectDisposedException` if the factory is disposed.  
  - `HttpRequestException` for transport‑level failures that are not handled by the resilience policies.

### `public async Task<ResilientHttpResponse> PostAsync(string requestUri, HttpContent content)`
Sends an asynchronous POST request to the specified `requestUri` with the provided `content`.  
- **Parameters**:  
  - `requestUri` – target URI (must not be `null`).  
  - `content` – the HTTP content to send (may be `null`).  
- **Return**: A `ResilientHttpResponse` representing the server’s response.  
- **Throws**:  
  - `ArgumentNullException` if `requestUri` is `null`.  
  - `ObjectDisposedException` if the factory is disposed.  
  - `HttpRequestException` for transport‑level failures not mitigated by resilience policies.

### `public List<string> GetClientNames()`
Returns a list of the names of all `HttpClient` instances currently managed by the factory. Returns an empty list if no clients are tracked. Throws `ObjectDisposedException` if the factory is disposed.

### `public void Dispose()`
Releases all resources held by the factory, including disposing of any managed `HttpClient` instances. After calling this method, all other members throw `ObjectDisposedException`. Calling `Dispose` multiple times is safe.

### `public bool Success`
Indicates whether the most recent resilient operation (via `GetAsync` or `PostAsync`) completed successfully (i.e., received a status code in the 2xx range). Valid only after awaiting one of the async methods; otherwise the value is undefined.

### `public System.Net.HttpStatusCode? StatusCode`
Gets the HTTP status code returned by the last resilient operation, or `null` if no response was received (e.g., due to a network failure). Valid only after awaiting `GetAsync` or `PostAsync`.

### `public string? Content`
Gets the response body as a string from the last resilient operation, or `null` if no content was available. Valid only after awaiting one of the async methods.

### `public Dictionary<string, string>? Headers`
Gets the response headers from the last resilient operation as a dictionary, or `null` if no headers were present. Valid only after awaiting `GetAsync` or `PostAsync`.

### `public string? Message`
Gets a descriptive message associated with the last resilient operation (e.g., reason phrase from the status line or an error message). Valid only after awaiting an async method.

### `public Exception? Exception`
Gets the exception thrown during the last resilient operation, if any; otherwise `null`. Valid only after awaiting `GetAsync` or `PostAsync`.

### `public DateTime Timestamp`
Gets the UTC timestamp when the last resilient operation completed (or failed). Valid only after awaiting one of the async methods.

## Usage

### Basic GET request with automatic disposal

```csharp
using var factory = new HttpClientFactory();

// The factory creates a client internally; we use it directly for a GET.
ResilientHttpResponse response = await factory.GetAsync("https://api.example.com/data");

if (response.Success)
{
    Console.WriteLine($"Status: {response.StatusCode}");
    Console.WriteLine($"Body: {response.Content}");
}
else
{
    Console.WriteLine($"Request failed: {response.Message}");
    if (response.Exception != null)
    {
        Console.WriteLine($"Exception: {response.Exception}");
    }
}
```

### Managing named clients and performing a POST

```csharp
var factory = new HttpClientFactory();

// Create and register a named client (implementation‑specific; assumes CreateClient registers it).
HttpClient client = factory.CreateClient();
// Assume the factory stores the client under the name "apiClient" internally.

// Perform a POST using the resilient wrapper.
var content = new StringContent("{\"id\":123}", Encoding.UTF8, "application/json");
ResilientHttpResponse postResponse = await factory.PostAsync("https://api.example.com/submit", content);

if (postResponse.Success)
{
    Console.WriteLine("Submit succeeded.");
}
else
{
    Console.WriteLine($"Submit failed with status {postResponse.StatusCode}: {postResponse.Message}");
}

// Clean up.
factory.RemoveClient();
factory.Dispose();
```

## Notes

- **Thread safety**: The factory instance is safe for concurrent calls to `CreateClient`, `GetClient`, `RemoveClient`, `GetAsync`, and `PostAsync` after construction, provided that the factory has not been disposed. However, the returned `HttpClient` instances themselves are not thread‑safe for simultaneous use; each caller should either use its own client or synchronize access.
- **Disposal**: Once `Dispose` is invoked, all members throw `ObjectDisposedException`. It is recommended to wrap the factory in a `using` statement or call `Dispose` explicitly after the last operation.
- **Null returns**: `GetClient` may return `null` if no client has been created or if it has been removed. Callers should check for null before using the result.
- **Response properties**: The properties `Success`, `StatusCode`, `Content`, `Headers`, `Message`, `Exception`, and `Timestamp` are only meaningful after awaiting `GetAsync` or `PostAsync`. Accessing them before an operation completes yields undefined behavior.
- **Exception handling**: Resilience policies (retry, circuit‑breaker, etc.) are applied internally; they may swallow certain transient failures. If all retry attempts are exhausted, the resulting `ResilientHttpResponse` will have `Success = false`, `Exception` set to the final exception, and `StatusCode` possibly `null`.
- **Naming**: The factory does not expose a way to specify a client name via the listed members; client management is implicit. If named client support is required, it must be inferred from the internal implementation of `CreateClient`/`RemoveClient`.
