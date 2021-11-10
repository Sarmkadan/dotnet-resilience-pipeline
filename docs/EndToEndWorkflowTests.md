# EndToEndWorkflowTests

Comprehensive integration tests validating end-to-end resilience workflows in .NET applications using Polly policies. These tests verify correct interaction between retry, circuit breaker, bulkhead, timeout, and fallback policies under various failure and load conditions.

## API

### Methods

#### `ReadmeMainUseCase_RetryThenCircuitBreaker_FailsOverToFallback`
Validates the primary scenario from the README: a transient failure triggers retries, the circuit breaker trips after repeated failures, and execution falls back to a secondary service. No parameters. Returns a `Task`. Throws if the fallback service is unavailable or returns an invalid result.

#### `RetryService_ExecutesRetries_ThenSucceeds`
Ensures that a transient fault triggers the configured retry policy and eventually succeeds on a subsequent attempt. No parameters. Returns a `Task`. Throws if the operation never recovers within the retry policy limits.

#### `CircuitBreaker_TripsAfterThreshold_BlocksSubsequentCalls`
Confirms that the circuit breaker transitions to an open state after the configured failure threshold is exceeded and blocks further calls until the reset timeout elapses. No parameters. Returns a `Task`. Throws if the circuit breaker does not trip or allows calls while open.

#### `CircuitBreaker_AfterOpenDurationElapses_AllowsHalfOpenProbe`
Tests that after the specified open duration elapses, the circuit breaker allows a single probe (half-open) call to assess recovery. No parameters. Returns a `Task`. Throws if the probe is not permitted or the circuit breaker does not return to closed on success.

#### `FallbackService_WhenPrimaryFails_ReturnsFallbackValue`
Verifies that when the primary service fails, the fallback policy returns the configured fallback value. No parameters. Returns a `Task<string>` containing the fallback value. Throws if the primary succeeds or the fallback is not invoked.

#### `FallbackService_WhenFallbackOnAnyExceptionFalse_ReturnsFailureResult`
Ensures that when `fallbackOnAnyException` is set to `false`, only specific exceptions trigger the fallback. No parameters. Returns a `Task<bool>` indicating whether the fallback was bypassed. Throws if the policy incorrectly applies fallback to non-matching exceptions.

#### `BulkheadService_ConcurrentRequests_LimitsParallelism`
Validates that the bulkhead policy restricts concurrent executions to the configured limit under load. No parameters. Returns a `Task`. Throws if more than the allowed number of concurrent operations execute simultaneously.

#### `BulkheadService_AfterRelease_AcceptsNewRequests`
Confirms that after a bulkhead slot is released, a new request can acquire it. No parameters. Returns `void`. Throws if the bulkhead remains saturated or rejects valid requests.

#### `TimeoutService_OperationCompletesBeforeTimeout_ReturnsResult`
Tests that an operation completing within the timeout period returns the expected result. No parameters. Returns a `Task<string>` with the result. Throws if the operation exceeds the timeout or the result is incorrect.

#### `TimeoutService_OperationExceedsTimeout_ThrowsOperationTimeoutException`
Ensures that an operation exceeding the timeout throws `OperationTimeoutException`. No parameters. Returns a `Task`. Throws if the timeout is not enforced or the correct exception is not raised.

#### `PipelineBuilder_CircuitBreakerOnly_BuildsSuccessfully`
Validates that a pipeline configured with only a circuit breaker policy builds without errors. No parameters. Returns `void`. Throws if the builder throws during construction or the pipeline is not created.

#### `PipelineBuilder_TimeoutWithCustomConfiguration_ConfiguresCorrectly`
Confirms that a pipeline with a timeout policy using custom timeout and timeout strategy is configured correctly. No parameters. Returns `void`. Throws if the timeout value or strategy is not applied.

#### `PipelineBuilder_BulkheadWithCustomLimits_ConfiguresCorrectly`
Ensures that a bulkhead policy with custom queue limit and parallelism is configured correctly. No parameters. Returns `void`. Throws if the limits are not enforced in the built pipeline.

#### `PipelineBuilder_WithFallbackAction_SetsFallbackCorrectly`
Validates that a fallback action is correctly assigned to the pipeline during configuration. No parameters. Returns `void`. Throws if the fallback is not set or the pipeline does not invoke it when expected.

#### `PipelineBuilder_WithFallbackActionBeforeFallbackPolicy_ThrowsInvalidOperationException`
Ensures that attempting to set a fallback action before a fallback policy throws `InvalidOperationException`. No parameters. Returns `void`. Throws if the exception is not raised or the pipeline is incorrectly built.

#### `CircuitBreaker_ConcurrentSuccesses_RecordsAllCorrectly`
Tests that concurrent successes are recorded correctly by the circuit breaker and do not interfere with state transitions. No parameters. Returns a `Task`. Throws if the circuit breaker state becomes inconsistent or statistics are incorrect.

#### `Bulkhead_ConcurrentAcquireAndRelease_MaintainsConsistentCount`
Validates that concurrent acquire and release operations maintain a consistent count of available slots in the bulkhead. No parameters. Returns a `Task`. Throws if the count becomes negative or exceeds the configured limit.

#### `FullWorkflow_ConfigureExecuteVerify_AllPoliciesTrackStatistics`
Runs a full workflow combining all policies and verifies that each policy tracks its statistics accurately. No parameters. Returns a `Task`. Throws if any policy’s statistics are incorrect or inconsistent with observed behavior.

## Usage

### Example 1: Validating a Resilience Pipeline with Fallback and Timeout
