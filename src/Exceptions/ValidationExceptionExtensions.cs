using System;
using System.Collections.Generic;

namespace DotNetResiliencePipeline.Exceptions
{
    /// <summary>
    /// Provides extension methods for <see cref="ValidationException"/> to simplify common validation error scenarios.
    /// </summary>
    public static class ValidationExceptionExtensions
    {
        /// <summary>
        /// Determines whether the validation exception contains any errors for the specified field.
        /// </summary>
        /// <param name="exception">The validation exception.</param>
        /// <param name="fieldName">Name of the field to check.</param>
        /// <returns><see langword="true"/> if the field has validation errors; otherwise, <see langword="false"/>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="fieldName"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
        public static bool HasErrorFor(this ValidationException exception, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            return exception.ValidationErrors.ContainsKey(fieldName);
        }

        /// <summary>
        /// Gets the error message for the specified field, or <see langword="null"/> if the field has no errors.
        /// </summary>
        /// <param name="exception">The validation exception.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>The error message, or <see langword="null"/> if no error exists for the field.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        /// <exception cref="ArgumentException"><paramref name="fieldName"/> is <see langword="null"/>, empty, or consists only of whitespace.</exception>
        public static string? GetErrorMessage(this ValidationException exception, string fieldName)
        {
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentException.ThrowIfNullOrWhiteSpace(fieldName);

            return exception.ValidationErrors.TryGetValue(fieldName, out var errorMessage)
                ? errorMessage
                : null;
        }

        /// <summary>
        /// Gets all field names that have validation errors.
        /// </summary>
        /// <param name="exception">The validation exception.</param>
        /// <returns>An enumerable of field names with errors.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        public static IEnumerable<string> GetErrorFields(this ValidationException exception)
        {
            ArgumentNullException.ThrowIfNull(exception);

            return exception.ValidationErrors.Keys;
        }

        /// <summary>
        /// Creates a new ValidationException with additional validation errors merged into the existing ones.
        /// </summary>
        /// <param name="exception">The original validation exception.</param>
        /// <param name="additionalErrors">Dictionary of additional validation errors to merge.</param>
        /// <returns>A new ValidationException with merged errors, or the original exception if <paramref name="additionalErrors"/> is <see langword="null"/> or empty.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="exception"/> is <see langword="null"/>.</exception>
        public static ValidationException WithAdditionalErrors(this ValidationException exception, Dictionary<string, string>? additionalErrors)
        {
            ArgumentNullException.ThrowIfNull(exception);

            if (additionalErrors is null || additionalErrors.Count == 0)
            {
                return exception;
            }

            var mergedErrors = new Dictionary<string, string>(exception.ValidationErrors);
            foreach (var kvp in additionalErrors)
            {
                mergedErrors[kvp.Key] = kvp.Value;
            }

            return new ValidationException("Validation failed with additional errors", mergedErrors);
        }
    }
}