#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Net;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Integration;

/// <summary>
/// Extension methods for <see cref="HttpClientFactory"/> to provide additional
/// convenience methods for common HTTP operations.
/// </summary>
public static class HttpClientFactoryExtensions
{
    /// <summary>
    /// Executes an HTTP GET request with resilience policies and returns the response
    /// as a string. Includes automatic error handling and response validation.
    /// </summary>
    /// <param name="factory">The HTTP client factory instance.</param>
    /// <param name="clientName">Name of the HTTP client to use.</param>
    /// <param name="url">The URL to send the GET request to.</param>
    /// <param name="policyName">Optional name of the resiliency policy to apply.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The response content as a string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when clientName or url is null.</exception>
    /// <exception cref="HttpClientException">Thrown when client is not found or request fails.</exception>
    public static async Task<string> GetStringAsync(
        this HttpClientFactory factory,
        string clientName,
        string url,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        var response = await factory.GetAsync(clientName, url, policyName, cancellationToken);

        if (!response.Success && response.StatusCode.HasValue)
        {
            throw new HttpResponseException(
                $"GET request failed with status {(int)response.StatusCode}: {response.Content}",
                (int)response.StatusCode,
                response.Exception,
                clientName,
                url);
        }
        else if (!response.Success)
        {
            throw new HttpClientException(
                $"GET request failed: {response.Message}",
                response.Exception,
                clientName,
                url);
        }

        return response.Content ?? string.Empty;
    }

