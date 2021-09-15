#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Exceptions;

/// <summary>
/// Thrown when validation of input parameters or configuration fails.
/// </summary>
public sealed class ValidationException : ResiliencyException
{
    public Dictionary<string, string> ValidationErrors { get; set; } = new();

    public ValidationException(string message)
        : base(message)
    {
    }

    public ValidationException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public ValidationException(string message, Dictionary<string, string> errors)
        : base(message)
    {
        ValidationErrors = errors;
    }

    public ValidationException(string message, Exception innerException, Dictionary<string, string> errors)
        : base(message, innerException)
    {
        ValidationErrors = errors;
    }
}