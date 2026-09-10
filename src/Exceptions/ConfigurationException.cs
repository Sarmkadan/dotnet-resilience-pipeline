#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Exceptions;

/// <summary>
/// Thrown when there is a configuration-related error.
/// </summary>
public sealed class ConfigurationException : DotnetResiliencePipelineException
{
    /// <summary>
    /// Gets or sets the configuration key that caused the exception.
    /// </summary>
    public string ConfigurationKey { get; set; }

    /// <summary>
    /// Initializes a new instance of the ConfigurationException class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="configurationKey">The configuration key associated with the error.</param>
    public ConfigurationException(string message, string configurationKey = "")
        : base(message)
    {
        ConfigurationKey = configurationKey;
    }

    /// <summary>
    /// Initializes a new instance of the ConfigurationException class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <param name="configurationKey">The configuration key associated with the error.</param>
    public ConfigurationException(string message, Exception innerException, string configurationKey = "")
        : base(message, innerException)
    {
        ConfigurationKey = configurationKey;
    }
}
