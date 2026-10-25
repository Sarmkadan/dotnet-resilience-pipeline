# ConfigurationException

Sealed exception thrown when a configuration value is missing or invalid, carrying the name of the offending configuration key so callers can tell which input was rejected.

## API

### Constructors

#### `ConfigurationException(string message, string configurationKey = "")`
- **Purpose**: Creates an exception with an error message and an optional configuration key.
- **Parameters**:
  - `message`: The error message that explains the reason for the exception.
  - `configurationKey`: The configuration key associated with the error. Defaults to `""`.
- **Remarks**: `InnerException` is `null`.

#### `ConfigurationException(string message, Exception innerException, string configurationKey = "")`
- **Purpose**: Creates an exception with an error message, an inner exception, and an optional configuration key.
- **Parameters**:
  - `message`: The error message that explains the reason for the exception.
  - `innerException`: The exception that caused this one.
  - `configurationKey`: The configuration key associated with the error. Defaults to `""`.
- **Remarks**: The inner exception is passed to the base `DotnetResiliencePipelineException` unchanged.

### Properties

#### `ConfigurationKey`
- **Purpose**: Gets or sets the configuration key that caused the exception.
- **Type**: `string`
- **Remarks**: Never `null`; it is `""` when no key was supplied. The value is usually the name of the offending parameter or property (for example `BaseUrl`, `baseAddress`, or `apiName`), not a full path into a configuration file. The setter is public, so the key can be changed after the exception is created.

### Base Class

#### `DotnetResiliencePipelineException`
- **Purpose**: Abstract base class for all exceptions raised by the Dotnet Resilience Pipeline.
- **Inheritance**: `ConfigurationException` derives from `DotnetResiliencePipelineException`, which derives from `Exception`.
- **Remarks**: Declared in the global namespace. It has no members beyond the standard `Exception` members; its two constructors take `(string? message)` and `(string? message, Exception? innerException)`.

## When It Is Thrown

`ConfigurationException` is thrown by the integration layer (`src/Integration/`) when a value fails validation before any request is made:

- **`ExternalApiClient.RegisterApi`**: the `BaseUrl` is empty or whitespace (`ConfigurationKey` = `BaseUrl`), or it is not an absolute URI (`ConfigurationKey` = `BaseUrl`).
- **`ExternalApiClient.GetAsync`, `PostAsync`, `TestConnectionAsync`**: no API has been registered under the given name (`ConfigurationKey` = `apiName`).
- **`HttpClientFactory.CreateClient`**: `baseAddress` is non-empty but not an absolute URI (`ConfigurationKey` = `baseAddress`).

Argument-null problems are reported with `ArgumentNullException` instead, not with `ConfigurationException`.

## Usage

### Catching a configuration error

```csharp
try
{
    var response = await client.GetAsync<string>("payments-api", "/status");
}
catch (ConfigurationException ex)
{
    // ex.ConfigurationKey is "apiName" here; ex.Message names the unregistered API.
    Console.Error.WriteLine($"Configuration problem with '{ex.ConfigurationKey}': {ex.Message}");
}
```

### Throwing with an inner exception

```csharp
try
{
    return JsonSerializer.Deserialize<ApiSettings>(json)!;
}
catch (JsonException ex)
{
    throw new ConfigurationException("Settings file is not valid JSON", ex, "ApiSettings");
}
```

### Catching the base type

Because `ConfigurationException` derives from `DotnetResiliencePipelineException`, a handler for the base type also catches it. Catch the base type only when every library exception should be handled the same way.

```csharp
try
{
    await client.PostAsync<string>("payments-api", "/charges", payload);
}
catch (DotnetResiliencePipelineException ex) when (ex is ConfigurationException config)
{
    // Handle configuration errors only; other pipeline exceptions propagate.
    Console.Error.WriteLine($"Fix configuration key '{config.ConfigurationKey}'.");
}
```