    /// <summary>
    /// Executes an HTTP GET request and deserializes the JSON response to the specified type.
    /// Uses System.Text.Json for deserialization with default options.
    /// </summary>
    /// <typeparam name="T">Type to deserialize the response to.</typeparam>
    /// <param name="factory">The HTTP client factory instance.</param>
    /// <param name="clientName">Name of the HTTP client to use.</param>
    /// <param name="url">The URL to send the GET request to.</param>
    /// <param name="policyName">Optional name of the resiliency policy to apply.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Deserialized object of type T.</returns>
    /// <exception cref="ArgumentNullException">Thrown when clientName or url is null.</exception>
    /// <exception cref="HttpClientException">Thrown when client is not found or request fails.</exception>
    /// <exception cref="JsonException">Thrown when response cannot be deserialized.</exception>
    public static async Task<T> GetFromJsonAsync<T>(
        this HttpClientFactory factory,
        string clientName,
        string url,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        var response = await factory.GetAsync(clientName, url, policyName, cancellationToken);

        if (!response.Success && response.StatusCode.HasValue)
        {
            throw new HttpResponseException(
                $"GET request failed with status {(int)response.StatusCode}: {response.Content}",
                (int)response.StatusCode,
                response.Exception,
                clientName,
                url);
        }
        else if (!response.Success)
        {
            throw new HttpClientException(
                $"GET request failed: {response.Message}",
                response.Exception,
                clientName,
                url);
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(
                response.Content ?? "{}",
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;
        }
        catch (Exception ex)
        {
            throw new HttpClientException(
                $"Failed to deserialize response to {typeof(T).Name}",
                ex,
                clientName,
                url);
        }
    }

    /// <summary>
    /// Executes an HTTP POST request with JSON content and returns the response.
    /// Automatically serializes the provided object to JSON and sets appropriate headers.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request object to serialize.</typeparam>
    /// <param name="factory">The HTTP client factory instance.</param>
    /// <param name="clientName">Name of the HTTP client to use.</param>
    /// <param name="url">The URL to send the POST request to.</param>
    /// <param name="request">The request object to serialize as JSON.</param>
    /// <param name="policyName">Optional name of the resiliency policy to apply.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The response content as a string.</returns>
    /// <exception cref="ArgumentNullException">Thrown when clientName, url, or request is null.</exception>
    /// <exception cref="HttpClientException">Thrown when client is not found or request fails.</exception>
    public static async Task<string> PostAsJsonAsync<TRequest>(
        this HttpClientFactory factory,
        string clientName,
        string url,
        TRequest request,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await factory.PostAsync(clientName, url, content, policyName, cancellationToken);

        if (!response.Success && response.StatusCode.HasValue)
        {
            throw new HttpResponseException(
                $"POST request failed with status {(int)response.StatusCode}: {response.Content}",
                (int)response.StatusCode,
                response.Exception,
                clientName,
                url);
        }
        else if (!response.Success)
        {
            throw new HttpClientException(
                $"POST request failed: {response.Message}",
                response.Exception,
                clientName,
                url);
        }

        return response.Content ?? string.Empty;
    }

    /// <summary>
    /// Executes an HTTP POST request with JSON content and deserializes the response.
    /// Automatically serializes the request object and deserializes the response.
    /// </summary>
    /// <typeparam name="TRequest">Type of the request object to serialize.</typeparam>
    /// <typeparam name="TResponse">Type to deserialize the response to.</typeparam>
    /// <param name="factory">The HTTP client factory instance.</param>
    /// <param name="clientName">Name of the HTTP client to use.</param>
    /// <param name="url">The URL to send the POST request to.</param>
    /// <param name="request">The request object to serialize as JSON.</param>
    /// <param name="policyName">Optional name of the resiliency policy to apply.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>Deserialized response object of type TResponse.</returns>
    /// <exception cref="ArgumentNullException">Thrown when clientName, url, or request is null.</exception>
    /// <exception cref="HttpClientException">Thrown when client is not found or request fails.</exception>
    /// <exception cref="JsonException">Thrown when response cannot be deserialized.</exception>
    public static async Task<TResponse> PostAsJsonAndGetAsync<TRequest, TResponse>(
        this HttpClientFactory factory,
        string clientName,
        string url,
        TRequest request,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        if (request is null)
        {
            throw new ArgumentNullException(nameof(request));
        }

        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await factory.PostAsync(clientName, url, content, policyName, cancellationToken);

        if (!response.Success && response.StatusCode.HasValue)
        {
            throw new HttpResponseException(
                $"POST request failed with status {(int)response.StatusCode}: {response.Content}",
                (int)response.StatusCode,
                response.Exception,
                clientName,
                url);
        }
        else if (!response.Success)
        {
            throw new HttpClientException(
                $"POST request failed: {response.Message}",
                response.Exception,
                clientName,
                url);
        }

        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<TResponse>(
                response.Content ?? "{}",
                new System.Text.Json.JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true
                })!;
        }
        catch (Exception ex)
        {
            throw new HttpClientException(
                $"Failed to deserialize response to {typeof(TResponse).Name}",
                ex,
                clientName,
                url);
        }
    }

    /// <summary>
    /// Checks if a client with the specified name exists in the factory.
    /// </summary>
    /// <param name="factory">The HTTP client factory instance.</param>
    /// <param name="clientName">Name of the client to check.</param>
    /// <returns>True if the client exists; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when clientName is null.</exception>
    public static bool HasClient(
        this HttpClientFactory factory,
        string clientName)
    {
        if (string.IsNullOrWhiteSpace(clientName))
        {
            throw new ArgumentNullException(nameof(clientName), "Client name cannot be null or whitespace");
        }

        return factory.GetClient(clientName) is not null;
    }

    /// <summary>
    /// Executes an HTTP GET request and returns the response status code.
    /// Useful for simple status checks without needing the full response.
    /// </summary>
    /// <param name="factory">The HTTP client factory instance.</param>
    /// <param name="clientName">Name of the HTTP client to use.</param>
    /// <param name="url">The URL to send the GET request to.</param>
    /// <param name="policyName">Optional name of the resiliency policy to apply.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The HTTP status code from the response.</returns>
    /// <exception cref="ArgumentNullException">Thrown when clientName or url is null.</exception>
    /// <exception cref="HttpClientException">Thrown when client is not found or request fails.</exception>
    public static async Task<HttpStatusCode> GetStatusCodeAsync(
        this HttpClientFactory factory,
        string clientName,
        string url,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        var response = await factory.GetAsync(clientName, url, policyName, cancellationToken);
        return response.StatusCode.HasValue ? (HttpStatusCode)response.StatusCode.Value : HttpStatusCode.InternalServerError;
    }
}