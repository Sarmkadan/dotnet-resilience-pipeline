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
- [FallbackPolicy](#fallbackpolicy)
- [API Reference](#api-reference)
- [Monitoring & Metrics](#monitoring--metrics)
- [Circuit Breaker Dashboard](#circuit-breaker-dashboard)
- [CliCommandValidator](#clicommandvalidator)
- [FailureInjectionService](#failureinjectionservice)
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

## DotnetResiliencePipelineOptions

The `DotnetResiliencePipelineOptions` class provides a centralized configuration for .NET Resilience Pipeline. It allows you to configure circuit breaker, retry, timeout, bulkhead, and fallback policies.

Here's an example usage:

```csharp
var options = new DotnetResiliencePipelineOptions
{
    CircuitBreaker = new DotnetResiliencePipelineOptions.CircuitBreakerOptions
    {
        FailureThreshold = 5,
        OpenDurationSeconds = 30,
        SuccessThresholdInHalfOpen = 3
    },
    Retry = new DotnetResiliencePipelineOptions.RetryOptions
    {
        MaxRetries = 3,
        InitialDelayMs = 100,
        Strategy = RetryPolicy.BackoffStrategy.Exponential,
        MaxDelayMs = 30000,
        BackoffMultiplier = 2.0,
        UseJitter = true,
        JitterFactor = 1.0
    },
    Timeout = new DotnetResiliencePipelineOptions.TimeoutOptions
    {
        TimeoutSeconds = 10
    },
    Bulkhead = new DotnetResiliencePipelineOptions.BulkheadOptions
    {
        MaxParallelization = 10,
        MaxQueueLength = 50
    },
    Fallback = new DotnetResiliencePipelineOptions.FallbackOptions
    {
        FallbackOnAnyException = true,
        FallbackTimeoutSeconds = 5
    }
};

var circuitBreakerPolicy = options.CircuitBreaker.ToPolicy("circuit-breaker-policy");
var retryPolicy = options.Retry.ToPolicy("retry-policy");
var timeoutPolicy = options.Timeout.ToPolicy("timeout-policy");

Console.WriteLine($"Circuit Breaker: {circuitBreakerPolicy.FailureThreshold}");
Console.WriteLine($"Retry: {retryPolicy.MaxRetries}");
Console.WriteLine($"Timeout: {timeoutPolicy.TimeoutSeconds}");
Console.WriteLine($"Bulkhead: {options.Bulkhead.MaxParallelization}");
Console.WriteLine($"Fallback: {options.Fallback.FallbackOnAnyException}");
```

## ...

