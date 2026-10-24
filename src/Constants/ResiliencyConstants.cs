#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Constants;

/// <summary>
/// Global constants for resilience pipeline configuration and behavior.
/// </summary>
public static class ResiliencyConstants
{
    /// <summary>Default number of failures that opens a circuit breaker.</summary>
    public const int DEFAULT_CIRCUIT_BREAKER_FAILURE_THRESHOLD = 5;

    /// <summary>Default number of consecutive successes needed to close a half-open circuit breaker.</summary>
    public const int DEFAULT_CIRCUIT_BREAKER_SUCCESS_THRESHOLD = 3;

    /// <summary>Default maximum number of retry attempts.</summary>
    public const int DEFAULT_RETRY_MAX_ATTEMPTS = 3;

    /// <summary>Default maximum number of concurrently executing operations in a bulkhead.</summary>
    public const int DEFAULT_BULKHEAD_MAX_PARALLELIZATION = 10;

    /// <summary>Default maximum number of operations waiting in a bulkhead queue.</summary>
    public const int DEFAULT_BULKHEAD_MAX_QUEUE_LENGTH = 50;

    /// <summary>Default timeout for an operation, in seconds.</summary>
    public const int DEFAULT_TIMEOUT_SECONDS = 10;

    /// <summary>Default timeout for a fallback operation, in seconds.</summary>
    public const int DEFAULT_FALLBACK_TIMEOUT_SECONDS = 5;

    // Circuit Breaker Defaults

    /// <summary>Default duration a circuit stays open before probing again, in seconds.</summary>
    public const int CIRCUIT_BREAKER_OPEN_DURATION_SECONDS = 30;

    /// <summary>Lowest accepted circuit breaker failure threshold.</summary>
    public const int CIRCUIT_BREAKER_MIN_FAILURE_THRESHOLD = 1;

    /// <summary>Highest accepted circuit breaker failure threshold.</summary>
    public const int CIRCUIT_BREAKER_MAX_FAILURE_THRESHOLD = 1000;

    // Retry Defaults

    /// <summary>Default delay before the first retry, in milliseconds.</summary>
    public const int RETRY_INITIAL_DELAY_MS = 100;

    /// <summary>Multiplier applied to the retry delay after each attempt (exponential backoff).</summary>
    public const double RETRY_BACKOFF_MULTIPLIER = 2.0;

    /// <summary>Upper bound on any single retry delay, in seconds.</summary>
    public const int RETRY_MAX_DELAY_SECONDS = 30;

    // Timeout Defaults

    /// <summary>Lowest accepted timeout, in milliseconds.</summary>
    public const int TIMEOUT_MIN_MILLISECONDS = 10;

    /// <summary>Highest accepted timeout, in seconds.</summary>
    public const int TIMEOUT_MAX_SECONDS = 300;

    // Bulkhead Defaults

    /// <summary>Lowest accepted bulkhead parallelization limit.</summary>
    public const int BULKHEAD_MIN_PARALLELIZATION = 1;

    /// <summary>Highest accepted bulkhead parallelization limit.</summary>
    public const int BULKHEAD_MAX_PARALLELIZATION = 1000;

    /// <summary>Lowest accepted bulkhead queue length.</summary>
    public const int BULKHEAD_MIN_QUEUE_LENGTH = 0;

    /// <summary>Highest accepted bulkhead queue length.</summary>
    public const int BULKHEAD_MAX_QUEUE_LENGTH = 10000;

    // Execution Settings

    /// <summary>How long execution metrics are retained, in minutes.</summary>
    public const int EXECUTION_METRICS_RETENTION_MINUTES = 60;

    /// <summary>Number of execution records processed per batch.</summary>
    public const int EXECUTION_BATCH_SIZE = 100;

    // Policy Management

    /// <summary>Regular expression that policy names must match.</summary>
    public const string POLICY_NAME_PATTERN = @"^[a-zA-Z0-9_\-\.]+$";

    /// <summary>Maximum length of a policy name.</summary>
    public const int POLICY_NAME_MAX_LENGTH = 255;

    /// <summary>Maximum length of a policy description.</summary>
    public const int POLICY_DESCRIPTION_MAX_LENGTH = 1000;

    // Monitoring

    /// <summary>Interval between health checks, in seconds.</summary>
    public const int HEALTH_CHECK_INTERVAL_SECONDS = 30;

    /// <summary>Interval between metrics snapshots, in seconds.</summary>
    public const int METRICS_SNAPSHOT_INTERVAL_SECONDS = 60;
}

/// <summary>
/// Policy execution states for tracking operation status.
/// </summary>
public enum ExecutionState
{
    /// <summary>The execution has been created but not started.</summary>
    Pending,

    /// <summary>The execution is in progress.</summary>
    Running,

    /// <summary>The execution finished successfully.</summary>
    Completed,

    /// <summary>The execution finished with an error.</summary>
    Failed,

    /// <summary>The execution was cancelled because it exceeded its timeout.</summary>
    TimedOut,

    /// <summary>The execution was rejected before it started (for example, by a bulkhead or open circuit).</summary>
    Rejected,

    /// <summary>The fallback path handled the execution.</summary>
    Fallback
}

/// <summary>
/// Severity levels for policy violations and alerts.
/// </summary>
public enum AlertSeverity
{
    /// <summary>Informational message; no action required.</summary>
    Info,

    /// <summary>Potential problem that should be watched.</summary>
    Warning,

    /// <summary>A failure that needs attention.</summary>
    Error,

    /// <summary>A severe failure that requires immediate action.</summary>
    Critical
}

/// <summary>
/// Indicates which policy in the pipeline caused an issue.
/// </summary>
public enum PolicyType
{
    /// <summary>The circuit breaker policy.</summary>
    CircuitBreaker,

    /// <summary>The bulkhead (concurrency limiting) policy.</summary>
    Bulkhead,

    /// <summary>The retry policy.</summary>
    Retry,

    /// <summary>The timeout policy.</summary>
    Timeout,

    /// <summary>The fallback policy.</summary>
    Fallback
}

/// <summary>
/// Result codes for pipeline execution outcomes.
/// </summary>
public enum ResultCode
{
    /// <summary>The operation completed successfully (HTTP-style 200).</summary>
    Success = 200,

    /// <summary>The operation completed partially (HTTP-style 206).</summary>
    PartialSuccess = 206,

    /// <summary>The request was invalid (HTTP-style 400).</summary>
    BadRequest = 400,

    /// <summary>The requested resource was not found (HTTP-style 404).</summary>
    NotFound = 404,

    /// <summary>The request conflicts with the current state (HTTP-style 409).</summary>
    Conflict = 409,

    /// <summary>The service is temporarily unavailable (HTTP-style 503).</summary>
    ServiceUnavailable = 503,

    /// <summary>The upstream dependency did not respond in time (HTTP-style 504).</summary>
    GatewayTimeout = 504,

    /// <summary>An unexpected error occurred (HTTP-style 500).</summary>
    UnknownError = 500
}
