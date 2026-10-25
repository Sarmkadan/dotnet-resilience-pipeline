# CircuitStateExtensions

Static extension class that adds display formatting to `CircuitBreakerPolicy.CircuitState`. It contains one method, `ToDisplayName`, which turns a circuit state into a label suitable for logs, dashboards, and CLI output.

The extension only reads the state. It never changes it. The transitions that move a circuit between states are described in [State Transitions](#state-transitions), because they are performed by `CircuitBreakerPolicy` and `CircuitBreakerService`, not by this class.

## Namespace and Type

- **Namespace**: `DotNetResiliencePipeline.Domain.Policies`
- **Declaration**: `public static class CircuitStateExtensions`
- **Extended type**: `CircuitBreakerPolicy.CircuitState` (a nested enum with members `Closed`, `Open`, `HalfOpen`)

The file is `src/Domain/Policies/CircuitStateExtensions.cs`. It has no dependencies beyond the enum.

## API

### `ToDisplayName`

```csharp
public static string ToDisplayName(this CircuitState state)
```

- **Purpose**: Converts a circuit state to a user-friendly label.
- **Parameters**:
  - `state`: The circuit state to convert.
- **Returns**: `string`. One of `"Closed"`, `"Open"`, or `"Half-Open"` for the defined states. For any other value, the result of `state.ToString()`.
- **Remarks**:
  - The label for `HalfOpen` contains a hyphen (`Half-Open`). The enum name does not (`HalfOpen`). Use `ToDisplayName()` when the text is shown to people, and `ToString()` or `nameof` when the text is a key or identifier.
  - Unknown values do not throw. They are cast from integers or added in a later version. The method returns the numeric text, for example `"42"`.
  - The method is pure. It has no side effects and is thread-safe.

#### Mapping

| `CircuitState` value | Integer value | `ToDisplayName()` result | `ToString()` result |
|----------------------|---------------|--------------------------|---------------------|
| `Closed`             | 0             | `Closed`                 | `Closed`            |
| `Open`               | 1             | `Open`                   | `Open`              |
| `HalfOpen`           | 2             | `Half-Open`              | `HalfOpen`          |
| any other value      | n/a           | `ToString()` of the value | `ToString()` of the value |

The integer values follow declaration order, which is the default for an enum without explicit values.

## State Transitions

`ToDisplayName` does not appear in this table because it does not change state. The table shows the transitions that produce the three states, so you can tell what label a circuit will report next. Each transition is taken by `CircuitBreakerPolicy` (state logic) or `CircuitBreakerService` (execution flow).

| From | To | Trigger | Condition | Side effects |
|------|----|---------|-----------|--------------|
| `Closed` | `Open` | `RecordFailure()` | `ConsecutiveFailures >= FailureThreshold` | `CircuitBreakerTrips` is incremented, `OpenedAt` is set in metadata |
| `Closed` | `Closed` | `RecordSuccess()` | none | `ConsecutiveFailures` is reset to `0` |
| `Open` | `HalfOpen` | `AttemptReset()`, called by `CircuitBreakerService.ExecuteAsync` | `DateTime.UtcNow - last state change >= OpenDuration` | `SuccessfulInHalfOpen` is reset to `0` |
| `HalfOpen` | `Closed` | `RecordSuccess()` | `SuccessfulInHalfOpen >= SuccessThresholdInHalfOpen` | `ConsecutiveFailures` is reset to `0`, `ClosedAt` is set in metadata |
| `HalfOpen` | `Open` | `RecordFailure()` | any failure in `HalfOpen` | `CircuitBreakerTrips` is incremented, `OpenedAt` is set in metadata |
| `Open` or `HalfOpen` | `Closed` | `ManualReset()`, via `CircuitBreakerService.ResetCircuit` | none | Statistics are cleared |
| `Open` | `Open` | `RecordFailure()` | none | Calls are rejected; the failure is counted only if `Closed` or `HalfOpen` |

Notes on the table:

- `Open` rejects calls with `CircuitBreakerOpenException` until `OpenDuration` has passed. After that, the first caller to reach `ExecuteAsync` claims the `Open` → `HalfOpen` transition. Other callers in the same window are rejected.
- In `HalfOpen`, the service limits the number of concurrent probe calls. Calls over the limit are rejected without changing the state.
- `CircuitBreakerService.OpenCircuit(policy)` calls `RecordFailure()` once. It does not force the state to `Open`. The circuit opens only if that one failure reaches `FailureThreshold` (for example, when `FailureThreshold` is `1`).
- `CircuitBreakerService.GetCircuitState(policy)` returns `CurrentState.ToString()`, which is the enum name (`HalfOpen`), not the display label. Use `ToDisplayName()` if you need `Half-Open`.

## Usage

### Displaying the state of a policy

```csharp
using DotNetResiliencePipeline.Domain.Policies;

var policy = new CircuitBreakerPolicy("payments-api");

Console.WriteLine(policy.CurrentState.ToDisplayName());   // Closed
```

### Following the state through transitions

The example below uses `RecordFailure` and `RecordSuccess` directly, so it runs without a real operation or timer. The `FailureThreshold` and `SuccessThresholdInHalfOpen` values are set for the example. `OpenDuration` is not changed, so the policy stays `Open` until `AttemptReset` is called after that interval.

```csharp
using DotNetResiliencePipeline.Domain.Policies;

var policy = new CircuitBreakerPolicy("payments-api")
{
    FailureThreshold = 3,
    OpenDuration = TimeSpan.FromSeconds(30),
    SuccessThresholdInHalfOpen = 2
};

Console.WriteLine(policy.CurrentState.ToDisplayName());   // Closed

policy.RecordFailure();
policy.RecordFailure();
policy.RecordFailure();                                    // threshold reached
Console.WriteLine(policy.CurrentState.ToDisplayName());   // Open

policy.AttemptReset();                                     // no-op until OpenDuration has passed
// After OpenDuration has elapsed, AttemptReset() moves the circuit to HalfOpen.
```

### Formatting a log message

```csharp
using DotNetResiliencePipeline.Domain.Policies;

static void LogState(CircuitBreakerPolicy policy)
{
    var state = policy.CurrentState;
    Console.WriteLine(
        $"{policy.Name}: {state.ToDisplayName()} " +
        $"(failures {policy.ConsecutiveFailures}/{policy.FailureThreshold}, " +
        $"trips {policy.CircuitBreakerTrips})");
}
```

### Using the label as a dashboard value

```csharp
using DotNetResiliencePipeline.Domain.Policies;

var rows = policies.Select(p => new
{
    p.Name,
    State = p.CurrentState.ToDisplayName(),
    p.ConsecutiveFailures
});
```

### Keep identifiers on `ToString()`

Use the enum name for keys and comparisons, and the display name only for text shown to people:

```csharp
var key = policy.CurrentState.ToString();               // "HalfOpen" - stable identifier
var label = policy.CurrentState.ToDisplayName();        // "Half-Open" - for display
```

## Behavior Notes

- **Stability**: The label strings are part of the public output. A change to any of them is a breaking change for any code that matches on them, such as log parsers or dashboards.
- **Unknown values**: The fallback returns `ToString()` instead of throwing, so new enum members do not break formatting. Add a `case` to `ToDisplayName` when a new member is added.
- **Thread safety**: The method reads only its argument, so it can be called from any thread. The `CurrentState` property it is usually given is read without a lock, so a value read at one moment can be stale by the time it is formatted.

## Related Types

- [`CircuitBreakerPolicy`](CircuitBreakerPolicy.md): Defines the `CircuitState` enum and the state transitions.
- [`CircuitBreakerService`](CircuitBreakerService.md): Executes operations through the circuit and performs the `Open` → `HalfOpen` claim.
- `PolicyTypeExtensions.ToDisplayName` (`src/Constants/PolicyTypeExtensions.cs`): The same pattern for `PolicyType`. See [PolicyTypeExtensions](PolicyTypeExtensions.md).
