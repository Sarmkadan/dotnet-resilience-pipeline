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
- [MetricsCollectorWorker](#metricscolllectorworker)
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

## HealthCheckWorker

The `HealthCheckWorker` is a background service that continuously monitors the health of resilience policies by periodically checking their success rates. It automatically detects when policies degrade below configured thresholds and publishes health change events. The worker provides real-time health status, historical metrics, and programmatic control over monitoring behavior.





```csharp
// Example: Setting up and using the HealthCheckWorker
var pipelineService = new ResiliencyPipelineService();
var eventPublisher = new ResiliencyEventPublisher();
var healthChecker = new HealthCheckWorker(pipelineService, eventPublisher);

// Configure health check interval (default: 30 seconds)
healthChecker.CheckInterval = TimeSpan.FromSeconds(45);

// Set health thresholds (default: 95% healthy, 80% degraded)
healthChecker.HealthyThreshold = 0.92; // 92% healthy threshold
healthChecker.DegradedThreshold = 0.75; // 75% degraded threshold

// Start the health checker
healthChecker.Start();

// Get current health status
var status = healthChecker.GetStatus();
Console.WriteLine($"Running: {status.IsRunning}");
Console.WriteLine($"Success rate: {status.PipelineSuccessRate:P}");
Console.WriteLine($"Overall health: {status.OverallHealth}");
Console.WriteLine($"Total policies: {status.TotalPolicies}");
Console.WriteLine($"Total executions: {status.TotalExecutions}");
Console.WriteLine($"Last check: {status.LastCheckTime:O}");

// Check if worker is running
if (healthChecker.IsRunning)
{
    Console.WriteLine("Health check worker is actively monitoring policies");
}

// Stop the health checker when application shuts down
await healthChecker.StopAsync();
```

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

## MetricsCollectorWorker

The `MetricsCollectorWorker` is a background service that periodically collects and aggregates resilience metrics from all configured policies. It maintains time-series data for trend analysis, generates performance reports, and provides programmatic access to historical metrics through a fluent API.


```csharp
// Example: Setting up and using the MetricsCollectorWorker
var pipelineService = new ResiliencyPipelineService();
var aggregator = new MetricsAggregator();
var metricsCollector = new MetricsCollectorWorker(pipelineService, aggregator);

// Configure collection interval (default: 10 seconds)
metricsCollector.CollectionInterval = TimeSpan.FromSeconds(15);

// Start the collector
metricsCollector.Start();

// Get current status
var status = metricsCollector.GetStatus();
Console.WriteLine($"Running: {status.IsRunning}, Collections: {status.TotalCollections}");

// Retrieve metrics for last 5 minutes
var recentMetrics = metricsCollector.GetMetricsForTimeRange(TimeSpan.FromMinutes(5));
Console.WriteLine($"Success rate: {recentMetrics.SuccessRate:P}");

// Generate a performance report for the last hour
var report = metricsCollector.GenerateReport(TimeSpan.FromHours(1));
Console.WriteLine($"Total executions: {report.TotalExecutions}");

// Analyze trends
var trend = metricsCollector.GetTrendAnalysis(TimeSpan.FromHours(24));
Console.WriteLine($"Trend direction: {trend.Direction}");

// Stop the collector when application shuts down
await metricsCollector.StopAsync();
```

## MetricsController

The `MetricsController` provides REST API endpoints for retrieving execution statistics, health metrics, and execution history from the resilience pipeline. It exposes endpoints for pipeline-level metrics, per-policy metrics, health status checks, execution history, and metrics reset functionality.







```csharp
// Example: Using the MetricsController in an ASP.NET Core application
var builder = WebApplication.CreateBuilder(args);

// Add required services
builder.Services.AddResiliencyPipelineServices();
builder.Services.AddSingleton<ExecutionHistoryRepository>();
builder.Services.AddControllers();

var app = builder.Build();

// Map endpoints
app.MapGet("/api/metrics/pipeline", async (MetricsController controller) =>
    await controller.GetPipelineMetricsAsync());

app.MapGet("/api/metrics/policies", async (MetricsController controller) =>
    await controller.GetPoliciesMetricsAsync());

app.MapGet("/api/metrics/health", async (MetricsController controller) =>
    await controller.GetHealthStatusAsync());

app.MapGet("/api/metrics/history", async (MetricsController controller, int limit = 100) =>
    await controller.GetExecutionHistoryAsync(limit));

app.MapPost("/api/metrics/reset", async (MetricsController controller) =>
    await controller.ResetMetricsAsync());

// Initialize controller with injected dependencies
var pipelineService = app.Services.GetRequiredService<ResiliencyPipelineService>();
var historyRepository = app.Services.GetRequiredService<ExecutionHistoryRepository>();
var metricsController = new MetricsController(pipelineService, historyRepository);

// Example: Calling controller methods directly
var pipelineMetrics = await metricsController.GetPipelineMetricsAsync();
Console.WriteLine($"Pipeline ID: {pipelineMetrics.Data?.PipelineId}");
Console.WriteLine($"Success Rate: {pipelineMetrics.Data?.SuccessRate:P}");
Console.WriteLine($"Total Executions: {pipelineMetrics.Data?.TotalExecutions}");

var healthStatus = await metricsController.GetHealthStatusAsync();
Console.WriteLine($"Health Status: {healthStatus.Data?.Status}");

var policiesMetrics = await metricsController.GetPoliciesMetricsAsync();
foreach (var policy in policiesMetrics.Data ?? new List<PolicyMetricsDto>()) {
    Console.WriteLine($"Policy {policy.PolicyName}: {policy.SuccessRate:P} success rate");
}
```

## PoliciesController

The `PoliciesController` is a REST API controller that manages resilience policies through CRUD operations and validation. It provides endpoints for creating, retrieving, updating, and deleting policies, as well as validating policy configurations before application. The controller supports multiple policy types including Circuit Breaker, Retry, Timeout, Bulkhead, and Fallback policies.










```csharp
// Example: Using the PoliciesController in an ASP.NET Core application
var builder = WebApplication.CreateBuilder(args);

// Add required services
builder.Services.AddResiliencyPipelineServices();
builder.Services.AddSingleton<PolicyRepository>();
builder.Services.AddControllers();

var app = builder.Build();

// Map endpoints
app.MapGet("/api/policies", async (PoliciesController controller) =>
    await controller.GetAllPoliciesAsync());

app.MapGet("/api/policies/{id}", async (PoliciesController controller, string id) =>
    await controller.GetPolicyAsync(id));

app.MapPost("/api/policies", async (PoliciesController controller, CreatePolicyRequest request) =>
    await controller.CreatePolicyAsync(request));

app.MapPut("/api/policies/{id}", async (PoliciesController controller, string id, UpdatePolicyRequest request) =>
    await controller.UpdatePolicyAsync(id, request));

app.MapDelete("/api/policies/{id}", async (PoliciesController controller, string id) =>
    await controller.DeletePolicyAsync(id));

app.MapPost("/api/policies/validate", async (PoliciesController controller, ValidatePolicyRequest request) =>
    await controller.ValidatePolicyAsync(request));

// Initialize controller with injected dependencies
var pipelineService = app.Services.GetRequiredService<ResiliencyPipelineService>();
var policyRepository = app.Services.GetRequiredService<PolicyRepository>();
var policiesController = new PoliciesController(pipelineService, policyRepository);

// Example: Creating a Circuit Breaker policy
var circuitBreakerResponse = await policiesController.CreatePolicyAsync(new CreatePolicyRequest {
    Name = "order-processing-circuit-breaker",
    Type = "circuitbreaker",
    FailureThreshold = 5,
    OpenDurationSeconds = 30
});

if (circuitBreakerResponse.Success) {
    Console.WriteLine($"Created policy: {circuitBreakerResponse.Data?.Name} ({circuitBreakerResponse.Data?.Id})");
}

// Example: Creating a Retry policy
var retryResponse = await policiesController.CreatePolicyAsync(new CreatePolicyRequest {
    Name = "api-call-retry",
    Type = "retry",
    MaxRetries = 3,
    InitialDelayMs = 100
});

if (retryResponse.Success) {
    Console.WriteLine($"Created policy: {retryResponse.Data?.Name} ({retryResponse.Data?.Id})");
}

// Example: Getting all policies
var allPolicies = await policiesController.GetAllPoliciesAsync();
if (allPolicies.Success) {
    foreach (var policy in allPolicies.Data ?? new List<PolicyDto>()) {
        Console.WriteLine($"Policy: {policy.Name} ({policy.Type}) - Status: {(policy.IsEnabled ? "Enabled" : "Disabled")}");
    }
}

// Example: Updating a policy
var updateResponse = await policiesController.UpdatePolicyAsync(
    circuitBreakerResponse.Data!.Id,
    new UpdatePolicyRequest {
        IsEnabled = true,
        CircuitBreakerConfig = new CircuitBreakerConfigDto {
            FailureThreshold = 3,
            OpenDurationSeconds = 60
        }
    }
);

// Example: Validating a policy configuration
var validationResponse = await policiesController.ValidatePolicyAsync(new ValidatePolicyRequest {
    Name = "test-policy",
    Type = "circuitbreaker",
    FailureThreshold = 5
});

Console.WriteLine($"Policy configuration is {(validationResponse.Data?.IsValid == true ? "valid" : "invalid")}");
```

## ...

