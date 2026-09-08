#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Exceptions;

/// <summary>
/// Extension methods for <see cref="Exception"/> providing utility operations.
/// </summary>
public static class ExceptionExtensions
{
    /// <summary>
    /// Walks the exception chain to find the innermost (deepest) exception.
    /// </summary>
    /// <param name="exception">The exception to traverse.</param>
    /// <returns>The innermost exception in the chain, or the original exception if no inner exceptions exist.</returns>
    /// <exception cref="ArgumentNullException">Thrown when the <paramref name="exception"/> parameter is <see langword="null"/>.</exception>
    public static Exception GetInnermost(this Exception exception)
    {
        ArgumentNullException.ThrowIfNull(exception);

        while (exception.InnerException != null)
        {
            exception = exception.InnerException;
        }

        return exception;
    }
}