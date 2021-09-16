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
    public string ConfigurationKey { get; set; }

    public ConfigurationException(string message, string configurationKey = "")
        : base(message)
    {
        ConfigurationKey = configurationKey;
    }

    public ConfigurationException(string message, Exception innerException, string configurationKey = "")
        : base(message, innerException)
    {
        ConfigurationKey = configurationKey;
    }
}
