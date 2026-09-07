# Resiliency events

The event model in `src/Events/ResiliencyEventPublisher.cs` represents observable outcomes and state changes from resiliency policies. All event types are in the `DotNetResiliencePipeline.Events` namespace and derive from `ResiliencyEvent`.

For publisher API details, see [ResiliencyEventPublisher](ResiliencyEventPublisher.md). For the higher-level observer, see [PipelineEventObserver](PipelineEventObserver.md).

## Base event

`ResiliencyEvent` is the abstract base class for every event described below.

| Property | Type | Default | Description |
| --- | --- | --- | --- |
| `Id` | `string` | A new GUID converted to a string | Identifies the event instance. |
| `Timestamp` | `DateTime` | `DateTime.UtcNow` | Records the event time. `PublishAsync` replaces this value with the current UTC time when publishing. |
| `SourcePolicy` | `string` | `string.Empty` | Identifies the policy that originated the event. |

All three properties have public getters and setters.

## Concrete events

Each concrete event inherits `Id`, `Timestamp`, and `SourcePolicy` from `ResiliencyEvent`. The tables list only the properties declared by that concrete type.

### `PolicyExecutedSuccessfullyEvent`

Raised when a policy executes successfully.

| Property | Type | Default |
| --- | --- | --- |
| `PolicyName` | `string` | `string.Empty` |
| `DurationMs` | `long` | `0` |
| `AttemptNumber` | `int` | `0` |

### `PolicyExecutionFailedEvent`

Raised when a policy execution fails.

| Property | Type | Default |
| --- | --- | --- |
| `PolicyName` | `string` | `string.Empty` |
| `ExceptionType` | `string` | `string.Empty` |
| `ExceptionMessage` | `string` | `string.Empty` |
| `DurationMs` | `long` | `0` |

### `CircuitBreakerStateChangedEvent`

Raised when a circuit breaker changes state.

| Property | Type | Default |
| --- | --- | --- |
| `PolicyName` | `string` | `string.Empty` |
| `PreviousState` | `string` | `string.Empty` |
| `NewState` | `string` | `string.Empty` |
| `ConsecutiveFailures` | `int` | `0` |

### `BulkheadRejectedEvent`

Raised when bulkhead capacity is exceeded.

| Property | Type | Default |
| --- | --- | --- |
| `PolicyName` | `string` | `string.Empty` |
| `ActiveExecutions` | `int` | `0` |
| `MaxCapacity` | `int` | `0` |
| `QueuedRequests` | `int` | `0` |

### `TimeoutOccurredEvent`

Raised when a timeout occurs.

| Property | Type | Default |
| --- | --- | --- |
| `PolicyName` | `string` | `string.Empty` |
| `TimeoutMs` | `long` | `0` |
| `ActualDurationMs` | `long` | `0` |

### `FallbackTriggeredEvent`

Raised when a fallback is triggered.

| Property | Type | Default |
| --- | --- | --- |
| `PolicyName` | `string` | `string.Empty` |
| `Reason` | `string` | `string.Empty` |
| `FallbackSucceeded` | `bool` | `false` |

### `PolicyHealthChangedEvent`

Raised when policy health changes.

| Property | Type | Default |
| --- | --- | --- |
| `PolicyName` | `string` | `string.Empty` |
| `PreviousHealth` | `string` | `string.Empty` |
| `NewHealth` | `string` | `string.Empty` |
| `SuccessRate` | `double` | `0` |

All properties declared by the concrete event types have public getters and setters.

## Subscribe and publish

`ResiliencyEventPublisher` stores subscriptions under a string key. `PublishAsync` looks up subscribers using the published event's runtime type name, so the subscription key should normally be the concrete type name. `nameof` keeps that key aligned with the type.

```csharp
using DotNetResiliencePipeline.Events;

var publisher = new ResiliencyEventPublisher();

Action<TimeoutOccurredEvent> onTimeout = timeout =>
{
    Console.WriteLine(
        $"{timeout.PolicyName} exceeded {timeout.TimeoutMs} ms " +
        $"after {timeout.ActualDurationMs} ms");
};

publisher.Subscribe(nameof(TimeoutOccurredEvent), onTimeout);

await publisher.PublishAsync(new TimeoutOccurredEvent
{
    SourcePolicy = "Timeout",
    PolicyName = "RemoteCallTimeout",
    TimeoutMs = 1_000,
    ActualDurationMs = 1_125
});

publisher.Unsubscribe(nameof(TimeoutOccurredEvent), onTimeout);
```

Publishing performs the following work:

1. Sets `Timestamp` to `DateTime.UtcNow`.
2. Increments the total counter and the counter associated with the concrete event type.
3. Adds the event to history, removing the oldest entry when `MaxHistorySize` is exceeded.
4. Invokes matching `Action<T>` subscribers.

Subscriber exceptions are caught and written to the console; they do not stop the remaining handlers. `PublishAsync` rejects a null event with `ArgumentNullException`.

Subscriptions can also be managed with `Unsubscribe`, `UnsubscribeAll`, and `ClearSubscribers`. Use `GetEventHistory` or `GetEvents<T>` to query recent published events and `GetStatistics` to read the event counters.

## Relationship to `PipelineEventObserver`

`PipelineEventObserver` is constructed with a `ResiliencyEventPublisher`. During construction it subscribes default handlers for all seven concrete event types, using their class names as publisher keys. Those handlers write event-specific status messages to the console.

`RegisterHandler<T>` adds a named handler to the observer and subscribes the supplied `Action<T>` to the underlying publisher. Consequently, an event sent through `publisher.PublishAsync(...)` is delivered to matching observer registrations and is reflected in the publisher statistics returned by `observer.GetStatistics()`.

The observer also exposes `RecordEvent(ResiliencyEvent)`, which queues an event on its internal channel for asynchronous event-specific processing. `RecordEvent` does not call `PublishAsync`; by itself it does not add the event to publisher history, update publisher statistics, or notify publisher subscribers. Use `PublishAsync` when those publisher behaviors are required.
