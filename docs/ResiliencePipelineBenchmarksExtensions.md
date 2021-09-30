# ResiliencePipelineBenchmarksExtensions

Extension methods for benchmarking resilience pipelines in .NET applications. These utilities simplify the process of measuring pipeline performance, collecting statistics, and resetting state between benchmark runs.

## API

### `ResetPipelineState`

Resets the internal state of all resilience pipelines in the current benchmark context. This ensures a clean state before subsequent benchmark runs, avoiding interference from previous executions.

- **Parameters**: None
- **Return value**: `void`
- **Exceptions**: None

### `RunAndCollectStatistics`

Executes a single resilience pipeline operation and collects detailed statistics about its execution. This is useful for measuring the performance of a single pipeline invocation under load.

- **Parameters**:
  - `pipeline` (`ResiliencePipeline`): The resilience pipeline to benchmark.
  - `operation` (`Func<Task>`): The asynchronous operation to execute within the pipeline.
- **Return value**: `Task<PipelineStatistics>`: A task that resolves to a `PipelineStatistics` object containing metrics such as execution time, retry attempts, and outcome.
- **Exceptions**:
  - Throws `ArgumentNullException` if `pipeline` or `operation` is `null`.
  - Throws `InvalidOperationException` if the pipeline is not in a valid state for execution.

### `RunFullPipelineAndCollectStatistics`

Executes a full resilience pipeline workflow, including all resilience strategies (e.g., retries, timeouts, circuit breakers), and collects comprehensive statistics. This is ideal for end-to-end benchmarking of a pipeline with all configured strategies active.

- **Parameters**:
  - `pipeline` (`ResiliencePipeline`): The resilience pipeline to benchmark.
  - `operation` (`Func<Task>`): The asynchronous operation to execute within the pipeline.
- **Return value**: `Task<PipelineStatistics>`: A task that resolves to a `PipelineStatistics` object containing detailed metrics such as execution time, retry attempts, circuit state, and outcome.
- **Exceptions**:
  - Throws `ArgumentNullException` if `pipeline` or `operation` is `null`.
  - Throws `InvalidOperationException` if the pipeline is not in a valid state for execution.

### `RunParallelOperationsAndCollectStatistics`

Executes multiple resilience pipeline operations in parallel and collects aggregated statistics. This is useful for measuring pipeline performance under concurrent load, simulating real-world usage patterns.

- **Parameters**:
  - `pipeline` (`ResiliencePipeline`): The resilience pipeline to benchmark.
  - `operations` (`IEnumerable<Func<Task>>`): A collection of asynchronous operations to execute in parallel within the pipeline.
  - `degreeOfParallelism` (`int`): The maximum number of concurrent operations to execute.
- **Return value**: `Task<PipelineStatistics>`: A task that resolves to a `PipelineStatistics` object containing aggregated metrics such as total execution time, average latency, retry distribution, and outcome.
- **Exceptions**:
  - Throws `ArgumentNullException` if `pipeline` or `operations` is `null`.
  - Throws `ArgumentOutOfRangeException` if `degreeOfParallelism` is less than 1.
  - Throws `InvalidOperationException` if the pipeline is not in a valid state for execution.

## Usage

### Example 1: Benchmarking a Single Pipeline Operation
