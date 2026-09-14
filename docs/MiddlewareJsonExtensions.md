# Middleware Json Extensions

Provides JSON serialization and deserialization extensions for middleware classes.

## RateLimitingMiddlewareJsonExtensions

Provides JSON serialization and deserialization extensions for <see cref="RateLimitingMiddleware"/>.

### ToJson(RateLimitingMiddleware value, bool indented = false)

Serializes the specified <see cref="RateLimitingMiddleware"/> instance to a JSON string.

#### Signature
```csharp
public static string ToJson(this RateLimitingMiddleware value, bool indented = false)
```

#### Parameters
- `value`: The <see cref="RateLimitingMiddleware"/> instance to serialize.
- `indented`: Indicates whether the JSON output should be indented.

#### Returns
A JSON string representation of the <paramref name="value"/>.

#### Exceptions
- <see cref="ArgumentNullException">: <paramref name="value"/> is <c>null</c>.

#### Sample JSON
```json
{
  "limiters": {
    "client-123": {
      "tokensPerSecond": 100,
      "tokensPerMinute": 5000,
      "tokensSecond": 100,
      "tokensMinute": 5000,
      "lastRefillSecond": "2026-09-14T10:30:00Z",
      "lastRefillMinute": "2026-09-14T10:30:00Z"
    }
  },
  "limitersLock": {},
  "defaultRequestsPerSecond": 100,
  "defaultRequestsPerMinute": 5000
}
```

#### Example
```csharp
var middleware = new RateLimitingMiddleware();
middleware.ConfigureLimits(50, 1000);

// Simulate some requests
middleware.IsRequestAllowed("client-123");

string json = middleware.ToJson(indented: true);
// json contains formatted JSON representation of the middleware
```

### FromJson(string json)

Deserializes the specified JSON string into a <see cref="RateLimitingMiddleware"/> instance.

#### Signature
```csharp
public static RateLimitingMiddleware? FromJson(string json)
```

#### Parameters
- `json`: The JSON string to deserialize.

#### Returns
A deserialized <see cref="RateLimitingMiddleware"/> instance, or null if the JSON is empty or whitespace.

#### Exceptions
- <see cref="ArgumentNullException">: <paramref name="json"/> is <c>null</c>.
- <see cref="JsonException">: The JSON is invalid or cannot be deserialized.

#### Example
```csharp
string json = @"{
  ""limiters"": {
    ""client-456"": {
      ""tokensPerSecond"": 100,
      ""tokensPerMinute"": 5000,
      ""tokensSecond"": 99,
      ""tokensMinute"": 4999,
      ""lastRefillSecond"": ""2026-09-14T11:00:00Z"",
      ""lastRefillMinute"": ""2026-09-14T11:00:00Z""
    }
  },
  ""limitersLock"": {},
  ""defaultRequestsPerSecond"": 100,
  ""defaultRequestsPerMinute"": 5000
}";

RateLimitingMiddleware? middleware = RateLimitingMiddlewareJsonExtensions.FromJson(json);
// middleware?.DefaultRequestsPerSecond == 100
// middleware?.DefaultRequestsPerMinute == 5000
```

### TryFromJson(string json, out RateLimitingMiddleware? value)

Attempts to deserialize the specified JSON string into a <see cref="RateLimitingMiddleware"/> instance.

#### Signature
```csharp
public static bool TryFromJson(string json, out RateLimitingMiddleware? value)
```

#### Parameters
- `json`: The JSON string to deserialize.
- `value`: When this method returns, contains the deserialized <see cref="RateLimitingMiddleware"/> instance if successful; otherwise, <c>null</c>.

#### Returns
<c>true</c> if deserialization succeeded; otherwise, <c>false</c>.

#### Exceptions
- <see cref="ArgumentNullException">: <paramref name="json"/> is <c>null</c>.

#### Example
```csharp
string json = @"{ ""defaultRequestsPerSecond"": 50 }"; // Partial JSON

if (RateLimitingMiddlewareJsonExtensions.TryFromJson(json, out var middleware))
{
    // Deserialization succeeded
    Console.WriteLine($"Default RPS: {middleware?.DefaultRequestsPerSecond}");
}
else
{
    // Deserialization failed
    Console.WriteLine("Invalid JSON for RateLimitingMiddleware");
}
```

## RateLimitStatusJsonExtensions

Provides JSON serialization extensions for <see cref="RateLimitStatus"/>.

### ToJson(RateLimitStatus status)

Serializes the specified rate limit status to a JSON string.

#### Signature
```csharp
public static string ToJson(this RateLimitStatus status)
```

#### Parameters
- `status`: The rate limit status to serialize.

#### Returns
A JSON string representation of <paramref name="status"/>.

#### Exceptions
- <see cref="ArgumentNullException">: Thrown when <paramref name="status"/> is <see langword="null"/>.

#### Sample JSON
```json
{
  "clientId": "client-123",
  "requestsPerSecond": 100,
  "requestsPerMinute": 5000,
  "remainingTokensPerSecond": 99,
  "remainingTokensPerMinute": 4999,
  "nextResetSecond": "2026-09-14T10:30:01Z",
  "nextResetMinute": "2026-09-14T10:31:00Z"
}
```

#### Example
```csharp
var middleware = new RateLimitingMiddleware();
middleware.ConfigureLimits(100, 5000);

// Check if request is allowed to consume a token
bool allowed = middleware.IsRequestAllowed("client-789");

// Get status after the request
RateLimitStatus status = middleware.GetStatus("client-789");

string json = status.ToJson();
// json contains JSON representation of the rate limit status
```