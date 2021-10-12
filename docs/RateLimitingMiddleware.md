# RateLimitingMiddleware

Provides token‑bucket based rate limiting for HTTP requests, allowing fine‑grained control of request traffic per client (identified by a client ID) with separate per‑second and per‑minute limits.

## API

### ConfigureLimits
**Purpose:** Sets the maximum request rates for the middleware.  
**Parameters:**  
- `requestsPerSecond` – maximum allowed requests per second (must be > 0).  
- `requestsPerMinute` – maximum allowed requests per minute (must be > 0).  
- `clientId` (optional) – the client identifier to which the limits apply; if `null` the limits apply to the default/global bucket.  
**Return:** `void`  
**Throws:**  
- `ArgumentOutOfRangeException` if either rate value is less than or equal to zero.  

### IsRequestAllowed
**Purpose:** Determines whether a request from a given client can proceed without exceeding the configured limits.  
**Parameters:**  
- `clientId` – the identifier of the client making the request.  
**Return:** `true` if the request is allowed; `false` if it would exceed the limit.  
**Throws:**  
- `ArgumentNullException` if `clientId` is `null`.  

### GetStatus()
**Purpose:** Retrieves the aggregated rate‑limit status of the middleware (default bucket).  
**Parameters:** none  
**Return:** A `RateLimitStatus` instance reflecting the current token counts and next reset times for the default bucket.  
**Throws:** none  

### GetStatus(string clientId)
**Purpose:** Retrieves the current rate‑limit status for a specific client.  
**Parameters:**  
- `clientId` – the identifier of the client whose status is requested.  
**Return:** A `RateLimitStatus` instance containing remaining tokens and reset timestamps for that client.  
**Throws:**  
- `ArgumentNullException` if `clientId` is `null`.  
- `KeyNotFoundException` if no tracking data exists for the supplied `clientId`.  

### GetAllStatus
**Purpose:** Returns a snapshot of the rate‑limit status for all tracked clients.  
**Parameters:** none  
**Return:** `Dictionary<string, RateLimitStatus>` where the key is the client ID and the value is its current `RateLimitStatus`.  
**Throws:** none  

### ResetClient
**Purpose:** Resets the token bucket for a specific client back to its configured limits.  
**Parameters:**  
- `clientId` – the identifier of the client to reset.  
**Return:** `void`  
**Throws:**  
- `ArgumentNullException` if `clientId` is `null`.  
- `KeyNotFoundException` if the client is not currently tracked.  

### ClearAll
**Purpose:** Removes all client‑specific tracking state, effectively resetting the middleware to its initial empty state.  
**Parameters:** none  
**Return:** `void`  
**Throws:** none  

### RateLimiter
**Purpose:** Provides access to the underlying rate‑limiter implementation used by this middleware.  
**Parameters:** none  
**Return:** The concrete `RateLimiter` (or `IRateLimiter`) instance that performs the token‑bucket logic.  
**Throws:** none  

### TryConsumeTokens
**Purpose:** Attempts to deduct a specified number of tokens from a client’s bucket, updating state only if sufficient tokens are available.  
**Parameters:**  
- `clientId` – the identifier of the client.  
- `tokensToConsume` (optional, default = 1) – number of tokens to attempt to consume; must be > 0.  
**Return:** `true` if the tokens were successfully consumed; `false` if the bucket did not contain enough tokens.  
**Throws:**  
- `ArgumentNullException` if `clientId` is `null`.  
- `ArgumentOutOfRangeException` if `tokensToConsume` is less than or equal to zero.  

### ClientId
**Purpose:** Gets or sets the client identifier associated with the current request context.  
**Parameters:** none  
**Return:** `string?` – the client ID, or `null` if no client ID is bound to the context.  
**Throws:** none  

### RequestsPerSecond
**Purpose:** Gets the configured maximum number of requests allowed per second.  
**Parameters:** none  
**Return:** `int` – the per‑second limit.  
**Throws:** none  

### RequestsPerMinute
**Purpose:** Gets the configured maximum number of requests allowed per minute.  
**Parameters:** none  
**Return:** `int` – the per‑minute limit.  
**Throws:** none  

### RemainingTokensPerSecond
**Purpose:** Gets the number of tokens currently available in the per‑second bucket.  
**Parameters:** none  
**Return:** `int` – remaining tokens for the per‑second limit.  
**Throws:** none  

### RemainingTokensPerMinute
**Purpose:** Gets the number of tokens currently available in the per‑minute bucket.  
**Parameters:** none  
**Return:** `int` – remaining tokens for the per‑minute limit.  
**Throws:** none  

### NextResetSecond
**Purpose:** Gets the UTC date and time when the per‑second token bucket will next be replenished.  
**Parameters:** none  
**Return:** `DateTime` – the next reset instant for the per‑second limit.  
**Throws:** none  

### NextResetMinute
**Purpose:** Gets the UTC date and time when the per‑minute token bucket will next be replenished.  
**Parameters:** none  
**Return:** `DateTime` – the next reset instant for the per‑minute limit.  
**Throws:** none  

## Usage

### Example 1: Configuring the middleware in an ASP.NET Core pipeline
```csharp
using Microsoft.AspNetCore.Builder;
using DotNetResiliencePipeline.RateLimiting;

var builder = WebApplication.CreateBuilder(args);

// Add the rate‑limiting middleware with 10 requests per second and 100 per minute
builder.Services.AddRateLimitingMiddleware(options =>
{
    options.ConfigureLimits(requestsPerSecond: 10, requestsPerMinute: 100);
});

var app = builder.Build();

app.UseRateLimiting(); // registers the middleware
app.MapGet("/", () => "Hello World!");
app.Run();
```

### Example 2: Checking allowance and consuming tokens manually
```csharp
using DotNetResiliencePipeline.RateLimiting;

var middleware = new RateLimitingMiddleware();
middleware.ConfigureLimits(requestsPerSecond: 5, requestsPerMinute: 50);

string clientId = "client-123";

if (middleware.IsRequestAllowed(clientId))
{
    // Proceed with request handling
    bool consumed = middleware.TryConsumeTokens(clientId, tokensToConsume: 1);
    if (!consumed)
    {
        // This should not happen if IsRequestAllowed returned true, but guard anyway
        throw new InvalidOperationException("Token consumption failed unexpectedly.");
    }
}
else
{
    // Respond with 429 Too Many Requests
    var status = middleware.GetStatus(clientId);
    // Use status.NextResetSecond/Minute to inform the client when to retry
}
```

## Notes
- The middleware is thread‑safe; concurrent calls to `IsRequestAllowed`, `TryConsumeTokens`, and the status methods will not corrupt internal state.  
- `ResetClient` and `ClearAll` modify the internal tracking dictionaries; if invoked while a request is being evaluated for the same client, the request may observe either the old or the new state, but no exceptions will be thrown.  
- `GetAllStatus` returns a snapshot; modifications to the returned dictionary do not affect the middleware’s internal state.  
- When a client has never been seen before, `IsRequestAllowed` will treat the bucket as full (i.e., allow the request) and subsequently create tracking entry upon the first successful token consumption.  
- Setting limits via `ConfigureLimits` with a non‑null `clientId` overrides the default limits for that specific client; subsequent calls with a `null` `clientId` affect the global/default bucket.  
- All `DateTime` values returned by `NextResetSecond` and `NextResetMinute` are expressed in UTC.  
- The underlying `RateLimiter` property is exposed for advanced scenarios; altering its behavior directly may bypass the middleware’s safeguards and is not recommended unless you are familiar with its contract.
