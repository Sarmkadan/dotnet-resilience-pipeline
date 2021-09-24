# CircuitBreakerBenchmarks

Overview of the benchmark suite used to measure the performance of the `CircuitBreakerPolicy` under various states and operations. The class contains benchmark methods that exercise the circuit breaker in closed, half‑open, and open states, record failures, and trigger state transitions, as well as helper members to inspect the current state and trip count.

## API

### `public void Setup`
**Purpose**  
Initializes a fresh `CircuitBreakerPolicy` instance and any supporting state required by the benchmark methods. This method is invoked by the benchmark runner before each iteration to ensure a clean slate.

**Parameters**  
None.

**Return value**  
None.

**Exceptions**  
May throw `InvalidOperationException` if the underlying policy cannot be created (e.g., due to misconfigured options) or if required resources are unavailable.

### `public void CircuitBreaker_Closed_State`
**Purpose**  
Measures the overhead of executing a successful operation while the circuit breaker is in the **Closed** state (i.e., calls are allowed through).

**Parameters**  
None.

**Return value**  
None.

**Exceptions**  
Throws `InvalidOperationException` if the circuit breaker is not in the Closed state at the start of the benchmark iteration.

### `public void CircuitBreaker_HalfOpen_State`
**Purpose**  
Measures the overhead of executing an operation while the circuit breaker is in the **Half‑Open** state (i.e., a trial call is permitted to test if the downstream service has recovered).

**Parameters**  
None.

**Return value**  
None.

**Exceptions**  
Throws `InvalidOperationException` if the circuit breaker is not in the Half‑Open state at the start of the benchmark iteration.

### `public void CircuitBreaker_Open_State`
**Purpose**  
Measures the overhead of executing an operation while the circuit breaker is in the **Open** state (i.e., calls are short‑circuited and an exception is thrown immediately).

**Parameters**  
None.

**Return value**  
None.

**Exceptions**  
Throws `InvalidOperationException` if the circuit breaker is not in the Open state at the start of the benchmark iteration.

### `public void CircuitBreaker_Failure_Recording`
**Purpose**  
Measures the cost of recording a failure against the circuit breaker (i.e., incrementing the failure counter and potentially triggering a state change).

**Parameters**  
None.

**Return value**  
None.

**Exceptions**  
May throw `InvalidOperationException` if the circuit breaker has not been initialized via `Setup`.

### `public void CircuitBreaker_State_Transition`
**Purpose**  
Measures the latency associated with a state transition (e.g., from Closed to Open or from Half‑Open to Closed) caused by successive calls that meet the configured failure/success thresholds.

**Parameters**  
None.

**Return value**  
None.

**Exceptions**  
Throws `InvalidOperationException` if the circuit breaker cannot transition (e.g., due to misconfigured thresholds) or if `Setup` has not been called.

### `public CircuitBreakerPolicy.CircuitState CircuitBreaker_Get_CurrentState`
**Purpose**  
Retrieves the current state of the circuit breaker after the most recent operation.

**Parameters**  
None.

**Return value**  
A value of type `CircuitBreakerPolicy.CircuitState` indicating whether the circuit is Closed, HalfOpen, or Open.

**Exceptions**  
Throws `InvalidOperationException` if the circuit breaker instance has not been initialized.

### `public long CircuitBreaker_Get_CircuitBreakerTrips`
**Purpose**  
Returns the total number of times the circuit breaker has tripped (i.e., transitioned to the Open state) since its creation.

**Parameters**  
None.

**Return value**  
A 64‑bit integer representing the trip count.

**Exceptions**  
Throws `InvalidOperationException` if the circuit breaker instance has not been initialized.

## Usage

### Example 1: Running a single benchmark iteration manually
```csharp
using DotNetResiliencePipeline.Benchmarks; // namespace containing CircuitBreakerBenchmarks

var bench = new CircuitBreakerBenchmarks();
bench.Setup();                                 // prepare a fresh policy
bench.CircuitBreaker_Closed_State();           // measure closed‑state execution
var state = bench.CircuitBreaker_Get_CurrentState; // should be Closed
var trips = bench.CircuitBreaker_Get_CircuitBreakerTrips; // typically 0
```

### Example 2: Simulating a failure‑recording scenario
```csharp
var bench = new CircuitBreakerBenchmarks();
bench.Setup();

// Force the circuit into a state where failures are recorded
bench.CircuitBreaker_Failure_Recording();      // record a failure
bench.CircuitBreaker_Failure_Recording();      // record another failure

// After enough failures, the circuit may transition to Open
var state = bench.CircuitBreaker_Get_CurrentState;
var trips = bench.CircuitBreaker_Get_CircuitBreakerTrips;
```

## Notes
- The class is **not thread‑safe**. BenchmarkDotNet invokes `Setup` before each iteration, but calling any of the benchmark methods or getters from multiple threads concurrently on the same instance can lead to undefined behavior.
- All methods assume that `Setup` has been called successfully; invoking them beforehand will result in an `InvalidOperationException`.
- The trip counter returned by `CircuitBreaker_Get_CircuitBreakerTrips` is cumulative across the lifetime of the policy instance; it is not reset by `Setup`.
- State‑transition benchmarks depend on the configured failure/success thresholds of the underlying `CircuitBreakerPolicy`. Altering those thresholds outside of the benchmark setup will affect measured latencies.
- Exceptions thrown by the benchmark methods are propagated to the benchmark runner and will cause the iteration to be marked as failed; they are not swallowed internally.
