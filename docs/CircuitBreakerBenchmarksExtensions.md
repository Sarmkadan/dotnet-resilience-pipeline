# CircuitBreakerBenchmarksExtensions

Provides a set of static helper methods designed for use in performance benchmarks that exercise the circuit breaker implementation. These methods allow a test to drive the circuit breaker into specific states, provoke failures, and assert expected behavior without exposing internal fields.

## API

### WaitForState
```csharp
public static void WaitForState(
    this CircuitBreaker circuitBreaker,
    CircuitBreakerState expectedState,
    TimeSpan timeout)
```
**Purpose** – Blocks the calling thread until the circuit breaker’s current state matches `expectedState` or the specified `timeout` elapses.  
**Parameters**  
- `circuitBreaker`: The circuit breaker instance to observe.  
- `expectedState`: The state to wait for (`Closed`, `Open`, or `HalfOpen`).  
- `timeout`: Maximum time to wait; if exceeded the method throws.  
**Return value** – `void`.  
**Exceptions**  
- `ArgumentNullException` if `circuitBreaker` is `null`.  
- `ArgumentOutOfRangeException` if `timeout` is negative.  
- `TimeoutException` if the circuit breaker does not reach `expectedState` within `timeout`.  

### AssertStateTransition
```csharp
public static void AssertStateTransition(
    this CircuitBreaker circuitBreaker,
    CircuitBreakerState fromState,
    CircuitBreakerState toState)
```
**Purpose** – Verifies that the circuit breaker has transitioned from `fromState` to `toState` since the last observation. The method reads the current state and compares it against the expected transition; it does not modify the circuit breaker.  
**Parameters**  
- `circuitBreaker`: The circuit breaker instance to inspect.  
- `fromState`: The state the breaker was expected to be in prior to the transition.  
- `toState`: The state the breaker is expected to be in after the transition.  
**Return value** – `void`.  
**Exceptions**  
- `ArgumentNullException` if `circuitBreaker` is `null`.  
- `InvalidOperationException` if the circuit breaker’s current state does not equal `toState` or the internal transition tracker does not record a change from `fromState` to `toState`.  

### TriggerFailures
```csharp
public static void TriggerFailures(
    this CircuitBreaker circuitBreaker,
    int failureCount)
```
**Purpose** – Invokes the circuit breaker’s execution pipeline `failureCount` times with operations that are configured to throw exceptions, thereby incrementing the failure counter.  
**Parameters**  
- `circuitBreaker`: The circuit breaker instance to stress.  
- `failureCount`: Number of failure invocations to perform; must be non‑negative.  
**Return value** – `void`.  
**Exceptions**  
- `ArgumentNullException` if `circuitBreaker` is `null`.  
- `ArgumentOutOfRangeException` if `failureCount` is less than zero.  
- `InvalidOperationException` if the circuit breaker is in a state that does not permit recording failures (e.g., already open and configured to short‑circuit).  

### VerifyTripCount
```csharp
public static void VerifyTripCount(
    this CircuitBreaker circuitBreaker,
    int expectedTripCount)
```
**Purpose** – Asserts that the total number of times the circuit breaker has tripped (transitioned to the Open state) matches `expectedTripCount`.  
**Parameters**  
- `circuitBreaker`: The circuit breaker instance to query.  
- `expectedTripCount`: The anticipated trip count.  
**Return value** – `void`.  
**Exceptions**  
- `ArgumentNullException` if `circuitBreaker` is `null`.  
- `InvalidOperationException` if the actual trip count differs from `expectedTripCount`.  

## Usage

### Example 1: Driving a circuit breaker to open state
```csharp
var breaker = new CircuitBreaker(options);

// Simulate workload that will cause failures
breaker.TriggerFailures(5);

// Wait until the breaker opens due to the failure threshold
breaker.WaitForState(CircuitBreakerState.Open, TimeSpan.FromSeconds(2));

// Verify that the breaker has indeed tripped once
breaker.VerifyTripCount(1);
```

### Example 2: Asserting a half‑open to closed transition after successful calls
```csharp
var breaker = new CircuitBreaker(options);

// Force the breaker open
breaker.TriggerFailures(breaker.Options.FailureThreshold);
breaker.WaitForState(CircuitBreakerState.Open, TimeSpan.FromSeconds(1));

// Allow a successful trial call (half‑open)
breaker.Execute(() => { /* success */ });

// Assert the transition from Open to HalfOpen then to Closed
breaker.AssertStateTransition(CircuitBreakerState.Open, CircuitBreakerState.HalfOpen);
breaker.WaitForState(CircuitBreakerState.HalfOpen, TimeSpan.FromSeconds(1));
breaker.Execute(() => { /* success */ });
breaker.AssertStateTransition(CircuitBreakerState.HalfOpen, CircuitBreakerState.Closed);
```

## Notes
- All methods assume exclusive access to the supplied `CircuitBreaker` instance. Concurrent calls from multiple threads without external synchronization may lead to race conditions where state observations are stale or transitions are missed.  
- Passing `null` for the `circuitBreaker` argument will always result in an `ArgumentNullException`.  
- Timeouts in `WaitForState` are measured using `System.Diagnostics.Stopwatch`; if the timeout is zero the method will perform a single state check and throw `TimeoutException` immediately if the state does not match.  
- `TriggerFailures` relies on the circuit breaker’s internal failure‑recording mechanism; if the breaker is configured to short‑circuit on the first failure, additional invocations after the breaker is open may be suppressed and not increase the failure counter, potentially causing the method to complete without throwing but with fewer actual invocations than requested.  
- The trip count verified by `VerifyTripCount` includes only transitions to the Open state; manual state changes or test‑induced resets are not reflected unless the implementation exposes them.  
- These helpers contain no internal state and are safe to call repeatedly after the circuit breaker has been disposed, provided the instance is not accessed after disposal (behavior then depends on the circuit breaker’s own disposed state).
