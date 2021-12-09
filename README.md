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
## ...
