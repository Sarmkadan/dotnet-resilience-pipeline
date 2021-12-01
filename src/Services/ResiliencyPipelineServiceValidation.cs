using System;
using System.Collections.Generic;

namespace DotNetResiliencePipeline.Services
{
    /// <summary>
    /// Provides validation methods for <see cref="ResiliencyPipelineService"/> instances.
    /// </summary>
    public static class ResiliencyPipelineServiceValidation
    {
        /// <summary>
        /// Validates a <see cref="ResiliencyPipelineService"/> instance and returns a list of validation problems.
        /// </summary>
        /// <param name="value">The service instance to validate.</param>
        /// <returns>A read-only list of validation error messages. Empty if valid.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        public static IReadOnlyList<string> Validate(this ResiliencyPipelineService value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = new List<string>();

            if (string.IsNullOrWhiteSpace(value.PipelineId))
            {
                problems.Add("PipelineId cannot be null or empty.");
            }

            if (value.CreatedAt == default)
            {
                problems.Add("CreatedAt cannot be the default DateTime value.");
            }

            if (value.TotalExecutions < 0)
            {
                problems.Add("TotalExecutions cannot be negative.");
            }

            if (value.SuccessfulExecutions < 0)
            {
                problems.Add("SuccessfulExecutions cannot be negative.");
            }

            if (value.FailedExecutions < 0)
            {
                problems.Add("FailedExecutions cannot be negative.");
            }

            // Validate that successful + failed executions don't exceed total executions
            if (value.TotalExecutions > 0 && value.SuccessfulExecutions + value.FailedExecutions > value.TotalExecutions)
            {
                problems.Add("SuccessfulExecutions + FailedExecutions cannot exceed TotalExecutions.");
            }

            // Validate that failed executions don't exceed total executions
            if (value.TotalExecutions > 0 && value.FailedExecutions > value.TotalExecutions)
            {
                problems.Add("FailedExecutions cannot exceed TotalExecutions.");
            }

            return problems.AsReadOnly();
        }

        /// <summary>
        /// Determines whether the specified <see cref="ResiliencyPipelineService"/> is valid.
        /// </summary>
        /// <param name="value">The service instance to check.</param>
        /// <returns><see langword="true"/> if valid; otherwise, <see langword="false"/>.</returns>
        public static bool IsValid(this ResiliencyPipelineService value) => value.Validate().Count == 0;

        /// <summary>
        /// Validates a <see cref="ResiliencyPipelineService"/> instance and throws an <see cref="ArgumentException"/> if invalid.
        /// </summary>
        /// <param name="value">The service instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when validation fails.</exception>
        public static void EnsureValid(this ResiliencyPipelineService value)
        {
            var problems = value.Validate();
            if (problems.Count > 0)
            {
                throw new ArgumentException($"ResiliencyPipelineService validation failed: {string.Join("; ", problems)}");
            }
        }
    }
}