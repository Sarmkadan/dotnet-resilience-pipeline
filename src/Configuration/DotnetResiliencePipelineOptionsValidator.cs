#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.ComponentModel.DataAnnotations;
using Microsoft.Extensions.Options;
using ValidateOptionsResult = Microsoft.Extensions.Options.ValidateOptionsResult;

namespace DotNetResiliencePipeline.Configuration;

/// <summary>
/// Validates DotnetResiliencePipelineOptions configuration.
/// </summary>
public sealed class DotnetResiliencePipelineOptionsValidator : IValidateOptions<DotnetResiliencePipelineOptions>
{
    private const int MinFailureThreshold = 1;
    private const int MaxFailureThreshold = 1000;
    private const int MinOpenDurationSeconds = 1;
    private const int MaxOpenDurationSeconds = 3600;
    private const int MinSuccessThresholdInHalfOpen = 1;
    private const int MaxSuccessThresholdInHalfOpen = 100;
    private const int MinRetries = 0;
    private const int MaxRetries = 20;
    private const int MinInitialDelayMs = 1;
    private const int MaxInitialDelayMs = 10000;
    private const int MinAllowedMaxDelayMs = 1;
    private const int MaxAllowedMaxDelayMs = 300000;
    private const double MinBackoffMultiplier = 1.0;
    private const double MaxBackoffMultiplier = 10.0;
    private const double MinJitterFactor = 0.0;
    private const double MaxJitterFactor = 1.0;
    private const int MinTimeoutSeconds = 1;
    private const int MaxTimeoutSeconds = 300;
    private const int MinParallelization = 1;
    private const int MaxParallelization = 1000;
    private const int MinQueueLength = 0;
    private const int MaxQueueLength = 10000;
    private const int MinFallbackTimeoutSeconds = 1;
    private const int MaxFallbackTimeoutSeconds = 60;

