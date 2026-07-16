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
- [CircuitBreakerDashboardController](#circuitbreakerdashboardcontroller)
- [MetricsCollectorWorker](#metricscolllectorworker)
- [CliCommandValidator](#clicommandvalidator)
- [FailureInjectionService](#failureinjectionservice)
- [Failure Injection Testing](#failure-injection-testing)
- [PolicyCacheService](#policycacheservice)
- [Resilience Metrics Export](#resilience-metrics-export)
- [Deployment](#deployment)
- [Troubleshooting](#troubleshooting)
- [Testing](#testing)
- [Benchmarks](#benchmarks)
- [Related Projects](#related-projects)
- [Contributing](#contributing)
- [License](#license)

## CircuitBreakerDashboardController

The `CircuitBreakerDashboardController` is a REST API controller that provides real-time visibility into all circuit breakers within the resilience pipeline. It exposes endpoints for retrieving dashboard summaries, individual breaker statuses, breaker resets, and tracking open breakers. The controller aggregates state information, failure metrics, and health indicators to help operations teams monitor circuit breaker behavior and troubleshoot failures.

```csharp
// Example: Using the CircuitBreakerDashboardController in an ASP.NET Core application
var builder = WebApplication.CreateBuilder(args);

// Add required services
builder.Services.AddResiliencyPipelineServices();
builder.Services.AddSingleton<CircuitBreakerService>();
builder.Services.AddControllers();

var app = builder.Build();

// Map endpoints
app.MapGet("/api/dashboard/circuit-breakers", async (CircuitBreakerDashboardController controller) =>
    await controller.GetDashboardAsync());

app.MapGet("/api/dashboard/circuit-breakers/{name}", async (CircuitBreakerDashboardController controller, string name) =>
    await controller.GetBreakerStatusAsync(name));

app.MapPost("/api/dashboard/circuit-breakers/{name}/reset", async (CircuitBreakerDashboardController controller, string name) =>
    await controller.ResetBreakerAsync(name));

app.MapGet("/api/dashboard/circuit-breakers/open", async (CircuitBreakerDashboardController controller) =>
    await controller.GetOpenBreakersAsync());

// Initialize controller with injected dependencies
var pipelineService = app.Services.GetRequiredService<ResiliencyPipelineService>();
var circuitBreakerService = app.Services.GetRequiredService<CircuitBreakerService>();
var dashboardController = new CircuitBreakerDashboardController(pipelineService, circuitBreakerService);

// Example: Getting the full dashboard
var dashboardResponse = await dashboardController.GetDashboardAsync();
if (dashboardResponse.Success && dashboardResponse.Data != null)
{
    var dashboard = dashboardResponse.Data;
    Console.WriteLine($"Dashboard generated at: {dashboard.GeneratedAt:O}");
    Console.WriteLine($"Total breakers: {dashboard.TotalBreakers}");
    Console.WriteLine($"Open breakers: {dashboard.OpenCount}");
    Console.WriteLine($"Half-open breakers: {dashboard.HalfOpenCount}");
    Console.WriteLine($"Total trips: {dashboard.TotalTrips}");
    Console.WriteLine($"Overall health: {dashboard.OverallHealth}");
    
    foreach (var breaker in dashboard.Breakers)
    {
        Console.WriteLine($"Breaker '{breaker.Name}': {breaker.State} (Failures: {breaker.ConsecutiveFailures}/{breaker.FailureThreshold})");
    }
}

// Example: Getting status for a specific breaker
var statusResponse = await dashboardController.GetBreakerStatusAsync("order-processing-circuit-breaker");
if (statusResponse.Success && statusResponse.Data != null)
{
    var status = statusResponse.Data;
    Console.WriteLine($"Breaker '{status.Name}' is {status.State}");
    Console.WriteLine($"Consecutive failures: {status.ConsecutiveFailures}");
    Console.WriteLine($"Trip count: {status.TripCount}");
    Console.WriteLine($"Seconds until half-open: {status.SecondsUntilHalfOpen}");
}

// Example: Resetting a breaker
var resetResponse = await dashboardController.ResetBreakerAsync("payment-service-circuit-breaker");
if (resetResponse.Success)
{
    Console.WriteLine("Circuit breaker reset successfully");
}

// Example: Getting all open breakers
var openBreakersResponse = await dashboardController.GetOpenBreakersAsync();
if (openBreakersResponse.Success && openBreakersResponse.Data != null)
{
    foreach (var breaker in openBreakersResponse.Data)
    {
        Console.WriteLine($"Open breaker: {breaker.Name} (Consecutive failures: {breaker.ConsecutiveFailures})");
    }
}
```

## ResiliencyHelper

The `ResiliencyHelper` utility class provides helper methods for working with resilience policies and execution records. It offers functionality for creating policy results from execution records, converting execution results to records, validating policy configurations, generating health reports, and exporting policy configurations for monitoring and debugging purposes.

```csharp
// Example: Using ResiliencyHelper to create policy results and execution records
var helper = new ResiliencyHelper();

// Create an execution record from a successful policy execution
var executionRecord = new ExecutionRecord
{
    PolicyName = "order-processing-circuit-breaker",
    OperationKey = "process-order",
    StartedAt = DateTime.UtcNow.AddSeconds(-5),
    CompletedAt = DateTime.UtcNow,
    IsSuccessful = true,
    Exception = null,
    Context = new Dictionary<string, object> { { "orderId", "12345" } }
};

// Convert execution record to policy result
var policyResult = ResiliencyHelper.CreateResultFromRecord(executionRecord);
Console.WriteLine($"Policy result created: Success={policyResult.Success}, Duration={policyResult.Duration.TotalMilliseconds}ms");

// Create execution record from policy result
var newRecord = ResiliencyHelper.CreateRecordFromResult(policyResult);
Console.WriteLine($"Record created: PolicyName={newRecord.PolicyName}, IsSuccessful={newRecord.IsSuccessful}");

// Validate a policy configuration
var validationErrors = ResiliencyHelper.ValidatePolicy("order-processing-circuit-breaker");
if (validationErrors.Count == 0)
{
    Console.WriteLine("Policy configuration is valid");
}
else
{
    Console.WriteLine($"Policy validation failed: {string.Join(", ", validationErrors)}");
}

// Generate a health report for the pipeline
var healthReport = ResiliencyHelper.GenerateHealthReport("order-processing-pipeline");
Console.WriteLine($"Pipeline health: {healthReport.HealthStatus}");
Console.WriteLine($"Total executions: {healthReport.TotalExecutions}");
Console.WriteLine($"Success rate: {healthReport.SuccessRate:P}");
Console.WriteLine($"Policy count: {healthReport.PolicyCount}");

// Determine pipeline health status
var healthStatus = ResiliencyHelper.DeterminePipelineHealth(healthReport);
Console.WriteLine($"Determined health status: {healthStatus}");

// Export policy configuration for monitoring
var configExport = ResiliencyHelper.ExportPolicyConfig("order-processing-circuit-breaker");
foreach (var kvp in configExport)
{
    Console.WriteLine($"{kvp.Key}: {kvp.Value}");
}

// Access pipeline health metrics
var metrics = new PipelineHealthReport
{
    PipelineId = "order-processing-pipeline",
    ReportGeneratedAt = DateTime.UtcNow,
    TotalExecutions = 1500,
    SuccessRate = 0.95,
    PolicyCount = 3,
    HealthStatus = HealthStatus.Healthy,
    Policies = new List<PolicySnapshot>(),
    HistoryStatistics = new Dictionary<string, object> { { "lastHour", 1200 } }
};

Console.WriteLine($"Pipeline {metrics.PipelineId} generated at {metrics.ReportGeneratedAt:O}");
Console.WriteLine($"Success rate: {metrics.SuccessRate:P}");
Console.WriteLine($"Health status: {metrics.HealthStatus}");
```

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

## MetricsAggregator

The `MetricsAggregator` class aggregates metrics across multiple resilience policies and provides system-wide analytics. It maintains a time-series of metrics snapshots, supports trend analysis, period comparisons, and generates comprehensive performance reports with health assessments.




```csharp
// Example: Using MetricsAggregator for system-wide metrics aggregation
var aggregator = new MetricsAggregator { MaxSnapshots = 500 };

// Record a metrics snapshot (typically done by MetricsCollectorWorker)
var snapshot = new MetricsAggregator.MetricsSnapshot
{
    Timestamp = DateTime.UtcNow,
    TotalExecutions = 1500,
    SuccessfulExecutions = 1425,
    FailedExecutions = 75,
    SuccessRate = 0.95,
    AverageExecutionTimeMs = 45.5,
    ActivePolicies = 8
};
aggregator.RecordSnapshot(snapshot);

// Get aggregated metrics for the last 5 minutes
var recentMetrics = aggregator.GetAggregatedMetrics(TimeSpan.FromMinutes(5));
Console.WriteLine($"Success rate: {recentMetrics.AverageSuccessRate:P}");
Console.WriteLine($"Average execution time: {recentMetrics.AverageExecutionTimeMs:F1}ms");
Console.WriteLine($"Total executions: {recentMetrics.TotalExecutions}");
Console.WriteLine($"Peak executions: {recentMetrics.PeakExecutions}");

// Analyze trends over the last hour
var trend = aggregator.AnalyzeTrend(TimeSpan.FromHours(1), "SuccessRate");
Console.WriteLine($"Trend direction: {trend.Direction}");
Console.WriteLine($"Change percentage: {trend.ChangePercentage:F1}%");
Console.WriteLine($"Current value: {trend.Current:P}");
Console.WriteLine($"Is anomaly: {trend.IsAnomaly}");

// Compare performance between two time periods
var comparison = aggregator.ComparePeriods(
    TimeSpan.FromHours(1),
    TimeSpan.FromHours(24)
);
Console.WriteLine($"Success rate difference: {comparison.SuccessRateDifference:P}");
Console.WriteLine($"Execution time difference: {comparison.ExecutionTimeDifference:F1}ms");
Console.WriteLine($"Is improving: {comparison.IsImproving}");

// Generate a comprehensive performance report
var report = aggregator.GenerateReport(TimeSpan.FromHours(24));
Console.WriteLine($"Health status: {report.HealthStatus}");
Console.WriteLine($"Generated at: {report.GeneratedAt:O}");
Console.WriteLine($"Snapshot count: {report.AggregatedMetrics.SnapshotCount}");

// Clear all recorded snapshots
aggregator.Clear();
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

## PolicyCacheService

The `PolicyCacheService` provides caching functionality for resilience policies, reducing repeated policy lookups and improving application performance. It caches policy configurations with configurable time-to-live (TTL) and enforces a maximum cache size to prevent memory exhaustion. The service tracks cache statistics including hit rates, access patterns, and expiration metrics.

## PolicyNameGenerator

The `PolicyNameGenerator` utility class generates meaningful, unique, and consistent names for resilience policies. It supports various naming conventions including service-based names, descriptive names with purpose, prefixed names for organizational purposes, and template-based naming. The generator ensures name uniqueness across the application and provides validation and management capabilities for registered policy names.

```csharp
// Example: Basic policy name generation
var generator = new PolicyNameGenerator();

// Generate a circuit breaker policy name for the OrderService
var circuitBreakerName = generator.GenerateName("OrderService", "CircuitBreaker");
Console.WriteLine(circuitBreakerName); // Output: orderservice-cb-1

// Generate a retry policy with a custom number
var retryName = generator.GenerateName("PaymentService", "Retry", 5);
Console.WriteLine(retryName); // Output: paymentservice-retry-5

// Generate a descriptive policy name
var descriptiveName = generator.GenerateDescriptiveName(
    "UserService", 
    "CircuitBreaker", 
    "external-api-failures");
Console.WriteLine(descriptiveName); // Output: userservice-external-api-failures-circuitbreaker

// Generate a name with prefix
var prefixedName = generator.GenerateNameWithPrefix("production", "InventoryService", "Timeout");
Console.WriteLine(prefixedName); // Output: production-inventoryservice-timeout-1

// Validate a policy name
bool isValid = generator.IsValidPolicyName("orderservice-cb-1");
Console.WriteLine(isValid); // Output: True

// Suggest a name based on context
var suggestedName = generator.SuggestName(
    "NotificationService", 
    "SendEmail", 
    "smtp-connection-failure");
Console.WriteLine(suggestedName); // Output: notificationservice-sendemail-smtp-connection-failure

// Register and manage names
var nameToRegister = generator.GenerateName("LoggingService", "Bulkhead");
generator.RegisterName(nameToRegister);
var allNames = generator.GetAllRegisteredNames();
Console.WriteLine($"Registered names: {string.Join(", ", allNames)}");

generator.UnregisterName(nameToRegister);
generator.Clear();
```

## NamingTemplate

The `NamingTemplate` class provides a structured approach to building policy names from template values. It allows you to construct names based on service, operation, policy type, and environment components, making it easy to maintain consistent naming conventions across different environments.

```csharp
// Example: Using the NamingTemplate
var template = new NamingTemplate
{
    Service = "OrderProcessing",
    Operation = "Checkout",
    PolicyType = "CircuitBreaker",
    Environment = "Production"
};

var templateName = template.BuildName();
Console.WriteLine(templateName); // Output: orderprocessing-checkout-circuitbreaker-production
```

```csharp
// Example: Using PolicyCacheService for caching policy configurations
var cacheService = new PolicyCacheService
{
    DefaultTtl = TimeSpan.FromMinutes(10),
    MaxCacheSize = 500
};

// Create a sample retry policy configuration
var retryConfig = new
{
    MaxRetries = 3,
    InitialDelayMs = 100,
    Strategy = "exponential"
};

// Cache the policy configuration
cacheService.Set("retry-policy", retryConfig, TimeSpan.FromMinutes(15));

// Retrieve the cached policy
var cachedPolicy = cacheService.Get("retry-policy");
if (cachedPolicy != null)
{
    Console.WriteLine($"Policy '{cachedPolicy.PolicyName}' cached at {cachedPolicy.CreatedAt:O}");
    Console.WriteLine($"Expires at: {cachedPolicy.ExpiresAt:O}");
    Console.WriteLine($"Remaining TTL: {cachedPolicy.RemainingTtl.TotalSeconds:F0} seconds");
    Console.WriteLine($"Access count: {cachedPolicy.AccessCount}");
    Console.WriteLine($"Config: {cachedPolicy.Config}");
}

// Get cache statistics
var stats = cacheService.GetStatistics();
Console.WriteLine($"Total entries: {stats.TotalEntries}");
Console.WriteLine($"Valid entries: {stats.ValidEntries}");
Console.WriteLine($"Expired entries: {stats.ExpiredEntries}");
Console.WriteLine($"Hit rate: {stats.HitRate:P}");
Console.WriteLine($"Average TTL: {stats.AverageTtl.TotalSeconds:F0} seconds");

// Invalidate a specific policy
cacheService.Invalidate("retry-policy");

// Clear the entire cache
cacheService.Clear();
```

## ...

