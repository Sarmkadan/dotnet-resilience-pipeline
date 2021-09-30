# ResiliencyEventPublisher

The `ResiliencyEventPublisher` is a utility class designed to collect, manage, and distribute resiliency-related events within a .NET application. It acts as a centralized event bus for resilience policies (e.g., retries, timeouts, circuit breakers), allowing subscribers to listen for specific events and enabling historical analysis of policy executions. This class is particularly useful for monitoring, debugging, and auditing resilience behavior in distributed systems.

## API

### `public int MaxHistorySize`
Gets or sets the maximum number of events retained in the history. When the limit is exceeded, the oldest events are discarded.
- **Purpose**: Controls the size of the event history buffer to prevent unbounded memory growth.
- **Default**: Typically initialized to a reasonable value (e.g., 100), but this may vary.
- **Thread Safety**: Thread-safe for reads and writes. Concurrent modifications are synchronized internally.

---

### `public void Subscribe<T>()`
Subscribes to events of type `T`, where `T` is a subtype of `ResiliencyEvent`.
- **Parameters**: None.
- **Return Value**: None.
- **Purpose**: Registers a subscriber for events of the specified type. Multiple subscriptions to the same type are allowed.
- **Thread Safety**: Thread-safe. Concurrent subscriptions are handled without race conditions.
- **Throws**: `ArgumentException` if `T` is not a subtype of `ResiliencyEvent`.

---

### `public bool Unsubscribe<T>()`
Unsubscribes from events of type `T`.
- **Parameters**: None.
- **Return Value**: `true` if a subscriber was removed; `false` if no subscribers existed for the type.
- **Purpose**: Removes a subscription for events of the specified type. If multiple subscribers exist, only one is removed per call.
- **Thread Safety**: Thread-safe. Concurrent unsubscriptions are handled without race conditions.

---

### `public async Task PublishAsync<T>(T @event)`
Publishes an event of type `T` to all subscribers and stores it in the event history.
- **Parameters**:
  - `@event` (`T`): The event to publish. Must be a subtype of `ResiliencyEvent`.
- **Return Value**: A `Task` representing the asynchronous operation.
- **Purpose**: Notifies all subscribers of the event and adds it to the history. If `MaxHistorySize` is exceeded, the oldest event is removed.
- **Thread Safety**: Thread-safe. Concurrent publications are serialized.
- **Throws**: `ArgumentNullException` if `@event` is `null`.

---

### `public List<ResiliencyEvent> GetEventHistory()`
Retrieves the entire event history as a list of `ResiliencyEvent` objects.
- **Parameters**: None.
- **Return Value**: A `List<ResiliencyEvent>` containing all stored events, ordered from oldest to newest.
- **Purpose**: Provides access to the raw event history for analysis or debugging.
- **Thread Safety**: The returned list is a snapshot and is not affected by subsequent modifications to the history. Concurrent calls are thread-safe.

---

### `public List<T> GetEvents<T>()`
Retrieves all events of type `T` from the history.
- **Parameters**: None.
- **Return Value**: A `List<T>` containing all events of the specified type, ordered from oldest to newest.
- **Purpose**: Filters the event history to return only events of a specific type.
- **Thread Safety**: The returned list is a snapshot and is not affected by subsequent modifications. Concurrent calls are thread-safe.
- **Throws**: `ArgumentException` if `T` is not a subtype of `ResiliencyEvent`.

---

### `public int GetSubscriberCount()`
Returns the number of active subscribers.
- **Parameters**: None.
- **Return Value**: The total count of subscribers across all event types.
- **Purpose**: Useful for diagnostics or monitoring subscriber activity.
- **Thread Safety**: Thread-safe. Concurrent calls are handled without race conditions.

---

### `public void ClearHistory()`
Clears the entire event history.
- **Parameters**: None.
- **Return Value**: None.
- **Purpose**: Resets the event history, freeing memory. Subscribers remain unaffected.
- **Thread Safety**: Thread-safe. Concurrent calls are serialized.

---

### `public string Id`
Gets the unique identifier of the event.
- **Purpose**: Represents a distinct identifier for the event, typically a GUID or similar unique value.
- **Thread Safety**: Read-only after publication; thread-safe.

---

### `public DateTime Timestamp`
Gets the timestamp when the event was published.
- **Purpose**: Records the exact moment the event occurred, useful for chronological analysis.
- **Thread Safety**: Read-only after publication; thread-safe.

---

### `public string SourcePolicy`
Gets the name of the resilience policy that generated the event.
- **Purpose**: Identifies the originating policy (e.g., "RetryPolicy", "CircuitBreaker").
- **Thread Safety**: Read-only after publication; thread-safe.

---

### `public string PolicyName`
Gets the name of the resilience policy instance (if named).
- **Purpose**: Distinguishes between multiple instances of the same policy type (e.g., "PrimaryRetry", "FallbackRetry").
- **Thread Safety**: Read-only after publication; thread-safe.

---

### `public long DurationMs`
Gets the duration of the operation (e.g., retry delay, timeout period) in milliseconds.
- **Purpose**: Provides timing metrics for performance analysis.
- **Thread Safety**: Read-only after publication; thread-safe.

---

### `public int AttemptNumber`
Gets the attempt number for retryable operations.
- **Purpose**: Indicates the current retry attempt (e.g., 1 for the first attempt, 2 for the first retry).
- **Thread Safety**: Read-only after publication; thread-safe.

---

### `public string ExceptionType`
Gets the type name of the exception that triggered the event (if applicable).
- **Purpose**: Identifies the exception type (e.g., "System.TimeoutException").
- **Thread Safety**: Read-only after publication; thread-safe.

---

### `public string ExceptionMessage`
Gets the message of the exception that triggered the event (if applicable).
- **Purpose**: Provides human-readable details about the exception.
- **Thread Safety**: Read-only after publication; thread-safe.

---

### `public string PreviousState`
Gets the previous state of a stateful policy (e.g., circuit breaker state before transition).
- **Purpose**: Useful for tracking state transitions (e.g., "Closed" → "Open").
- **Thread Safety**: Read-only after publication; thread-safe.

## Usage

### Example 1: Monitoring Retry Events
