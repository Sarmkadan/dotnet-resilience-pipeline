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

## HttpClientExceptionExtensions

The `HttpClientExceptionExtensions` class provides a set of convenience extensions for `HttpRequestException` and other `Exception` types commonly encountered when working with HTTP clients.

The extensions allow you to easily determine the type and severity of HTTP errors.

### Example Usage
```csharp
try
{
    var response = await httpClient.GetAsync("https://example.com/nonexistent");
    response.EnsureSuccessStatusCode();
}
catch (HttpRequestException ex)
{
    var errorCode = ex.GetErrorCode();
    var fullErrorMessage = ex.GetFullErrorMessage();

    Console.WriteLine($"HTTP Error Code: {errorCode}");
    Console.WriteLine($"Detailed Error Message: {fullErrorMessage}");

    if (ex.IsClientError())
    {
        Console.WriteLine("Client error occurred.");
    }
    else if (ex.IsServerError())
    {
        Console.WriteLine("Server error occurred.");
    }
    else if (ex.IsTimeoutError())
    {
        Console.WriteLine("Timeout error occurred.");
    }
}
```

## PipelineEventObserverExtensions

`PipelineEventObserverExtensions` offers a collection of helper methods for inspecting and managing the event handlers that a `PipelineEventObserver` has registered.  
These extensions make it easy to query active/inactive handler counts, locate specific handlers, toggle their activation state, and obtain formatted diagnostics about the observer’s current configuration.

### Example Usage
```csharp
using System;
using System.Collections.Generic;
using Resilience.Pipeline.Events; // Adjust the namespace to match your project

// Assume a concrete observer instance exists
var observer = new PipelineEventObserver();

// 1. Query handler statistics
int activeCount   = PipelineEventObserverExtensions.GetActiveHandlersCount(observer);
int inactiveCount = PipelineEventObserverExtensions.GetInactiveHandlersCount(observer);
Console.WriteLine($"Active: {activeCount}, Inactive: {inactiveCount}");

// 2. Find a handler for a specific event type (e.g., HttpRequestEvent)
EventHandler? handler = PipelineEventObserverExtensions.FindHandler(observer, typeof(HttpRequestEvent));
if (handler != null)
{
    Console.WriteLine($"Found handler for {handler.GetType().Name}");
}

// 3. Toggle the activation state of a handler (if one was found)
if (handler != null)
{
    bool toggled = PipelineEventObserverExtensions.ToggleHandlerActive(observer, handler);
    Console.WriteLine($"Handler active state toggled: {toggled}");
}

// 4. Retrieve a formatted statistics string
string stats = PipelineEventObserverExtensions.GetStatisticsFormatted(observer);
Console.WriteLine("Observer statistics:");
Console.WriteLine(stats);

// 5. Get a summary of all registered handlers
string summary = PipelineEventObserverExtensions.GetHandlersSummary(observer);
Console.WriteLine("Handlers summary:");
Console.WriteLine(summary);

// 6. List all handlers for a particular event type
List<EventHandler> httpHandlers = PipelineEventObserverExtensions.GetHandlersByEventType(observer, typeof(HttpRequestEvent));
Console.WriteLine($"Number of HTTP request handlers: {httpHandlers.Count}");

// 7. Quick boolean check for any active handlers
bool hasActive = PipelineEventObserverExtensions.HasActiveHandlers(observer);
Console.WriteLine($"Observer has active handlers: {hasActive}");
```

## ...

