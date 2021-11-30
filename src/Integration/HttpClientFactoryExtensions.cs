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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clientName"/> or <paramref name="url"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientName"/> or <paramref name="url"/> is empty or whitespace.</exception>
    /// <exception cref="HttpClientException">Thrown when client is not found or request fails.</exception>
    public static async Task<string> GetStringAsync(
        this HttpClientFactory factory,
        string clientName,
        string url,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var response = await factory.GetAsync(clientName, url, policyName, cancellationToken);

        return response switch
        {
            { Success: true } => response.Content ?? string.Empty,
            { StatusCode: var statusCode } when statusCode.HasValue => throw new HttpResponseException(
                $"GET request failed with status {(int)statusCode}: {response.Content}",
                (int)statusCode,
                response.Exception,
                clientName,
                url),
            _ => throw new HttpClientException(
                $"GET request failed: {response.Message}",
                response.Exception,
                clientName,
                url)
        };
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clientName"/> or <paramref name="url"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientName"/> or <paramref name="url"/> is empty or whitespace.</exception>
    /// <exception cref="HttpClientException">Thrown when client is not found or request fails.</exception>
    /// <exception cref="JsonException">Thrown when response cannot be deserialized.</exception>
    public static async Task<T> GetFromJsonAsync<T>(
        this HttpClientFactory factory,
        string clientName,
        string url,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var response = await factory.GetAsync(clientName, url, policyName, cancellationToken);

        return response switch
        {
            { Success: true } => DeserializeResponse<T>(response, clientName, url),
            { StatusCode: var statusCode } when statusCode.HasValue => throw new HttpResponseException(
                $"GET request failed with status {(int)statusCode}: {response.Content}",
                (int)statusCode,
                response.Exception,
                clientName,
                url),
            _ => throw new HttpClientException(
                $"GET request failed: {response.Message}",
                response.Exception,
                clientName,
                url)
        };
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clientName"/>, <paramref name="url"/>, or <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientName"/> or <paramref name="url"/> is empty or whitespace.</exception>
    /// <exception cref="HttpClientException">Thrown when client is not found or request fails.</exception>
    public static async Task<string> PostAsJsonAsync<TRequest>(
        this HttpClientFactory factory,
        string clientName,
        string url,
        TRequest request,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(request);

        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await factory.PostAsync(clientName, url, content, policyName, cancellationToken);

        return response switch
        {
            { Success: true } => response.Content ?? string.Empty,
            { StatusCode: var statusCode } when statusCode.HasValue => throw new HttpResponseException(
                $"POST request failed with status {(int)statusCode}: {response.Content}",
                (int)statusCode,
                response.Exception,
                clientName,
                url),
            _ => throw new HttpClientException(
                $"POST request failed: {response.Message}",
                response.Exception,
                clientName,
                url)
        };
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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clientName"/>, <paramref name="url"/>, or <paramref name="request"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientName"/> or <paramref name="url"/> is empty or whitespace.</exception>
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
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);
        ArgumentNullException.ThrowIfNull(request);

        var json = System.Text.Json.JsonSerializer.Serialize(request);
        var content = new StringContent(json, System.Text.Encoding.UTF8, "application/json");

        var response = await factory.PostAsync(clientName, url, content, policyName, cancellationToken);

        return response switch
        {
            { Success: true } => DeserializeResponse<TResponse>(response, clientName, url),
            { StatusCode: var statusCode } when statusCode.HasValue => throw new HttpResponseException(
                $"POST request failed with status {(int)statusCode}: {response.Content}",
                (int)statusCode,
                response.Exception,
                clientName,
                url),
            _ => throw new HttpClientException(
                $"POST request failed: {response.Message}",
                response.Exception,
                clientName,
                url)
        };
    }

    /// <summary>
    /// Checks if a client with the specified name exists in the factory.
    /// </summary>
    /// <param name="factory">The HTTP client factory instance.</param>
    /// <param name="clientName">Name of the client to check.</param>
    /// <returns>True if the client exists; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clientName"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientName"/> is empty or whitespace.</exception>
    public static bool HasClient(
        this HttpClientFactory factory,
        string clientName)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(clientName);

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
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="clientName"/> or <paramref name="url"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="clientName"/> or <paramref name="url"/> is empty or whitespace.</exception>
    /// <exception cref="HttpClientException">Thrown when client is not found or request fails.</exception>
    public static async Task<HttpStatusCode> GetStatusCodeAsync(
        this HttpClientFactory factory,
        string clientName,
        string url,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(clientName);
        ArgumentException.ThrowIfNullOrWhiteSpace(url);

        var response = await factory.GetAsync(clientName, url, policyName, cancellationToken);

        return response.StatusCode.HasValue
            ? (HttpStatusCode)response.StatusCode.Value
            : HttpStatusCode.InternalServerError;
    }

    private static T DeserializeResponse<T>(
        object response,
        string clientName,
        string url)
    {
        try
        {
            return System.Text.Json.JsonSerializer.Deserialize<T>(
                response.GetType().GetProperty("Content")?.GetValue(response) as string ?? "{}",
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
}