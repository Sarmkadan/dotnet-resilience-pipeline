# PipelineEventObserverExtensions

Provides diagnostic and management utilities for inspecting, formatting, and controlling event handlers registered on resilience pipeline event observers. This static extension class enables runtime introspection of handler counts, active/inactive state, and event-type grouping without requiring direct access to the underlying observer internals.

## API

### GetActiveHandlersCount

```csharp
public static int GetActiveHandlersCount(this PipelineEventObserver observer)
```

Returns the total number of event handlers currently in an active state across all event types on the specified observer. An active handler is one that will be invoked when its associated event is raised.

**Parameters:**
- `observer` — The `PipelineEventObserver` instance to inspect. Must not be null.

**Return Value:** A non-negative integer representing the count of active handlers.

**Exceptions:**
- `ArgumentNullException` — Thrown when `observer` is null.

---

### GetInactiveHandlersCount

```csharp
public static int GetInactiveHandlersCount(this PipelineEventObserver observer)
```

Returns the total number of event handlers currently in an inactive (toggled-off) state across all event types. Inactive handlers remain registered but are skipped during event invocation.

**Parameters:**
- `observer` — The `PipelineEventObserver` instance to inspect. Must not be null.

**Return Value:** A non-negative integer representing the count of inactive handlers.

**Exceptions:**
- `ArgumentNullException` — Thrown when `observer` is null.

---

### FindHandler

```csharp
public static EventHandler? FindHandler(this PipelineEventObserver observer, string handlerId)
```

Locates a specific event handler by its unique identifier. Returns null if no handler with the given ID is registered on the observer.

**Parameters:**
- `observer` — The `PipelineEventObserver` instance to search. Must not be null.
- `handlerId` — The case-sensitive unique identifier of the handler to locate. Must not be null or empty.

**Return Value:** The matching `EventHandler` instance, or null if not found.

**Exceptions:**
- `ArgumentNullException` — Thrown when `observer` or `handlerId` is null.
- `ArgumentException` — Thrown when `handlerId` is an empty string.

---

### GetStatisticsFormatted

```csharp
public static string GetStatisticsFormatted(this PipelineEventObserver observer)
```

Produces a human-readable, formatted string containing summary statistics of the observer's handler state, including total handlers, active/inactive breakdown, and per-event-type counts.

**Parameters:**
- `observer` — The `PipelineEventObserver` instance to inspect. Must not be null.

**Return Value:** A formatted multi-line string with handler statistics.

**Exceptions:**
- `ArgumentNullException` — Thrown when `observer` is null.

---

### HasActiveHandlers

```csharp
public static bool HasActiveHandlers(this PipelineEventObserver observer)
```

Determines whether the observer has at least one handler in the active state. Useful as a quick check before performing operations that depend on handler presence.

**Parameters:**
- `observer` — The `PipelineEventObserver` instance to inspect. Must not be null.

**Return Value:** `true` if one or more active handlers exist; otherwise `false`.

**Exceptions:**
- `ArgumentNullException` — Thrown when `observer` is null.

---

### GetHandlersByEventType

```csharp
public static List<EventHandler> GetHandlersByEventType(this PipelineEventObserver observer, string eventType)
```

Retrieves all handlers registered for a specific event type, regardless of their active/inactive state. The returned list is a snapshot copy; modifications to it do not affect the observer.

**Parameters:**
- `observer` — The `PipelineEventObserver` instance to query. Must not be null.
- `eventType` — The case-sensitive event type name to filter by. Must not be null or empty.

**Return Value:** A `List<EventHandler>` containing all handlers for the specified event type. Returns an empty list if no handlers are registered for that type.

**Exceptions:**
- `ArgumentNullException` — Thrown when `observer` or `eventType` is null.
- `ArgumentException` — Thrown when `eventType` is an empty string.

---

### ToggleHandlerActive

```csharp
public static bool ToggleHandlerActive(this PipelineEventObserver observer, string handlerId)
```

