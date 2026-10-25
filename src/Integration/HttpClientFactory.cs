#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Integration;

/// <summary>
/// Factory for creating HTTP clients with integrated resilience policies.
/// Manages client lifecycle and applies policies to all HTTP operations.
/// </summary>
public sealed class HttpClientFactory : IDisposable
{
    private readonly ConcurrentDictionary<string, HttpClient> _clients = new();
    private readonly ResiliencyPipelineService _pipelineService;
    private readonly object _lockObj = new object();
    private bool _disposed;

    /// <summary>
    /// Initializes a new instance of the <see cref="HttpClientFactory"/> class.
    /// </summary>
    public HttpClientFactory(ResiliencyPipelineService pipelineService)
    {
        _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));
    }

    /// <summary>
    /// Creates or gets an HTTP client with resilience policies.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when clientName is null.</exception>
    /// <exception cref="ConfigurationException">Thrown when baseAddress is invalid.</exception>
    public HttpClient CreateClient(
        string clientName,
        string? baseAddress = null,
        ResiliencyPolicy? resiliencyPolicy = null,
        TimeSpan? timeout = null)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentNullException(nameof(clientName), "Client name cannot be null or whitespace");

        if (!string.IsNullOrEmpty(baseAddress) && !Uri.TryCreate(baseAddress, UriKind.Absolute, out _))
            throw new ConfigurationException("Invalid base address format", nameof(baseAddress));

        return _clients.GetOrAdd(clientName, name =>
        {
            var client = new HttpClient();

            if (!string.IsNullOrEmpty(baseAddress))
                client.BaseAddress = new Uri(baseAddress);

            client.Timeout = timeout ?? TimeSpan.FromSeconds(30);

            return client;
        });
    }

    /// <summary>
    /// Gets an existing client by name.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when clientName is null.</exception>
    public HttpClient? GetClient(string clientName)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentNullException(nameof(clientName), "Client name cannot be null or whitespace");

        return _clients.TryGetValue(clientName, out var client) ? client : null;
    }

    /// <summary>
    /// Removes a client from the factory.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when clientName is null.</exception>
    public bool RemoveClient(string clientName)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentNullException(nameof(clientName), "Client name cannot be null or whitespace");

        if (_clients.TryRemove(clientName, out var client))
        {
            client?.Dispose();
            return true;
        }
        return false;
    }

    /// <summary>
    /// Executes an HTTP GET request with resilience policies.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when clientName or url is null.</exception>
    /// <exception cref="HttpClientException">Thrown when client is not found.</exception>
    public async Task<ResilientHttpResponse> GetAsync(
        string clientName,
        string url,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentNullException(nameof(clientName));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));

        var client = GetClient(clientName);
        if (client is null)
            throw new HttpClientException("HTTP client not found", clientName, url);

        var policy = !string.IsNullOrEmpty(policyName)
            ? _pipelineService.GetPolicyByName(policyName)
            : null;

        try
        {
            using var response = await client.GetAsync(url, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ResilientHttpResponse
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                Content = content,
                Headers = ExtractHeaders(response),
                Timestamp = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new HttpTimeoutException(
                $"GET request to {url} was cancelled",
                client.Timeout,
                ex,
                clientName,
                url);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpClientException(
                $"HTTP GET request failed to {url}",
                ex,
                clientName,
                url);
        }
        catch (Exception ex)
        {
            throw new HttpClientException(
                $"Request to {url} failed: {ex.Message}",
                ex,
                clientName,
                url);
        }
    }

    /// <summary>
    /// Executes an HTTP POST request with resilience policies.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when clientName, url, or content is null.</exception>
    /// <exception cref="HttpClientException">Thrown when client is not found.</exception>
    public async Task<ResilientHttpResponse> PostAsync(
        string clientName,
        string url,
        HttpContent content,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(clientName))
            throw new ArgumentNullException(nameof(clientName));

        if (string.IsNullOrWhiteSpace(url))
            throw new ArgumentNullException(nameof(url));

        if (content is null)
            throw new ArgumentNullException(nameof(content));

        var client = GetClient(clientName);
        if (client is null)
            throw new HttpClientException("HTTP client not found", clientName, url);

        try
        {
            using var response = await client.PostAsync(url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ResilientHttpResponse
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                Content = responseContent,
                Timestamp = DateTime.UtcNow
            };
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new HttpTimeoutException(
                $"POST request to {url} was cancelled",
                client.Timeout,
                ex,
                clientName,
                url);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpClientException(
                $"HTTP POST request failed to {url}",
                ex,
                clientName,
                url);
        }
        catch (Exception ex)
        {
            throw new HttpClientException(
                $"Request to {url} failed: {ex.Message}",
                ex,
                clientName,
                url);
        }
    }

    /// <summary>
    /// Gets all registered client names.
    /// </summary>
    public List<string> GetClientNames()
    {
        return _clients.Keys.ToList();
    }

    /// <summary>
    /// Disposes all clients.
    /// </summary>
    public void Dispose()
    {
        if (_disposed)
            return;

        lock (_lockObj)
        {
            foreach (var client in _clients.Values)
                client?.Dispose();

            _clients.Clear();
            _disposed = true;
        }

        GC.SuppressFinalize(this);
    }

    private Dictionary<string, string>? ExtractHeaders(HttpResponseMessage response)
    {
        try
        {
            return response.Headers.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault() ?? "");
        }
        catch
        {
            return null;
        }
    }
}

/// <summary>
/// Response from a resilient HTTP operation.
/// </summary>
public sealed class ResilientHttpResponse
{
    /// <summary>
    /// Gets or sets a value indicating whether the request succeeded.
    /// </summary>
    public bool Success { get; set; }

    /// <summary>
    /// Gets or sets the HTTP status code, if a response was received.
    /// </summary>
    public System.Net.HttpStatusCode? StatusCode { get; set; }

    /// <summary>
    /// Gets or sets the response body as a string, if available.
    /// </summary>
    public string? Content { get; set; }

    /// <summary>
    /// Gets or sets the response headers, if available.
    /// </summary>
    public Dictionary<string, string>? Headers { get; set; }

    /// <summary>
    /// Gets or sets a message describing the outcome of the request.
    /// </summary>
    public string? Message { get; set; }

    /// <summary>
    /// Gets or sets the exception raised while executing the request, if any.
    /// </summary>
    public Exception? Exception { get; set; }

    /// <summary>
    /// Gets or sets the UTC time at which the response was created.
    /// </summary>
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Returns a string representation of the response.
    /// </summary>
    public override string ToString() => $"ResilientHttpResponse {{ Success = {Success}, Content = {Content}, Headers = {Headers}, Message = {Message}, Exception = {Exception}, Timestamp = {Timestamp} }}";
}
