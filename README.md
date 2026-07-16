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
Flags = new List<string> {"--dry-run", "-v"}
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
Flags = new List<string> {"--dry-run", "-v"}
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

## ErrorHandlingMiddleware

The `ErrorHandlingMiddleware` class provides centralized error handling and recovery strategies for resilience policies. It tracks exceptions, classifies them by type and policy, and provides recovery recommendations. The middleware maintains statistics on error frequency and recency, enabling proactive error detection and circuit breaker optimization.

### Example Usage
```csharp
using DotNetResiliencePipeline.Middleware;
using DotNetResiliencePipeline.Exceptions;

// Create middleware instance
var errorHandler = new ErrorHandlingMiddleware();

// Handle an exception from a retry policy
try
{
    // Simulate a failing operation
    throw new TimeoutException("Request to external service timed out");
}
catch (Exception ex)
{
    // Record the error with context
    var errorContext = errorHandler.HandleException(ex, "user-service-retry", "GetUserProfile");
    
    Console.WriteLine($"Error recorded: {errorContext}");
    Console.WriteLine($"Recoverable: {errorContext.IsRecoverable}");
    Console.WriteLine($"Recommendation: {errorContext.RecoveryRecommendation}");
}

// Access error statistics
var statistics = errorHandler.GetErrorStatistics();
foreach (var stat in statistics)
{
    Console.WriteLine($"{stat.Key}: {stat.Value.Count} occurrences");
}

// Get errors for a specific policy
var retryErrors = errorHandler.GetErrorsForPolicy("user-service-retry");
Console.WriteLine($"Retry policy has {retryErrors.Count} errors");

// Get most common errors
var commonErrors = errorHandler.GetMostCommonErrors(5);
foreach (var (error, count) in commonErrors)
{
    Console.WriteLine($"{error}: {count} times");
}

// Clear tracking data when needed
// errorHandler.Clear();
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
Console.WriteLine($" - {error}");
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


## TimeoutPolicy

The `TimeoutPolicy` class enforces maximum execution time for operations, ensuring that long-running operations are terminated to prevent resource exhaustion. It tracks execution times, identifies timeouts, and provides detailed metrics including average, longest, and shortest execution times, as well as percentile-based performance analysis.

### Example Usage

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using System;

// Create a timeout policy with a 5-second timeout
var timeoutPolicy = new TimeoutPolicy("user-service-timeout")
{
Timeout = TimeSpan.FromSeconds(5)
};

// Validate configuration
if (!timeoutPolicy.IsValidConfiguration(out var configError))
{
Console.WriteLine($"Invalid configuration: {configError}");
}

// Record successful execution times
// Simulate an API call that takes 150ms
timeoutPolicy.RecordExecutionTime(150);
timeoutPolicy.RecordExecutionTime(250);
timeoutPolicy.RecordExecutionTime(80);

// Check if an execution time would exceed the timeout
bool isTimedOut = timeoutPolicy.IsTimedOutMs(6000); // 6 seconds
Console.WriteLine($"Would 6 seconds timeout? {isTimedOut}"); // True

// Access timeout statistics
Console.WriteLine($"Timeout count: {timeoutPolicy.TimeoutCount}");
Console.WriteLine($"Average execution time: {timeoutPolicy.AverageExecutionTimeMs:F1}ms");
Console.WriteLine($"Longest execution time: {timeoutPolicy.LongestExecutionTimeMs}ms");
Console.WriteLine($"Shortest execution time: {timeoutPolicy.ShortestExecutionTimeMs}ms");
Console.WriteLine($"Timeout percentage: {timeoutPolicy.GetTimeoutPercentage():F1}%");

// Record a timeout event
// Simulate an operation that timed out after 7 seconds
timeoutPolicy.RecordTimeout(7000);

// Get percentile execution times for performance analysis
Console.WriteLine($"P95 execution time: {timeoutPolicy.GetPercentile95ExecutionTime()}ms");
Console.WriteLine($"P99 execution time: {timeoutPolicy.GetPercentile99ExecutionTime()}ms");

// Get detailed snapshot for monitoring
var snapshot = timeoutPolicy.GetSnapshot();
Console.WriteLine($"Policy snapshot - Timeout: {snapshot.Metadata["TimeoutMs"]}ms");
Console.WriteLine($"Policy snapshot - P95: {snapshot.Metadata["P95ExecutionTimeMs"]}ms");
Console.WriteLine($"Policy snapshot - Timeout percentage: {snapshot.Metadata["TimeoutPercentage"]}%");

// Reset statistics when needed (e.g., after maintenance)
timeoutPolicy.ResetStatistics();
Console.WriteLine("Statistics reset. All counters cleared.");
```

