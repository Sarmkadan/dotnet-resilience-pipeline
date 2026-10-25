#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Integration;

/// <summary>
/// Extension methods for <see cref="ExternalApiClient"/> and <see cref="ApiConfiguration"/>
/// providing fluent configuration helpers and fallback-friendly request helpers.
/// </summary>
public static class ExternalApiClientExtensions
{
    /// <summary>
    /// Sets the bearer token used for the <c>Authorization</c> header and returns the same configuration,
    /// so calls can be chained while building an <see cref="ApiConfiguration"/>.
    /// </summary>
    /// <param name="config">The API configuration to update.</param>
    /// <param name="token">The bearer token to send with every request.</param>
    /// <returns>The same <paramref name="config"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> or <paramref name="token"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="token"/> is empty or whitespace.</exception>
    public static ApiConfiguration WithBearerToken(this ApiConfiguration config, string token)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(token);

        config.BearerToken = token;
        return config;
    }

    /// <summary>
    /// Adds or replaces a default header that is sent with every request and returns the same configuration,
    /// so calls can be chained while building an <see cref="ApiConfiguration"/>.
    /// </summary>
    /// <param name="config">The API configuration to update.</param>
    /// <param name="name">The header name, for example <c>Accept</c>.</param>
    /// <param name="value">The header value.</param>
    /// <returns>The same <paramref name="config"/> instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/>, <paramref name="name"/>, or <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty or whitespace.</exception>
    public static ApiConfiguration WithHeader(this ApiConfiguration config, string name, string value)
    {
        ArgumentNullException.ThrowIfNull(config);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);
        ArgumentNullException.ThrowIfNull(value);

        config.DefaultHeaders[name] = value;
        return config;
    }

    /// <summary>
    /// Determines whether the configuration carries any credentials, either an API key or a bearer token.
    /// </summary>
    /// <param name="config">The API configuration to inspect.</param>
    /// <returns><c>true</c> if <see cref="ApiConfiguration.ApiKey"/> or <see cref="ApiConfiguration.BearerToken"/> is set; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="config"/> is null.</exception>
    public static bool IsAuthenticated(this ApiConfiguration config)
    {
        ArgumentNullException.ThrowIfNull(config);

        return !string.IsNullOrEmpty(config.ApiKey) || !string.IsNullOrEmpty(config.BearerToken);
    }

    /// <summary>
    /// Makes a resilient GET request and returns the response data, or <paramref name="defaultValue"/> if the
    /// request fails with an HTTP-level error (non-success status, deserialization failure, timeout, or transport error).
    /// Configuration errors such as an unregistered API name still throw, and cancellation requested by
    /// <paramref name="cancellationToken"/> is not swallowed.
    /// </summary>
    /// <typeparam name="T">Type to deserialize the response body to.</typeparam>
    /// <param name="client">The external API client instance.</param>
    /// <param name="apiName">The name the API was registered under.</param>
    /// <param name="endpoint">The endpoint path relative to the API base URL.</param>
    /// <param name="defaultValue">The value returned when the request fails or the response body is empty.</param>
    /// <param name="headers">Optional additional headers for this request.</param>
    /// <param name="cancellationToken">Cancellation token for the operation.</param>
    /// <returns>The deserialized response data, or <paramref name="defaultValue"/> when unavailable.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="client"/>, <paramref name="apiName"/>, or <paramref name="endpoint"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="apiName"/> or <paramref name="endpoint"/> is empty or whitespace.</exception>
    /// <exception cref="ConfigurationException">Thrown when the API is not registered.</exception>
    /// <exception cref="HttpTimeoutException">Thrown when <paramref name="cancellationToken"/> is cancelled during the request.</exception>
    public static async Task<T> GetOrDefaultAsync<T>(
        this ExternalApiClient client,
        string apiName,
        string endpoint,
        T defaultValue,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(client);
        ArgumentException.ThrowIfNullOrWhiteSpace(apiName);
        ArgumentException.ThrowIfNullOrWhiteSpace(endpoint);

        try
        {
            var response = await client.GetAsync<T>(apiName, endpoint, headers, cancellationToken);
            return response.Data ?? defaultValue;
        }
        catch (HttpClientException) when (!cancellationToken.IsCancellationRequested)
        {
            return defaultValue;
        }
    }
}
