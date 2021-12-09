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

## HttpClientExceptionExtensions

The `HttpClientExceptionExtensions` class provides a set of convenience extensions for `HttpRequestException` and other `Exception` types commonly encountered when working with HTTP clients.

...

