#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Options;
using ValidateOptionsResult = Microsoft.Extensions.Options.ValidateOptionsResult;

namespace DotNetResiliencePipeline.Configuration;

/// <summary>
/// Validates DotnetResiliencePipelineOptions configuration.
/// </summary>
public sealed class DotnetResiliencePipelineOptionsValidator : IValidateOptions<DotnetResiliencePipelineOptions>
{
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
        if (options.FailureThreshold < 1 || options.FailureThreshold > 1000)
        {
            return ValidateOptionsResult.Fail("CircuitBreaker.FailureThreshold must be between 1 and 1000");
        }

        if (options.OpenDurationSeconds < 1 || options.OpenDurationSeconds > 3600)
        {
            return ValidateOptionsResult.Fail("CircuitBreaker.OpenDurationSeconds must be between 1 and 3600");
        }

        if (options.SuccessThresholdInHalfOpen < 1 || options.SuccessThresholdInHalfOpen > 100)
        {
            return ValidateOptionsResult.Fail("CircuitBreaker.SuccessThresholdInHalfOpen must be between 1 and 100");
        }

        return ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateRetry(DotnetResiliencePipelineOptions.RetryOptions options)
    {
        if (options.MaxRetries < 0 || options.MaxRetries > 20)
        {
            return ValidateOptionsResult.Fail("Retry.MaxRetries must be between 0 and 20");
        }

        if (options.InitialDelayMs < 1 || options.InitialDelayMs > 10000)
        {
            return ValidateOptionsResult.Fail("Retry.InitialDelayMs must be between 1 and 10000");
        }

        if (options.MaxDelayMs < 1 || options.MaxDelayMs > 300000)
        {
            return ValidateOptionsResult.Fail("Retry.MaxDelayMs must be between 1 and 300000");
        }

        if (options.MaxDelayMs < options.InitialDelayMs)
        {
            return ValidateOptionsResult.Fail("Retry.MaxDelayMs must be greater than or equal to Retry.InitialDelayMs");
        }

        if (options.BackoffMultiplier < 1.0 || options.BackoffMultiplier > 10.0)
        {
            return ValidateOptionsResult.Fail("Retry.BackoffMultiplier must be between 1.0 and 10.0");
        }

        if (options.JitterFactor < 0.0 || options.JitterFactor > 1.0)
        {
            return ValidateOptionsResult.Fail("Retry.JitterFactor must be between 0.0 and 1.0");
        }

        return ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateTimeout(DotnetResiliencePipelineOptions.TimeoutOptions options)
    {
        if (options.TimeoutSeconds < 1 || options.TimeoutSeconds > 300)
        {
            return ValidateOptionsResult.Fail("Timeout.TimeoutSeconds must be between 1 and 300");
        }

        return ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateBulkhead(DotnetResiliencePipelineOptions.BulkheadOptions options)
    {
        if (options.MaxParallelization < 1 || options.MaxParallelization > 1000)
        {
            return ValidateOptionsResult.Fail("Bulkhead.MaxParallelization must be between 1 and 1000");
        }

        if (options.MaxQueueLength < 0 || options.MaxQueueLength > 10000)
        {
            return ValidateOptionsResult.Fail("Bulkhead.MaxQueueLength must be between 0 and 10000");
        }

        return ValidateOptionsResult.Success;
    }

    private ValidateOptionsResult ValidateFallback(DotnetResiliencePipelineOptions.FallbackOptions options)
    {
        if (options.FallbackTimeoutSeconds < 1 || options.FallbackTimeoutSeconds > 60)
        {
            return ValidateOptionsResult.Fail("Fallback.FallbackTimeoutSeconds must be between 1 and 60");
        }

        return ValidateOptionsResult.Success;
    }
}