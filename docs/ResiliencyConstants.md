# Resiliency Constants

`ResiliencyConstants` contains the default values and limits used to configure resilience policies and pipeline behavior.

## Constants

| Name | Value | Meaning |
| --- | --- | --- |
| `DEFAULT_CIRCUIT_BREAKER_FAILURE_THRESHOLD` | `5` | Default number of failures used as the circuit-breaker failure threshold. |
| `DEFAULT_CIRCUIT_BREAKER_SUCCESS_THRESHOLD` | `3` | Default number of successes used as the circuit-breaker success threshold. |
| `DEFAULT_RETRY_MAX_ATTEMPTS` | `3` | Default maximum number of retry attempts. |
| `DEFAULT_BULKHEAD_MAX_PARALLELIZATION` | `10` | Default maximum number of operations that the bulkhead permits to execute in parallel. |
| `DEFAULT_BULKHEAD_MAX_QUEUE_LENGTH` | `50` | Default maximum number of operations that can wait in the bulkhead queue. |
| `DEFAULT_TIMEOUT_SECONDS` | `10` | Default timeout duration, in seconds. |
| `DEFAULT_FALLBACK_TIMEOUT_SECONDS` | `5` | Default fallback timeout duration, in seconds. |
| `CIRCUIT_BREAKER_OPEN_DURATION_SECONDS` | `30` | Default duration, in seconds, for which a circuit breaker remains open. |
| `CIRCUIT_BREAKER_MIN_FAILURE_THRESHOLD` | `1` | Minimum permitted circuit-breaker failure threshold. |
| `CIRCUIT_BREAKER_MAX_FAILURE_THRESHOLD` | `1000` | Maximum permitted circuit-breaker failure threshold. |
| `RETRY_INITIAL_DELAY_MS` | `100` | Initial delay between retry attempts, in milliseconds. |
| `RETRY_BACKOFF_MULTIPLIER` | `2.0` | Multiplier applied when increasing the delay between retry attempts. |
| `RETRY_MAX_DELAY_SECONDS` | `30` | Maximum delay between retry attempts, in seconds. |
| `TIMEOUT_MIN_MILLISECONDS` | `10` | Minimum permitted timeout duration, in milliseconds. |
| `TIMEOUT_MAX_SECONDS` | `300` | Maximum permitted timeout duration, in seconds. |
| `BULKHEAD_MIN_PARALLELIZATION` | `1` | Minimum permitted bulkhead parallelization. |
| `BULKHEAD_MAX_PARALLELIZATION` | `1000` | Maximum permitted bulkhead parallelization. |
| `BULKHEAD_MIN_QUEUE_LENGTH` | `0` | Minimum permitted bulkhead queue length. |
| `BULKHEAD_MAX_QUEUE_LENGTH` | `10000` | Maximum permitted bulkhead queue length. |
| `EXECUTION_METRICS_RETENTION_MINUTES` | `60` | Duration, in minutes, for retaining execution metrics. |
| `EXECUTION_BATCH_SIZE` | `100` | Number of executions in a processing batch. |
| `POLICY_NAME_PATTERN` | `^[a-zA-Z0-9_\-\.]+$` | Regular-expression pattern accepted for policy names. |
| `POLICY_NAME_MAX_LENGTH` | `255` | Maximum permitted policy-name length. |
| `POLICY_DESCRIPTION_MAX_LENGTH` | `1000` | Maximum permitted policy-description length. |
| `HEALTH_CHECK_INTERVAL_SECONDS` | `30` | Interval between health checks, in seconds. |
| `METRICS_SNAPSHOT_INTERVAL_SECONDS` | `60` | Interval between metrics snapshots, in seconds. |

## `ExecutionState`

```csharp
Pending,
Running,
Completed,
Failed,
TimedOut,
Rejected,
Fallback
```

## `AlertSeverity`

```csharp
Info,
Warning,
Error,
Critical
```

## `PolicyType`

```csharp
CircuitBreaker,
Bulkhead,
Retry,
Timeout,
Fallback
```

## `ResultCode`

```csharp
Success = 200,
PartialSuccess = 206,
BadRequest = 400,
NotFound = 404,
Conflict = 409,
ServiceUnavailable = 503,
GatewayTimeout = 504,
UnknownError = 500
```
