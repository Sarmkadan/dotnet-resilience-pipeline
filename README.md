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

## Benchmarks

### RetryBenchmarks

The `RetryBenchmarks` class provides performance benchmarks for different retry strategies implemented in the `RetryPolicy` class. It measures the execution time and memory allocation of various retry operations including fixed interval, exponential backoff, and exponential backoff with jitter strategies.

### Example Usage

```csharp
using DotNetResiliencePipeline.Benchmarks;
using DotNetResiliencePipeline.Domain.Policies;

// Create retry policies with different backoff strategies
var fixedRetryPolicy = new RetryPolicy("fixed-retry")
{
    MaxRetries = 3,
    InitialDelay = TimeSpan.FromMilliseconds(100),
    Strategy = RetryPolicy.BackoffStrategy.Fixed
};

var exponentialRetryPolicy = new RetryPolicy("exponential-retry")
{
    MaxRetries = 5,
    InitialDelay = TimeSpan.FromMilliseconds(100),
    Strategy = RetryPolicy.BackoffStrategy.Exponential,
    BackoffMultiplier = 2.0,
    MaxDelay = TimeSpan.FromSeconds(30)
};

var jitterRetryPolicy = new RetryPolicy("jitter-retry")
{
    MaxRetries = 5,
    InitialDelay = TimeSpan.FromMilliseconds(100),
    Strategy = RetryPolicy.BackoffStrategy.ExponentialWithJitter,
    BackoffMultiplier = 2.0,
    MaxDelay = TimeSpan.FromSeconds(30),
    UseJitter = true,
    JitterFactor = 1.0
};

// Calculate delays for retry attempts
var fixedDelay = fixedRetryPolicy.GetNextDelayMs(1);
var exponentialDelay = exponentialRetryPolicy.GetNextDelayMs(2);
var jitterDelay = jitterRetryPolicy.GetNextDelayMs(3);

// Check if an exception is retryable
var isRetryable = fixedRetryPolicy.IsRetryable(new TimeoutException());

// Get policy configuration
var strategy = fixedRetryPolicy.Strategy;
var maxRetries = fixedRetryPolicy.MaxRetries;
```

## CircuitBreakerDiagnosticsValidation

The `CircuitBreakerDiagnosticsValidation` class provides methods to validate circuit breaker configurations. It ensures that the configuration is valid and throws exceptions if it's not.

### Example Usage
```csharp
using Resilience.Utilities;

// Validate circuit breaker configuration
var validationErrors = CircuitBreakerDiagnosticsValidation.Validate(new CircuitBreakerConfiguration
{
    // Initialize properties
});

// Check if configuration is valid
var isValid = CircuitBreakerDiagnosticsValidation.IsValid(new CircuitBreakerConfiguration
{
    // Initialize properties
});

// Ensure configuration is valid, throws if not
CircuitBreakerDiagnosticsValidation.EnsureValid(new CircuitBreakerConfiguration
{
    // Initialize properties
});
```
## PoliciesControllerExtensions

The `PoliciesControllerExtensions` class provides a set of extension methods for working with policy-related operations. It enables creating, retrieving, validating, and checking the existence of policies.

### Example Usage
```csharp
using Resilience.Api;
using Resilience.Dtos;

// Create a policy
var policy = new PolicyDto { /* initialize properties */ };
var createResponse = await PoliciesControllerExtensions.CreatePolicyAsync(policy);
Console.WriteLine(createResponse.ToJson());

// Get all policies
var policies = await PoliciesControllerExtensions.GetAllPoliciesListAsync();
foreach (var p in policies)
{
    Console.WriteLine(p.ToJson());
}

// Get a policy by id
var policyId = Guid.NewGuid();
var policyById = await PoliciesControllerExtensions.GetPolicyAsync<PolicyDto>(policyId);
Console.WriteLine(policyById?.ToJson());

// Validate policy configuration
var validationResult = await PoliciesControllerExtensions.ValidatePolicyConfigurationAsync(policy);
Console.WriteLine(validationResult.ToJson());

// Check if policy exists
var exists = await PoliciesControllerExtensions.PolicyExistsAsync(policyId);
Console.WriteLine(exists);
```
## ...
