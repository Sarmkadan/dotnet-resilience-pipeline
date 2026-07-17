using System;
using System.Collections.Generic;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Validation helpers for <see cref="BulkheadBenchmarks"/> benchmark class
/// </summary>
internal static class BulkheadBenchmarksValidation
{
    /// <summary>
    /// Validates that a <see cref="BulkheadBenchmarks"/> instance is properly configured for benchmarking.
    /// </summary>
    /// <param name="value">The benchmark instance to validate</param>
    /// <returns>A list of validation messages (empty if valid)</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this BulkheadBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate that the bulkhead policy is initialized
        if (value._bulkheadPolicy == null)
        {
            errors.Add("BulkheadPolicy field '_bulkheadPolicy' is null. Setup() method must be called first.");
        }
        else
        {
            // Validate bulkhead policy configuration
            if (value._bulkheadPolicy.MaxParallelization <= 0)
            {
                errors.Add("BulkheadPolicy.MaxParallelization must be greater than 0");
            }

            if (value._bulkheadPolicy.MaxQueueLength < 0)
            {
                errors.Add("BulkheadPolicy.MaxQueueLength cannot be negative");
            }
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether the specified <see cref="BulkheadBenchmarks"/> instance is valid for benchmarking.
    /// </summary>
    /// <param name="value">The benchmark instance to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static bool IsValid(this BulkheadBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return value.Validate().Count == 0;
    }

    /// <summary>
    /// Ensures that the specified <see cref="BulkheadBenchmarks"/> instance is valid for benchmarking.
    /// </summary>
    /// <param name="value">The benchmark instance to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when the benchmark instance is invalid, containing the validation errors</exception>
    public static void EnsureValid(this BulkheadBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"BulkheadBenchmarks instance is invalid. Validation errors: {string.Join("; ", errors)}",
                nameof(value));
        }
    }
}