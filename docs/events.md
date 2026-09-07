# Event Model

The event model in `DotNetResiliencePipeline` provides a centralized way to track, monitor, and react to resilience policy executions and state changes. It consists of an abstract base event, several concrete event types representing specific policy behaviors, and a publisher mechanism for distributing these events.

## Base Event

### `ResiliencyEvent`
The abstract base class for all resilience events.
- `Id` (`string`): Unique identifier for the event.
- `Timestamp` (`DateTime`): UTC timestamp when the event was published.
- `SourcePolicy` (`string`): Name of the resilience policy that generated the event.

## Concrete Events

### `PolicyExecutedSuccessfullyEvent`
Raised when a policy executes successfully.
- `PolicyName` (`string`): Name of the policy.
- `DurationMs` (`long`): Duration of the operation in milliseconds.
- `AttemptNumber` (`int`): The attempt number for retryable operations.

### `PolicyExecutionFailedEvent`
Raised when a policy execution fails.
- `PolicyName` (`string`): Name of the policy.
- `ExceptionType` (`string`): Type name of the exception that triggered the event.
- `ExceptionMessage` (`string`): Message of the exception that triggered the event.
- `DurationMs` (`long`): Duration of the operation in milliseconds.

### `CircuitBreakerStateChangedEvent`
Raised when a circuit breaker state changes.
- `PolicyName` (`string`): Name of the policy.
- `PreviousState` (`string`): Previous state of the circuit breaker.
- `NewState` (`string`): New state of the circuit breaker.
- `ConsecutiveFailures` (`int`): Number of consecutive failures recorded.

### `BulkheadRejectedEvent`
Raised when bulkhead capacity is exceeded.
- `PolicyName` (`string`): Name of the policy.
- `ActiveExecutions` (`int`): Current number of active executions.
- `MaxCapacity` (`int`): Maximum allowed concurrent executions.
- `QueuedRequests` (`int`): Number of requests currently queued.

### `TimeoutOccurredEvent`
Raised when a timeout occurs.
- `PolicyName` (`string`): Name of the policy.
- `TimeoutMs` (`long`): Configured timeout duration in milliseconds.
- `ActualDurationMs` (`long`): Actual duration before the timeout triggered.

### `FallbackTriggeredEvent`
Raised when a fallback is triggered.
- `PolicyName` (`string`): Name of the policy.
- `Reason` (`string`): Reason why the fallback was triggered.
- `FallbackSucceeded` (`bool`): Indicates whether the fallback execution succeeded.

### `PolicyHealthChangedEvent`
Raised when policy health changes.
- `PolicyName` (`string`): Name of the policy.
- `PreviousHealth` (`string`): Previous health status.
- `NewHealth` (`string`): New health status.
- `SuccessRate` (`double`): Current success rate of the policy.

## Publishing and Subscribing

Events are managed and distributed via the `ResiliencyEventPublisher` class.

### Subscribing
Use `Subscribe<T>` to register a handler for a specific event type:
```csharp
publisher.Subscribe<PolicyExecutedSuccessfullyEvent>("policy.success", e => {
    Console.WriteLine($"Policy {e.PolicyName} succeeded in {e.DurationMs}ms");
});
```

### Publishing
Use `PublishAsync<T>` to emit an event. The publisher automatically records it in the history and notifies all registered subscribers:
```csharp
var successEvent = new PolicyExecutedSuccessfullyEvent { PolicyName = "RetryPolicy", DurationMs = 150, AttemptNumber = 1 };
await publisher.PublishAsync(successEvent);
```

### Querying History
- `GetEventHistory(int limit)`: Retrieves the most recent events.
- `GetEvents<T>(int limit)`: Retrieves recent events of a specific type.
- `GetSubscriberCount(string eventType)`: Returns the number of active subscribers for an event type.
- `GetStatistics()`: Returns aggregated counters for total events, successes, failures, circuit breaker changes, bulkhead rejections, timeouts, fallbacks, and health changes.
- `ClearHistory()` / `ClearSubscribers()`: Resets history or removes all subscribers.

For a complete API reference, see [ResiliencyEventPublisher.md](ResiliencyEventPublisher.md).

## Relationship to PipelineEventObserver

While `ResiliencyEventPublisher` focuses on a pub-sub model for distributing events to multiple listeners, `PipelineEventObserver` provides a centralized observer mechanism for tracking and managing events emitted by resilience pipelines. It allows components to register typed handlers, query execution statistics, and dynamically enable or disable observers without affecting the underlying pipeline behavior.

Both systems can be used independently or together depending on whether you need a broadcast pub-sub model (`ResiliencyEventPublisher`) or a dedicated observer with built-in statistics and handler lifecycle management (`PipelineEventObserver`).

For more details on the observer pattern implementation, see [PipelineEventObserver.md](PipelineEventObserver.md).
