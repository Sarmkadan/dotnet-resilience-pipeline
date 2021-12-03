#nullable enable

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetResiliencePipeline.Tests
{
    /// <summary>
    /// Provides validation extension methods for <see cref="ResiliencyPipelineServiceTests"/> instances.
    /// </summary>
    public static class ResiliencyPipelineServiceTestsValidation
    {
        /// <summary>
        /// Validates the public members of a <see cref="ResiliencyPipelineServiceTests"/> instance.
        /// Since the test class only contains test methods and no state, this validation
        /// always returns an empty list, indicating no problems.
        /// </summary>
        /// <param name="value">The test instance to validate. Cannot be <see langword="null"/>.</param>
        /// <returns>A read-only list of human-readable validation problems.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static IReadOnlyList<string> Validate(this ResiliencyPipelineServiceTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            // The test class does not expose any data members that can be invalid.
            // Returning an empty list signals that the instance is considered valid.
            return Array.Empty<string>();
        }

        /// <summary>
        /// Determines whether the supplied <see cref="ResiliencyPipelineServiceTests"/> instance is valid.
        /// </summary>
        /// <param name="value">The test instance to check. Cannot be <see langword="null"/>.</param>
        /// <returns><c>true</c> if no validation problems were found; otherwise <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        public static bool IsValid(this ResiliencyPipelineServiceTests value)
        {
            ArgumentNullException.ThrowIfNull(value);
            return !value.Validate().Any();
        }

        /// <summary>
        /// Ensures that the supplied <see cref="ResiliencyPipelineServiceTests"/> instance is valid.
        /// Throws an <see cref="ArgumentException"/> if any validation problems are detected.
        /// </summary>
        /// <param name="value">The test instance to validate. Cannot be <see langword="null"/>.</param>
        /// <exception cref="ArgumentNullException"><paramref name="value"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException">Thrown when validation problems are present.</exception>
        public static void EnsureValid(this ResiliencyPipelineServiceTests value)
        {
            ArgumentNullException.ThrowIfNull(value);

            var problems = value.Validate();
            if (problems.Any())
            {
                var message = $"ResiliencyPipelineServiceTests instance is invalid: {string.Join("; ", problems)}";
                throw new ArgumentException(message, nameof(value));
            }
        }
    }
}