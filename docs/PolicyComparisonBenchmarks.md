# PolicyComparisonBenchmarks
The `PolicyComparisonBenchmarks` type is designed to facilitate the comparison and benchmarking of different resilience policies in a .NET application. It provides a set of methods that can be used to test and evaluate the performance of various retry, circuit breaker, and bulkhead strategies under different scenarios, allowing developers to make informed decisions about which policies to use in their applications.

## API
The `PolicyComparisonBenchmarks` type exposes the following public members:
* `Setup`: Sets up the benchmarking environment.
* `RetryComparison_Fixed_Strategy`: Compares the performance of a fixed retry strategy.
	+ Parameters: None
	+ Return value: The result of the comparison as a `long` value.
	+ Throws: No exceptions are specified.
* `RetryComparison_Linear_Strategy`: Compares the performance of a linear retry strategy.
	+ Parameters: None
	+ Return value: The result of the comparison as a `long` value.
	+ Throws: No exceptions are specified.
* `RetryComparison_Exponential_Strategy`: Compares the performance of an exponential retry strategy.
	+ Parameters: None
	+ Return value: The result of the comparison as a `long` value.
	+ Throws: No exceptions are specified.
* `RetryComparison_ExponentialWithJitter_Strategy`: Compares the performance of an exponential retry strategy with jitter.
	+ Parameters: None
	+ Return value: The result of the comparison as a `long` value.
	+ Throws: No exceptions are specified.
* `RetryComparison_RecordRetryAttempt_All_Strategies`: Records retry attempts for all strategies.
	+ Parameters: None
	+ Return value: None
	+ Throws: No exceptions are specified.
* `CircuitBreakerComparison_LowThreshold_RecordSuccess`: Compares the performance of a circuit breaker with a low threshold and records successes.
	+ Parameters: None
	+ Return value: None
	+ Throws: No exceptions are specified.
* `CircuitBreakerComparison_HighThreshold_RecordSuccess`: Compares the performance of a circuit breaker with a high threshold and records successes.
	+ Parameters: None
	+ Return value: None
	+ Throws: No exceptions are specified.
* `CircuitBreakerComparison_ShortDuration_RecordFailure`: Compares the performance of a circuit breaker with a short duration and records failures.
	+ Parameters: None
	+ Return value: None
	+ Throws: No exceptions are specified.
* `CircuitBreakerComparison_LongDuration_AttemptReset`: Compares the performance of a circuit breaker with a long duration and attempts to reset.
	+ Parameters: None
	+ Return value: None
	+ Throws: No exceptions are specified.
* `CircuitBreakerComparison_GetState_All`: Gets the state of the circuit breaker for all scenarios.
	+ Parameters: None
	+ Return value: The state of the circuit breaker as a `CircuitBreakerPolicy.CircuitState` value.
	+ Throws: No exceptions are specified.
* `CircuitBreakerComparison_GetTrips_All`: Gets the number of trips for the circuit breaker in all scenarios.
	+ Parameters: None
	+ Return value: The number of trips as a `long` value.
	+ Throws: No exceptions are specified.
* `BulkheadComparison_Small_TryAcquireSlot`: Compares the performance of a bulkhead with a small size and attempts to acquire a slot.
	+ Parameters: None
	+ Return value: A `bool` value indicating whether the slot was acquired.
	+ Throws: No exceptions are specified.
* `BulkheadComparison_Medium_TryAcquireSlot`: Compares the performance of a bulkhead with a medium size and attempts to acquire a slot.
	+ Parameters: None
	+ Return value: A `bool` value indicating whether the slot was acquired.
	+ Throws: No exceptions are specified.
* `BulkheadComparison_Large_TryAcquireSlot`: Compares the performance of a bulkhead with a large size and attempts to acquire a slot.
	+ Parameters: None
	+ Return value: A `bool` value indicating whether the slot was acquired.
	+ Throws: No exceptions are specified.
* `BulkheadComparison_RecordQueueWaitTime_All`: Records the queue wait time for all bulkhead scenarios.
	+ Parameters: None
	+ Return value: None
	+ Throws: No exceptions are specified.
* `BulkheadComparison_GetUtilization_All`: Gets the utilization of the bulkhead for all scenarios.
	+ Parameters: None
	+ Return value: The utilization as a `double` value.
	+ Throws: No exceptions are specified.
* `CircuitBreakerComparison_Transition_Closed_To_Open`: Compares the performance of a circuit breaker transitioning from closed to open.
	+ Parameters: None
	+ Return value: None
	+ Throws: No exceptions are specified.
* `RetryComparison_Multiple_Retry_Attempts`: Compares the performance of multiple retry attempts.
	+ Parameters: None
	+ Return value: None
	+ Throws: No exceptions are specified.
* `BulkheadComparison_Queue_And_Reject`: Compares the performance of a bulkhead with a queue and rejects excess requests.
	+ Parameters: None
	+ Return value: A `bool` value indicating whether the request was rejected.
	+ Throws: No exceptions are specified.

## Usage
The following examples demonstrate how to use the `PolicyComparisonBenchmarks` type:
```csharp
// Create an instance of PolicyComparisonBenchmarks
var benchmarks = new PolicyComparisonBenchmarks();

// Set up the benchmarking environment
benchmarks.Setup();

// Compare the performance of different retry strategies
var fixedRetryResult = benchmarks.RetryComparison_Fixed_Strategy();
var linearRetryResult = benchmarks.RetryComparison_Linear_Strategy();
var exponentialRetryResult = benchmarks.RetryComparison_Exponential_Strategy();

// Compare the performance of a circuit breaker with a low threshold
benchmarks.CircuitBreakerComparison_LowThreshold_RecordSuccess();

// Attempt to acquire a slot in a bulkhead with a small size
var acquired = benchmarks.BulkheadComparison_Small_TryAcquireSlot();
```

```csharp
// Create an instance of PolicyComparisonBenchmarks
var benchmarks = new PolicyComparisonBenchmarks();

// Set up the benchmarking environment
benchmarks.Setup();

// Record retry attempts for all strategies
benchmarks.RetryComparison_RecordRetryAttempt_All_Strategies();

// Get the state of the circuit breaker for all scenarios
var circuitBreakerState = benchmarks.CircuitBreakerComparison_GetState_All();

// Get the utilization of the bulkhead for all scenarios
var bulkheadUtilization = benchmarks.BulkheadComparison_GetUtilization_All();
```

## Notes
When using the `PolicyComparisonBenchmarks` type, consider the following:
* The `Setup` method should be called before using any other methods to ensure the benchmarking environment is properly set up.
* The `RetryComparison_*` methods may throw exceptions if the retry strategy fails, so it's recommended to handle these exceptions accordingly.
* The `CircuitBreakerComparison_*` methods may transition the circuit breaker between different states, so it's recommended to check the state of the circuit breaker before and after calling these methods.
* The `BulkheadComparison_*` methods may reject excess requests, so it's recommended to handle these rejections accordingly.
* The `PolicyComparisonBenchmarks` type is not thread-safe, so it's recommended to use it in a single-threaded environment or to synchronize access to it using locks or other synchronization mechanisms.
