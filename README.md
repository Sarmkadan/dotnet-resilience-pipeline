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

## CircuitBreakerBenchmarks

The `CircuitBreakerBenchmarks` class provides performance benchmarks for the `CircuitBreakerPolicy`. It measures the execution time and memory allocation of various circuit breaker operations, including recording success and failure events, state transitions, and retrieving the current state and circuit breaker trips. 

### Example Usage
```csharp
using DotNetResiliencePipeline.Benchmarks;

// Create an instance of the benchmark class
var benchmarks = new CircuitBreakerBenchmarks();
benchmarks.Setup();

// Record a success event in the closed state
benchmarks.CircuitBreaker_Closed_State();

// Record a success event in the half-open state
benchmarks.CircuitBreaker_HalfOpen_State();

// Attempt to reset the circuit breaker in the open state
benchmarks.CircuitBreaker_Open_State();

// Record a failure event
benchmarks.CircuitBreaker_Failure_Recording();

// Transition the circuit breaker from closed to open
benchmarks.CircuitBreaker_State_Transition();

// Get the current state of the circuit breaker
var currentState = benchmarks.CircuitBreaker_Get_CurrentState();

// Get the number of circuit breaker trips
var trips = benchmarks.CircuitBreaker_Get_CircuitBreakerTrips();
```

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


## FallbackBenchmarks

The `FallbackBenchmarks` class provides performance benchmarks for the `FallbackPolicy`, measuring execution time and memory allocation for various fallback operations. It benchmarks recording successful and failed fallback attempts, checking fallback conditions, and retrieving fallback metrics such as success rate, invocation percentage, timeout configuration, and invocation counts.

#### Example Usage

```csharp
using DotNetResiliencePipeline.Benchmarks;

// Create an instance of the benchmark class
var benchmarks = new FallbackBenchmarks();
benchmarks.Setup(); // Initializes the FallbackPolicy with default configuration

// Record a successful fallback with 100ms duration
benchmarks.FallbackPolicy_RecordSuccessfulFallback(100);

// Record a failed fallback with an exception and 150ms duration
benchmarks.FallbackPolicy_RecordFailedFallback(new InvalidOperationException("Service unavailable"), 150);

// Check if a TimeoutException should trigger fallback (fallback on any exception by default)
bool shouldTriggerAny = benchmarks.FallbackPolicy_ShouldTriggerFallback_Any();

// Configure to trigger fallback only for specific exceptions
benchmarks.FallbackPolicy_RecordSuccessfulFallback(200); // Reset metrics
benchmarks.FallbackPolicy_FallbackOnAnyException = false;
benchmarks.FallbackPolicy_AddFallbackTrigger(typeof(TimeoutException));
bool shouldTriggerSpecific = benchmarks.FallbackPolicy_ShouldTriggerFallback_Specific();

// Get fallback success rate (successful fallbacks / total fallbacks)
double successRate = benchmarks.FallbackPolicy_GetFallbackSuccessRate();

// Get fallback invocation percentage (fallback invocations / total operations)
double invocationPct = benchmarks.FallbackPolicy_GetFallbackInvocationPercentage();

// Get the configured fallback timeout
TimeSpan timeout = benchmarks.FallbackPolicy_Get_FallbackTimeout();

// Get the total number of fallback invocations recorded
long invocationCount = benchmarks.FallbackPolicy_Get_FallbackInvocationCount();
```

## BulkheadBenchmarks

The `BulkheadBenchmarks` class provides performance benchmarks for the `BulkheadPolicy`, measuring execution time and memory allocation for various bulkhead operations. It benchmarks slot acquisition and release, queue wait time recording, and retrieval of utilization metrics including active executions, queued percentage, rejection percentage, and configured capacity limits.


### Example Usage

```csharp
using DotNetResiliencePipeline.Benchmarks;

// Create an instance of the benchmark class
var benchmarks = new BulkheadBenchmarks();
benchmarks.Setup(); // Initializes the BulkheadPolicy with MaxParallelization=10 and MaxQueueLength=50

// Attempt to acquire a slot in the bulkhead
bool acquired = benchmarks.BulkheadPolicy_TryAcquireSlot_Available();

// Release the acquired slot
benchmarks.BulkheadPolicy_ReleaseSlot();

// Record queue wait time of 150 milliseconds
benchmarks.BulkheadPolicy_RecordQueueWaitTime(150);

// Get the current utilization percentage (0-100)
double utilization = benchmarks.BulkheadPolicy_GetUtilizationPercentage();

// Get the percentage of operations currently queued
double queuedPct = benchmarks.BulkheadPolicy_GetQueuedPercentage();

// Get the percentage of operations that were rejected due to capacity limits
double rejectionPct = benchmarks.BulkheadPolicy_GetRejectionPercentage();

// Get the configured maximum parallel executions allowed
int maxParallel = benchmarks.BulkheadPolicy_Get_MaxParallelization();

// Get the configured maximum queue length
int maxQueue = benchmarks.BulkheadPolicy_Get_MaxQueueLength();

// Get the current number of active executions
int activeExecutions = benchmarks.BulkheadPolicy_Get_ActiveExecutions();
```

