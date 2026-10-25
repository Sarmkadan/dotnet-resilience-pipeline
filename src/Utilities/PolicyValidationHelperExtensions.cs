#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Convenience extension methods over <see cref="ValidationReport"/> and <see cref="PolicyValidationHelper"/>.
/// All methods delegate to the existing public API and add argument guards.
/// </summary>
public static class PolicyValidationHelperExtensions
{
    /// <summary>
    /// Determines whether the report contains at least one warning.
    /// </summary>
    /// <param name="report">The validation report to inspect.</param>
    /// <returns><c>true</c> if <see cref="ValidationReport.Warnings"/> is not empty; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is null.</exception>
    public static bool HasWarnings(this ValidationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.Warnings.Count > 0;
    }

    /// <summary>
    /// Determines whether the report contains at least one suggestion.
    /// </summary>
    /// <param name="report">The validation report to inspect.</param>
    /// <returns><c>true</c> if <see cref="ValidationReport.Suggestions"/> is not empty; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is null.</exception>
    public static bool HasSuggestions(this ValidationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        return report.Suggestions.Count > 0;
    }

    /// <summary>
    /// Combines two reports into a new report. Errors, warnings and suggestions are concatenated in order.
    /// </summary>
    /// <remarks>
    /// Neither input is modified. The result keeps the policy identity (<see cref="ValidationReport.PolicyId"/>
    /// and <see cref="ValidationReport.PolicyName"/>) of <paramref name="report"/>.
    /// </remarks>
    /// <param name="report">The primary report. Its identity is used for the result.</param>
    /// <param name="other">The report whose messages are appended after the primary report's messages.</param>
    /// <returns>A new <see cref="ValidationReport"/> containing the messages of both reports.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> or <paramref name="other"/> is null.</exception>
    public static ValidationReport Merge(this ValidationReport report, ValidationReport other)
    {
        ArgumentNullException.ThrowIfNull(report);
        ArgumentNullException.ThrowIfNull(other);

        var merged = new ValidationReport
        {
            PolicyId = report.PolicyId,
            PolicyName = report.PolicyName
        };

        merged.Errors.AddRange(report.Errors);
        merged.Errors.AddRange(other.Errors);
        merged.Warnings.AddRange(report.Warnings);
        merged.Warnings.AddRange(other.Warnings);
        merged.Suggestions.AddRange(report.Suggestions);
        merged.Suggestions.AddRange(other.Suggestions);

        return merged;
    }

    /// <summary>
    /// Throws a <see cref="ValidationException"/> if the report contains any errors.
    /// </summary>
    /// <remarks>
    /// Warnings and suggestions never cause an exception. Each error is stored in
    /// <see cref="ValidationException.ValidationErrors"/> under a key of the form <c>Error[n]</c>,
    /// because policy errors are plain messages without a field name.
    /// </remarks>
    /// <param name="report">The validation report to check.</param>
    /// <exception cref="ArgumentNullException"><paramref name="report"/> is null.</exception>
    /// <exception cref="ValidationException">The report has one or more errors.</exception>
    public static void ThrowIfInvalid(this ValidationReport report)
    {
        ArgumentNullException.ThrowIfNull(report);

        if (report.IsValid)
            return;

        var errors = new Dictionary<string, string>();
        for (var i = 0; i < report.Errors.Count; i++)
            errors[$"Error[{i}]"] = report.Errors[i];

        var message = $"Policy '{report.PolicyName}' ({report.PolicyId}) is invalid: {string.Join("; ", report.Errors)}";

        throw new ValidationException(message, errors);
    }

    /// <summary>
    /// Validates each policy with <see cref="PolicyValidationHelper.ValidatePolicy"/>.
    /// </summary>
    /// <remarks>
    /// The method is eager: every policy is validated before the method returns, so any
    /// exception from the underlying validation surfaces at the call site.
    /// </remarks>
    /// <param name="policies">The policies to validate.</param>
    /// <returns>One report per policy, in the same order as <paramref name="policies"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="policies"/> is null, or one of its elements is null.</exception>
    public static IReadOnlyList<ValidationReport> ValidateAll(this IEnumerable<ResiliencyPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(policies);

        return policies
            .Select(PolicyValidationHelper.ValidatePolicy)
            .ToList();
    }
}
