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
    // Default Policy Values
    public const int DEFAULT_CIRCUIT_BREAKER_FAILURE_THRESHOLD = 5;
    public const int DEFAULT_CIRCUIT_BREAKER_SUCCESS_THRESHOLD = 3;
    public const int DEFAULT_RETRY_MAX_ATTEMPTS = 3;
    public const int DEFAULT_BULKHEAD_MAX_PARALLELIZATION = 10;
    public const int DEFAULT_BULKHEAD_MAX_QUEUE_LENGTH = 50;
    public const int DEFAULT_TIMEOUT_SECONDS = 10;
    public const int DEFAULT_FALLBACK_TIMEOUT_SECONDS = 5;

    // Circuit Breaker Defaults
    public const int CIRCUIT_BREAKER_OPEN_DURATION_SECONDS = 30;
    public const int CIRCUIT_BREAKER_MIN_FAILURE_THRESHOLD = 1;
    public const int CIRCUIT_BREAKER_MAX_FAILURE_THRESHOLD = 1000;

    // Retry Defaults
    public const int RETRY_INITIAL_DELAY_MS = 100;
    public const double RETRY_BACKOFF_MULTIPLIER = 2.0;
    public const int RETRY_MAX_DELAY_SECONDS = 30;

    // Timeout Defaults
    public const int TIMEOUT_MIN_MILLISECONDS = 10;
    public const int TIMEOUT_MAX_SECONDS = 300;

    // Bulkhead Defaults
    public const int BULKHEAD_MIN_PARALLELIZATION = 1;
    public const int BULKHEAD_MAX_PARALLELIZATION = 1000;
    public const int BULKHEAD_MIN_QUEUE_LENGTH = 0;
    public const int BULKHEAD_MAX_QUEUE_LENGTH = 10000;

    // Execution Settings
    public const int EXECUTION_METRICS_RETENTION_MINUTES = 60;
    public const int EXECUTION_BATCH_SIZE = 100;

    // Policy Management
    public const string POLICY_NAME_PATTERN = @"^[a-zA-Z0-9_\-\.]+$";
    public const int POLICY_NAME_MAX_LENGTH = 255;
    public const int POLICY_DESCRIPTION_MAX_LENGTH = 1000;

    // Monitoring
    public const int HEALTH_CHECK_INTERVAL_SECONDS = 30;
    public const int METRICS_SNAPSHOT_INTERVAL_SECONDS = 60;
}

/// <summary>
/// Policy execution states for tracking operation status.
/// </summary>
public enum ExecutionState
{
    Pending,
    Running,
    Completed,
    Failed,
    TimedOut,
    Rejected,
    Fallback
}

/// <summary>
/// Severity levels for policy violations and alerts.
/// </summary>
public enum AlertSeverity
{
    Info,
    Warning,
    Error,
    Critical
}

/// <summary>
/// Indicates which policy in the pipeline caused an issue.
/// </summary>
public enum PolicyType
{
    CircuitBreaker,
    Bulkhead,
    Retry,
    Timeout,
    Fallback
}

/// <summary>
/// Result codes for pipeline execution outcomes.
/// </summary>
public enum ResultCode
{
    Success = 200,
    PartialSuccess = 206,
    BadRequest = 400,
    NotFound = 404,
    Conflict = 409,
    ServiceUnavailable = 503,
    GatewayTimeout = 504,
    UnknownError = 500
}
