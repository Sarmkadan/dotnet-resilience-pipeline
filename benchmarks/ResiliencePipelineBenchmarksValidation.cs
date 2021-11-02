using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotNetResiliencePipeline.Benchmarks;

/// <summary>
/// Provides validation helpers for <see cref="ResiliencePipelineBenchmarks"/> instances
/// </summary>
public static class ResiliencePipelineBenchmarksValidation
{
    /// <summary>
    /// Validates a <see cref="ResiliencePipelineBenchmarks"/> instance for common issues
    /// </summary>
    /// <param name="value">The instance to validate</param>
    /// <returns>An empty list if valid; otherwise, a list of human-readable validation errors</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static IReadOnlyList<string> Validate(this ResiliencePipelineBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        // Validate Setup method exists and is public
        if (value.Setup.GetMethodInfo() is null)
        {
            errors.Add("The Setup method is not accessible or does not exist.");
        }

        // Validate benchmark methods exist and are public
        ValidateBenchmarkMethod(errors, nameof(ResiliencePipelineBenchmarks.ResiliencePipeline_Execute_Successful_Operation),
            value.ResiliencePipeline_Execute_Successful_Operation);
        ValidateBenchmarkMethod(errors, nameof(ResiliencePipelineBenchmarks.ResiliencePipeline_Execute_With_CircuitBreaker),
            value.ResiliencePipeline_Execute_With_CircuitBreaker);
        ValidateBenchmarkMethod(errors, nameof(ResiliencePipelineBenchmarks.ResiliencePipeline_Execute_With_Retry),
            value.ResiliencePipeline_Execute_With_Retry);
        ValidateBenchmarkMethod(errors, nameof(ResiliencePipelineBenchmarks.ResiliencePipeline_Execute_With_Timeout),
            value.ResiliencePipeline_Execute_With_Timeout);
        ValidateBenchmarkMethod(errors, nameof(ResiliencePipelineBenchmarks.ResiliencePipeline_Execute_With_Bulkhead),
            value.ResiliencePipeline_Execute_With_Bulkhead);
        ValidateBenchmarkMethod(errors, nameof(ResiliencePipelineBenchmarks.ResiliencePipeline_Execute_With_Fallback),
            value.ResiliencePipeline_Execute_With_Fallback);
        ValidateBenchmarkMethod(errors, nameof(ResiliencePipelineBenchmarks.ResiliencePipeline_Execute_Full_Pipeline),
            value.ResiliencePipeline_Execute_Full_Pipeline);
        ValidateBenchmarkMethod(errors, nameof(ResiliencePipelineBenchmarks.ResiliencePipeline_Get_Statistics),
            value.ResiliencePipeline_Get_Statistics);
        ValidateBenchmarkMethod(errors, nameof(ResiliencePipelineBenchmarks.ResiliencePipeline_Execute_Multiple_Operations_Parallel),
            value.ResiliencePipeline_Execute_Multiple_Operations_Parallel);

        return errors;
    }

    /// <summary>
    /// Determines whether a <see cref="ResiliencePipelineBenchmarks"/> instance is valid
    /// </summary>
    /// <param name="value">The instance to check</param>
    /// <returns>True if valid; otherwise, false</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    public static bool IsValid(this ResiliencePipelineBenchmarks value)
    {
        return Validate(value).Count == 0;
    }

    /// <summary>
    /// Ensures that a <see cref="ResiliencePipelineBenchmarks"/> instance is valid, throwing an exception if not
    /// </summary>
    /// <param name="value">The instance to validate</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null</exception>
    /// <exception cref="ArgumentException">Thrown when the instance is not valid, with a detailed message listing all validation errors</exception>
    public static void EnsureValid(this ResiliencePipelineBenchmarks value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = Validate(value);
        if (errors.Count > 0)
        {
            throw new ArgumentException(
                $"The ResiliencePipelineBenchmarks instance is not valid. Validation errors:{Environment.NewLine}-
{string.Join($"{Environment.NewLine}- ", errors)}");
        }
    }

    private static void ValidateBenchmarkMethod<TDelegate>(List<string> errors, string methodName, TDelegate method) where TDelegate : Delegate
    {
        if (method is null)
        {
            errors.Add($"The benchmark method '{methodName}' is null or not accessible.");
        }
    }
}
