using System;
using System.Collections.Generic;
using System.Linq;

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
        /// <returns>True if the field has validation errors; otherwise, false.</returns>
        public static bool HasErrorFor(this ValidationException exception, string fieldName)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("Field name cannot be null or whitespace.", nameof(fieldName));
            }

            return exception.ValidationErrors.ContainsKey(fieldName);
        }

        /// <summary>
        /// Gets the error message for the specified field, or null if the field has no errors.
        /// </summary>
        /// <param name="exception">The validation exception.</param>
        /// <param name="fieldName">Name of the field.</param>
        /// <returns>The error message, or null if no error exists for the field.</returns>
        public static string GetErrorMessage(this ValidationException exception, string fieldName)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (string.IsNullOrWhiteSpace(fieldName))
            {
                throw new ArgumentException("Field name cannot be null or whitespace.", nameof(fieldName));
            }

            if (exception.ValidationErrors.TryGetValue(fieldName, out var errorMessage))
            {
                return errorMessage;
            }

            return null;
        }

        /// <summary>
        /// Gets all field names that have validation errors.
        /// </summary>
        /// <param name="exception">The validation exception.</param>
        /// <returns>An enumerable of field names with errors.</returns>
        public static IEnumerable<string> GetErrorFields(this ValidationException exception)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            return exception.ValidationErrors.Keys;
        }

        /// <summary>
        /// Creates a new ValidationException with additional validation errors merged into the existing ones.
        /// </summary>
        /// <param name="exception">The original validation exception.</param>
        /// <param name="additionalErrors">Dictionary of additional validation errors to merge.</param>
        /// <returns>A new ValidationException with merged errors.</returns>
        public static ValidationException WithAdditionalErrors(this ValidationException exception, Dictionary<string, string> additionalErrors)
        {
            if (exception is null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

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