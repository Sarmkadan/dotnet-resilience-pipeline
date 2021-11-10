# ResiliencyPipelineServiceTests

The `ResiliencyPipelineServiceTests` class contains unit tests for the `ResiliencyPipelineService`, which is the core component of the `dotnet-resilience-pipeline` library. These tests validate that policies can be registered and that operations executed through the pipeline behave correctly under various conditions, including success, failure, and fallback scenarios. They also verify that execution statistics are tracked as expected.

## API

### `RegisterPolicy_ShouldAddPolicyToPipeline`
- **Purpose**: Verifies that a policy can be registered with the pipeline and that it is subsequently applied to operations.
- **Parameters**: None.
- **Return value**: `void`.
- **Throws**: Does not throw under normal conditions. Test failures are reported as assertion exceptions.

### `ExecuteAsync_ShouldReturnSuccess_WhenOperationSucceeds`
- **Purpose**: Ensures that when an asynchronous operation completes successfully, the pipeline returns the operation’s result without applying any retry or fallback logic.
- **Parameters**: None.
- **Return value**: `Task`.
- **Throws**: Does not throw under normal conditions. Test failures are reported as assertion exceptions.

### `ExecuteAsync_ShouldTrackExecutionStats`
- **Purpose**: Confirms that the pipeline records execution statistics (e.g., success count, failure count, duration) after an operation is executed.
- **Parameters**: None.
- **Return value**: `Task`.
- **Throws**: Does not throw under normal conditions. Test failures are reported as assertion exceptions.

### `ExecuteAsync_ShouldReturnFailure_WhenOperationFailsAndNoFallback`
- **Purpose**: Validates that when an operation throws an exception and no fallback policy is registered, the pipeline propagates the exception to the caller.
- **Parameters**: None.
- **Return value**: `Task`.
- **Throws**: Does not throw under normal conditions. Test failures are reported as assertion exceptions.

### `ExecuteAsync_ShouldUseFallback_WhenOperationFails`
- **Purpose**: Checks that when an operation fails and a fallback policy is registered, the pipeline executes the fallback and returns its result instead of propagating the original exception.
- **Parameters**: None.
- **Return value**: `Task`.
- **Throws**: Does not throw under normal conditions. Test failures are reported as assertion exceptions.

## Usage

The following examples demonstrate the behavior that the tests verify. They use the `ResiliencyPipelineService` directly.

### Example 1: Successful execution with a retry policy

```csharp
using dotnet_resilience_pipeline;

var pipeline = new ResiliencyPipelineService();
pipeline.RegisterPolicy(new RetryPolicy(3));

// This operation succeeds on the first attempt.
var result = await pipeline.ExecuteAsync(() => Task.FromResult(42));
Console.WriteLine(result); // Output: 42
```

### Example 2: Failure with a fallback policy

```csharp
using dotnet_resilience_pipeline;

var pipeline = new ResiliencyPipelineService();
pipeline.RegisterPolicy(new FallbackPolicy(() => Task.FromResult(-1)));

// This operation always throws, but the fallback returns -1.
var result = await pipeline.ExecuteAsync<int>(() => throw new InvalidOperationException());
Console.WriteLine(result); // Output: -1
```

## Notes

- **Edge cases**:  
  - If an operation fails and no fallback is registered, the original exception is thrown. The test `ExecuteAsync_ShouldReturnFailure_WhenOperationFailsAndNoFallback` covers this scenario.  
  - If a fallback policy is registered but the fallback itself throws, the pipeline will propagate that exception. This case is not covered by the listed tests.  
  - Execution statistics are tracked per pipeline instance; resetting the pipeline clears all accumulated data.

- **Thread safety**:  
  The tests are designed to run sequentially and do not verify concurrent access. The `ResiliencyPipelineService` implementation should be thread-safe for registration and execution, but these tests do not exercise that property. For production use, ensure that policy registration and execution are not performed concurrently without proper synchronization.
