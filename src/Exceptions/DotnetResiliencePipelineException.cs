public abstract class DotnetResiliencePipelineException : Exception
{
    public DotnetResiliencePipelineException(string message) : base(message)
    {
    }

    public DotnetResiliencePipelineException(string message, Exception innerException) : base(message, innerException)
    {
    }
}
