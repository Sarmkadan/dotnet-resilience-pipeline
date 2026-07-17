#nullable enable

using System.Globalization;

namespace DotNetResiliencePipeline.Data;

/// <summary>
/// Provides validation helpers for <see cref="ExecutionRecord"/> instances.
/// </summary>
public static class ExecutionRecordValidation
{
    /// <summary>
    /// Validates an execution record and returns a list of human-readable problems.
    /// </summary>
    /// <param name="value">The execution record to validate.</param>
    /// <returns>A list of validation problems, or an empty list if valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this ExecutionRecord value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = new List<string>();

        if (string.IsNullOrWhiteSpace(value.ExecutionId))
        {
            errors.Add("Execution ID cannot be null or whitespace.");
        }
        else if (!Guid.TryParse(value.ExecutionId, out _))
        {
            errors.Add("Execution ID must be a valid GUID.");
        }

        if (string.IsNullOrWhiteSpace(value.PolicyName))
        {
            errors.Add("Policy name cannot be null or whitespace.");
        }

        if (string.IsNullOrWhiteSpace(value.PolicyId))
        {
            errors.Add("Policy ID cannot be null or whitespace.");
        }

        if (value.ExecutionTimeMs < 0)
        {
            errors.Add("Execution time (ms) cannot be negative.");
        }

        if (value.AttemptCount < 0)
        {
            errors.Add("Attempt count cannot be negative.");
        }

        if (value.ExecutedAt == default)
        {
            errors.Add("Executed at timestamp cannot be default (Unix epoch).");
        }
        else if (value.ExecutedAt > DateTime.UtcNow.AddMinutes(5))
        {
            errors.Add("Executed at timestamp cannot be in the future.");
        }

        if (value.Metadata is null)
        {
            errors.Add("Metadata dictionary cannot be null.");
        }

        if (!string.IsNullOrEmpty(value.ErrorMessage) && value.ErrorMessage.Length > 10000)
        {
            errors.Add("Error message cannot exceed 10,000 characters.");
        }

        if (!string.IsNullOrEmpty(value.ErrorType) && value.ErrorType.Length > 500)
        {
            errors.Add("Error type cannot exceed 500 characters.");
        }

        return errors.AsReadOnly();
    }

    /// <summary>
    /// Determines whether an execution record is valid.
    /// </summary>
    /// <param name="value">The execution record to check.</param>
    /// <returns>True if the record is valid; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static bool IsValid(this ExecutionRecord? value)
        => value?.Validate().Count == 0;

    /// <summary>
    /// Ensures that an execution record is valid, throwing an <see cref="ArgumentException"/> if not.
    /// </summary>
    /// <param name="value">The execution record to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when the record contains validation errors.</exception>
    public static void EnsureValid(this ExecutionRecord value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var errors = value.Validate();

        if (errors.Count == 0)
        {
            return;
        }

        throw new ArgumentException(
            $"Execution record is invalid. Problems:\n{string.Join("\n", errors)}",
            nameof(value));
    }
}
