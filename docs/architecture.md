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

### Pipeline Execution (actual composition order)

`ResiliencyPipelineService.ExecuteAsync` accepts each policy as an optional
parameter. The nesting, from outermost to innermost, is:

```
User Code
    │
    └─→ ResiliencyPipelineService.ExecuteAsync(operation, ct, cb, retry, timeout, bulkhead, fallback)
            │
            ├─→ Circuit Breaker (if enabled)
            │   └─→ If Open and open-duration not elapsed: CircuitBreakerOpenException
            │
            ├─→ Bulkhead (if enabled)
            │   └─→ TryAcquireSlot; on rejection: BulkheadRejectedException (no queuing/waiting)
            │
            ├─→ Timeout (if enabled)
            │   └─→ Linked CancellationTokenSource with CancelAfter(policy.Timeout)
            │
            ├─→ Retry loop (if enabled)
            │   └─→ Backoff per RetryPolicy.Strategy (+ optional jitter)
            │
            ├─→ User Operation (receives the effective CancellationToken)
            │
            └─→ On any exception, if Fallback enabled:
                └─→ FallbackService.ExecuteAsync → PolicyResult<T> with FallbackUsed metadata
```

Notes grounded in the code (`ResiliencyPipelineService._executeWithRetryTimeoutBulkhead`):

- When both timeout and retry are enabled, the timeout wraps the whole retry
  loop - the timeout budget covers all attempts, not each attempt individually.
- The bulkhead is fail-fast: `TryAcquireSlot` either grants a slot or throws
  `BulkheadRejectedException`; there is no waiting queue despite
  `MaxQueueLength` being configurable on the policy.
- `ExecutionHistoryRepository`, `MetricsAggregator` and
  `ResiliencyEventPublisher` are NOT invoked automatically inside
  `ExecuteAsync`. They are standalone components the caller wires up (see
  `src/Program.cs`, which records `ExecutionRecord`s manually after each call).

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

`ResiliencyPolicy` is a data/statistics base class - it has no abstract
`ExecuteAsync`. A subclass carries configuration and counters; execution logic
lives in a service. To add a new policy type you therefore:

1. Derive from `ResiliencyPolicy` (gets `Id`, `Name`, `IsEnabled`,
   success/failure counters, `GetSnapshot()`, `ResetStatistics()`).
2. Write a corresponding service with an `ExecuteAsync` that interprets it
   (see `RetryService` / `TimeoutService` for the shape).
3. Register the policy via `ResiliencyPipelineService.RegisterPolicy` or the
   `AddPolicy<TPolicy>` DI extension so it shows up in statistics and snapshots.

```csharp
public class CustomPolicy : ResiliencyPolicy
{
    public CustomPolicy(string name) { Name = name; }
    // configuration properties; call RecordSuccess()/RecordFailure() from your service
}
```

### Custom Repositories

Implement the synchronous CRUD contract in `IRepository<T>`:

```csharp
public class CustomRepository<T> : IRepository<T> where T : class
{
    public void Create(T entity) { /* ... */ }
    public T? Read(string id) { /* ... */ }
    public bool Update(T entity) { /* ... */ }
    public bool Delete(string id) { /* ... */ }
    public List<T> GetAll() { /* ... */ }
}
```

### Event Subscribers

`ResiliencyEventPublisher.Subscribe` is keyed by event type name:

```csharp
publisher.Subscribe<ResiliencyEvent>("CircuitBreakerStateChanged", evt =>
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

## Known Limitations

- **Bulkhead has no real queue.** `BulkheadService.TryAcquireSlot` rejects
  immediately when `MaxParallelization` is reached; `MaxQueueLength` is
  tracked for statistics but callers are never parked waiting for a slot.
- **One policy per type per builder.** `ResiliencyPipelineBuilder` keeps a
  single field per policy kind; calling `WithRetry` twice registers both
  policies in the service but only the last one is returned by
  `GetRetryPolicy()`.
- **Policies are passed explicitly per call.** Registering a policy in the
  pipeline does not make `ExecuteAsync` use it automatically - the caller must
  fetch it (`GetPolicyByName`) and pass it as an argument (see `src/Program.cs`).
- **Observability is opt-in.** Execution history, metrics aggregation and
  events must be wired by the host application; the orchestrator only keeps
  its own success/failure counters.
- **All state is in-memory.** Circuit breaker state, execution history and
  policy repositories do not survive process restarts and are not shared
  across instances.
