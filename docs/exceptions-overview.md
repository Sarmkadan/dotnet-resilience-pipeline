# Exception Hierarchy Overview

This document provides a comprehensive overview of the exception hierarchy in the dotnet-resilience-pipeline library, including inheritance relationships, usage contexts, and key properties.

## Inheritance Tree

```
System.Exception
├── DotnetResiliencePipelineException
│   ├── ConfigurationException
│   └── ValidationException
└── ResiliencyException
    ├── CircuitBreakerOpenException
    ├── BulkheadRejectedException
    ├── OperationTimeoutException
    ├── MaxRetriesExceededException
    ├── FallbackFailedException
    ├── InvalidPolicyConfigurationException
    ├── PipelineExecutionException
    ├── HttpClientException
    │   ├── InvalidHttpRequestException
    │   ├── HttpResponseException
    │   └── HttpTimeoutException
    └── WebhookException
        ├── WebhookDeliveryFailedException
        ├── WebhookRegistrationException
        └── InvalidWebhookException
```

## Exception Reference Table

| Exception | Thrown By | Key Properties |
|-----------|-----------|----------------|
| **ResiliencyException** | Base class for all resilience pipeline failures | `PolicyName`, `PolicyType`, `OccurredAt` |
| **CircuitBreakerOpenException** | Circuit breaker policy when open | `TimeUntilRetry`, `ConsecutiveFailures` |
| **BulkheadRejectedException** | Bulkhead policy when capacity exceeded | `CurrentExecutions`, `MaxExecutions`, `QueuedRequests` |
| **OperationTimeoutException** | Timeout policy when operation exceeds limit | `Timeout`, `ActualExecutionTimeMs` |
| **MaxRetriesExceededException** | Retry policy when all attempts exhausted | `AttemptCount`, `AttemptExceptions` |
| **FallbackFailedException** | Fallback policy when both primary and fallback fail | `PrimaryException`, `FallbackException` |
| **InvalidPolicyConfigurationException** | Policy validation during pipeline setup | `ConfigurationErrors` |
| **PipelineExecutionException** | Pipeline execution engine for unrecoverable errors | `ExecutionId`, `AppliedPolicies` |
| **HttpClientException** | Base class for HTTP client failures | `ClientName`, `RequestUrl` |
| **InvalidHttpRequestException** | HTTP client when request configuration invalid | `HttpMethod` |
| **HttpResponseException** | HTTP client when response indicates error status | `StatusCode` |
| **HttpTimeoutException** | HTTP client when operation times out | `Timeout` |
| **WebhookException** | Base class for webhook-related failures | `WebhookId`, `WebhookUrl` |
| **WebhookDeliveryFailedException** | Webhook service when delivery fails after retries | `AttemptCount`, `EventType` |
| **WebhookRegistrationException** | Webhook service when registration fails | *(inherits WebhookId, WebhookUrl)* |
| **InvalidWebhookException** | Webhook service when subscription invalid | *(inherits WebhookId, WebhookUrl)* |
| **DotnetResiliencePipelineException** | Base class for library-specific exceptions | *(inherits from System.Exception)* |
| **ConfigurationException** | Configuration loading/validation failures | *(inherits from DotnetResiliencePipelineException)* |
| **ValidationException** | Input parameter or configuration validation failures | `ValidationErrors` |

## Detailed Documentation

For detailed information about each exception type, refer to the specific documentation files:

- [ResiliencyException](ResiliencyException.md) - Base exception for all resilience pipeline failures
- [HttpClientException](HttpClientException.md) - Base exception for HTTP client-related failures
- [WebhookException](WebhookException.md) - Base exception for webhook-related failures
- [ValidationException](ValidationException.md) - Thrown when validation of input parameters or configuration fails

## Usage Guidelines

All exceptions in this library follow these patterns:

1. **Policy Context**: Resiliency exceptions include policy metadata (`PolicyName`, `PolicyType`) to identify which resilience strategy caused the failure
2. **Timing Information**: `OccurredAt` timestamp captures when the exception was raised
3. **Strategy-Specific Properties**: Each derived exception type includes properties relevant to its specific failure mode
4. **Exception Chaining**: Inner exceptions are preserved to maintain the original error context
5. **Consistent Formatting**: Override `ToString()` methods provide detailed, formatted output for logging

## Handling Recommendations

When catching exceptions from the resilience pipeline:

```csharp
try
{
    await pipeline.ExecuteAsync(operation);
}
catch (CircuitBreakerOpenException ex)
{
    // Handle circuit breaker open state
    Log.Warning($"Circuit breaker {ex.PolicyName} is open. Retry after {ex.TimeUntilRetry}");
}
catch (BulkheadRejectedException ex)
{
    // Handle bulkhead rejection
    Log.Warning($"Bulkhead {ex.PolicyName} saturated: {ex.CurrentExecutions}/{ex.MaxExecutions}");
}
catch (OperationTimeoutException ex)
{
    // Handle timeout
    Log.Error($"Operation timed out after {ex.ActualExecutionTimeMs}ms (limit: {ex.Timeout})");
}
catch (MaxRetriesExceededException ex)
{
    // Handle exhausted retries
    Log.Error($"All {ex.AttemptCount} retry attempts failed for policy {ex.PolicyName}");
}
catch (HttpResponseException ex) when (ex.StatusCode >= 500)
{
    // Handle HTTP 5xx errors
    Log.Error($"HTTP {ex.StatusCode} from {ex.RequestUrl}");
}
catch (ValidationException ex)
{
    // Handle validation failures
    foreach (var error in ex.ValidationErrors)
    {
        Log.Error($"Validation failed for {error.Key}: {error.Value}");
    }
}
catch (ResiliencyException ex)
{
    // Handle any other resilience exception
    Log.Error($"Resilience failure in {ex.PolicyType}: {ex.Message}");
}
```