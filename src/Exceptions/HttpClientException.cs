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
    /// <summary>
    /// Gets or sets the name of the HTTP client.
    /// </summary>
    public string? ClientName { get; set; }
    /// <summary>
    /// Gets or sets the URL of the request.
    /// </summary>
    public string? RequestUrl { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="clientName">The name of the HTTP client.</param>
    /// <param name="requestUrl">The URL of the request.</param>
    public HttpClientException(string message, string? clientName = null, string? requestUrl = null)
        : base(message)
    {
        ClientName = clientName;
        RequestUrl = requestUrl;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <param name="clientName">The name of the HTTP client.</param>
    /// <param name="requestUrl">The URL of the request.</param>
    public HttpClientException(string message, Exception innerException, string? clientName = null, string? requestUrl = null)
        : base(message, innerException)
    {
        ClientName = clientName;
        RequestUrl = requestUrl;
    }

    public override string ToString()
    {
        return this switch
        {
            InvalidHttpRequestException e => $"InvalidHttpRequestException {{ ClientName = {e.ClientName}, RequestUrl = {e.RequestUrl}, HttpMethod = {e.HttpMethod} }}",
            HttpResponseException e => $"HttpResponseException {{ ClientName = {e.ClientName}, RequestUrl = {e.RequestUrl}, StatusCode = {e.StatusCode} }}",
            HttpTimeoutException e => $"HttpTimeoutException {{ ClientName = {e.ClientName}, RequestUrl = {e.RequestUrl}, Timeout = {e.Timeout} }}",
            _ => $"HttpClientException {{ ClientName = {ClientName}, RequestUrl = {RequestUrl} }}"
        };
    }
}

/// <summary>
/// Thrown when HTTP request configuration is invalid.
/// </summary>
public sealed class InvalidHttpRequestException : HttpClientException
{
    /// <summary>
    /// Gets or sets the HTTP method of the request.
    /// </summary>
    public string? HttpMethod { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidHttpRequestException"/> class with a specified error message.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="clientName">The name of the HTTP client.</param>
    /// <param name="requestUrl">The URL of the request.</param>
    /// <param name="httpMethod">The HTTP method of the request.</param>
    public InvalidHttpRequestException(string message, string? clientName = null, string? requestUrl = null, string? httpMethod = null)
        : base(message, clientName, requestUrl)
    {
        HttpMethod = httpMethod;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="InvalidHttpRequestException"/> class with a specified error message and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <param name="clientName">The name of the HTTP client.</param>
    /// <param name="requestUrl">The URL of the request.</param>
    /// <param name="httpMethod">The HTTP method of the request.</param>
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
    /// <summary>
    /// Gets or sets the HTTP status code of the response.
    /// </summary>
    public int StatusCode { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpResponseException"/> class with a specified error message and HTTP status code.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="clientName">The name of the HTTP client.</param>
    /// <param name="requestUrl">The URL of the request.</param>
    public HttpResponseException(string message, int statusCode, string? clientName = null, string? requestUrl = null)
        : base(message, clientName, requestUrl)
    {
        StatusCode = statusCode;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpResponseException"/> class with a specified error message, HTTP status code, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="statusCode">The HTTP status code of the response.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <param name="clientName">The name of the HTTP client.</param>
    /// <param name="requestUrl">The URL of the request.</param>
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
    /// <summary>
    /// Gets or sets the timeout duration.
    /// </summary>
    public TimeSpan Timeout { get; set; }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpTimeoutException"/> class with a specified error message and timeout duration.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="clientName">The name of the HTTP client.</param>
    /// <param name="requestUrl">The URL of the request.</param>
    public HttpTimeoutException(string message, TimeSpan timeout, string? clientName = null, string? requestUrl = null)
        : base(message, clientName, requestUrl)
    {
        Timeout = timeout;
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpTimeoutException"/> class with a specified error message, timeout duration, and a reference to the inner exception that is the cause of this exception.
    /// </summary>
    /// <param name="message">The error message that explains the reason for the exception.</param>
    /// <param name="timeout">The timeout duration.</param>
    /// <param name="innerException">The exception that is the cause of the current exception, or a null reference if no inner exception is specified.</param>
    /// <param name="clientName">The name of the HTTP client.</param>
    /// <param name="requestUrl">The URL of the request.</param>
    public HttpTimeoutException(string message, TimeSpan timeout, Exception innerException, string? clientName = null, string? requestUrl = null)
        : base(message, innerException, clientName, requestUrl)
    {
        Timeout = timeout;
    }
}