# CircuitBreakerPolicyTests

The `CircuitBreakerPolicyTests` class serves as the dedicated test suite for validating the state transition logic and configuration constraints of the circuit breaker implementation within the resilience pipeline. It verifies critical behaviors such as the enforcement of naming conventions, the accumulation of failures to trigger an open state, the recovery process from a half-open state upon success, and the functionality of manual reset operations to clear statistics and restore the closed state.

## API

### `Constructor_WithWhitespaceName_ThrowsArgumentException`
Validates that the circuit breaker policy constructor enforces strict naming rules by rejecting names that consist entirely of whitespace.
*   **Parameters**: None (instantiates the test context internally).
*   **Return Value**: `void`.
*   **Throws**: Asserts that an `ArgumentException` is thrown during construction when a whitespace-only name is provided.

### `RecordFailure_AtFailureThreshold_TransitionsToOpenState`
Verifies that the circuit breaker correctly transitions from the `Closed` state to the `Open` state immediately after the number of recorded failures meets the configured failure threshold.
*   **Parameters**: None (utilizes a pre-configured policy instance with a specific threshold).
*   **Return Value**: `void`.
*   **Throws**: Fails the test if the state does not transition to `Open` exactly at the threshold count.

### `RecordFailure_BelowFailureThreshold_RemainsInClosedState`
Ensures that the circuit breaker remains in the `Closed` state when the number of recorded failures is strictly less than the configured failure threshold.
*   **Parameters**: None (utilizes a pre-configured policy instance).
*   **Return Value**: `void`.
*   **Throws**: Fails the test if the state transitions to `Open` or `HalfOpen` prematurely.

### `RecordSuccess_InHalfOpenAtSuccessThreshold_TransitionsToClosedState`
Confirms that when the circuit breaker is in the `HalfOpen` state, recording a success event that meets the required success threshold causes a transition back to the `Closed` state.
*   **Parameters**: None (simulates the half-open context and success recording).
*   **Return Value**: `void`.
*   **Throws**: Fails the test if the state does not return to `Closed` after the success condition is met.

### `ManualReset_AfterCircuitOpens_ResetsToClosedAndClearsStatistics`
Tests the manual reset functionality, ensuring that invoking a reset after the circuit has opened forces an immediate transition to the `Closed` state and clears any accumulated failure statistics.
*   **Parameters**: None (invokes the reset method on an opened policy instance).
*   **Return Value**: `void`.
*   **Throws**: Fails the test if the state is not `Closed` post-reset or if internal counters are not zeroed.

## Usage

The following examples demonstrate how the test methods validate specific resilience behaviors.

**Example 1: Validating State Transition on Failure Threshold**
This test scenario ensures that the circuit opens only when the failure count reaches the exact limit, preventing premature tripping.

```csharp
[Test]
public void RecordFailure_AtFailureThreshold_TransitionsToOpenState()
{
    // Arrange
    var options = new CircuitBreakerOptions
    {
        FailureThreshold = 3,
        Name = "TestCircuit"
    };
    var policy = new CircuitBreakerPolicy(options);

    // Act: Record failures up to the threshold
    policy.RecordFailure();
    policy.RecordFailure();
    Assert.AreEqual(CircuitState.Closed, policy.State);

    policy.RecordFailure(); // The 3rd failure

    // Assert
    Assert.AreEqual(CircuitState.Open, policy.State);
}
```

**Example 2: Verifying Manual Reset Clears Statistics**
This scenario confirms that a manual reset not only closes the circuit but also wipes the slate clean regarding previous failure counts.

```csharp
[Test]
public void ManualReset_AfterCircuitOpens_ResetsToClosedAndClearsStatistics()
{
    // Arrange
    var policy = new CircuitBreakerPolicy(new CircuitBreakerOptions { FailureThreshold = 2 });
    policy.RecordFailure();
    policy.RecordFailure();
    Assert.AreEqual(CircuitState.Open, policy.State);

    // Act
    policy.ManualReset();

    // Assert
    Assert.AreEqual(CircuitState.Closed, policy.State);
    // Assuming a property or method exists to verify internal count is 0
    Assert.AreEqual(0, policy.GetFailureCount()); 
}
```

## Notes

*   **Input Validation**: The constructor strictly validates the `Name` parameter. Passing `null`, empty strings, or strings containing only whitespace characters will result in an `ArgumentException`. This prevents ambiguous logging and monitoring identifiers.
*   **State Machine Integrity**: The tests assume a deterministic state machine. Transitions from `Closed` to `Open` depend strictly on the failure count meeting the threshold, while transitions from `HalfOpen` to `Closed` depend on the success count. There is no implicit auto-closing based on time duration in these specific unit tests; time-based transitions are handled by separate timeout mechanisms.
*   **Thread Safety**: While the specific test methods execute sequentially, the underlying `CircuitBreakerPolicy` they validate is designed for concurrent access. The `RecordFailure` and `RecordSuccess` methods utilize atomic operations to ensure that state transitions occur correctly even under high concurrency. However, the `ManualReset` operation should be treated as a strong synchronization point; calling it while other threads are recording failures may result in those subsequent failures being counted against the newly reset zero-based counter.
*   **Statistics Clearance**: The `ManualReset` operation is comprehensive; it does not merely change the state flag but explicitly resets internal counters (failure count, success count in half-open) to zero. This is critical for ensuring that historical data from a previous circuit break event does not influence the stability of the newly closed circuit.
