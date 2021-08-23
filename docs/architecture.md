# Architecture Guide

Comprehensive overview of the DotNet Resilience Pipeline architecture, design patterns, and component interactions.

## System Architecture

### High-Level Overview

```
┌─────────────────────────────────────────────────────────────────┐
│                      Application Code                           │
└────────────────────────────┬────────────────────────────────────┘
                             │
┌────────────────────────────▼────────────────────────────────────┐
│             ResiliencyPipelineService (Orchestrator)             │
│              - Coordinates policy execution                      │
│              - Manages policy composition                        │
│              - Tracks execution metrics                          │
└────────────────────────────┬────────────────────────────────────┘
                             │
        ┌────────┬──────┬───┼───┬──────────┐
        ▼        ▼      ▼   ▼   ▼          ▼
   ┌────────┐ ┌──────┐ ┌──────┐ ┌──────────┐ ┌──────────┐
   │Circuit │ │Retry │ │Timeout│ │Bulkhead │ │Fallback  │
   │Breaker │ │      │ │       │ │         │ │          │
   │Service │ │Service│ │Service│ │Service  │ │Service   │
   └────────┘ └──────┘ └──────┘ └──────────┘ └──────────┘
        │        │       │          │            │
        └────────┴───────┴──────────┴────────────┘
                      │
        ┌─────────────┼─────────────┐
        ▼             ▼             ▼
   ┌──────────┐ ┌──────────┐ ┌──────────────┐
   │  Event   │ │ Metrics  │ │ Execution    │
   │Publisher │ │Aggregator│ │ History      │
   └──────────┘ └──────────┘ └──────────────┘
```

## Component Layers

### 1. Domain Layer

**Location:** `src/Domain/`

Contains core business logic and policy implementations.

**Key Classes:**

- **ResiliencyPolicy** (Abstract)
  - Base class for all policies
  - Defines common interface
  - State management contract

- **CircuitBreakerPolicy**
  - State machine: Closed → Open → Half-Open
  - Failure threshold tracking
  - Time-based state transitions

- **RetryPolicy**
  - Backoff strategy implementation
  - Retry attempt tracking
  - Delay calculation logic

- **TimeoutPolicy**
  - Duration enforcement
  - CancellationToken integration
  - Timeout detection

- **BulkheadPolicy**
  - Semaphore-based capacity management
  - Queue length tracking
  - Resource isolation

- **FallbackPolicy**
  - Fallback execution orchestration
  - Exception type filtering
  - Timeout-aware execution

- **PolicyResult<T>** (Generic)
  - Execution outcome wrapper
  - Success/failure indication
  - Metadata (duration, retry count, etc.)

### 2. Service Layer

**Location:** `src/Services/`

Implements execution logic for each policy.

**Key Classes:**

- **ResiliencyPipelineService**
  - Main orchestrator
  - Policy composition
  - Execution flow control
  - Statistics aggregation

- **CircuitBreakerService**
  - State management
  - Failure rate calculation
  - State transition logic

- **RetryService**
  - Retry loop implementation
  - Backoff calculation
  - Attempt tracking

- **TimeoutService**
  - CancellationToken creation
  - Timeout enforcement
  - Exception translation

- **BulkheadService**
  - Semaphore management
  - Slot acquisition/release
  - Queue management

- **FallbackService**
  - Fallback execution
  - Exception filtering
  - Result determination

### 3. Data Layer

**Location:** `src/Data/`

Manages persistence and data access.

**Key Classes:**

- **PolicyRepository**
  - Policy CRUD operations
  - In-memory storage (configurable)
  - Policy lookup by name

- **ExecutionHistoryRepository**
  - Execution record storage
  - Metrics query interface
  - History aggregation

- **IRepository** (Interface)
  - Generic repository contract
  - Enables custom implementations

### 4. Configuration Layer

**Location:** `src/Configuration/`

Handles setup and dependency injection.

**Key Classes:**

- **ResiliencyPipelineBuilder**
  - Fluent builder pattern
  - Policy registration
  - Configuration validation

