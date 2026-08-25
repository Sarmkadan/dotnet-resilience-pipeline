#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.Json;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Integration;

/// <summary>
/// Wrapper for external API calls with built-in resilience policies.
/// Handles authentication, retries, timeouts, and error recovery.
/// </summary>
public sealed class ExternalApiClient
{
    private readonly HttpClientFactory _httpClientFactory;
    private readonly ResiliencyPipelineService _pipelineService;
    private readonly Dictionary<string, ApiConfiguration> _apiConfigs = new();

    public ExternalApiClient(HttpClientFactory httpClientFactory, ResiliencyPipelineService pipelineService)
    {
        _httpClientFactory = httpClientFactory ?? throw new ArgumentNullException(nameof(httpClientFactory));
        _pipelineService = pipelineService ?? throw new ArgumentNullException(nameof(pipelineService));
    }

    /// <summary>
    /// Registers an external API configuration.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when apiName or config is null.</exception>
    /// <exception cref="ConfigurationException">Thrown when configuration is invalid.</exception>
    public void RegisterApi(string apiName, ApiConfiguration config)
    {
        if (string.IsNullOrWhiteSpace(apiName))
            throw new ArgumentNullException(nameof(apiName), "API name cannot be null or whitespace");

        if (config is null)
            throw new ArgumentNullException(nameof(config));

        if (string.IsNullOrWhiteSpace(config.BaseUrl))
            throw new ConfigurationException("Base URL is required for API configuration", nameof(config.BaseUrl));

        if (!Uri.TryCreate(config.BaseUrl, UriKind.Absolute, out _))
            throw new ConfigurationException("Invalid base URL format", nameof(config.BaseUrl));

        _apiConfigs[apiName] = config;
    }

