#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Configuration;

/// <summary>
/// Configuration options for the .NET Resilience Pipeline library.
/// </summary>
public sealed class DotnetResiliencePipelineOptions
{
    /// <summary>
    /// Gets or sets circuit breaker configuration.
    /// </summary>
    public CircuitBreakerOptions CircuitBreaker { get; set; } = new();

    /// <summary>
    /// Gets or sets retry configuration.
    /// </summary>
    public RetryOptions Retry { get; set; } = new();

    /// <summary>
    /// Gets or sets timeout configuration.
    /// </summary>
    public TimeoutOptions Timeout { get; set; } = new();

    /// <summary>
    /// Gets or sets bulkhead configuration.
    /// </summary>
    public BulkheadOptions Bulkhead { get; set; } = new();

    /// <summary>
    /// Gets or sets fallback configuration.
    /// </summary>
    public FallbackOptions Fallback { get; set; } = new();

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <returns>True if valid, false otherwise.</returns>
    public bool Validate(out List<ValidationResult> validationResults)
    {
        validationResults = new List<ValidationResult>();
        var context = new ValidationContext(this);
        return Validator.TryValidateObject(this, context, validationResults, validateAllProperties: true);
    }

    /// <summary>
    /// Circuit breaker specific configuration options.
    /// </summary>
    public sealed class CircuitBreakerOptions
    {
        /// <summary>
        /// Number of consecutive failures before opening the circuit.
        /// Default: 5
        /// </summary>
        [Range(1, 1000, ErrorMessage = "FailureThreshold must be between 1 and 1000")]
        public int FailureThreshold { get; set; } = 5;

        /// <summary>
        /// Duration the circuit remains open before transitioning to half-open (in seconds).
        /// Default: 30
        /// </summary>
        [Range(1, 3600, ErrorMessage = "OpenDurationSeconds must be between 1 and 3600")]
        public int OpenDurationSeconds { get; set; } = 30;

        /// <summary>
        /// Number of successful executions in half-open state to close the circuit.
        /// Default: 3
        /// </summary>
        [Range(1, 100, ErrorMessage = "SuccessThresholdInHalfOpen must be between 1 and 100")]
        public int SuccessThresholdInHalfOpen { get; set; } = 3;

        /// <summary>
        /// Converts to CircuitBreakerPolicy configuration.
        /// </summary>
        public CircuitBreakerPolicy ToPolicy(string name)
        {
            return new CircuitBreakerPolicy(name)
            {
                FailureThreshold = FailureThreshold,
                OpenDuration = TimeSpan.FromSeconds(OpenDurationSeconds),
                SuccessThresholdInHalfOpen = SuccessThresholdInHalfOpen
            };
        }
    }

    /// <summary>
    /// Retry specific configuration options.
    /// </summary>
    public sealed class RetryOptions
    {
        /// <summary>
        /// Maximum number of retry attempts.
        /// Default: 3
        /// </summary>
        [Range(0, 20, ErrorMessage = "MaxRetries must be between 0 and 20")]
        public int MaxRetries { get; set; } = 3;

        /// <summary>
        /// Initial delay between retries (in milliseconds).
        /// Default: 100
        /// </summary>
        [Range(1, 10000, ErrorMessage = "InitialDelayMs must be between 1 and 10000")]
        public int InitialDelayMs { get; set; } = 100;

        /// <summary>
        /// Backoff strategy for calculating delays.
        /// Default: Exponential
        /// </summary>
        public RetryPolicy.BackoffStrategy Strategy { get; set; } = RetryPolicy.BackoffStrategy.Exponential;

        /// <summary>
        /// Maximum delay between retries (in milliseconds).
        /// Default: 30000
        /// </summary>
        [Range(1, 300000, ErrorMessage = "MaxDelayMs must be between 1 and 300000")]
        public int MaxDelayMs { get; set; } = 30000;

