using System;
using System.Collections.Generic;

namespace DotNetResiliencePipeline.Services
{
    /// <summary>
    /// Provides a collection of extension methods that validate <see cref="ResiliencyPipelineService"/> objects.
    /// </summary>
    /// <remarks>
    /// The methods examine the service's identifier, timestamps and execution counters and return
    /// human‑readable error messages for any rule violations. They are intended for use in
    /// configuration or health‑check scenarios where a pipeline definition must be verified before
    /// being registered or executed.
    /// </remarks>
    public static class ResiliencyPipelineServiceValidation
    {
        /// <summary>
        /// Validates the state of a <see cref="ResiliencyPipelineService"/> instance and returns any problems found.
        /// </summary>
        /// <param name="value">The <see cref="ResiliencyPipelineService"/> instance to validate.</param>
        /// <returns>
        /// A read‑only list of error messages describing each validation failure. The list is empty when the
        /// instance satisfies all validation rules.
        /// </returns>
        /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <c>null</c>.</exception>
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
        /// Determines whether the specified <see cref="ResiliencyPipelineService"/> instance passes all validation checks.
        /// </summary>
        /// <param name="value">The instance to evaluate.</param>
        /// <returns><c>true</c> if the instance has no validation problems; otherwise, <c>false</c>.</returns>
        public static bool IsValid(this ResiliencyPipelineService value) => value.Validate().Count == 0;

        /// <summary>
        /// Validates the supplied <see cref="ResiliencyPipelineService"/> and throws an <see cref="ArgumentException"/>
        /// if any validation rule fails.
        /// </summary>
        /// <param name="value">The instance to validate.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentException">Thrown when one or more validation problems are detected; the exception
        /// message contains a concatenated list of the individual error messages.</exception>
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
