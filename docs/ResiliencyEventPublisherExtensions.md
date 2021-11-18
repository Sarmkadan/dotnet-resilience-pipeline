# ResiliencyEventPublisherExtensions

`ResiliencyEventPublisherExtensions` provides static convenience methods for publishing typed resilience events with optional history tracking, retrieving the last published event of a given type, inspecting subscriber counts, and resetting internal state. It is designed to simplify event-driven observability around resilience pipeline execution without requiring direct interaction with the underlying publisher infrastructure.

## API

### PublishWithHistoryAsync

```csharp
public static async Task PublishWithHistoryAsync<T>(T event, ResilienceContext? context = null)
```

Publishes an event of type `T` and records it as the most recent event for that type, enabling later retrieval via `GetLastEvent<T>`.

- **Parameters**:
  - `event` (`T`): The event instance to publish. Must not be null.
  - `context` (`ResilienceContext?`): Optional resilience context associated with the event. Defaults to `null`.
- **Returns**: A `Task` representing the asynchronous publish operation.
- **Throws**: `ArgumentNullException` if `event` is `null`. May propagate exceptions from underlying subscribers.

### GetLastEvent\<T\>

```csharp
public static T? GetLastEvent<T>()
```

Retrieves the last event of type `T` that was published through `PublishWithHistoryAsync`. Returns the default value for `T` if no event of that type has been published since the last reset.

- **Type Parameters**: `T` — the event type to query.
- **Returns**: The most recently published event of type `T`, or `default(T)` if none exists.
- **Throws**: No documented exceptions.

### PublishExceptionAsync

```csharp
public static async Task PublishExceptionAsync(Exception exception, ResilienceContext? context = null)
```

Publishes an exception event, typically used to signal failures encountered during resilience pipeline execution.

- **Parameters**:
  - `exception` (`Exception`): The exception to publish. Must not be null.
  - `context` (`ResilienceContext?`): Optional resilience context. Defaults to `null`.
- **Returns**: A `Task` representing the asynchronous publish operation.
- **Throws**: `ArgumentNullException` if `exception` is `null`. May propagate exceptions from subscribers.

### GetSubscriberCount

```csharp
public static int GetSubscriberCount()
```

Returns the total number of subscribers currently registered across all event types.

- **Returns**: The aggregate subscriber count as an `int`.
- **Throws**: No documented exceptions.

### GetSubscriberCount\<T\>

```csharp
public static int GetSubscriberCount<T>()
```

Returns the number of subscribers registered specifically for events of type `T`.

- **Type Parameters**: `T` — the event type to query.
- **Returns**: The subscriber count for type `T` as an `int`.
- **Throws**: No documented exceptions.

### Reset

```csharp
public static void Reset()
```

Clears all stored last-event history and resets internal tracking state. Subscriber registrations are not affected.

- **Throws**: No documented exceptions.

## Usage

### Example 1: Publishing and retrieving the last resilience event

```csharp
// Define a custom resilience event
public record ExecutionAttemptEvent(int AttemptNumber, TimeSpan Duration);

// Publish an event with history tracking
var evt = new ExecutionAttemptEvent(1, TimeSpan.FromMilliseconds(120));
await ResiliencyEventPublisherExtensions.PublishWithHistoryAsync(evt);

// Later, retrieve the last published event of that type
ExecutionAttemptEvent? last = ResiliencyEventPublisherExtensions.GetLastEvent<ExecutionAttemptEvent>();
Console.WriteLine($"Last attempt: {last?.AttemptNumber}, Duration: {last?.Duration}");
```

### Example 2: Publishing an exception and inspecting subscriber counts

```csharp
try
{
    // Simulated operation that may fail
    throw new InvalidOperationException("Transient failure");
}
catch (Exception ex)
{
    // Publish the exception through the resilience event system
    await ResiliencyEventPublisherExtensions.PublishExceptionAsync(ex);
}

// Check how many subscribers are listening for exception events
int exceptionSubscribers = ResiliencyEventPublisherExtensions.GetSubscriberCount<Exception>();
int totalSubscribers = ResiliencyEventPublisherExtensions.GetSubscriberCount();

Console.WriteLine($"Exception subscribers: {exceptionSubscribers}, Total: {totalSubscribers}");
```

## Notes

- **History scope**: `GetLastEvent<T>` returns only the single most recent event per type. Publishing a new event of the same type overwrites the previous value. The history is stored in static state and is not bounded by time or count beyond the single-entry-per-type limit.
- **Reset behavior**: Calling `Reset` clears all tracked last-event entries but does not unsubscribe any listeners. Subscriber counts remain unchanged after a reset.
- **Thread safety**: The static methods operate on shared state. Concurrent calls to `PublishWithHistoryAsync` for the same type may race; the last event recorded is the one whose write completes last. `GetSubscriberCount` and `GetSubscriberCount<T>` reflect a point-in-time snapshot and may change immediately after the call returns.
- **Null handling**: `PublishWithHistoryAsync` and `PublishExceptionAsync` enforce non-null arguments for their primary payloads. `GetLastEvent<T>` returns `null` for reference types when no event has been published, which is indistinguishable from a deliberately published `null` value — publishing `null` is prohibited, so this ambiguity does not arise in practice.
- **Asynchronous completion**: `PublishWithHistoryAsync` and `PublishExceptionAsync` return `Task` and should be awaited to ensure subscribers have processed the event before proceeding. Fire-and-forget usage may lead to unobserved exceptions from subscribers.
