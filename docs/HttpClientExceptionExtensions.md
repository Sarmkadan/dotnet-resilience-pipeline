# HttpClientExceptionExtensions

Extension methods for analyzing and extracting details from `HttpClient`-related exceptions, particularly those thrown by `HttpClient` or derived from `HttpRequestException`.

## API

### `public static string GetFullErrorMessage(Exception exception)`

Extracts a human-readable error message from an exception, including the full chain of inner exceptions.

- **Parameters**
  - `exception`: The exception to analyze. Must not be `null`.
- **Return value**
  - A concatenated string containing the message of each exception in the chain, separated by ` -> `.
- **Exceptions**
  - Throws `ArgumentNullException` if `exception` is `null`.

---

### `public static bool IsClientError(Exception exception)`

Determines whether the exception represents a client error (HTTP 4xx status code).

- **Parameters**
  - `exception`: The exception to check. Must not be `null`.
- **Return value**
  - `true` if the exception is an `HttpRequestException` with a 4xx status code; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `exception` is `null`.

---

### `public static bool IsServerError(Exception exception)`

Determines whether the exception represents a server error (HTTP 5xx status code).

- **Parameters**
  - `exception`: The exception to check. Must not be `null`.
- **Return value**
  - `true` if the exception is an `HttpRequestException` with a 5xx status code; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `exception` is `null`.

---
### `public static bool IsTimeoutError(Exception exception)`

Determines whether the exception represents a timeout error.

- **Parameters**
  - `exception`: The exception to check. Must not be `null`.
- **Return value**
  - `true` if the exception is a `TimeoutException` or an `HttpRequestException` with a timeout-related status; otherwise, `false`.
- **Exceptions**
  - Throws `ArgumentNullException` if `exception` is `null`.

---
### `public static string GetErrorCode(Exception exception)`

Extracts a standardized error code from an exception, if available.

- **Parameters**
  - `exception`: The exception to analyze. Must not be `null`.
- **Return value**
  - A string representing the error code, or `null` if no code is found.
- **Exceptions**
  - Throws `ArgumentNullException` if `exception` is `null`.

## Usage

```csharp
using System;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;

public class Example
{
    public static async Task CheckResponse(HttpResponseMessage response)
    {
        try
        {
            response.EnsureSuccessStatusCode();
        }
        catch (HttpRequestException ex)
        {
            if (HttpClientExceptionExtensions.IsClientError(ex))
            {
                Console.WriteLine($"Client error: {HttpClientExceptionExtensions.GetFullErrorMessage(ex)}");
            }
            else if (HttpClientExceptionExtensions.IsServerError(ex))
            {
                Console.WriteLine($"Server error: {HttpClientExceptionExtensions.GetErrorCode(ex)}");
            }
        }
    }
}
```

```csharp
using System;
using System.Net.Http;
using System.Threading.Tasks;

public class Example
{
    public static async Task LogTimeout(HttpClient client)
    {
        try
        {
            await client.GetAsync("https://example.com");
        }
        catch (Exception ex) when (HttpClientExceptionExtensions.IsTimeoutError(ex))
        {
            Console.WriteLine($"Timeout occurred: {HttpClientExceptionExtensions.GetFullErrorMessage(ex)}");
        }
    }
}
```

## Notes

- All methods validate input and throw `ArgumentNullException` if `exception` is `null`.
- Methods are thread-safe as they perform only read operations on the exception and do not mutate state.
- Timeout detection relies on the presence of a `TimeoutException` or an `HttpRequestException` with a status code indicating a timeout (e.g., 504 Gateway Timeout). Custom timeout implementations may not be detected.
