# JsonSerializerOptionsProvider

`JsonSerializerOptionsProvider` centralizes the JSON serialization contract used by selected JSON extension helpers. It is an `internal static` class and exposes one `internal static readonly` option set, `SharedOptions`.

## Configuration

`SharedOptions` is created with the `JsonSerializerDefaults.Web` baseline and then explicitly configures the following settings:

| Setting | Value | Effect |
| --- | --- | --- |
| `PropertyNamingPolicy` | `JsonNamingPolicy.CamelCase` | Serializes property names in camel case. |
| `WriteIndented` | `false` | Produces compact JSON by default. |
| `DefaultIgnoreCondition` | `JsonIgnoreCondition.WhenWritingNull` | Omits properties whose values are `null` during serialization. |
| `ReferenceHandler` | `ReferenceHandler.IgnoreCycles` | Ignores object references that would create cycles. |
| `PropertyNameCaseInsensitive` | `true` | Matches JSON property names without regard to case during deserialization. |
| `Converters` | `new JsonStringEnumConverter(JsonNamingPolicy.CamelCase)` | Reads and writes enum values as strings, using camel case when writing enum names. |

The options are shared and should be treated as the library's common, compact serializer configuration. Helpers that support formatted output copy `SharedOptions` into a new `JsonSerializerOptions` instance and set `WriteIndented` to `true`; they do not mutate the shared instance.

## Usage

The following `*JsonExtensions` classes directly reference `JsonSerializerOptionsProvider.SharedOptions`:

- `src/Exceptions/ResiliencyExceptionJsonExtensions.cs` uses it for serialization, deserialization, and try-deserialization of `ResiliencyException`. Its `ToJson` overload creates an indented copy when requested.
- `src/Events/ResiliencyEventPublisherJsonExtensions.cs` uses it for serialization, deserialization, and try-deserialization of `ResiliencyEventPublisher`. Its `ToJson` overload also creates an indented copy when requested.

## Example

The provider is internal, so this pattern is intended for code within the library:

```csharp
var options = indented
    ? new JsonSerializerOptions(JsonSerializerOptionsProvider.SharedOptions)
    {
        WriteIndented = true
    }
    : JsonSerializerOptionsProvider.SharedOptions;

string json = JsonSerializer.Serialize(value, options);
```