- **DependencyInjectionExtensions**
  - ServiceCollection extensions
  - Service registration
  - Dependency wiring

### 5. Infrastructure Layer

**Location:** `src/Utilities/`, `src/Middleware/`, `src/Integration/`

Supporting components for operation.

**Utilities:**
- `PolicyValidationHelper` - Configuration validation
- `PerformanceMonitor` - Performance tracking
- `MetricsAggregator` - Statistics calculation
- `ResiliencyHelper` - General utilities
- `CircuitBreakerDiagnostics` - Diagnostic information

**Middleware:**
- `ResiliencyLoggingMiddleware` - Request/response logging
- `ErrorHandlingMiddleware` - Global error handling
- `RateLimitingMiddleware` - Rate limit enforcement

**Integration:**
- `ExternalApiClient` - External service calls
- `HttpClientFactory` - HTTP client creation
- `WebhookManager` - Webhook notification

**Events:**
- `ResiliencyEventPublisher` - Event publishing
- `PipelineEventObserver` - Event subscription

## Execution Flow

### Single Policy Execution

```
User Code
    │
    └─→ ResiliencyPipelineService.ExecuteAsync()
            │
            ├─→ PolicyService.ExecuteAsync()
            │       │
            │       ├─→ Policy State Check (if applicable)
            │       │
            │       ├─→ Pre-Execution Hooks
            │       │
            │       ├─→ ExecutionHistoryRepository.RecordStart()
            │       │
            │       ├─→ User Operation (CancellationToken aware)
            │       │
            │       ├─→ ExecutionHistoryRepository.RecordCompletion()
            │       │
            │       ├─→ MetricsAggregator.UpdateMetrics()
            │       │
            │       └─→ ResiliencyEventPublisher.PublishEvent()
            │
            └─→ Return PolicyResult<T>
```

### Multi-Policy Pipeline Execution

```
User Code
    │
    └─→ ResiliencyPipelineService.ExecuteAsync()
            │
            ├─→ Validation Phase
            │
            ├─→ CircuitBreaker Check
            │   └─→ If Open: throw CircuitBreakerOpenException
            │
            ├─→ Retry Loop (if retry policy present)
            │   ├─→ Timeout Check (if timeout policy present)
            │   │   └─→ CancellationToken enforcement
            │   │
            │   ├─→ Bulkhead Check (if bulkhead policy present)
            │   │   └─→ Slot acquisition
            │   │
            │   ├─→ Execute User Operation
            │   │
            │   ├─→ Bulkhead Release (if applicable)
            │   │
            │   └─→ Check retry condition
            │       └─→ If needed: calculate backoff, wait, retry
            │
            ├─→ Fallback Execution (if primary failed & fallback present)
            │   └─→ Execute fallback operation
            │
            ├─→ Record Execution
            │   └─→ History, metrics, events
            │
            └─→ Return PolicyResult<T>
```

## State Management

### Circuit Breaker State Machine

```
    ┌──────────┐
    │  CLOSED  │ ← normal operation
    │ (pass    │
    │ through) │
    └────┬─────┘
         │ failure threshold exceeded
         │
    ┌────▼─────┐
    │   OPEN   │ ← reject all requests
    │(fail     │
    │ fast)    │
    └────┬─────┘
         │ open duration elapsed
         │
    ┌────▼──────────┐
    │ HALF-OPEN     │ ← test recovery
    │ (limited      │
    │  pass through)│
    └────┬──────────┘
         │
         ├─ success threshold reached → CLOSED
         │
         └─ new failure → OPEN
```

### Bulkhead Slot State

```
    ┌────────────────────────────┐
    │   Available Slots (max=10)  │
    │  ┌─┐ ┌─┐ ┌─┐ ┌─┐ ┌─┐      │
    │  │ │ │ │ │ │ │ │ │ │      │
    │  └─┘ └─┘ └─┘ └─┘ └─┘      │
    └────────────────────────────┘
            ↓ (slot acquisition)
    ┌────────────────────────────┐
    │   Executing Operations      │
    │  [Op1][Op2][Op3][Op4][Op5] │
    └────────────────────────────┘
            ↓ (slot release)
    ┌────────────────────────────┐
    │   Queue (max length=50)     │
    │  [Op6→][Op7→][Op8→]...     │
    └────────────────────────────┘
```

