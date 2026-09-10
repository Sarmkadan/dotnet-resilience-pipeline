#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Exceptions;

/// <summary>
/// Thrown when validation of input parameters or configuration fails.
/// </summary>
public sealed class ValidationException : DotnetResiliencePipelineException
{
    /// <summary>
    /// Gets or sets the validation errors.
    /// </summary>
    public Dictionary<string, string> ValidationErrors { get; set; } = new();

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    public ValidationException(string message)
        : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message and a dictionary of validation errors.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="errors">The validation errors.</param>
    public ValidationException(string message, Dictionary<string, string> errors)
        : base(message)
    {
        ValidationErrors = errors;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ValidationException"/> class with a specified error message, a reference to the inner exception that is the cause of this exception, and a dictionary of validation errors.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <param name="errors">The validation errors.</param>
    public ValidationException(string message, Exception innerException, Dictionary<string, string> errors)
        : base(message, innerException)
    {
        ValidationErrors = errors;
    }
}