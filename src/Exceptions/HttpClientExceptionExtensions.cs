#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Net;
using System.Text;

namespace DotNetResiliencePipeline.Exceptions;

/// <summary>
/// Extension methods for HttpClientException and derived types.
/// </summary>
public static class HttpClientExceptionExtensions
{
    /// <summary>
    /// Gets a formatted error message that includes client name, request URL, and HTTP method (if available).
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <returns>A formatted error message string.</returns>
    public static string GetFullErrorMessage(this HttpClientException exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        var messageBuilder = new StringBuilder();
        messageBuilder.Append(exception.Message);

        if (!string.IsNullOrEmpty(exception.ClientName))
        {
            messageBuilder.Append($" | Client: {exception.ClientName}");
        }

        if (!string.IsNullOrEmpty(exception.RequestUrl))
        {
            messageBuilder.Append($" | URL: {exception.RequestUrl}");
        }

        if (exception is InvalidHttpRequestException invalidRequestException && !string.IsNullOrEmpty(invalidRequestException.HttpMethod))
        {
            messageBuilder.Append($" | Method: {invalidRequestException.HttpMethod}");
        }

        if (exception is HttpResponseException responseException)
        {
            messageBuilder.Append($" | Status: {(int)responseException.StatusCode} ({responseException.StatusCode})");
        }

        if (exception is HttpTimeoutException timeoutException)
        {
            messageBuilder.Append($" | Timeout: {timeoutException.Timeout.TotalSeconds}s");
        }

        return messageBuilder.ToString();
    }

    /// <summary>
    /// Determines if the exception represents a client error (4xx status code).
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <returns>True if the exception is a client error; otherwise, false.</returns>
    public static bool IsClientError(this HttpClientException exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        return exception is HttpResponseException responseException &&
               responseException.StatusCode >= (int)HttpStatusCode.BadRequest &&
               responseException.StatusCode < (int)HttpStatusCode.InternalServerError;
    }

    /// <summary>
    /// Determines if the exception represents a server error (5xx status code).
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <returns>True if the exception is a server error; otherwise, false.</returns>
    public static bool IsServerError(this HttpClientException exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        return exception is HttpResponseException responseException &&
               responseException.StatusCode >= (int)HttpStatusCode.InternalServerError;
    }

    /// <summary>
    /// Determines if the exception represents a timeout error.
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <returns>True if the exception is a timeout error; otherwise, false.</returns>
    public static bool IsTimeoutError(this HttpClientException exception)
    {
        return exception is HttpTimeoutException;
    }

    /// <summary>
    /// Gets a simplified error code for the exception type.
    /// </summary>
    /// <param name="exception">The exception instance.</param>
    /// <returns>A string representing the error code.</returns>
    public static string GetErrorCode(this HttpClientException exception)
    {
        if (exception is null)
        {
            throw new ArgumentNullException(nameof(exception));
        }

        return exception switch
        {
            HttpTimeoutException => "HTTP_TIMEOUT",
            HttpResponseException => $"HTTP_{exception.GetType().Name.Replace("Http", "").Replace("Exception", "")}",
            InvalidHttpRequestException => "HTTP_INVALID_REQUEST",
            _ => "HTTP_CLIENT_ERROR"
        };
    }
}