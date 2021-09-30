# HttpClientException

Exception type representing failures encountered when making HTTP requests using `HttpClient` within resilience pipelines. Used to carry contextual information about the failed request such as the target URL, HTTP method, response status, or timeout duration to facilitate error handling and diagnostics.

## API

### Properties

#### `ClientName`
- **Purpose**: Identifies the logical name of the HTTP client or pipeline that raised the exception.
- **Type**: `string?`
- **Usage**: Useful in systems with multiple named clients to determine which client failed.

#### `RequestUrl`
- **Purpose**: Contains the full URL of the HTTP request that failed.
- **Type**: `string?`
- **Usage**: Enables logging or retry logic to inspect the exact endpoint that caused the failure.

#### `HttpMethod`
- **Purpose**: Indicates the HTTP method (e.g., GET, POST) used in the failed request.
- **Type**: `string?`
- **Usage**: Helps differentiate between request types when handling errors.

#### `StatusCode`
- **Purpose**: Provides the HTTP status code from the response, if available.
- **Type**: `int`
- **Usage**: Used to implement conditional logic based on HTTP status (e.g., 429 Too Many Requests).

#### `Timeout`
- **Purpose**: Specifies the timeout duration that was exceeded during the request.
- **Type**: `TimeSpan`
- **Usage**: Useful for diagnosing slow responses or configuring retry delays.

### Constructors

#### `HttpClientException()`
- **Purpose**: Initializes a new instance of the `HttpClientException` class with default values.
- **Parameters**: None.
- **Usage**: Used when no additional context beyond the base exception is required.

#### `HttpClientException(string? message)`
- **Purpose**: Initializes a new instance with a custom error message.
- **Parameters**:
  - `message` (string?): The error message describing the exception.
- **Usage**: Allows for descriptive error reporting when throwing the exception.

#### `HttpClientException(string? message, Exception? innerException)`
- **Purpose**: Initializes a new instance with a message and an inner exception.
- **Parameters**:
  - `message` (string?): The error message.
  - `innerException` (Exception?): The inner exception that caused this exception.
- **Usage**: Used to wrap lower-level exceptions with HTTP-specific context.

#### `HttpTimeoutException()`
- **Purpose**: Initializes a new instance of the `HttpTimeoutException` class with default values.
- **Parameters**: None.
- **Usage**: Used when a request times out and no additional context is provided.

#### `HttpTimeoutException(string? message)`
- **Purpose**: Initializes a new instance with a custom error message.
- **Parameters**:
  - `message` (string?): The error message describing the timeout.
- **Usage**: Used to report timeouts with a descriptive message.

#### `HttpTimeoutException(string? message, Exception? innerException)`
- **Purpose**: Initializes a new instance with a message and an inner exception.
- **Parameters**:
  - `message` (string?): The error message.
  - `innerException` (Exception?): The inner exception that caused the timeout.
- **Usage**: Used to wrap underlying timeout exceptions (e.g., `TaskCanceledException`) with HTTP context.

#### `HttpResponseException(int statusCode)`
- **Purpose**: Initializes a new instance with an HTTP status code.
- **Parameters**:
  - `statusCode` (int): The HTTP status code from the response.
- **Usage**: Used when an HTTP response with a non-success status code is received.

#### `HttpResponseException(int statusCode, string? message)`
- **Purpose**: Initializes a new instance with a status code and a custom message.
- **Parameters**:
  - `statusCode` (int): The HTTP status code.
  - `message` (string?): The error message.
- **Usage**: Used to report non-success responses with additional context.

#### `HttpResponseException(int statusCode, string? message, Exception? innerException)`
- **Purpose**: Initializes a new instance with a status code, message, and inner exception.
- **Parameters**:
  - `statusCode` (int): The HTTP status code.
  - `message` (string?): The error message.
  - `innerException` (Exception?): The inner exception.
- **Usage**: Used to wrap lower-level exceptions with HTTP response context.

#### `InvalidHttpRequestException(string? message)`
- **Purpose**: Initializes a new instance indicating an invalid HTTP request.
- **Parameters**:
  - `message` (string?): The error message describing the invalid request.
- **Usage**: Used when the request itself is malformed or invalid before being sent.

#### `InvalidHttpRequestException(string? message, Exception? innerException)`
- **Purpose**: Initializes a new instance with a message and an inner exception.
- **Parameters**:
  - `message` (string?): The error message.
  - `innerException` (Exception?): The inner exception that caused the invalid request.
- **Usage**: Used to wrap exceptions from request validation or construction.

## Usage

### Example 1: Handling HTTP 429 Response
