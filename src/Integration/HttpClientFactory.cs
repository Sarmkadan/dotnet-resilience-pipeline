#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Integration;

/// <summary>
/// Factory for creating HTTP clients with integrated resilience policies.
/// Manages client lifecycle and applies policies to all HTTP operations.
/// </summary>
public class HttpClientFactory
{
    private readonly ConcurrentDictionary<string, HttpClient> _clients = new();
    private readonly ResiliencyPipelineService _pipelineService;
    private readonly object _lockObj = new object();

    public HttpClientFactory(ResiliencyPipelineService pipelineService)
    {
        _pipelineService = pipelineService;
    }

    /// <summary>
    /// Creates or gets an HTTP client with resilience policies.
    /// </summary>
    public HttpClient CreateClient(
        string clientName,
        string? baseAddress = null,
        ResiliencyPolicy? resiliencyPolicy = null,
        TimeSpan? timeout = null)
    {
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
    public HttpClient? GetClient(string clientName)
    {
        return _clients.TryGetValue(clientName, out var client) ? client : null;
    }

    /// <summary>
    /// Removes a client from the factory.
    /// </summary>
    public bool RemoveClient(string clientName)
    {
        return _clients.TryRemove(clientName, out var client) && (client?.Dispose(), true).Item2;
    }

    /// <summary>
    /// Executes an HTTP GET request with resilience policies.
    /// </summary>
    public async Task<ResilientHttpResponse> GetAsync(
        string clientName,
        string url,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient(clientName);
        if (client is null)
            return new ResilientHttpResponse { Success = false, Message = "Client not found" };

        var policy = !string.IsNullOrEmpty(policyName)
            ? _pipelineService.GetPolicyByName(policyName)
            : null;

        try
        {
            var response = await client.GetAsync(url, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ResilientHttpResponse
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                Content = content,
                Headers = response.Headers.ToDictionary(h => h.Key, h => h.Value.FirstOrDefault() ?? "")
            };
        }
        catch (Exception ex)
        {
            return new ResilientHttpResponse
            {
                Success = false,
                Message = ex.Message,
                Exception = ex
            };
        }
    }

    /// <summary>
    /// Executes an HTTP POST request with resilience policies.
    /// </summary>
    public async Task<ResilientHttpResponse> PostAsync(
        string clientName,
        string url,
        HttpContent content,
        string? policyName = null,
        CancellationToken cancellationToken = default)
    {
        var client = GetClient(clientName);
        if (client is null)
            return new ResilientHttpResponse { Success = false, Message = "Client not found" };

        try
        {
            var response = await client.PostAsync(url, content, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            return new ResilientHttpResponse
            {
                Success = response.IsSuccessStatusCode,
                StatusCode = response.StatusCode,
                Content = responseContent
            };
        }
        catch (Exception ex)
        {
            return new ResilientHttpResponse
            {
                Success = false,
                Message = ex.Message,
                Exception = ex
            };
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
        lock (_lockObj)
        {
            foreach (var client in _clients.Values)
                client?.Dispose();

            _clients.Clear();
        }
    }
}

/// <summary>
/// Response from a resilient HTTP operation.
/// </summary>
public class ResilientHttpResponse
{
    public bool Success { get; set; }
    public System.Net.HttpStatusCode? StatusCode { get; set; }
    public string? Content { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
    public string? Message { get; set; }
    public Exception? Exception { get; set; }
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
}
