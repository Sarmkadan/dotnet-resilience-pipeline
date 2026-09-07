# JSON extension reference

The `src/` tree contains the JSON helper classes below. Each class targets one
type and uses `System.Text.Json`. `ToJson` is an extension method; `FromJson`
and `TryFromJson` are static methods on the helper class.

## Available helpers

### `PolicyResultJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Domain`
- Target type: `PolicyResult`
- `string ToJson(this PolicyResult value, bool indented = false)`
- `PolicyResult? FromJson(string json)`
- `bool TryFromJson(string json, out PolicyResult? value)`

### `BulkheadPolicyJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Domain.Policies`
- Target type: `BulkheadPolicy`
- `string ToJson(this BulkheadPolicy value, bool indented = false)`
- `BulkheadPolicy? FromJson(string json)`
- `bool TryFromJson(string json, out BulkheadPolicy? value)`

### `RetryServiceJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Services`
- Target type: `RetryService`
- `string ToJson(this RetryService value, bool indented = false)`
- `RetryService? FromJson(string json)`
- `bool TryFromJson(string json, out RetryService? value)`

### `CircuitBreakerServiceJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Services`
- Target type: `CircuitBreakerService`
- `string ToJson(this CircuitBreakerService value, bool indented = false)`
- `CircuitBreakerService? FromJson(string json)`
- `bool TryFromJson(string json, out CircuitBreakerService? value)`

### `FallbackServiceJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Services`
- Target type: `FallbackService`
- `string ToJson(this FallbackService value, bool indented = false)`
- `FallbackService? FromJson(string json)`
- `bool TryFromJson(string json, out FallbackService? value)`
- `bool TryFromJson(string json, JsonSerializerOptions options, out FallbackService? value)`

The second `TryFromJson` overload accepts caller-provided
`System.Text.Json.JsonSerializerOptions`.

### `BulkheadServiceJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Services`
- Target type: `BulkheadService`
- `string ToJson(this BulkheadService value, bool indented = false)`
- `BulkheadService? FromJson(string json)`
- `bool TryFromJson(string json, out BulkheadService? value)`

### `ResiliencyExceptionJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Exceptions`
- Target type: `ResiliencyException` (including derived instances when serializing)
- `string ToJson(this ResiliencyException value, bool indented = false)`
- `ResiliencyException? FromJson(string json)`
- `bool TryFromJson(string json, out ResiliencyException? value)`

### `ResiliencyEventPublisherJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Events`
- Target type: `ResiliencyEventPublisher`
- `string ToJson(this ResiliencyEventPublisher value, bool indented = false)`
- `ResiliencyEventPublisher FromJson(string json)`
- `bool TryFromJson(string json, out ResiliencyEventPublisher? value)`

### `MetricsAggregatorJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Utilities`
- Target type: `MetricsAggregator`
- `string ToJson(this MetricsAggregator value, bool indented = false)`
- `MetricsAggregator? FromJson(string json)`
- `bool TryFromJson(string json, out MetricsAggregator? value)`

### `MetricsCollectorWorkerJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Workers`
- Target type: `MetricsCollectorWorker`
- `string ToJson(this MetricsCollectorWorker value, bool indented = false)`
- `MetricsCollectorWorker FromJson(string json)`
- `bool TryFromJson(string json, out MetricsCollectorWorker? value)`

### `CsvReportFormatterJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Formatters`
- Target type: `CsvReportFormatter`
- `string ToJson(this CsvReportFormatter value, bool indented = false)`
- `CsvReportFormatter? FromJson(string json)`
- `bool TryFromJson(string json, out CsvReportFormatter? value)`

The `TryFromJson` output is annotated as non-null when the method returns
`true`.

### `CliCommandValidatorJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Cli`
- Target type: `CliCommandValidator`
- `string ToJson(this CliCommandValidator value, bool indented = false)`
- `CliCommandValidator? FromJson(string json)`
- `bool TryFromJson(string json, out CliCommandValidator? value)`

### `RateLimitingMiddlewareJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Middleware`
- Target type: `RateLimitingMiddleware`
- `string ToJson(this RateLimitingMiddleware value, bool indented = false)`
- `RateLimitingMiddleware? FromJson(string json)`
- `bool TryFromJson(string json, out RateLimitingMiddleware? value)`

### `CircuitBreakerDashboardControllerJsonExtensions`

- Namespace: `DotNetResiliencePipeline.Api.Controllers`
- Target type: `CircuitBreakerDashboardController`
- `string ToJson(this CircuitBreakerDashboardController value, bool indented = false)`
- `CircuitBreakerDashboardController? FromJson(string json)`
- `bool TryFromJson(string json, out CircuitBreakerDashboardController? value)`

## Shared usage pattern

Import the target type's namespace, serialize an existing instance with the
extension method, and call the helper class directly to deserialize it. For
example, with `PolicyResult`:

```csharp
using DotNetResiliencePipeline.Domain;

PolicyResult result = GetPolicyResult();

string json = result.ToJson(indented: true);

PolicyResult? restored = PolicyResultJsonExtensions.FromJson(json);

if (PolicyResultJsonExtensions.TryFromJson(json, out PolicyResult? parsed))
{
    // Use parsed according to the target helper's nullable return contract.
}
```

Replace `PolicyResult` and `PolicyResultJsonExtensions` with any target/helper
pair listed above. Input validation and failure behavior vary between helpers:
some reject null, empty, or whitespace input, and some return null or false for
those inputs. Consult the selected method's XML documentation when that
distinction matters.