    /// <summary>
    /// Makes a resilient GET request to an external API.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when apiName or endpoint is null.</exception>
    /// <exception cref="ConfigurationException">Thrown when API is not registered.</exception>
    public async Task<ApiResponse<T>> GetAsync<T>(
        string apiName,
        string endpoint,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiName))
            throw new ArgumentNullException(nameof(apiName));

        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentNullException(nameof(endpoint));

        if (!_apiConfigs.TryGetValue(apiName, out var config))
            throw new ConfigurationException($"API not registered: {apiName}", nameof(apiName));

        try
        {
            var client = _httpClientFactory.CreateClient(apiName, config.BaseUrl, timeout: config.Timeout);
            var url = $"{config.BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            ApplyHeaders(request, headers, config);

            var response = await client.SendAsync(request, cancellationToken);
            var content = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var data = JsonSerializer.Deserialize<T>(content);
                    return new ApiResponse<T> { Success = true, Data = data, Headers = ExtractHeaders(response) };
                }
                catch (JsonException ex)
                {
                    throw new HttpResponseException(
                        $"Failed to deserialize response from {apiName}. Content: {content.Substring(0, Math.Min(200, content.Length))}...",
                        (int)response.StatusCode,
                        ex,
                        apiName,
                        url);
                }
            }

            throw new HttpResponseException(
                $"API error: {response.StatusCode}",
                (int)response.StatusCode,
                apiName,
                url);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new HttpTimeoutException(
                $"Request to {apiName} was cancelled",
                config.Timeout,
                ex,
                apiName,
                endpoint);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpClientException(
                $"HTTP request failed to {apiName}",
                ex,
                apiName,
                endpoint);
        }
        catch (Exception ex)
        {
            throw new HttpClientException(
                $"Request to {apiName} failed: {ex.Message}",
                ex,
                apiName,
                endpoint);
        }
    }

    /// <summary>
    /// Makes a resilient POST request to an external API.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when apiName, endpoint, or payload is null.</exception>
    /// <exception cref="ConfigurationException">Thrown when API is not registered.</exception>
    public async Task<ApiResponse<T>> PostAsync<T>(
        string apiName,
        string endpoint,
        object payload,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiName))
            throw new ArgumentNullException(nameof(apiName));

        if (string.IsNullOrWhiteSpace(endpoint))
            throw new ArgumentNullException(nameof(endpoint));

        if (payload is null)
            throw new ArgumentNullException(nameof(payload));

        if (!_apiConfigs.TryGetValue(apiName, out var config))
            throw new ConfigurationException($"API not registered: {apiName}", nameof(apiName));

        try
        {
            var client = _httpClientFactory.CreateClient(apiName, config.BaseUrl, timeout: config.Timeout);
            var url = $"{config.BaseUrl.TrimEnd('/')}/{endpoint.TrimStart('/')}";

            var jsonContent = JsonSerializer.Serialize(payload);
            var content = new StringContent(jsonContent, Encoding.UTF8, "application/json");

            var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = content };
            ApplyHeaders(request, headers, config);

            var response = await client.SendAsync(request, cancellationToken);
            var responseContent = await response.Content.ReadAsStringAsync(cancellationToken);

            if (response.IsSuccessStatusCode)
            {
                try
                {
                    var data = JsonSerializer.Deserialize<T>(responseContent);
                    return new ApiResponse<T> { Success = true, Data = data, Headers = ExtractHeaders(response) };
                }
                catch (JsonException ex)
                {
                    throw new HttpResponseException(
                        $"Failed to deserialize response from {apiName}. Content: {responseContent.Substring(0, Math.Min(200, responseContent.Length))}...",
                        (int)response.StatusCode,
                        ex,
                        apiName,
                        url);
                }
            }

            throw new HttpResponseException(
                $"API error: {response.StatusCode}",
                (int)response.StatusCode,
                apiName,
                url);
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new HttpTimeoutException(
                $"Request to {apiName} was cancelled",
                config.Timeout,
                ex,
                apiName,
                endpoint);
        }
        catch (HttpRequestException ex)
        {
            throw new HttpClientException(
                $"HTTP request failed to {apiName}",
                ex,
                apiName,
                endpoint);
        }
        catch (Exception ex)
        {
            throw new HttpClientException(
                $"Request to {apiName} failed: {ex.Message}",
                ex,
                apiName,
                endpoint);
        }
    }

    /// <summary>
    /// Gets all registered API configurations.
    /// </summary>
    public List<string> GetRegisteredApis()
    {
        return _apiConfigs.Keys.ToList();
    }

    /// <summary>
    /// Tests connectivity to an external API.
    /// </summary>
    /// <exception cref="ArgumentNullException">Thrown when apiName is null.</exception>
    /// <exception cref="ConfigurationException">Thrown when API is not registered.</exception>
    public async Task<bool> TestConnectionAsync(string apiName, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(apiName))
            throw new ArgumentNullException(nameof(apiName));

        if (!_apiConfigs.TryGetValue(apiName, out var config))
            throw new ConfigurationException($"API not registered: {apiName}", nameof(apiName));

        try
        {
            var client = _httpClientFactory.CreateClient(apiName, config.BaseUrl, timeout: TimeSpan.FromSeconds(5));
            var response = await client.GetAsync(config.BaseUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch (OperationCanceledException ex) when (cancellationToken.IsCancellationRequested)
        {
            throw new HttpTimeoutException(
                $"Connection test to {apiName} was cancelled",
                TimeSpan.FromSeconds(5),
                ex,
                apiName,
                config.BaseUrl);
        }
        catch (HttpRequestException)
        {
            return false;
        }
        catch (Exception ex)
        {
            throw new HttpClientException(
                $"Connection test to {apiName} failed",
                ex,
                apiName,
                config.BaseUrl);
        }
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

    private void ApplyHeaders(HttpRequestMessage request, Dictionary<string, string>? customHeaders, ApiConfiguration config)
    {
        // Apply default headers
        foreach (var header in config.DefaultHeaders)
            request.Headers.Add(header.Key, header.Value);

        // Apply custom headers
        if (customHeaders is not null)
        {
            foreach (var header in customHeaders)
                request.Headers.Add(header.Key, header.Value);
        }

        // Apply authentication
        if (!string.IsNullOrEmpty(config.ApiKey))
            request.Headers.Add("X-API-Key", config.ApiKey);

        if (!string.IsNullOrEmpty(config.BearerToken))
            request.Headers.Add("Authorization", $"Bearer {config.BearerToken}");
    }
}

/// <summary>
/// Configuration for an external API.
/// </summary>
public sealed class ApiConfiguration
{
    public string BaseUrl { get; set; } = string.Empty;
    public string? ApiKey { get; set; }
    public string? BearerToken { get; set; }
    public TimeSpan Timeout { get; set; } = TimeSpan.FromSeconds(30);
    public Dictionary<string, string> DefaultHeaders { get; set; } = new();
    public int MaxRetries { get; set; } = 3;
    public TimeSpan RetryDelay { get; set; } = TimeSpan.FromMilliseconds(100);

    public override string ToString() => $"ApiConfiguration {{ BaseUrl = {BaseUrl}, ApiKey = {ApiKey}, BearerToken = {BearerToken}, Timeout = {Timeout}, DefaultHeaders = {DefaultHeaders}, MaxRetries = {MaxRetries} }}";
}

/// <summary>
/// Response wrapper for API calls.
/// </summary>
public sealed class ApiResponse<T>
{
    public bool Success { get; set; }
    public T? Data { get; set; }
    public string? Message { get; set; }
    public Dictionary<string, string>? Headers { get; set; }
}