Toggles the active state of a specific handler identified by its unique ID. If the handler is currently active, it becomes inactive; if inactive, it becomes active. Returns the new active state after the toggle operation.

**Parameters:**
- `observer` — The `PipelineEventObserver` instance containing the handler. Must not be null.
- `handlerId` — The case-sensitive unique identifier of the handler to toggle. Must not be null or empty.

**Return Value:** `true` if the handler is active after the toggle; `false` if inactive.

**Exceptions:**
- `ArgumentNullException` — Thrown when `observer` or `handlerId` is null.
- `ArgumentException` — Thrown when `handlerId` is an empty string.
- `InvalidOperationException` — Thrown when no handler with the specified `handlerId` exists on the observer.

---

### GetHandlersSummary

```csharp
public static string GetHandlersSummary(this PipelineEventObserver observer)
```

Returns a concise single-line or short-formatted summary of all registered handlers, including their IDs, associated event types, and active/inactive status. Designed for logging and quick diagnostic output.

**Parameters:**
- `observer` — The `PipelineEventObserver` instance to summarize. Must not be null.

**Return Value:** A string containing a compact summary of all handlers.

**Exceptions:**
- `ArgumentNullException` — Thrown when `observer` is null.

---

## Usage

### Example 1: Diagnostic Logging of Handler State

```csharp
PipelineEventObserver observer = pipeline.GetObserver();

// Log overall statistics before executing the pipeline
Console.WriteLine(observer.GetStatisticsFormatted());

if (!observer.HasActiveHandlers())
{
    Console.WriteLine("Warning: No active handlers configured.");
}

// Execute pipeline steps
await pipeline.ExecuteAsync();

// Check post-execution state
int activeCount = observer.GetActiveHandlersCount();
int inactiveCount = observer.GetInactiveHandlersCount();
Console.WriteLine($"Post-execution — Active: {activeCount}, Inactive: {inactiveCount}");
```

### Example 2: Targeted Handler Management During Testing

```csharp
PipelineEventObserver observer = pipeline.GetObserver();

// Disable a specific logging handler to reduce noise during stress test
string loggingHandlerId = "DetailedRequestLogger";
EventHandler? handler = observer.FindHandler(loggingHandlerId);

if (handler != null)
{
    bool wasActive = observer.ToggleHandlerActive(loggingHandlerId);
    Console.WriteLine($"Handler '{loggingHandlerId}' toggled. Now active: {wasActive}");
}

// Inspect handlers for a specific event type
List<EventHandler> retryHandlers = observer.GetHandlersByEventType("OnRetry");
Console.WriteLine(observer.GetHandlersSummary());

// Run stress test
await pipeline.ExecuteAsync();

// Re-enable the logging handler
observer.ToggleHandlerActive(loggingHandlerId);
```

---

## Notes

- **Null Handling:** All methods throw `ArgumentNullException` when the `observer` argument is null. Methods accepting string parameters additionally validate for null and empty strings, throwing `ArgumentNullException` or `ArgumentException` respectively.
- **Handler Identity:** Handler IDs are case-sensitive. Passing an ID with incorrect casing to `FindHandler` or `ToggleHandlerActive` will result in a null return or `InvalidOperationException`.
- **Snapshot Semantics:** `GetHandlersByEventType` returns a new `List<EventHandler>` that is a snapshot of the current state. Adding or removing items from the returned list does not modify the observer's handler registrations.
- **Thread Safety:** These methods are designed for diagnostic and management scenarios. Concurrent modifications to the observer's handler collection (e.g., adding or removing handlers on another thread) while calling these methods may produce inconsistent or stale results. External synchronization is recommended if the observer is mutated concurrently with introspection calls.
- **Toggle Behavior:** `ToggleHandlerActive` is an atomic flip of the active flag for a single handler. It does not affect other handlers, even those registered for the same event type. The return value reflects the state *after* the toggle completes.
- **Format Stability:** The output format of `GetStatisticsFormatted` and `GetHandlersSummary` is intended for human consumption and may change across versions. Do not parse these strings programmatically.
