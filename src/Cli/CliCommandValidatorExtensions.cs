#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Cli;

/// <summary>
/// Extension methods for <see cref="ValidationResult"/> that query its errors and warnings,
/// build summaries, and convert a failed result into a <see cref="ValidationException"/>.
/// </summary>
public static class CliCommandValidatorExtensions
{
    /// <summary>
    /// Determines whether the validation result contains at least one warning.
    /// </summary>
    /// <param name="result">The validation result to inspect.</param>
    /// <returns><c>true</c> if <see cref="ValidationResult.Warnings"/> is not empty; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public static bool HasWarnings(this ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Warnings.Count > 0;
    }

    /// <summary>
    /// Determines whether the validation result contains at least one error.
    /// </summary>
    /// <param name="result">The validation result to inspect.</param>
    /// <returns><c>true</c> if <see cref="ValidationResult.Errors"/> is not empty; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public static bool HasErrors(this ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        return result.Errors.Count > 0;
    }

    /// <summary>
    /// Returns every message from the validation result: all errors first, followed by all warnings,
    /// each in its original order.
    /// </summary>
    /// <param name="result">The validation result to read messages from.</param>
    /// <returns>A new read-only list containing the errors and warnings. The list is empty when there are none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public static IReadOnlyList<string> GetAllMessages(this ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var messages = new List<string>(result.Errors.Count + result.Warnings.Count);
        messages.AddRange(result.Errors);
        messages.AddRange(result.Warnings);

        return messages;
    }

    /// <summary>
    /// Builds a one-line summary of the validation outcome, such as
    /// <c>Invalid: 2 error(s), 1 warning(s)</c>.
    /// </summary>
    /// <param name="result">The validation result to summarize.</param>
    /// <returns>A single-line summary stating whether validation passed and how many errors and warnings were reported.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    public static string ToSummaryString(this ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        var status = IsFailed(result) ? "Invalid" : "Valid";

        return $"{status}: {result.Errors.Count} error(s), {result.Warnings.Count} warning(s)";
    }

    /// <summary>
    /// Throws a <see cref="ValidationException"/> when the validation result is not valid.
    /// Each error is recorded in <see cref="ValidationException.ValidationErrors"/> under a key of the form <c>Error[n]</c>.
    /// </summary>
    /// <param name="result">The validation result to check.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="result"/> is null.</exception>
    /// <exception cref="ValidationException">Thrown when <see cref="ValidationResult.IsValid"/> is false or the result contains errors.</exception>
    public static void ThrowIfInvalid(this ValidationResult result)
    {
        ArgumentNullException.ThrowIfNull(result);

        if (!IsFailed(result))
            return;

        var errors = new Dictionary<string, string>(result.Errors.Count);
        for (var i = 0; i < result.Errors.Count; i++)
            errors[$"Error[{i}]"] = result.Errors[i];

        var message = result.Errors.Count > 0
            ? $"Validation failed: {string.Join("; ", result.Errors)}"
            : "Validation failed.";

        throw new ValidationException(message, errors);
    }

    /// <summary>
    /// Determines whether the result should be treated as failed, either by its <c>IsValid</c> flag or by the presence of errors.
    /// </summary>
    private static bool IsFailed(ValidationResult result) => !result.IsValid || result.Errors.Count > 0;
}