## Thread Safety

### Synchronization Mechanisms

1. **Lock-Based Synchronization**
   - Circuit breaker state transitions
   - Execution history updates
   - Policy repositories

2. **Atomic Operations**
   - Metrics counters
   - Failure rate calculations

3. **Concurrent Collections**
   - Event subscriber lists
   - Execution history storage (when applicable)

4. **Semaphore-Based Resource Control**
   - Bulkhead slot management
   - Queue length enforcement

### Thread-Safe Components

- ✅ CircuitBreakerService
- ✅ RetryService
- ✅ TimeoutService
- ✅ BulkheadService
- ✅ FallbackService
- ✅ ResiliencyPipelineService
- ✅ MetricsAggregator
- ✅ PolicyRepository

## Performance Characteristics

### Time Complexity

| Operation | Complexity | Notes |
|-----------|-----------|-------|
| Circuit Breaker Check | O(1) | Direct state lookup |
| Retry Decision | O(1) | Based on attempt count |
| Timeout Check | O(1) | Elapsed time comparison |
| Bulkhead Acquisition | O(1) | Semaphore operation |
| Fallback Execution | O(1) | Direct call to fallback |

### Space Complexity

| Component | Space | Notes |
|-----------|-------|-------|
| Circuit Breaker | O(1) | Single state object |
| Retry Metadata | O(1) | Attempt count, last delay |
| Execution History | O(n) | Linear with execution count |
| Metrics | O(1) | Constant-size aggregates |

### Memory Footprint

- Per-policy: ~1-5 KB (depending on configuration)
- Per-execution: ~100 bytes (result + metadata)
- Execution history: 100-200 bytes per record

## Extension Points

### Custom Policies

Implement `ResiliencyPolicy` base class:

```csharp
public class CustomPolicy : ResiliencyPolicy
{
    public override Task<PolicyResult<T>> ExecuteAsync<T>(
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        // Custom implementation
    }
}
```

### Custom Repositories

Implement `IRepository<T>` interface:

```csharp
public class CustomRepository<T> : IRepository<T>
{
    public Task<T> GetAsync(string key) { }
    public Task SaveAsync(string key, T value) { }
    // ...
}
```

### Event Subscribers

Subscribe to pipeline events:

```csharp
eventPublisher.Subscribe((PolicyEvent @event) =>
{
    // Custom event handling
});
```

## Design Patterns Used

1. **Strategy Pattern**
   - Different retry backoff strategies
   - Policy implementations as strategies

2. **State Pattern**
   - Circuit breaker state management

3. **Builder Pattern**
   - Fluent pipeline configuration

4. **Observer Pattern**
   - Event publishing and subscription

5. **Repository Pattern**
   - Data access abstraction

6. **Dependency Injection**
   - Loose coupling through DI containers

7. **Decorator Pattern**
   - Policy composition and wrapping

## Best Practices

1. **Combine Policies Strategically**
   - Retry → Timeout → Circuit Breaker
   - Bulkhead for resource isolation
   - Fallback as last resort

2. **Configure Timeouts Appropriately**
   - Must be longer than retry backoff max
   - Shorter than overall SLA

3. **Set Reasonable Thresholds**
   - Circuit breaker failure threshold
   - Bulkhead max parallelization
   - Retry max attempts

4. **Monitor and Alert**
   - Subscribe to policy events
   - Track metrics regularly
   - Set up alerts on threshold violations

5. **Test Under Load**
   - Verify timeout enforcement
   - Test bulkhead saturation
   - Validate circuit breaker transitions

## Scalability Considerations

- Stateless service design enables horizontal scaling
- Minimal memory footprint per policy instance
- Lock-based synchronization suitable for moderate concurrency
- Consider distributed circuit breakers for multi-instance scenarios
