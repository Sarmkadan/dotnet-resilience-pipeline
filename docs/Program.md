# Program.cs Documentation

This file contains the entry point for the DotNet Resilience Pipeline demonstration application. It showcases various resilience patterns including circuit breaker, retry, timeout, bulkhead, and fallback mechanisms.

## Overview

The `Program.cs` file sets up a dependency injection container, configures resilience policies, and runs demo scenarios to illustrate how each pattern behaves under different conditions.

## Constants

The following constants are defined at the top of the file for configuring the resilience policies and demo behavior:

| Constant | Value | Description |
|----------|-------|-------------|
| `CircuitBreakerFailureThreshold` | 5 | Number of failures before opening the circuit breaker |
| `CircuitBreakerOpenDuration` | 30 seconds | Time the circuit breaker remains open before attempting to close |
| `RetryMaxAttempts` | 3 | Maximum number of retry attempts |
| `RetryInitialDelay` | 100 ms | Initial delay between retry attempts (uses exponential backoff) |
| `OperationTimeout` | 10 seconds | Maximum time allowed for an operation before timing out |
| `BulkheadMaxParallelization` | 10 | Maximum number of concurrent executions allowed by the bulkhead |
| `BulkheadMaxQueueLength` | 50 | Maximum number of queued requests when bulkhead is at capacity |
| `FallbackTimeout` | 5 seconds | Timeout for fallback operations |
| `SimpleOperationDelayMilliseconds` | 50 ms | Delay for successful simple operation demo |
| `CircuitBreakerDemoIterationCount` | 8 | Number of iterations in the circuit breaker demo |
| `SimulatedFailureCount` | 5 | Number of simulated failures before allowing success in circuit breaker demo |
| `SuccessfulPaymentDelayMilliseconds` | 10 ms | Delay for successful payment simulation |
| `FastOperationDelayMilliseconds` | 500 ms | Delay for fast operation in timeout demo |
| `SlowOperationDelayMilliseconds` | 15000 ms | Delay for slow operation that will trigger timeout |
| `BulkheadDemoTaskCount` | 15 | Number of concurrent tasks in bulkhead demo |
| `BulkheadTaskDelayMilliseconds` | 200 ms | Delay for each task in bulkhead demo |

## Demo Scenarios

The program runs four main demonstration scenarios:

### 1. Simple Operation with Retry Policy
Demonstrates a successful operation that uses the retry policy (though no retries are needed as the operation always succeeds).

### 2. Circuit Breaker Pattern
Simulates a service that fails consistently for the first 5 attempts, then succeeds. Shows how the circuit breaker transitions from Closed → Open → Half-Open → Closed states.

### 3. Timeout Policy
Demonstrates both a fast operation that completes within the timeout limit and a slow operation that exceeds the timeout and triggers a timeout exception.

### 4. Bulkhead Pattern
Shows how the bulkhead limits concurrent executions and queues excess requests when the concurrency limit is reached.

### 5. Fallback Mechanism
While not demonstrated in a separate method, the fallback policy is registered in the pipeline configuration and would be triggered when any exception occurs in a protected operation.

## How to Run

To compile and run the demonstration:

```bash
dotnet run
```

The program will output:
- Results of each demo scenario
- Pipeline statistics (total executions, success/failure counts, success rate)
- Pipeline health report

## Dependencies

This program relies on the following project components:
- `DotNetResiliencePipeline.Configuration` - Extension methods for adding resilience policies
- `DotNetResiliencePipeline.Data` - Execution history repository
- `DotNetResiliencePipeline.Domain.Policies` - Policy implementations
- `DotNetResiliencePipeline.Services` - ResiliencyPipelineService implementation
- `DotNetResiliencePipeline.Utilities` - Helper methods for health reports

## Notes

- All resilience policies are registered via the `AddResiliencePipeline` extension method
- The program uses dependency injection to obtain the pipeline service and history repository
- Execution history is recorded for each demo scenario to enable statistics and health reporting