## AdaptiveTimeoutPolicy

The `AdaptiveTimeoutPolicy` implements an intelligent timeout strategy that automatically adjusts its timeout ceiling based on observed response-time percentiles within a sliding window of recent executions. This policy learns from actual performance characteristics and dynamically scales timeouts to balance responsiveness with reliability, preventing both premature timeouts and excessive waiting.

Key features include:
- Sliding window percentile tracking (P50, P95, P99)
- Configurable target percentile and headroom factor
- Minimum and maximum timeout bounds for safety
- Automatic adaptation based on adjustment intervals
- Comprehensive observability through execution metrics

### Example Usage

```csharp
using DotNetResiliencePipeline.Domain.Policies;
using System;
using System.Threading.Tasks;

// Create an adaptive timeout policy with a name
var adaptiveTimeout = new AdaptiveTimeoutPolicy("UserServiceTimeout")
{
InitialTimeout = TimeSpan.FromSeconds(10),
MinTimeout = TimeSpan.FromMilliseconds(200),
MaxTimeout = TimeSpan.FromSeconds(60),
TargetPercentile = 95.0,      // Adapt based on 95th percentile
HeadroomFactor = 1.2,          // Add 20% headroom above percentile
WindowSize = 100,              // Track last 100 executions
MinSampleSize = 10,            // Require at least 10 samples before adapting
AdjustmentInterval = TimeSpan.FromSeconds(30) // Adjust every 30 seconds
};

// Validate configuration before use
if (!adaptiveTimeout.IsValidConfiguration(out var configError))
{
Console.WriteLine($"Invalid configuration: {configError}");
return;
}

// Execute an operation with adaptive timeout protection
var result = await adaptiveTimeout.ExecuteAsync(async () =>
{
// Simulate an external API call
await Task.Delay(Random.Shared.Next(50, 500));
return "Operation completed successfully";
});

// Record execution time after the operation completes
adaptiveTimeout.RecordExecutionTime(150); // 150ms execution time

// Check current timeout and statistics
Console.WriteLine($"Current timeout: {adaptiveTimeout.CurrentTimeout.TotalMilliseconds}ms");
Console.WriteLine($"Timeout count: {adaptiveTimeout.TimeoutCount}");
Console.WriteLine($"Total adjustments: {adaptiveTimeout.TotalAdjustments}");
Console.WriteLine($"Timeout percentage: {adaptiveTimeout.GetTimeoutPercentage():F1}%");

// Get percentile execution times
Console.WriteLine($"P50 execution time: {adaptiveTimeout.GetPercentileExecutionTime(50)}ms");
Console.WriteLine($"P95 execution time: {adaptiveTimeout.GetPercentileExecutionTime(95)}ms");
Console.WriteLine($"P99 execution time: {adaptiveTimeout.GetPercentileExecutionTime(99)}ms");

// Handle timeout events
try
{
await adaptiveTimeout.ExecuteAsync(async () =>
{
await Task.Delay(5000); // Will timeout
return "Should not reach here";
});
}
catch (TimeoutException)
{
// Record the timeout event
adaptiveTimeout.RecordTimeout(5000);
Console.WriteLine("Timeout occurred and was recorded");
}

// Access detailed snapshot for monitoring
var snapshot = adaptiveTimeout.GetSnapshot();
Console.WriteLine($"Policy '{snapshot.PolicyName}' - Current timeout: {snapshot.Metadata["CurrentTimeoutMs"]}ms");
Console.WriteLine($"P95 execution: {snapshot.Metadata["P95ExecutionTimeMs"]}ms");
Console.WriteLine($"Timeout percentage: {snapshot.Metadata["TimeoutPercentage"]}%");

// Reset statistics when needed (e.g., after maintenance)
adaptiveTimeout.ResetStatistics();
Console.WriteLine("Statistics reset. Current timeout reverted to initial value.");
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
```