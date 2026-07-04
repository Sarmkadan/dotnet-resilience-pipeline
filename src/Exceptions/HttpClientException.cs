#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Exceptions;

/// <summary>
/// Base exception for HTTP client-related failures.
/// </summary>
public class HttpClientException : ResiliencyException
{
    public string? ClientName { get; set; }
    public string? RequestUrl { get; set; }

    public HttpClientException(string message, string? clientName = null, string? requestUrl = null)
        : base(message)
    {
        ClientName = clientName;
        RequestUrl = requestUrl;
    }

    public HttpClientException(string message, Exception innerException, string? clientName = null, string? requestUrl = null)
        : base(message, innerException)
    {
        ClientName = clientName;
        RequestUrl = requestUrl;
    }
}

/// <summary>
/// Thrown when HTTP request configuration is invalid.
/// </summary>
public sealed class InvalidHttpRequestException : HttpClientException
{
    public string? HttpMethod { get; set; }

    public InvalidHttpRequestException(string message, string? clientName = null, string? requestUrl = null, string? httpMethod = null)
        : base(message, clientName, requestUrl)
    {
        HttpMethod = httpMethod;
    }

    public InvalidHttpRequestException(string message, Exception innerException, string? clientName = null, string? requestUrl = null, string? httpMethod = null)
        : base(message, innerException, clientName, requestUrl)
    {
        HttpMethod = httpMethod;
    }
}

/// <summary>
/// Thrown when HTTP response indicates an error status code.
/// </summary>
public sealed class HttpResponseException : HttpClientException
{
    public int StatusCode { get; set; }

    public HttpResponseException(string message, int statusCode, string? clientName = null, string? requestUrl = null)
        : base(message, clientName, requestUrl)
    {
        StatusCode = statusCode;
    }

    public HttpResponseException(string message, int statusCode, Exception innerException, string? clientName = null, string? requestUrl = null)
        : base(message, innerException, clientName, requestUrl)
    {
        StatusCode = statusCode;
    }
}

/// <summary>
/// Thrown when HTTP client operation times out.
/// </summary>
public sealed class HttpTimeoutException : HttpClientException
{
    public TimeSpan Timeout { get; set; }

    public HttpTimeoutException(string message, TimeSpan timeout, string? clientName = null, string? requestUrl = null)
        : base(message, clientName, requestUrl)
    {
        Timeout = timeout;
    }

    public HttpTimeoutException(string message, TimeSpan timeout, Exception innerException, string? clientName = null, string? requestUrl = null)
        : base(message, innerException, clientName, requestUrl)
    {
        Timeout = timeout;
    }
}