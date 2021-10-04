# IPipelineMetrics

`IPipelineMetrics` exposes aggregated execution statistics and policy state snapshots for a resilience pipeline. It provides a read-only view of outcomes across all executions processed by the pipeline, enabling monitoring, alerting, and diagnostic analysis without allowing external mutation of internal counters.

## API

### `long TotalExecutions`

Gets the total number of executions that have been initiated through the pipeline, regardless of outcome. This counter increments before any resilience strategies are applied and includes both synchronous and asynchronous executions.

### `long SuccessfulExecutions`

Gets the number of executions that completed without triggering any failure-handling strategy and returned a successful result. An execution is considered successful only if no retries, circuit-breaking, or fallback mechanisms were invoked on its behalf.

### `long FailedExecutions`

Gets the number of executions that ultimately failed after all resilience strategies were exhausted. This includes executions that threw unhandled exceptions, were terminated by a timeout strategy with no fallback, or were rejected by an open circuit breaker.

### `double SuccessRate`

Gets the ratio of successful executions to total executions, expressed as a value between 0.0 and 1.0. When `TotalExecutions` is zero, this property returns `0.0`. The value is computed from the current `SuccessfulExecutions` and `TotalExecutions` counters and may reflect floating-point precision limitations at extremely high execution counts.

### `long RetryCount`

Gets the cumulative number of retry attempts performed across all executions. This counter increments each time a retry strategy re-invokes an operation after a failure, including both individual retries within a single execution and retries across multiple executions.

### `long CircuitBreakerTrips`

Gets the number of times the circuit breaker transitioned from a closed or half-open state to an open state. Each trip represents a distinct circuit-breaking event, not the duration the circuit remained open.

### `long TimeoutCount`

Gets the number of executions that were forcibly terminated by a timeout strategy. This counter increments when an operation exceeds its configured time limit and the timeout strategy aborts it, regardless of whether a fallback subsequently handled the result.

### `IReadOnlyList<Policies.PolicySnapshot> PolicySnapshots`

Gets an immutable list of snapshots representing the current state of each resilience policy configured in the pipeline. Each `PolicySnapshot` contains policy-specific metadata such as circuit state, retry attempt windows, or timeout configuration. The returned list is a snapshot at the time of access and does not update as policy states change.

## Usage

### Monitoring Pipeline Health in a Background Service

```csharp
public sealed class PipelineHealthReporter : BackgroundService
{
    private readonly ResiliencePipeline _pipeline;
    private readonly ILogger _logger;

    public PipelineHealthReporter(ResiliencePipeline pipeline, ILogger<PipelineHealthReporter> logger)
    {
        _pipeline = pipeline;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            IPipelineMetrics metrics = _pipeline.GetMetrics();

            _logger.LogInformation(
                "Pipeline stats: {Total} total, {SuccessRate:P} success rate, {Retries} retries, {Timeouts} timeouts, {Trips} CB trips",
                metrics.TotalExecutions,
                metrics.SuccessRate,
                metrics.RetryCount,
                metrics.TimeoutCount,
                metrics.CircuitBreakerTrips);

            if (metrics.SuccessRate < 0.95 && metrics.TotalExecutions > 100)
            {
                _logger.LogWarning("Pipeline success rate dropped below 95% threshold");
            }

            await Task.Delay(TimeSpan.FromSeconds(30), stoppingToken);
        }
    }
}
```

### Inspecting Circuit Breaker State Before Execution

```csharp
public async Task<ActionResult> GetWithCircuitAwareness(CancellationToken cancellationToken)
{
    IPipelineMetrics metrics = _pipeline.GetMetrics();

    foreach (PolicySnapshot snapshot in metrics.PolicySnapshots)
    {
        if (snapshot is CircuitBreakerPolicySnapshot cbSnapshot && cbSnapshot.CircuitState == CircuitState.Open)
        {
            return new StatusCodeResult(503);
        }
    }

    try
    {
        var result = await _pipeline.ExecuteAsync(
            async ct => await _downstreamService.FetchDataAsync(ct),
            cancellationToken);

        return Ok(result);
    }
    catch (OperationCanceledException)
    {
        return new StatusCodeResult(408);
    }
}
```

## Notes

- All numeric counters are monotonic for the lifetime of the pipeline instance and do not reset. `SuccessRate` is the only property that can both increase and decrease as new executions complete.
- The properties reflect a point-in-time view. Between reading `TotalExecutions` and `SuccessfulExecutions`, new executions may complete, causing the computed `SuccessRate` to appear inconsistent with the individually read values. For a consistent snapshot, capture all needed values into local variables in a single sequential read block.
- `PolicySnapshots` returns a new list instance on each access. The individual `PolicySnapshot` objects within the list are immutable copies of policy state at the moment of the call.
- Thread safety: all property getters are safe to call concurrently from multiple threads without external synchronization. The underlying counters are updated atomically, and the `PolicySnapshots` list is constructed as an isolated copy.
- When `TotalExecutions` is zero, `SuccessRate` returns `0.0` rather than `double.NaN` or throwing an exception. Callers that distinguish "no data" from "zero success" should check `TotalExecutions` explicitly.
- `FailedExecutions` does not necessarily equal `TotalExecutions - SuccessfulExecutions` if the pipeline has executions that are still in flight or were cancelled before reaching a terminal outcome.
