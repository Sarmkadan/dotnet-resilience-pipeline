using System;
using System.Collections.Generic;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Provides validation helpers for <see cref="PolicyComparisonBenchmarks"/> instances.
/// Validates that benchmark configuration values are within expected ranges and not in default/empty states.
/// </summary>
public static class PolicyComparisonBenchmarksValidation
{
    /// <summary>
    /// Validates the specified <see cref="PolicyComparisonBenchmarks"/> instance.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <returns>An enumerable of validation messages. Empty if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this PolicyComparisonBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate retry policy configurations
        ValidateRetryPolicy(value, nameof(value.RetryComparison_Fixed_Strategy), errors);
        ValidateRetryPolicy(value, nameof(value.RetryComparison_Linear_Strategy), errors);
        ValidateRetryPolicy(value, nameof(value.RetryComparison_Exponential_Strategy), errors);
        ValidateRetryPolicy(value, nameof(value.RetryComparison_ExponentialWithJitter_Strategy), errors);

        // Validate circuit breaker configurations
        ValidateCircuitBreakerPolicy(value, nameof(value.CircuitBreakerComparison_LowThreshold_RecordSuccess), errors);
        ValidateCircuitBreakerPolicy(value, nameof(value.CircuitBreakerComparison_HighThreshold_RecordSuccess), errors);
        ValidateCircuitBreakerPolicy(value, nameof(value.CircuitBreakerComparison_ShortDuration_RecordFailure), errors);
        ValidateCircuitBreakerPolicy(value, nameof(value.CircuitBreakerComparison_LongDuration_AttemptReset), errors);
        ValidateCircuitBreakerState(value, nameof(value.CircuitBreakerComparison_GetState_All), errors);
        ValidateCircuitBreakerTrips(value, nameof(value.CircuitBreakerComparison_GetTrips_All), errors);

