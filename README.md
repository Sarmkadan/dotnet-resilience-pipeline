# DotNet Resilience Pipeline

![CI](https://github.com/sarmkadan/dotnet-resilience-pipeline/actions/workflows/ci.yml/badge.svg)
![License](https://img.shields.io/github/license/sarmkadan/dotnet-resilience-pipeline)
![.NET](https://img.shields.io/badge/.NET-10.0-512BD4)

A comprehensive, production-grade resilience library for .NET applications featuring circuit breaker, bulkhead, retry, timeout, and fallback patterns with fluent configuration and built-in observability.

## Table of Contents

- [Overview](#overview)
- [Features](#features)
- [Installation](#installation)
- [Quick Start](#quick-start)
- [Architecture](#architecture)
- [Policy Types](#policy-types)
- [Configuration](#configuration)
- [Examples](#examples)
- [API Reference](#api-reference)
- [Monitoring & Metrics](#monitoring--metrics)
- [Circuit Breaker Dashboard](#circuit-breaker-dashboard)
- [Failure Injection Testing](#failure-injection-testing)
- [Resilience Metrics Export](#resilience-metrics-export)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Testing](#testing)
- [Benchmarks](#benchmarks)
- [Related Projects](#related-projects)
- [Contributing](#contributing)
- [License](#license)

## ...

## ResiliencyEventPublisherExtensions

The `ResiliencyEventPublisherExtensions` class provides methods for publishing resiliency events with historical tracking, retrieving the last event of a specific type, and managing subscriber counts. It supports exception publishing and state reset for event tracking.

### Example Usage
```csharp
using Resilience.Events;

// Assume a concrete publisher instance exists
var publisher = new ResiliencyEventPublisher();

// 1. Publish an event with history tracking
await publisher.PublishWithHistoryAsync(new MyResiliencyEvent("Test"));

// 2. Retrieve the last event of a specific type
var lastEvent = publisher.GetLastEvent<MyResiliencyEvent>();

// 3. Get total subscriber count
int totalSubscribers = publisher.GetSubscriberCount();

// 4. Get subscriber count for a specific event type
int specificSubscribers = publisher.GetSubscriberCount<MyResiliencyEvent>();

// 5. Reset all tracked state
publisher.Reset();
```

## HttpClientFactoryExtensions

The `HttpClientFactoryExtensions` class provides simplified HTTP client operations with built-in resiliency policies. It offers typed methods for common HTTP patterns like JSON serialization and status code inspection.

### Example Usage
```csharp
using Resilience.Http;

var client = new HttpClient
{
    BaseAddress = new Uri("https://api.example.com")
};

// Check if client is configured
if (client.HasClient())
{
    // GET as string
    var rawResponse = await client.GetStringAsync("/users/1");
    
    // GET as deserialized object
    var user = await client.GetFromJsonAsync<User>("/users/1");
    
    // POST with JSON payload
    var request = new UserRequest { Name = "Alice" };
    await client.PostAsJsonAsync("/users", request);
    
    // POST with JSON payload and response
    var response = await client.PostAsJsonAndGetAsync<UserRequest, UserResponse>(
        "/users", 
        request);
    
    // GET status code only
    var statusCode = await client.GetStatusCodeAsync("/health");
}
```

## HttpClientExceptionExtensions

The `HttpClientExceptionExtensions` class provides a set of convenience extensions for `HttpRequestException` and other `Exception` types commonly encountered when working with HTTP clients.

## RetryPolicyExtensions

The `RetryPolicyExtensions` class provides a set of extension methods for working with `RetryPolicy` instances. It allows you to add or remove retryable exceptions, execute actions with retry, and get configuration summaries.

### Example Usage
```csharp
using Resilience.Policies;

var retryPolicy = new RetryPolicy();

// Add a retryable exception
retryPolicy = retryPolicy.AddRetryableException<TimeoutException>();

// Execute an action with retry
bool success = retryPolicy.ExecuteWithRetry(() => {
    // Code to be executed with retry
});

// Get the configuration summary
string summary = retryPolicy.GetConfigurationSummary();
```

...