    /// <summary>
    /// Validates the configuration options.
    /// </summary>
    /// <param name="name">The name of the options instance being validated.</param>
    /// <param name="options">The options instance.</param>
    /// <returns>A ValidateOptionsResult indicating whether validation succeeded or failed.</returns>
    public ValidateOptionsResult Validate(string? name, DotnetResiliencePipelineOptions options)
    {
        if (options is null)
        {
            return ValidateOptionsResult.Fail("Configuration options cannot be null");
        }

        var validationResults = new List<ValidationResult>();
        var context = new ValidationContext(options);

        if (!Validator.TryValidateObject(options, context, validationResults, validateAllProperties: true))
        {
            var errors = validationResults.Select(r => r.ErrorMessage).Where(m => m != null);
            return ValidateOptionsResult.Fail(string.Join(" ", errors));
        }

        // Validate nested options
        var circuitBreakerValidation = ValidateCircuitBreaker(options.CircuitBreaker);
        if (circuitBreakerValidation.Failed)
        {
            return circuitBreakerValidation;
        }

        var retryValidation = ValidateRetry(options.Retry);
        if (retryValidation.Failed)
        {
            return retryValidation;
        }

        var timeoutValidation = ValidateTimeout(options.Timeout);
        if (timeoutValidation.Failed)
        {
            return timeoutValidation;
        }

        var bulkheadValidation = ValidateBulkhead(options.Bulkhead);
        if (bulkheadValidation.Failed)
        {
            return bulkheadValidation;
        }

        var fallbackValidation = ValidateFallback(options.Fallback);
        if (fallbackValidation.Failed)
        {
            return fallbackValidation;
        }

        return ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateCircuitBreaker(DotnetResiliencePipelineOptions.CircuitBreakerOptions options)
    {
        if (options.FailureThreshold < MinFailureThreshold || options.FailureThreshold > MaxFailureThreshold)
        {
            return ValidateOptionsResult.Fail($"CircuitBreaker.FailureThreshold must be between {MinFailureThreshold} and {MaxFailureThreshold}");
        }

        if (options.OpenDurationSeconds < MinOpenDurationSeconds || options.OpenDurationSeconds > MaxOpenDurationSeconds)
        {
            return ValidateOptionsResult.Fail($"CircuitBreaker.OpenDurationSeconds must be between {MinOpenDurationSeconds} and {MaxOpenDurationSeconds}");
        }

        if (options.SuccessThresholdInHalfOpen < MinSuccessThresholdInHalfOpen || options.SuccessThresholdInHalfOpen > MaxSuccessThresholdInHalfOpen)
        {
            return ValidateOptionsResult.Fail($"CircuitBreaker.SuccessThresholdInHalfOpen must be between {MinSuccessThresholdInHalfOpen} and {MaxSuccessThresholdInHalfOpen}");
        }

        return ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateRetry(DotnetResiliencePipelineOptions.RetryOptions options)
    {
        if (options.MaxRetries < MinRetries || options.MaxRetries > MaxRetries)
        {
            return ValidateOptionsResult.Fail($"Retry.MaxRetries must be between {MinRetries} and {MaxRetries}");
        }

        if (options.InitialDelayMs < MinInitialDelayMs || options.InitialDelayMs > MaxInitialDelayMs)
        {
            return ValidateOptionsResult.Fail($"Retry.InitialDelayMs must be between {MinInitialDelayMs} and {MaxInitialDelayMs}");
        }

        if (options.MaxDelayMs < MinAllowedMaxDelayMs || options.MaxDelayMs > MaxAllowedMaxDelayMs)
        {
            return ValidateOptionsResult.Fail($"Retry.MaxDelayMs must be between {MinAllowedMaxDelayMs} and {MaxAllowedMaxDelayMs}");
        }

        if (options.MaxDelayMs < options.InitialDelayMs)
        {
            return ValidateOptionsResult.Fail("Retry.MaxDelayMs must be greater than or equal to Retry.InitialDelayMs");
        }

        if (options.BackoffMultiplier < MinBackoffMultiplier || options.BackoffMultiplier > MaxBackoffMultiplier)
        {
            return ValidateOptionsResult.Fail(FormattableString.Invariant($"Retry.BackoffMultiplier must be between {MinBackoffMultiplier:F1} and {MaxBackoffMultiplier:F1}"));
        }

        if (options.JitterFactor < MinJitterFactor || options.JitterFactor > MaxJitterFactor)
        {
            return ValidateOptionsResult.Fail(FormattableString.Invariant($"Retry.JitterFactor must be between {MinJitterFactor:F1} and {MaxJitterFactor:F1}"));
        }

        return ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateTimeout(DotnetResiliencePipelineOptions.TimeoutOptions options)
    {
        if (options.TimeoutSeconds < MinTimeoutSeconds || options.TimeoutSeconds > MaxTimeoutSeconds)
        {
            return ValidateOptionsResult.Fail($"Timeout.TimeoutSeconds must be between {MinTimeoutSeconds} and {MaxTimeoutSeconds}");
        }

        return ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateBulkhead(DotnetResiliencePipelineOptions.BulkheadOptions options)
    {
        if (options.MaxParallelization < MinParallelization || options.MaxParallelization > MaxParallelization)
        {
            return ValidateOptionsResult.Fail($"Bulkhead.MaxParallelization must be between {MinParallelization} and {MaxParallelization}");
        }

        if (options.MaxQueueLength < MinQueueLength || options.MaxQueueLength > MaxQueueLength)
        {
            return ValidateOptionsResult.Fail($"Bulkhead.MaxQueueLength must be between {MinQueueLength} and {MaxQueueLength}");
        }

        return ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateFallback(DotnetResiliencePipelineOptions.FallbackOptions options)
    {
        if (options.FallbackTimeoutSeconds < MinFallbackTimeoutSeconds || options.FallbackTimeoutSeconds > MaxFallbackTimeoutSeconds)
        {
            return ValidateOptionsResult.Fail($"Fallback.FallbackTimeoutSeconds must be between {MinFallbackTimeoutSeconds} and {MaxFallbackTimeoutSeconds}");
        }

        return ValidateOptionsResult.Success;
    }
}
