# CircuitBreakerPolicy

A fault-handling policy that temporarily blocks executions when a specified number of consecutive failures (`FailureThreshold`) occur within a defined period. Once the threshold is exceeded, the circuit "opens" for a fixed duration (`OpenDuration`), during which all attempts are rejected. After the open period elapses, the circuit enters a "half-open" state where a single attempt is allowed to test recovery. If successful, the circuit closes; otherwise, it reopens.

## API

### `FailureThreshold`
- **Type**: `int`
- **Description**: Gets the number of consecutive failures required to open the circuit. Must be a positive integer.
- **Default**: `5`
- **Remarks**: Throws `ArgumentOutOfRangeException` if set to a value ≤ `0`.

### `OpenDuration`
- **Type**: `TimeSpan`
- **Description**: Gets the duration for which the circuit remains open after tripping. Must be a positive time span.
- **Default**: `TimeSpan.FromSeconds(30)`
- **Remarks**: Throws `ArgumentOutOfRangeException` if set to a value ≤ `TimeSpan.Zero`.

### `CircuitBreakerPolicy(string name) : base`
- **Parameters**:
  - `name` (string): A human-readable identifier for the policy instance.
- **Description**: Initializes a new instance of the `CircuitBreakerPolicy` class.
- **Throws**: `ArgumentNullException` if `name` is `null`.

### `override void RecordSuccess()`
- **Description**: Records a successful execution, resetting the failure count and transitioning the circuit to a closed state if it was in a half-open or open state.
- **Remarks**: Thread-safe. No return value or exceptions.

### `override void RecordFailure()`
- **Description**: Records a failed execution, incrementing the failure count. If the count reaches `FailureThreshold`, the circuit opens for `OpenDuration`.
- **Remarks**: Thread-safe. No return value or exceptions.

### `void AttemptReset()`
- **Description**: Forces the circuit into a half-open state, allowing a single attempt to test recovery. Useful for manual recovery testing.
- **Remarks**: Thread-safe. No return value or exceptions.

### `void ManualReset()`
- **Description**: Forces the circuit into a closed state, resetting all internal counters and state. Use with caution.
- **Remarks**: Thread-safe. No return value or exceptions.

### `override PolicySnapshot GetSnapshot()`
- **Returns**: A `PolicySnapshot` object representing the current state of the circuit breaker.
- **Description**: Captures the current state, including whether the circuit is open, half-open, or closed, the current failure count, and the timestamp of the last state change.
- **Remarks**: Thread-safe. The returned snapshot is immutable.

## Usage

### Example 1: Basic Usage with HttpClient