        // Validate bulkhead configurations
        ValidateBulkheadAcquisition(value, nameof(value.BulkheadComparison_Small_TryAcquireSlot), errors);
        ValidateBulkheadAcquisition(value, nameof(value.BulkheadComparison_Medium_TryAcquireSlot), errors);
        ValidateBulkheadAcquisition(value, nameof(value.BulkheadComparison_Large_TryAcquireSlot), errors);
        ValidateBulkheadUtilization(value, nameof(value.BulkheadComparison_GetUtilization_All), errors);
        ValidateBulkheadQueueAndReject(value, nameof(value.BulkheadComparison_Queue_And_Reject), errors);

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="PolicyComparisonBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to check.</param>
    /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this PolicyComparisonBenchmarks value) => Validate(value).Count == 0;

    /// <summary>
    /// Ensures that the specified <see cref="PolicyComparisonBenchmarks"/> instance is valid.
    /// </summary>
    /// <param name="value">The benchmarks instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the instance is not valid, containing all validation errors.</exception>
    public static void EnsureValid(this PolicyComparisonBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException($"PolicyComparisonBenchmarks instance is not valid. Errors: {string.Join("; ", errors)}");
        }
    }

    private static void ValidateRetryPolicy(PolicyComparisonBenchmarks benchmarks, string propertyName, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        try
        {
            // These are delay values returned by GetNextDelayMs, should be positive and reasonable
            var delay = GetRetryDelay(benchmarks, propertyName);
            if (delay <= 0)
            {
                errors.Add($"{propertyName}: Expected positive delay, got {delay}ms");
            }
            else if (delay > TimeSpan.FromMinutes(5).TotalMilliseconds)
            {
                errors.Add($"{propertyName}: Delay {delay}ms exceeds reasonable maximum of 5 minutes");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"{propertyName}: Failed to validate - {ex.Message}");
        }
    }

    private static long GetRetryDelay(PolicyComparisonBenchmarks benchmarks, string propertyName)
    {
        return propertyName switch
        {
            nameof(PolicyComparisonBenchmarks.RetryComparison_Fixed_Strategy) => benchmarks.RetryComparison_Fixed_Strategy,
            nameof(PolicyComparisonBenchmarks.RetryComparison_Linear_Strategy) => benchmarks.RetryComparison_Linear_Strategy,
            nameof(PolicyComparisonBenchmarks.RetryComparison_Exponential_Strategy) => benchmarks.RetryComparison_Exponential_Strategy,
            nameof(PolicyComparisonBenchmarks.RetryComparison_ExponentialWithJitter_Strategy) => benchmarks.RetryComparison_ExponentialWithJitter_Strategy,
            _ => throw new ArgumentException($"Unknown retry property: {propertyName}")
        };
    }

    private static void ValidateCircuitBreakerPolicy(PolicyComparisonBenchmarks benchmarks, string propertyName, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        // These methods don't return values that need validation, but we ensure they can be called
        try
        {
            _ = propertyName switch
            {
                nameof(PolicyComparisonBenchmarks.CircuitBreakerComparison_LowThreshold_RecordSuccess) => benchmarks.CircuitBreakerComparison_LowThreshold_RecordSuccess(),
                nameof(PolicyComparisonBenchmarks.CircuitBreakerComparison_HighThreshold_RecordSuccess) => benchmarks.CircuitBreakerComparison_HighThreshold_RecordSuccess(),
                nameof(PolicyComparisonBenchmarks.CircuitBreakerComparison_ShortDuration_RecordFailure) => benchmarks.CircuitBreakerComparison_ShortDuration_RecordFailure(),
                nameof(PolicyComparisonBenchmarks.CircuitBreakerComparison_LongDuration_AttemptReset) => benchmarks.CircuitBreakerComparison_LongDuration_AttemptReset(),
                _ => throw new ArgumentException($"Unknown circuit breaker property: {propertyName}")
            };
        }
        catch (Exception ex)
        {
            errors.Add($"{propertyName}: Failed to execute - {ex.Message}");
        }
    }

    private static void ValidateCircuitBreakerState(PolicyComparisonBenchmarks benchmarks, string propertyName, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        try
        {
            var state = benchmarks.CircuitBreakerComparison_GetState_All();
            if (!Enum.IsDefined(typeof(CircuitBreakerPolicy.CircuitState), state))
            {
                errors.Add($"{propertyName}: Invalid circuit state value: {state}");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"{propertyName}: Failed to validate state - {ex.Message}");
        }
    }

    private static void ValidateCircuitBreakerTrips(PolicyComparisonBenchmarks benchmarks, string propertyName, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        try
        {
            var trips = benchmarks.CircuitBreakerComparison_GetTrips_All();
            if (trips < 0)
            {
                errors.Add($"{propertyName}: Trip count cannot be negative, got {trips}");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"{propertyName}: Failed to validate trips - {ex.Message}");
        }
    }

    private static void ValidateBulkheadAcquisition(PolicyComparisonBenchmarks benchmarks, string propertyName, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        try
        {
            var result = propertyName switch
            {
                nameof(PolicyComparisonBenchmarks.BulkheadComparison_Small_TryAcquireSlot) => benchmarks.BulkheadComparison_Small_TryAcquireSlot(),
                nameof(PolicyComparisonBenchmarks.BulkheadComparison_Medium_TryAcquireSlot) => benchmarks.BulkheadComparison_Medium_TryAcquireSlot(),
                nameof(PolicyComparisonBenchmarks.BulkheadComparison_Large_TryAcquireSlot) => benchmarks.BulkheadComparison_Large_TryAcquireSlot(),
                _ => throw new ArgumentException($"Unknown bulkhead property: {propertyName}")
            };

            // Result should be a boolean, no further validation needed
            _ = result;
        }
        catch (Exception ex)
        {
            errors.Add($"{propertyName}: Failed to validate acquisition - {ex.Message}");
        }
    }

    private static void ValidateBulkheadUtilization(PolicyComparisonBenchmarks benchmarks, string propertyName, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        try
        {
            var utilization = benchmarks.BulkheadComparison_GetUtilization_All();
            if (utilization < 0.0 || utilization > 100.0)
            {
                errors.Add($"{propertyName}: Utilization percentage must be between 0 and 100, got {utilization:F2}%");
            }
        }
        catch (Exception ex)
        {
            errors.Add($"{propertyName}: Failed to validate utilization - {ex.Message}");
        }
    }

    private static void ValidateBulkheadQueueAndReject(PolicyComparisonBenchmarks benchmarks, string propertyName, List<string> errors)
    {
        ArgumentNullException.ThrowIfNull(benchmarks);
        ArgumentException.ThrowIfNullOrEmpty(propertyName);

        try
        {
            var result = benchmarks.BulkheadComparison_Queue_And_Reject();
            // Result should be a boolean, no further validation needed
            _ = result;
        }
        catch (Exception ex)
        {
            errors.Add($"{propertyName}: Failed to validate queue behavior - {ex.Message}");
        }
    }
}