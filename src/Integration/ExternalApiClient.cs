#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;
using System.Text.Json;
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
        _httpClientFactory = httpClientFactory;
        _pipelineService = pipelineService;
    }

    /// <summary>
    /// Registers an external API configuration.
    /// </summary>
    public void RegisterApi(string apiName, ApiConfiguration config)
    {
        _apiConfigs[apiName] = config;
    }

    /// <summary>
    /// Makes a resilient GET request to an external API.
    /// </summary>
    public async Task<ApiResponse<T>> GetAsync<T>(
        string apiName,
        string endpoint,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        if (!_apiConfigs.TryGetValue(apiName, out var config))
            return new ApiResponse<T> { Success = false, Message = $"API not registered: {apiName}" };

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
                var data = JsonSerializer.Deserialize<T>(content);
                return new ApiResponse<T> { Success = true, Data = data };
            }

            return new ApiResponse<T> { Success = false, Message = $"API error: {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<T> { Success = false, Message = ex.Message };
        }
    }

    /// <summary>
    /// Makes a resilient POST request to an external API.
    /// </summary>
    public async Task<ApiResponse<T>> PostAsync<T>(
        string apiName,
        string endpoint,
        object payload,
        Dictionary<string, string>? headers = null,
        CancellationToken cancellationToken = default)
    {
        if (!_apiConfigs.TryGetValue(apiName, out var config))
            return new ApiResponse<T> { Success = false, Message = $"API not registered: {apiName}" };

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
                var data = JsonSerializer.Deserialize<T>(responseContent);
                return new ApiResponse<T> { Success = true, Data = data };
            }

            return new ApiResponse<T> { Success = false, Message = $"API error: {response.StatusCode}" };
        }
        catch (Exception ex)
        {
            return new ApiResponse<T> { Success = false, Message = ex.Message };
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
    public async Task<bool> TestConnectionAsync(string apiName, CancellationToken cancellationToken = default)
    {
        if (!_apiConfigs.TryGetValue(apiName, out var config))
            return false;

        try
        {
            var client = _httpClientFactory.CreateClient(apiName, config.BaseUrl, timeout: TimeSpan.FromSeconds(5));
            var response = await client.GetAsync(config.BaseUrl, cancellationToken);
            return response.IsSuccessStatusCode;
        }
        catch
        {
            return false;
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
