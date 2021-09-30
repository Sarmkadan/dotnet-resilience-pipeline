# PipelineEventObserver

The `PipelineEventObserver` class provides a centralized mechanism for observing and managing events emitted by resilience pipelines. It allows components to register typed handlers, query execution statistics, and dynamically enable or disable observers without affecting the underlying pipeline behavior.

## API

### Constructor
```csharp
public PipelineEventObserver()
```
Creates a new observer instance with a unique identifier, the current UTC time as `CreatedAt`, and default values for all counters. The observer starts in an active state (`IsActive = true`).

### RegisterHandler<T>
```csharp
public void RegisterHandler<T>(EventHandler<T> handler)
```
Registers a handler delegate for events of type `T`.

- **Parameters**
  - `handler`: The method to invoke when an event of type `T` is observed. Must not be `null`.
- **Return Value**: None.
- **Exceptions**
  - `ArgumentNullException` if `handler` is `null`.
- **Remarks**: The observer maintains an internal list of handlers per event type. Multiple handlers can be registered for the same `T`; they are invoked in the order of registration.

### UnregisterHandler
```csharp
public bool UnregisterHandler(EventHandler handler)
```
Attempts to remove a previously registered handler.

- **Parameters**
  - `handler`: The handler instance to remove. Must not be `null`.
- **Return Value**: `true` if the handler was found and removed; otherwise `false`.
- **Exceptions**
  - `ArgumentNullException` if `handler` is `null`.

### GetHandlers
```csharp
public List<EventHandler> GetHandlers()
```
Returns a snapshot of all currently registered handlers, regardless of their event type.

- **Parameters**: None.
- **Return Value**: A new `List<EventHandler>` containing copies of the registered handler delegates. Modifying the returned list does not affect the observer’s internal state.
- **Exceptions**: None.

### SetHandlerActive
```csharp
public bool SetHandlerActive(EventHandler handler, bool isActive)
```
Enables or disables a specific handler without removing it from the observer.

- **Parameters**
  - `handler`: The handler to modify. Must not be `null`.
  - `isActive`: Desired active state (`true` to enable, `false` to disable).
- **Return Value**: `true` if the handler was found and its active state updated; `false` if the handler is not registered.
- **Exceptions**
  - `ArgumentNullException` if `handler` is `null`.

### GetStatistics
```csharp
public EventStatistics GetStatistics()
```
Retrieves cumulative statistics for all events observed since the observer’s creation or the last reset.

- **Parameters**: None.
- **Return Value**: An `EventStatistics` instance containing the current counts.
- **Exceptions**: None.

### Properties
| Property | Type | Description |
|----------|------|-------------|
| `Id` | `string` | Immutable identifier assigned at construction (typically a GUID string). |
| `EventType` | `string` | The concrete event type this observer is associated with (set at construction and never changes). |
| `CreatedAt` | `DateTime` | UTC timestamp indicating when the observer instance was created. |
| `IsActive` | `bool` | Global active state of the observer. When `false`, all registered handlers are ignored regardless of their individual active state. |
| `TotalEventsEmitted` | `int` | Total number of events that have been raised through this observer. |
| `SuccessfulExecutions` | `int` | Count of handlers that executed without throwing an exception. |
| `FailedExecutions` | `int` | Count of handlers that threw an exception during execution. |
| `CircuitBreakerChanges` | `int` | Number of times the associated circuit breaker changed state (e.g., closed → open). |
| `BulkheadRejections` | `int` | Number of event executions rejected due to bulkhead limits. |
| `Timeouts` | `int` | Number of event executions that exceeded their configured timeout. |
| `FallbacksTriggered` | `int` | Number of times a fallback strategy was invoked for an event. |

All properties are read‑only after construction; their values are updated internally by the pipeline as events are processed.

## Usage

### Example 1: Registering a handler and observing events
```csharp
var observer = new PipelineEventObserver();

// Register a handler for latency‑warning events.
observer.RegisterHandler<LatencyWarningEvent>(e =>
{
    Console.WriteLine($"High latency detected: {e.LatencyMs} ms on {e.Operation}");
});

// Simulate pipeline raising an event.
observer.OnEvent(new LatencyWarningEvent { LatencyMs = 1200, Operation = "GetUser" });

// Retrieve statistics.
var stats = observer.GetStatistics();
Console.WriteLine($"Total events: {stats.TotalEventsEmitted}");
```

### Example 2: Dynamically enabling/disabling a handler
```csharp
var observer = new PipelineEventObserver();
var handler = new EventHandler<ErrorEvent>(e => Log.Error(e.Exception));
observer.RegisterHandler(handler);

// Temporarily silence error logging while performing a bulk operation.
observer.SetHandlerActive(handler, false);
// … bulk operation …
observer.SetHandlerActive(handler, true);

// Later, remove the handler completely.
observer.UnregisterHandler(handler);
```

## Notes
- **Thread safety**: All public members of `PipelineEventObserver` are safe to call concurrently from multiple threads. Internal collections are protected by locks, and counters are updated using atomic operations.
- **Handler snapshot**: `GetHandlers` returns a copy of the internal handler list. Enumerating this list while handlers are being added or removed elsewhere will not throw, but the enumeration may not reflect the very latest state.
- **Active state precedence**: A handler will only be invoked if both the observer’s `IsActive` property and the handler’s individual active state (set via `SetHandlerActive`) are `true`.
- **Exception handling**: If a registered handler throws an exception, the observer increments `FailedExecutions` but continues to invoke remaining handlers for the same event. The exception is not propagated outward.
- **Immutable identifiers**: Once created, the `Id` and `EventType` properties cannot be altered; attempting to assign to them will result in a compile‑time error.
- **Statistics reset**: The observer does not expose a method to reset its counters; a new instance must be created to start with zeroed statistics.
