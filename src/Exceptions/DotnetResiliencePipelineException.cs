/// <summary>
/// Represents an exception that occurs within the Dotnet Resilience Pipeline.
/// </summary>
public abstract class DotnetResiliencePipelineException : Exception
{
    /// <summary>
    /// Initializes a new instance of the <see cref="DotnetResiliencePipelineException"/> class with the specified message.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    public DotnetResiliencePipelineException(string? message) : base(message)
    {
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="DotnetResiliencePipelineException"/> class with the specified message and inner exception.
    /// </summary>
    /// <param name="message">The message that describes the error.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    public DotnetResiliencePipelineException(string? message, Exception? innerException) : base(message, innerException)
    {
    }
}