## PolicyComparisonBenchmarks

The `PolicyComparisonBenchmarks` class provides performance benchmarks for comparing different resilience policy configurations and scenarios. It measures execution time and memory allocation across retry strategies (fixed, linear, exponential, exponential with jitter), circuit breaker configurations (low/high failure thresholds, short/long durations), and bulkhead configurations (small/medium/large capacity). This enables direct comparison of performance characteristics and resource utilization between different resilience strategy implementations.

### Example Usage

```csharp
using DotNetResiliencePipeline.Benchmarks;

// Create an instance of the benchmark class
var benchmarks = new PolicyComparisonBenchmarks();
benchmarks.Setup(); // Initializes all policy configurations

// Compare retry strategies - get next delay for fixed strategy
long fixedDelay = benchmarks.RetryComparison_Fixed_Strategy();

// Compare retry strategies - get next delay for exponential strategy
long exponentialDelay = benchmarks.RetryComparison_Exponential_Strategy();

// Record retry attempts across all retry strategies
benchmarks.RetryComparison_RecordRetryAttempt_All_Strategies();

// Compare circuit breaker configurations - record success with low threshold
benchmarks.CircuitBreakerComparison_LowThreshold_RecordSuccess();

// Compare circuit breaker configurations - record success with high threshold
benchmarks.CircuitBreakerComparison_HighThreshold_RecordSuccess();

// Compare circuit breaker configurations - record failure with short duration
benchmarks.CircuitBreakerComparison_ShortDuration_RecordFailure();

// Get current state of circuit breaker
var currentState = benchmarks.CircuitBreakerComparison_GetState_All();

// Compare bulkhead configurations - try to acquire slot with small bulkhead
bool acquiredSmall = benchmarks.BulkheadComparison_Small_TryAcquireSlot();

// Compare bulkhead configurations - try to acquire slot with medium bulkhead
bool acquiredMedium = benchmarks.BulkheadComparison_Medium_TryAcquireSlot();

// Record queue wait times across all bulkhead configurations
benchmarks.BulkheadComparison_RecordQueueWaitTime_All();

// Get utilization percentage for bulkhead
double utilization = benchmarks.BulkheadComparison_GetUtilization_All();

// Simulate circuit breaker transition from closed to open
benchmarks.CircuitBreakerComparison_Transition_Closed_To_Open();

// Record multiple retry attempts
benchmarks.RetryComparison_Multiple_Retry_Attempts();

// Test bulkhead capacity limits - queue and reject
bool rejected = benchmarks.BulkheadComparison_Queue_And_Reject();
```

## ConcurrencyBenchmarks

The `ConcurrencyBenchmarks` class provides performance benchmarks for concurrent operations and thread safety across all resilience policies. It measures the performance of recording success/failure events, state access, retry attempts, delay calculations, execution time tracking, timeout events, slot acquisition, queue wait times, fallback operations, and utilization metrics under parallel load.


### Example Usage

```csharp
using DotNetResiliencePipeline.Benchmarks;

// Create an instance of the benchmark class
var benchmarks = new ConcurrencyBenchmarks();

// Set up all policies with default configurations
benchmarks.Setup();

// Benchmark concurrent success recording for circuit breaker
benchmarks.CircuitBreaker_Concurrent_Success_Recording();

// Benchmark concurrent failure recording for circuit breaker
benchmarks.CircuitBreaker_Concurrent_Failure_Recording();

// Benchmark concurrent state access for circuit breaker
benchmarks.CircuitBreaker_Concurrent_State_Access();

// Benchmark concurrent retry recording
benchmarks.RetryPolicy_Concurrent_Retry_Recording();

// Benchmark concurrent delay calculations
benchmarks.RetryPolicy_Concurrent_Delay_Calculation();

// Benchmark concurrent execution time recording for timeout policy
benchmarks.TimeoutPolicy_Concurrent_Execution_Recording();

// Benchmark concurrent timeout recording
benchmarks.TimeoutPolicy_Concurrent_Timeout_Recording();

// Benchmark concurrent slot acquisition for bulkhead policy
benchmarks.BulkheadPolicy_Concurrent_Slot_Acquisition();

// Benchmark concurrent queue wait time recording
benchmarks.BulkheadPolicy_Concurrent_Queue_Wait_Recording();

// Benchmark concurrent fallback recording
benchmarks.FallbackPolicy_Concurrent_Fallback_Recording();

// Benchmark concurrent fallback checks
benchmarks.FallbackPolicy_Concurrent_Fallback_Check();

// Benchmark mixed concurrent operations across all policies
benchmarks.All_Policies_Concurrent_Mixed_Operations();

// Get concurrent circuit breaker trip count
long trips = benchmarks.CircuitBreaker_Get_CircuitBreakerTrips_Concurrent();

// Get concurrent bulkhead utilization
double utilization = benchmarks.Bulkhead_Get_Utilization_Concurrent();
```
## ...
