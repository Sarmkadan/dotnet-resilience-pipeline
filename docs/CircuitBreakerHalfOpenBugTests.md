# CircuitBreakerHalfOpenBugTests

The `CircuitBreakerHalfOpenBugTests` class is a unit test suite that validates the behavior of a circuit breaker when it transitions to the half-open state. It focuses on detecting regressions or bugs in the half-open logic, specifically around success threshold enforcement, request blocking after the threshold is met, failure handling that reopens the circuit, and the transition from open to half-open after the configured duration elapses. Each test method exercises a single scenario and throws an assertion exception if the circuit breaker’s actual behavior deviates from the expected outcome.

## API

### `public void RecordSuccess_InHalfOpen_ShouldOnlyAllowSuccessThresholdRequests`

Verifies that when the circuit breaker is in the half-open state, it permits only a number of requests equal to the configured success threshold. Any requests beyond that threshold are rejected (e.g., by throwing a `BrokenCircuitException` or returning a fallback).  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** `AssertionException` if the circuit breaker allows more or fewer requests than the threshold.

### `public void RecordSuccess_InHalfOpen_ShouldBlockAdditionalRequestsAfterSuccessThreshold`

Confirms that after the success threshold is reached in the half-open state, all subsequent requests are blocked until the circuit transitions to the closed state.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** `AssertionException` if additional requests are not blocked after the threshold.

### `public void RecordFailure_InHalfOpen_ShouldReopenCircuit`

Ensures that a single failure recorded while the circuit breaker is half-open immediately reopens the circuit, returning it to the open state.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** `AssertionException` if the circuit does not reopen after a failure.

### `public void AttemptReset_WhenOpenDurationElapsed_TransitionsToHalfOpen`

Validates that the circuit breaker automatically transitions from the open state to the half-open state once the configured open duration has elapsed.  
**Parameters:** None.  
**Return value:** `void`.  
**Throws:** `AssertionException` if the transition does not occur after the duration.

## Usage

The following examples demonstrate typical scenarios that the tests verify. They assume a circuit breaker implementation with configurable `SuccessThreshold`, `FailureThreshold`, and `OpenDuration`.

**Example 1: Configuring a circuit breaker and verifying half-open success threshold**

```csharp
var options = new CircuitBreakerOptions
{
    FailureThreshold = 3,
    SuccessThreshold = 2,
    OpenDuration = TimeSpan.FromSeconds(5)
};
var circuitBreaker = new CircuitBreaker(options);

// Simulate failures to open the circuit
for (int i = 0; i < 3; i++)
{
    circuitBreaker.Execute(() => throw new Exception());
}

// Wait for open duration to elapse (in tests, use a shorter duration or mock time)
Thread.Sleep(5000); // Not recommended in production; use a test scheduler

// Circuit is now half-open. Only 2 successes should be allowed.
circuitBreaker.Execute(() => "first");   // allowed
circuitBreaker.Execute(() => "second");  // allowed
// The next call should be blocked:
Assert.Throws<BrokenCircuitException>(() => circuitBreaker.Execute(() => "third"));
```

**Example 2: Failure in half-open reopens the circuit**

```csharp
var options = new CircuitBreakerOptions
{
    FailureThreshold = 2,
    SuccessThreshold = 3,
    OpenDuration = TimeSpan.FromSeconds(10)
};
var circuitBreaker = new CircuitBreaker(options);

// Open the circuit
circuitBreaker.Execute(() => throw new Exception());
circuitBreaker.Execute(() => throw new Exception());

// Wait for open duration
Thread.Sleep(10000);

// Circuit is half-open. A failure should reopen it.
Assert.Throws<Exception>(() => circuitBreaker.Execute(() => throw new Exception()));
// Now the circuit should be open again, so any call is blocked:
Assert.Throws<BrokenCircuitException>(() => circuitBreaker.Execute(() => "blocked"));
```

## Notes

- **Edge cases:** The tests assume that the success threshold is greater than zero and that the open duration is finite. If the threshold is set to zero, the half-open state may behave unexpectedly (e.g., never closing). The tests do not cover concurrent requests; they are designed for single-threaded execution to isolate half-open logic.
- **Thread safety:** A production circuit breaker must be thread-safe, as multiple threads may attempt to record success or failure concurrently. The tests in this class do not verify thread safety; they assume the implementation under test handles synchronization correctly. When running these tests, ensure that the circuit breaker instance is not shared across test methods to avoid state leakage.
- **Timing dependencies:** The `AttemptReset_WhenOpenDurationElapsed_TransitionsToHalfOpen` test relies on real-time waiting. In a continuous integration environment, use a virtual time provider or a mocked `ISystemClock` to avoid flakiness. The other tests do not depend on time and can be executed deterministically.
