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
- [CliCommandValidator](#clicommandvalidator)
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

## CommandOptions

The `CommandOptions` class represents the command-line options and arguments for the CLI interface. It serves as the primary data structure for parsing and validating user input when executing resilience policy operations, monitoring commands, and configuration operations. The class includes properties for common policy configuration options like retry counts, timeout durations, failure thresholds, and output formatting preferences.

### Example Usage

```csharp
using DotNetResiliencePipeline.Cli;

// Create command options for creating a retry policy
var options = new CommandOptions
{
    Command = "policy",
    Subcommand = "create",
    PolicyName = "user-service-retry",
    PolicyType = "retry",
    MaxRetries = 5,
    Timeout = TimeSpan.FromSeconds(30),
    Verbose = true,
    JsonOutput = true,
    Arguments = new Dictionary<string, string>
    {
        {"endpoint", "https://api.example.com/users"},
        {"method", "GET"}
    },
    Flags = new List<string> { "--dry-run", "-v" }
};

// Access properties
Console.WriteLine($"Executing command: {options.Command} {options.Subcommand}");
Console.WriteLine($"Policy: {options.PolicyName} ({options.PolicyType})");
Console.WriteLine($"Configuration: {options.MaxRetries} retries, {options.Timeout?.TotalSeconds} second timeout");

// Check flags
if (options.HasFlag("dry-run", "dryrun"))
{
    Console.WriteLine("Running in dry-run mode");
}

// Get argument values
string endpoint = options.GetArgument("endpoint", "https://default.example.com");
string method = options.GetArgument("method");

// Validate options
var validationErrors = options.Validate();
if (validationErrors.Count > 0)
{
    Console.WriteLine("Validation errors:");
    foreach (var error in validationErrors)
    {
        Console.WriteLine($"- {error}");
    }
}
```

## CliCommandValidator

The `CliCommandValidator` class is designed to validate CLI commands and their arguments before execution, ensuring that all required parameters are present and values are within acceptable ranges. It returns a `ValidationResult` which provides information about the validation status, including any detected errors or warnings.

### Example Usage
```csharp
using DotNetResiliencePipeline.Cli;

// Create the validator
var validator = new CliCommandValidator();

// Define command options
var options = new CommandOptions { 
    Command = "policy", 
    Subcommand = "create",
    PolicyName = "my-policy",
    PolicyType = "retry" 
};

// Validate the options
ValidationResult result = validator.Validate(options);

// Check if valid using IsValid property
if (result.IsValid)
{
    Console.WriteLine("Command is valid");
}
else
{
    // Use ToString() to print validation details
    Console.WriteLine(result.ToString());
}

// Access Errors and Warnings lists
foreach (var error in result.Errors)
{
    Console.WriteLine($"Error: {error}");
}

foreach (var warning in result.Warnings)
{
    Console.WriteLine($"Warning: {warning}");
}
```

## CliCommandHandler

The `CliCommandHandler` class executes CLI commands by routing them to appropriate service layer methods, handling error management, and formatting output. It validates commands using `CliCommandValidator`, delegates execution to specialized handlers based on the command type, and returns structured results containing success status, messages, exceptions, and exit codes.

### Example Usage
```csharp
using DotNetResiliencePipeline.Cli;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Data;

// Create required services
var pipelineService = new ResiliencyPipelineService();
var policyRepository = new PolicyRepository();
var historyRepository = new ExecutionHistoryRepository();

// Create the command handler
var commandHandler = new CliCommandHandler(
    pipelineService,
    policyRepository,
    historyRepository
);

// Execute a policy creation command
var createOptions = new CommandOptions
{
    Command = "policy",
    Subcommand = "create",
    PolicyName = "user-service-retry",
    PolicyType = "retry",
    MaxRetries = 5,
    Timeout = TimeSpan.FromSeconds(30),
    Verbose = true,
    JsonOutput = false,
    Arguments = new Dictionary<string, string>
    {
        {"endpoint", "https://api.example.com/users"},
        {"method", "GET"}
    },
    Flags = new List<string> { "--dry-run", "-v" }
};

// Execute the command
CommandExecutionResult result = await commandHandler.ExecuteAsync(createOptions);

// Check execution result
if (result.Success)
{
    Console.WriteLine(result.Message);
    Console.WriteLine($"Exit code: {result.ExitCode}");
}
else
{
    Console.WriteLine($"Command failed: {result.Message}");
    if (result.Error != null)
    {
        Console.WriteLine($"Error: {result.Error.Message}");
    }
    Console.WriteLine($"Exit code: {result.ExitCode}");
}

// Execute a policy listing command
var listOptions = new CommandOptions
{
    Command = "policy",
    Subcommand = "list"
};

CommandExecutionResult listResult = await commandHandler.ExecuteAsync(listOptions);
Console.WriteLine(listResult.Message);

// Execute a metrics command
var metricsOptions = new CommandOptions
{
    Command = "metrics"
};

CommandExecutionResult metricsResult = await commandHandler.ExecuteAsync(metricsOptions);
Console.WriteLine(metricsResult.Message);
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

## PolicyResult

The `PolicyResult<T>` class encapsulates the outcome of a resilience policy execution, providing a standardized way to handle successes, failures, and fallback results. It contains metadata about the execution, such as attempt count, execution time, and any associated exceptions or custom metadata, allowing for consistent monitoring and error handling across the pipeline.

### Example Usage
```csharp
using DotNetResiliencePipeline.Domain;

// Create a successful result
var successResult = PolicyResult<string>.Success("Operation completed successfully", "MyRetryPolicy");
successResult.OnSuccess(() => Console.WriteLine("Success!"));

// Create a failed result
var failureResult = PolicyResult<string>.Failure(new Exception("Operation failed"), "MyRetryPolicy");
failureResult.OnFailure((ex) => Console.WriteLine($"Failed: {ex.Message}"));

// Map to a new type
var mappedResult = successResult.Map(data => data.Length);
Console.WriteLine($"Length: {mappedResult.Data}");

// Access result properties
Console.WriteLine($"Policy: {successResult.PolicyName}");
Console.WriteLine($"Time: {successResult.ExecutionTimeMs}ms");
Console.WriteLine($"Attempts: {successResult.AttemptCount}");
```

## RetryPolicy

The `RetryPolicy` class implements retry logic with configurable backoff strategies to handle transient failures in distributed systems. It supports multiple backoff algorithms (fixed, linear, exponential), configurable retry limits, jitter for avoiding thundering herd problems, and comprehensive retry tracking with detailed metrics.

### Example Usage
```csharp
using DotNetResiliencePipeline.Domain.Policies;
using System;

// Create a retry policy with exponential backoff strategy
var retryPolicy = new RetryPolicy("user-service-retry")
{
    MaxRetries = 5,
    InitialDelay = TimeSpan.FromMilliseconds(100),
    Strategy = BackoffStrategy.Exponential,
    MaxDelay = TimeSpan.FromSeconds(30),
    BackoffMultiplier = 2.0,
    UseJitter = true,
    JitterFactor = 0.2,
    RetryableExceptions = new List<Type> { typeof(TimeoutException), typeof(HttpRequestException) }
};

// Validate configuration
if (!retryPolicy.IsValidConfiguration)
{
    Console.WriteLine("Invalid retry policy configuration!");
    foreach (var error in retryPolicy.GetSnapshot().ValidationErrors)
    {
        Console.WriteLine($"  - {error}");
    }
}

// Check if an exception is retryable
bool shouldRetry = retryPolicy.IsRetryable(new TimeoutException("Request timed out"));
Console.WriteLine($"Should retry on TimeoutException: {shouldRetry}");

// Calculate delay for the next retry attempt
long nextDelayMs = retryPolicy.GetNextDelayMs(3); // 3rd retry attempt
Console.WriteLine($"Next delay for attempt 3: {nextDelayMs}ms");

// Record a retry attempt
retryPolicy.RecordRetryAttempt();
Console.WriteLine($"Total retry attempts so far: {retryPolicy.TotalRetryAttempts}");

// Execute an operation with retry protection
var result = retryPolicy.Execute(() =>
{
    // Your transient operation here
    return CallExternalService();
});

// Access execution statistics
var snapshot = retryPolicy.GetSnapshot();
Console.WriteLine($"Policy '{snapshot.PolicyName}' executed {snapshot.ExecutionCount} times");
Console.WriteLine($"Success rate: {snapshot.SuccessRate:P}");
Console.WriteLine($"Total retry attempts: {retryPolicy.TotalRetryAttempts}");

// Configure retry policy with linear backoff
var linearRetry = new RetryPolicy("linear-retry")
{
    MaxRetries = 3,
    InitialDelay = TimeSpan.FromSeconds(1),
    Strategy = BackoffStrategy.Linear,
    MaxDelay = TimeSpan.FromSeconds(10)
};

// Execute with linear backoff
var linearResult = linearRetry.Execute(() => ExternalApi.GetUser(123));

// Configure retry policy with fixed interval
var fixedRetry = new RetryPolicy("fixed-retry")
{
    MaxRetries = 2,
    InitialDelay = TimeSpan.FromMilliseconds(500),
    Strategy = BackoffStrategy.Fixed
};

// Execute with fixed interval
var fixedResult = fixedRetry.Execute(() => Database.Query("SELECT * FROM users"));
```

## ResiliencyPolicy

The `ResiliencyPolicy` class serves as the base class for all resilience policies in the DotNet Resilience Pipeline library. It provides core functionality for tracking execution statistics, managing policy state, and generating snapshots for observability purposes. The class maintains comprehensive metrics including execution counts, success/failure rates, and timestamps for creation and modification, enabling detailed monitoring and analysis of policy behavior.

### Example Usage

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using System;

// Create a resiliency policy with a unique identifier and name
var policy = new ResiliencyPolicy("user-service-policy-001", "UserServicePolicy")
{
    IsEnabled = true,
    Tags = new List<string> { "user-service", "api", "production" },
    Metadata = new Dictionary<string, object>
    {
        {"ServiceName", "UserService"},
        {"Environment", "Production"},
        {"Owner", "PlatformTeam"}
    }
};

// Access basic properties
Console.WriteLine($"Policy ID: {policy.Id}");
Console.WriteLine($"Policy Name: {policy.Name}");
Console.WriteLine($"Is Enabled: {policy.IsEnabled}");
Console.WriteLine($"Created At: {policy.CreatedAt:yyyy-MM-dd HH:mm:ss}");
Console.WriteLine($"Modified At: {policy.ModifiedAt:yyyy-MM-dd HH:mm:ss}");

// Record successful executions
policy.RecordSuccess();
policy.RecordSuccess();

// Record failed executions
policy.RecordFailure();
policy.RecordFailure();
policy.RecordFailure();

// Get success rate
Console.WriteLine($"Success Rate: {policy.GetSuccessRate():P2}");

// Access statistics
Console.WriteLine($"Total Executions: {policy.TotalExecutions}");
Console.WriteLine($"Successful Executions: {policy.SuccessfulExecutions}");
Console.WriteLine($"Failed Executions: {policy.FailedExecutions}");

// Get a snapshot for monitoring
var snapshot = policy.GetSnapshot();
Console.WriteLine($"Snapshot - Policy ID: {snapshot.PolicyId}");
Console.WriteLine($"Snapshot - Policy Name: {snapshot.PolicyName}");
Console.WriteLine($"Snapshot - Policy Type: {snapshot.PolicyType}");
Console.WriteLine($"Snapshot - Is Enabled: {snapshot.IsEnabled}");
Console.WriteLine($"Snapshot - Total Executions: {snapshot.TotalExecutions}");

// Reset statistics when needed (e.g., after maintenance or testing)
policy.ResetStatistics();
Console.WriteLine($"Statistics reset. Total executions: {policy.TotalExecutions}");

// Update policy configuration
policy.IsEnabled = false;
policy.Tags.Add("disabled");
policy.Metadata["LastModified"] = DateTime.UtcNow;
```

## CircuitBreakerPolicy

The `CircuitBreakerPolicy` implements the circuit breaker pattern to prevent cascading failures in distributed systems. It monitors execution failures and, when a configurable threshold is reached, opens the circuit to fail-fast and avoid overwhelming failing dependencies. After a specified duration, it transitions to a half-open state to test recovery, and if successful, closes the circuit to resume normal operation.

### Example Usage
```csharp
using DotNetResiliencePipeline.Domain.Policies;
using System;

// Create a circuit breaker policy with a name
var circuitBreaker = new CircuitBreakerPolicy("DatabaseCircuitBreaker")
{
    FailureThreshold = 5,       // Open circuit after 5 consecutive failures
    OpenDuration = TimeSpan.FromSeconds(30) // Keep circuit open for 30 seconds
};

// Check initial state
Console.WriteLine($"Initial state: {circuitBreaker.CurrentState}"); // Closed

// Simulate successful operations
circuitBreaker.RecordSuccess();
circuitBreaker.RecordSuccess();

// Simulate failures until threshold is reached
for (int i = 0; i < 4; i++)
{
    circuitBreaker.RecordFailure();
    Console.WriteLine($"Consecutive failures: {circuitBreaker.ConsecutiveFailures}");
}

// This failure will open the circuit
circuitBreaker.RecordFailure();
Console.WriteLine($"Circuit state after threshold: {circuitBreaker.CurrentState}"); // Open
Console.WriteLine($"Circuit breaker trips: {circuitBreaker.CircuitBreakerTrips}");

// Attempt to reset checks if enough time has passed
circuitBreaker.AttemptReset();
Console.WriteLine($"State after attempt reset: {circuitBreaker.CurrentState}"); // HalfOpen

// Simulate successful recovery in half-open state
circuitBreaker.RecordSuccess();
circuitBreaker.RecordSuccess();
circuitBreaker.RecordSuccess(); // 3 successful operations close the circuit
Console.WriteLine($"Circuit state after recovery: {circuitBreaker.CurrentState}"); // Closed

// Manually reset the circuit breaker
circuitBreaker.ManualReset();
Console.WriteLine($"Circuit state after manual reset: {circuitBreaker.CurrentState}"); // Closed

// Get detailed snapshot
var snapshot = circuitBreaker.GetSnapshot();
Console.WriteLine($"Snapshot - Circuit state: {snapshot.Metadata["CircuitState"]}");
Console.WriteLine($"Consecutive failures: {snapshot.Metadata["ConsecutiveFailures"]}");
Console.WriteLine($"Failure threshold: {snapshot.Metadata["FailureThreshold"]}");
```

```
## ...

## IPipelineMetrics

The `IPipelineMetrics` interface provides a unified view of execution counters across all policies in the pipeline. It allows for the retrieval of an aggregated `PipelineMetricsSnapshot` containing statistics such as total, successful, and failed executions, as well as retry counts, circuit breaker trips, and timeout occurrences.

### Example Usage

```csharp
using DotNetResiliencePipeline.Domain;

// Assume pipelineMetrics is an implementation of IPipelineMetrics
IPipelineMetrics pipelineMetrics = GetPipelineMetrics();

// Get the aggregated metrics snapshot
PipelineMetricsSnapshot stats = pipelineMetrics.GetStats();

Console.WriteLine($"Total Executions: {stats.TotalExecutions}");
Console.WriteLine($"Success Rate: {stats.SuccessRate}%");
Console.WriteLine($"Retries: {stats.RetryCount}");
Console.WriteLine($"Circuit Breaker Trips: {stats.CircuitBreakerTrips}");
Console.WriteLine($"Timeouts: {stats.TimeoutCount}");

// Inspect per-policy snapshots
foreach (var snapshot in stats.PolicySnapshots)
{
    Console.WriteLine($"Policy '{snapshot.PolicyName}': {snapshot.ExecutionCount} executions");
}
```

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

## ResiliencyException

The `ResiliencyException` class serves as the base exception type for all resilience pipeline failures. It provides contextual information about policy execution including the policy name, policy type, and the timestamp when the exception occurred. This exception hierarchy enables consistent error handling and logging across all resilience strategies.

#### Example Usage

```csharp
using DotNetResiliencePipeline.Exceptions;

// Create a base resiliency exception with policy context
var resilienceEx = new ResiliencyException( 
  "Operation failed due to circuit breaker policy",
  policyName: "UserServiceCircuitBreaker",
  policyType: "CircuitBreaker"
);

Console.WriteLine($"Policy: {resilienceEx.PolicyName}");
Console.WriteLine($"Type: {resilienceEx.PolicyType}");
Console.WriteLine($"Occurred: {resilienceEx.OccurredAt:O}");

// Create a resiliency exception with inner exception
var resilienceExWithInner = new ResiliencyException(
  "Database operation timed out",
  new TimeoutException("Connection timed out after 30 seconds"),
  policyName: "DatabaseTimeoutPolicy",
  policyType: "Timeout"
);

// Access base exception properties
Console.WriteLine($"Message: {resilienceExWithInner.Message}");
Console.WriteLine($"Inner exception: {resilienceExWithInner.InnerException?.GetType().Name}");
```

## WebhookException

The `WebhookException` class serves as the base exception type for all webhook-related failures in the resilience pipeline. It provides contextual information about webhook operations including the webhook identifier, URL, and the underlying exception that triggered the failure. This exception hierarchy enables consistent error handling and logging for webhook delivery and registration scenarios.

#### Example Usage

```csharp
using DotNetResiliencePipeline.Exceptions;

// Create a base webhook exception with webhook context
var webhookEx = new WebhookException(
  "Failed to process webhook delivery",
  webhookId: "wh_789",
  webhookUrl: "https://api.example.com/webhooks/wh_789"
);

// Create a webhook exception with inner exception
var webhookExWithInner = new WebhookException(
  "Webhook delivery failed",
  new InvalidOperationException("Connection timeout"),
  webhookId: "wh_456",
  webhookUrl: "https://api.example.com/webhooks/wh_456"
);

// Access webhook properties
Console.WriteLine($"Webhook ID: {webhookEx.WebhookId}");
Console.WriteLine($"Webhook URL: {webhookEx.WebhookUrl}");

// Create a delivery failure exception
var deliveryFailedEx = new WebhookDeliveryFailedException(
  webhookId: "wh_123",
  webhookUrl: "https://api.example.com/webhooks/wh_123",
  eventType: "order.created",
  attemptCount: 3,
  innerException: new TimeoutException("Request timed out after 5 seconds")
);

// Create a registration exception
var registrationEx = new WebhookRegistrationException(
  "Failed to register webhook: invalid payload schema",
  webhookUrl: "https://api.example.com/webhooks"
);

// Create an invalid webhook exception
var invalidWebhookEx = new InvalidWebhookException(
  "Webhook subscription is not valid for this event type",
  webhookId: "wh_invalid",
  webhookUrl: "https://api.example.com/webhooks/wh_invalid"
);
```

## ExternalApiClient

The `ExternalApiClient` class is a wrapper for external API calls with built-in resilience policies. It handles authentication, retries, timeouts, and error recovery, providing a robust and reliable way to interact with external services. The client supports both GET and POST requests with automatic JSON serialization and deserialization, custom headers, and comprehensive error handling.

### Example Usage

```csharp
using DotNetResiliencePipeline.Integration;
using System.Text.Json;

// Create required services
var httpClientFactory = new HttpClientFactory();
var pipelineService = new ResiliencyPipelineService();

// Create the external API client
var apiClient = new ExternalApiClient(httpClientFactory, pipelineService);

// Register an API configuration
apiClient.RegisterApi("weather-api", new ExternalApiClient.ApiConfiguration
{
    BaseUrl = "https://api.weatherapi.com/v1",
    ApiKey = "your-api-key-here",
    Timeout = TimeSpan.FromSeconds(15),
    MaxRetries = 3,
    RetryDelay = TimeSpan.FromMilliseconds(200),
    DefaultHeaders = new Dictionary<string, string>
    {
        {"Accept", "application/json"},
        {"User-Agent", "DotNetResiliencePipeline/1.0"}
    }
});

// Get weather data from external API
var weatherResponse = await apiClient.GetAsync<WeatherData>(
    "weather-api",
    "current.json",
    new Dictionary<string, string>
    {
        {"q", "New York"}
    }
);

if (weatherResponse.Success && weatherResponse.Data != null)
{
    Console.WriteLine($"Temperature: {weatherResponse.Data.Current?.TempC}°C");
    Console.WriteLine($"Condition: {weatherResponse.Data.Current?.Condition?.Text}");
    Console.WriteLine($"Headers: {JsonSerializer.Serialize(weatherResponse.Headers)}");
}
else
{
    Console.WriteLine($"Error: {weatherResponse.Message}");
}

// Post data to external API
var postData = new { userId = 123, action = "login" };
var postResponse = await apiClient.PostAsync<ApiResult>(
    "auth-api",
    "users/login",
    postData,
    new Dictionary<string, string>
    {
        {"X-Request-ID", Guid.NewGuid().ToString()}
    }
);

// Check registered APIs
var registeredApis = apiClient.GetRegisteredApis();
Console.WriteLine($"Registered APIs: {string.Join(", ", registeredApis)}");

// Test API connectivity
bool isConnected = await apiClient.TestConnectionAsync("weather-api");
Console.WriteLine($"API connection test: {(isConnected ? "SUCCESS" : "FAILED")}");

// Access configuration properties
var config = new ExternalApiClient.ApiConfiguration
{
    BaseUrl = "https://api.example.com",
    Timeout = TimeSpan.FromSeconds(30),
    MaxRetries = 5,
    RetryDelay = TimeSpan.FromSeconds(1),
    DefaultHeaders = new Dictionary<string, string> { {"Content-Type", "application/json"} }
};

Console.WriteLine($"Base URL: {config.BaseUrl}");
Console.WriteLine($"Timeout: {config.Timeout.TotalSeconds} seconds");
Console.WriteLine($"Max Retries: {config.MaxRetries}");
Console.WriteLine($"Retry Delay: {config.RetryDelay.TotalMilliseconds} ms");
```

## HttpClientFactory

The `HttpClientFactory` class provides a factory pattern for creating and managing HTTP clients with built-in resilience policies. It creates named clients that can be reused across your application, handles client lifecycle management, and provides resilient HTTP operations (GET/POST) with automatic error handling and response tracking. The factory ensures proper resource cleanup and supports multiple client instances with different configurations.

### Example Usage

```csharp
using DotNetResiliencePipeline.Integration;
using System.Net;

// Create the HttpClientFactory with a resiliency pipeline service
var pipelineService = new ResiliencyPipelineService();
var httpClientFactory = new HttpClientFactory(pipelineService);

// Create a named HTTP client with base address
var client = httpClientFactory.CreateClient(
    clientName: "weather-api",
    baseAddress: "https://api.weatherapi.com/v1",
    timeout: TimeSpan.FromSeconds(15)
);

// Get weather data using resilient GET request
var getResponse = await httpClientFactory.GetAsync(
    clientName: "weather-api",
    url: "current.json?q=London"
);

if (getResponse.Success)
{
    Console.WriteLine($"Status: {getResponse.StatusCode}");
    Console.WriteLine($"Content: {getResponse.Content}");
    Console.WriteLine($"Headers: {string.Join(", ", getResponse.Headers?.Select(h => h.Key))}");
    Console.WriteLine($"Timestamp: {getResponse.Timestamp:O}");
}
else
{
    Console.WriteLine($"Request failed: {getResponse.Message}");
}

// Post data using resilient POST request
var postContent = new StringContent(
    "{\"location\":\"Paris\"}",
    Encoding.UTF8,
    "application/json"
);

var postResponse = await httpClientFactory.PostAsync(
    clientName: "weather-api",
    url: "forecast.json",
    content: postContent
);

if (postResponse.Success)
{
    Console.WriteLine($"POST successful: {postResponse.StatusCode}");
    Console.WriteLine($"Response: {postResponse.Content}");
}

// Get all registered client names
var clientNames = httpClientFactory.GetClientNames();
Console.WriteLine($"Registered clients: {string.Join(", ", clientNames)}");

// Get an existing client
var existingClient = httpClientFactory.GetClient("weather-api");

// Remove a client when no longer needed
bool removed = httpClientFactory.RemoveClient("weather-api");
Console.WriteLine($"Client removed: {removed}");
```

## HttpClientException

The `HttpClientException` class serves as the base exception type for all HTTP client-related failures in the resilience pipeline. It provides contextual information about HTTP requests including the client name, request URL, and the underlying exception that triggered the failure. This exception hierarchy enables consistent error handling and logging for HTTP operations.

#### Example Usage

```csharp
using DotNetResiliencePipeline.Exceptions;

// Create a base HTTP client exception with client context
var httpClientEx = new HttpClientException(
  "Failed to execute HTTP request",
  clientName: "UserServiceClient",
  requestUrl: "https://api.example.com/users/123"
);

// Create an HTTP client exception with inner exception
var httpClientExWithInner = new HttpClientException(
  "HTTP request failed",
  new InvalidOperationException("Connection timeout"),
  clientName: "PaymentServiceClient",
  requestUrl: "https://api.example.com/payments/process"
);

// Access HTTP client properties
Console.WriteLine($"Client Name: {httpClientEx.ClientName}");
Console.WriteLine($"Request URL: {httpClientEx.RequestUrl}");

// Create an invalid HTTP request exception (missing required headers)
var invalidRequestEx = new InvalidHttpRequestException(
  "HTTP request configuration is invalid: missing authorization header",
  clientName: "AuthServiceClient",
  requestUrl: "https://api.example.com/auth/validate",
  httpMethod: "GET"
);

// Create an HTTP response exception (404 Not Found)
var responseEx = new HttpResponseException(
  "The requested resource was not found",
  statusCode: 404,
  clientName: "ContentServiceClient",
  requestUrl: "https://api.example.com/content/articles/999"
);

// Create an HTTP timeout exception
var timeoutEx = new HttpTimeoutException(
  "HTTP request timed out",
  timeout: TimeSpan.FromSeconds(30),
  clientName: "AnalyticsServiceClient",
  requestUrl: "https://api.example.com/analytics/events"
);

// Access derived exception properties
Console.WriteLine($"Status Code: {responseEx.StatusCode}");
Console.WriteLine($"Timeout: {timeoutEx.Timeout.TotalSeconds} seconds");
Console.WriteLine($"HTTP Method: {invalidRequestEx.HttpMethod}");
```

## WebhookManager

The `WebhookManager` class manages webhook subscriptions and event deliveries for pipeline events. It handles registration, delivery with retry logic, tracking delivery history, and provides statistics about webhook operations. The manager supports custom headers, exponential backoff for retries, and maintains a configurable history of delivery attempts.

#### Example Usage

```csharp
using DotNetResiliencePipeline.Integration;
using System.Net;

// Create a webhook manager instance
var webhookManager = new WebhookManager
{
    MaxHistoryEntries = 500 // Configure history size
};

// Register a webhook subscription for specific events
var webhookId = webhookManager.RegisterWebhook(
    url: "https://api.example.com/webhooks/order-events",
    events: new[] { "order.created", "order.updated", "order.cancelled" },
    headers: new Dictionary<string, string>
    {
        {"Authorization", "Bearer token123"},
        {"X-Webhook-Secret", "secret456"}
    }
);

Console.WriteLine($"Registered webhook: {webhookId}");

// Get all registered webhooks
var allWebhooks = webhookManager.GetAllWebhooks();
foreach (var webhook in allWebhooks)
{
    Console.WriteLine($"Webhook {webhook.Id}: {webhook.Url}");
    Console.WriteLine($"  Events: {string.Join(", ", webhook.Events)}");
    Console.WriteLine($"  Active: {webhook.IsActive}");
    Console.WriteLine($"  Created: {webhook.CreatedAt:yyyy-MM-dd}");
}

// Enable/disable a webhook
bool enabled = webhookManager.SetWebhookActive(webhookId, true);
Console.WriteLine($"Webhook enabled: {enabled}");

// Trigger an event to all subscribed webhooks
try
{
    await webhookManager.TriggerEventAsync(
        eventType: "order.created",
        eventData: new { OrderId = 12345, CustomerId = 67890, Amount = 99.99 }
    );
    Console.WriteLine("Event triggered successfully");
}
catch (WebhookDeliveryFailedException ex)
{
    Console.WriteLine($"Failed to deliver webhook after {ex.AttemptCount} attempts: {ex.Message}");
}

// Get delivery history for a specific webhook
var deliveryHistory = webhookManager.GetDeliveryHistory(webhookId, limit: 10);
foreach (var delivery in deliveryHistory)
{
    Console.WriteLine($"Delivery {delivery.Id} at {delivery.Timestamp:HH:mm:ss}: " +
                     $"{(delivery.Success ? "SUCCESS" : "FAILED")} " +
                     $"(attempt {delivery.AttemptCount}, status {(int)delivery.StatusCode ?? 0})");
}

// Get overall webhook statistics
var stats = webhookManager.GetStatistics();
Console.WriteLine($"Total deliveries: {stats.TotalDeliveries}");
Console.WriteLine($"Successful: {stats.SuccessfulDeliveries} ({stats.SuccessRate:F1}%)");
Console.WriteLine($"Failed: {stats.FailedDeliveries}");
Console.WriteLine($"Active subscriptions: {stats.ActiveSubscriptions}");

// Unregister a webhook when no longer needed
bool unregistered = webhookManager.UnregisterWebhook(webhookId);
Console.WriteLine($"Webhook unregistered: {unregistered}");
```

## ResiliencyEventPublisher

The `ResiliencyEventPublisher` class implements a pub-sub pattern for publishing and subscribing to resilience pipeline events. It enables decoupled communication between resilience components by allowing subscribers to register handlers for specific event types and receive notifications when events occur. The publisher maintains an event history that can be queried for monitoring and debugging purposes.


#### Example Usage

```csharp
using DotNetResiliencePipeline.Events;
using System.Diagnostics;

// Create a publisher instance
var publisher = new ResiliencyEventPublisher();

// Configure maximum history size (default: 1000)
publisher.MaxHistorySize = 500;

// Subscribe to successful policy execution events
publisher.Subscribe<PolicyExecutedSuccessfullyEvent>("PolicyExecutedSuccessfullyEvent", 
    (ev) => 
    {
        Console.WriteLine($"[{ev.Timestamp:HH:mm:ss.fff}] Policy '{ev.PolicyName}' executed successfully in {ev.DurationMs}ms (attempt #{ev.AttemptNumber})");
        
        // Log to application monitoring system
        Debug.WriteLine($"Policy success: {ev.PolicyName}, Duration: {ev.DurationMs}ms");
    });

// Subscribe to policy execution failures
publisher.Subscribe<PolicyExecutionFailedEvent>("PolicyExecutionFailedEvent", 
    (ev) => 
    {
        Console.WriteLine($"[{ev.Timestamp:HH:mm:ss.fff}] Policy '{ev.PolicyName}' failed: {ev.ExceptionType} - {ev.ExceptionMessage}");
        
        // Send alert to monitoring system
        AlertSystem.SendAlert(
            AlertLevel.Warning,
            $"Policy {ev.PolicyName} failed",
            details: new { ev.ExceptionType, ev.DurationMs }
        );
    });

// Subscribe to circuit breaker state changes
publisher.Subscribe<CircuitBreakerStateChangedEvent>("CircuitBreakerStateChangedEvent",
    (ev) => 
    {
        Console.WriteLine($"[{ev.Timestamp:HH:mm:ss.fff}] Circuit breaker '{ev.PolicyName}' state changed from '{ev.PreviousState}' to '{ev.NewState}'");
        
        // Update dashboard
        Dashboard.UpdateCircuitBreakerState(ev.PolicyName, ev.NewState, ev.ConsecutiveFailures);
    });

// Simulate publishing events from resilience policies
try
{
    // Simulate a successful policy execution
    await publisher.PublishAsync(new PolicyExecutedSuccessfullyEvent
    {
        PolicyName = "UserServiceRetryPolicy",
        SourcePolicy = "UserService",
        DurationMs = 150,
        AttemptNumber = 3
    });
    
    // Simulate a failed policy execution
    await publisher.PublishAsync(new PolicyExecutionFailedEvent
    {
        PolicyName = "PaymentServiceCircuitBreaker",
        SourcePolicy = "PaymentService",
        ExceptionType = "TimeoutException",
        ExceptionMessage = "Request timed out after 30 seconds",
        DurationMs = 30500
    });
    
    // Simulate a circuit breaker state change
    await publisher.PublishAsync(new CircuitBreakerStateChangedEvent
    {
        PolicyName = "DatabaseCircuitBreaker",
        SourcePolicy = "DatabaseService",
        PreviousState = "Closed",
        NewState = "Open",
        ConsecutiveFailures = 5
    });
}
catch (Exception ex)
{
    Console.WriteLine($"Error publishing events: {ex.Message}");
}

// Query event history for monitoring
var recentEvents = publisher.GetEventHistory(limit: 10);
Console.WriteLine($"\nLast {recentEvents.Count} events:");
foreach (var ev in recentEvents)
{
    Console.WriteLine($"- [{ev.Timestamp:HH:mm:ss}] {ev.GetType().Name}");
}

// Get specific event types
var failedEvents = publisher.GetEvents<PolicyExecutionFailedEvent>(limit: 5);
Console.WriteLine($"\nFailed events count: {failedEvents.Count}");

// Check subscriber counts
var subscriberCount = publisher.GetSubscriberCount("PolicyExecutedSuccessfullyEvent");
Console.WriteLine($"Subscribers to success events: {subscriberCount}");

// Clear history when needed (e.g., during maintenance)
publisher.ClearHistory();
```

## PipelineEventObserver

The `PipelineEventObserver` class provides a centralized observer for monitoring and reacting to resilience pipeline events. It automatically registers default handlers for key event types (successful executions, failures, circuit breaker changes, bulkhead rejections, timeouts, and fallbacks) while allowing custom handlers to be registered for specific event types. The observer maintains statistics about event occurrences and provides methods to manage handlers dynamically.



#### Example Usage

```csharp
using DotNetResiliencePipeline.Events;
using DotNetResiliencePipeline.Policies;

// Create a resiliency event publisher
var publisher = new ResiliencyEventPublisher();

// Create the pipeline event observer with the publisher
var observer = new PipelineEventObserver(publisher);

// Get current statistics
var stats = observer.GetStatistics();
Console.WriteLine($"Total events: {stats.TotalEventsEmitted}");
Console.WriteLine($"Successful executions: {stats.SuccessfulExecutions}");
Console.WriteLine($"Failed executions: {stats.FailedExecutions}");
Console.WriteLine($"Failure rate: {stats.FailureRate:P}");

// Register a custom handler for specific event types
observer.RegisterHandler<TimeoutOccurredEvent>("TimeoutLogger", 
    (timeoutEvent) => 
    {
        Console.WriteLine($"[TIMEOUT] {timeoutEvent.PolicyName}: " +
                        $"Actual={timeoutEvent.ActualDurationMs}ms, " +
                        $"Configured={timeoutEvent.TimeoutMs}ms");
    });

// List all registered handlers
var handlers = observer.GetHandlers();
Console.WriteLine($"Registered handlers: {handlers.Count}");
foreach (var handler in handlers)
{
    Console.WriteLine($"- {handler.Id} ({handler.EventType}) " +
                     $"Created: {handler.CreatedAt:yyyy-MM-dd}, " +
                     $"Active: {handler.IsActive}");
}

// Disable a handler temporarily
observer.SetHandlerActive("TimeoutLogger", false);

// Re-enable the handler
observer.SetHandlerActive("TimeoutLogger", true);

// Unregister a handler when no longer needed
observer.UnregisterHandler("TimeoutLogger");

// Get updated statistics after events have been processed
var updatedStats = observer.GetStatistics();
```

## ValidationException

The `ValidationException` class is thrown when validation of input parameters or configuration fails. It provides detailed validation error information through the `ValidationErrors` dictionary, which maps field names to error messages. This exception enables consistent validation error handling and reporting across the resilience pipeline.

#### Example Usage

```csharp
using DotNetResiliencePipeline.Exceptions;

// Create a validation exception with a simple message
var validationEx = new ValidationException("Configuration validation failed");

// Create a validation exception with detailed error dictionary
var errors = new Dictionary<string, string>
{
    {"MaxRetries", "Value must be between 1 and 10"},
    {"Timeout", "Value must be greater than 0 milliseconds"},
    {"CircuitBreakerThreshold", "Value must be between 0 and 100"}
};
var detailedValidationEx = new ValidationException(
    "Configuration validation failed",
    errors
);

// Access validation errors
foreach (var error in detailedValidationEx.ValidationErrors)
{
    Console.WriteLine($"{error.Key}: {error.Value}");
}

// Create a validation exception with inner exception
var validationWithInner = new ValidationException(
    "Policy configuration validation failed",
    new ArgumentException("Invalid retry configuration"),
    errors
);

// Check if validation errors exist
if (detailedValidationEx.ValidationErrors.Any())
{
    Console.WriteLine($"Validation failed with {detailedValidationEx.ValidationErrors.Count} errors");
}
```

## ...