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

### TimeoutBenchmarks

The `TimeoutBenchmarks` class measures the performance of the `TimeoutPolicy`, including execution time recording, timeout handling, and percentile calculations. It demonstrates how the policy tracks execution durations, determines timeouts, and provides metrics such as timeout percentage and execution time percentiles.

#### Example Usage

```csharp
using DotNetResiliencePipeline.Benchmarks;

// Create an instance of the benchmark class
var benchmarks = new TimeoutBenchmarks();
benchmarks.Setup(); // Initializes the TimeoutPolicy

// Record an execution time of 50 ms
benchmarks.TimeoutPolicy_RecordExecutionTime(50);

// Record a timeout event (15 seconds)
benchmarks.TimeoutPolicy_RecordTimeout(15000);

// Check if a 50 ms duration is within the timeout
bool within = benchmarks.TimeoutPolicy_IsTimedOut_Within();

// Check if a 15 second duration exceeds the timeout
bool exceeds = benchmarks.TimeoutPolicy_IsTimedOut_Exceeds();

// Get the 95th percentile execution time
long p95 = benchmarks.TimeoutPolicy_GetPercentile95ExecutionTime();

// Get the 99th percentile execution time
long p99 = benchmarks.TimeoutPolicy_GetPercentile99ExecutionTime();

// Get the percentage of timeouts
double timeoutPct = benchmarks.TimeoutPolicy_GetTimeoutPercentage();

// Get the configured timeout
TimeSpan timeout = benchmarks.TimeoutPolicy_Get_Timeout();

// Get the total number of timeouts recorded
long timeoutCount = benchmarks.TimeoutPolicy_Get_TimeoutCount();
```

### CircuitBreakerDiagnosticsValidation

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

## ResiliencePipelineBenchmarks

The `ResiliencePipelineBenchmarks` class provides performance benchmarks for the Resiliency Pipeline Service. It measures the execution time and memory allocation of various pipeline operations including successful operations, circuit breaker, retry, timeout, bulkhead, and fallback.

### Example Usage

```csharp
using DotNetResiliencePipeline.Benchmarks;

// Create an instance of the benchmark class
var benchmarks = new ResiliencePipelineBenchmarks();

// Set up the pipeline service
benchmarks.Setup();

// Execute a successful operation
await benchmarks.ResiliencePipeline_Execute_Successful_Operation();

// Execute an operation with circuit breaker
await benchmarks.ResiliencePipeline_Execute_With_CircuitBreaker();

// Execute an operation with retry
await benchmarks.ResiliencePipeline_Execute_With_Retry();

// Execute an operation with timeout
await benchmarks.ResiliencePipeline_Execute_With_Timeout();

// Execute an operation with bulkhead
await benchmarks.ResiliencePipeline_Execute_With_Bulkhead();

// Execute an operation with fallback
await benchmarks.ResiliencePipeline_Execute_With_Fallback();

// Execute the full pipeline
await benchmarks.ResiliencePipeline_Execute_Full_Pipeline();

// Get pipeline statistics
var statistics = benchmarks.ResiliencePipeline_Get_Statistics();

// Execute multiple operations in parallel
await benchmarks.ResiliencePipeline_Execute_Multiple_Operations_Parallel();
```
## ...