        /// <summary>
        /// Multiplier for exponential/linear backoff.
        /// Default: 2.0
        /// </summary>
        [Range(1.0, 10.0, ErrorMessage = "BackoffMultiplier must be between 1.0 and 10.0")]
        public double BackoffMultiplier { get; set; } = 2.0;

        /// <summary>
        /// Whether to apply jitter to exponential backoff strategies.
        /// Default: true
        /// </summary>
        public bool UseJitter { get; set; } = true;

        /// <summary>
        /// Random jitter factor applied to exponential backoff (0.0 = no jitter, 1.0 = full jitter).
        /// Default: 1.0
        /// </summary>
        [Range(0.0, 1.0, ErrorMessage = "JitterFactor must be between 0.0 and 1.0")]
        public double JitterFactor { get; set; } = 1.0;

        /// <summary>
        /// Converts to RetryPolicy configuration.
        /// </summary>
        public RetryPolicy ToPolicy(string name)
        {
            return new RetryPolicy(name)
            {
                MaxRetries = MaxRetries,
                InitialDelay = TimeSpan.FromMilliseconds(InitialDelayMs),
                Strategy = Strategy,
                MaxDelay = TimeSpan.FromMilliseconds(MaxDelayMs),
                BackoffMultiplier = BackoffMultiplier,
                UseJitter = UseJitter,
                JitterFactor = JitterFactor
            };
        }
    }

    /// <summary>
    /// Timeout specific configuration options.
    /// </summary>
    public sealed class TimeoutOptions
    {
        /// <summary>
        /// Timeout duration (in seconds).
        /// Default: 10
        /// </summary>
        [Range(1, 300, ErrorMessage = "TimeoutSeconds must be between 1 and 300")]
        public int TimeoutSeconds { get; set; } = 10;

        /// <summary>
        /// Converts to TimeoutPolicy configuration.
        /// </summary>
        public TimeoutPolicy ToPolicy(string name)
        {
            return new TimeoutPolicy(name)
            {
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds)
            };
        }
    }

    /// <summary>
    /// Bulkhead specific configuration options.
    /// </summary>
    public sealed class BulkheadOptions
    {
        /// <summary>
        /// Maximum number of concurrent executions.
        /// Default: 10
        /// </summary>
        [Range(1, 1000, ErrorMessage = "MaxParallelization must be between 1 and 1000")]
        public int MaxParallelization { get; set; } = 10;

        /// <summary>
        /// Maximum queue length for requests waiting to execute.
        /// Default: 50
        /// </summary>
        [Range(0, 10000, ErrorMessage = "MaxQueueLength must be between 0 and 10000")]
        public int MaxQueueLength { get; set; } = 50;

        /// <summary>
        /// Converts to BulkheadPolicy configuration.
        /// </summary>
        public BulkheadPolicy ToPolicy(string name)
        {
            return new BulkheadPolicy(name)
            {
                MaxParallelization = MaxParallelization,
                MaxQueueLength = MaxQueueLength
            };
        }
    }

    /// <summary>
    /// Fallback specific configuration options.
    /// </summary>
    public sealed class FallbackOptions
    {
        /// <summary>
        /// Whether to fallback on any exception.
        /// Default: true
        /// </summary>
        public bool FallbackOnAnyException { get; set; } = true;

        /// <summary>
        /// Fallback timeout duration (in seconds).
        /// Default: 5
        /// </summary>
        [Range(1, 60, ErrorMessage = "FallbackTimeoutSeconds must be between 1 and 60")]
        public int FallbackTimeoutSeconds { get; set; } = 5;

        /// <summary>
        /// Converts to FallbackPolicy configuration.
        /// </summary>
        public FallbackPolicy ToPolicy(string name)
        {
            return new FallbackPolicy(name)
            {
                FallbackOnAnyException = FallbackOnAnyException,
                FallbackTimeout = TimeSpan.FromSeconds(FallbackTimeoutSeconds)
            };
        }
    }
}