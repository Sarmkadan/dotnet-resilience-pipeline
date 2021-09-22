using System;
using System.Collections.Generic;

namespace DotNetResiliencePipeline.Services
{
    public static class ResiliencyPipelineServiceValidation
    {
        public static IReadOnlyList<string> Validate(this ResiliencyPipelineService value)
        {
            var problems = new List<string>();

            if (value == null)
            {
                problems.Add("ResiliencyPipelineService instance cannot be null.");
                return problems;
            }

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

            return problems;
        }

        public static bool IsValid(this ResiliencyPipelineService value)
        {
            return value.Validate().Count == 0;
        }

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
