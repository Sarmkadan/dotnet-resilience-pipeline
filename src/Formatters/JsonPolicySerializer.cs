#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Formatters;

/// <summary>
/// Serializes and deserializes policies to/from JSON format.
/// Supports configuration persistence and API data exchange.
/// </summary>
public sealed class JsonPolicySerializer
{
    private readonly JsonSerializerOptions _options;

    public JsonPolicySerializer()
    {
        _options = new JsonSerializerOptions
        {
            WriteIndented = true,
            PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
            DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull
        };
    }

    /// <summary>
    /// Serializes a policy to JSON string.
    /// </summary>
    public string Serialize(ResiliencyPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(nameof(policy));
        var dto = MapToDto(policy);
        return JsonSerializer.Serialize(dto, _options);
    }

    /// <summary>
    /// Serializes multiple policies to JSON string.
    /// </summary>
    public string SerializeMultiple(IEnumerable<ResiliencyPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(nameof(policies));
        var dtos = policies.Select(MapToDto).ToList();
        return JsonSerializer.Serialize(dtos, _options);
    }

    /// <summary>
    /// Deserializes a JSON string to a policy.
    /// </summary>
    public ResiliencyPolicy? Deserialize(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(json));
        var dto = JsonSerializer.Deserialize<PolicyJson>(json, _options);
        return dto?.ToPolicy();
    }

    /// <summary>
    /// Serializes policy metrics to JSON.
    /// </summary>
    public string SerializeMetrics(object metrics)
    {
        ArgumentNullException.ThrowIfNull(nameof(metrics));
        return JsonSerializer.Serialize(metrics, _options);
    }

    /// <summary>
    /// Exports policies to a JSON file.
    /// </summary>
    public async Task ExportToFileAsync(List<ResiliencyPolicy> policies, string filePath)
    {
        ArgumentNullException.ThrowIfNull(nameof(policies));
        ArgumentException.ThrowIfNullOrEmpty(nameof(filePath));
        var json = SerializeMultiple(policies);
        await File.WriteAllTextAsync(filePath, json);
    }

    /// <summary>
    /// Imports policies from a JSON file.
    /// </summary>
    public async Task<List<ResiliencyPolicy>> ImportFromFileAsync(string filePath)
    {
        ArgumentException.ThrowIfNullOrEmpty(nameof(filePath));
        var json = await File.ReadAllTextAsync(filePath);
        var dtos = JsonSerializer.Deserialize<List<PolicyJson>>(json, _options);
        return dtos?.Select(dto => dto.ToPolicy()).OfType<ResiliencyPolicy>().ToList() ?? new();
    }

    private PolicyJson MapToDto(ResiliencyPolicy policy)
    {
        var dto = new PolicyJson
        {
            Id = policy.Id,
            Name = policy.Name,
            Type = policy.GetType().Name,
            IsEnabled = policy.IsEnabled,
            CreatedAt = DateTime.UtcNow
        };

        if (policy is CircuitBreakerPolicy cb)
        {
            dto.FailureThreshold = cb.FailureThreshold;
            dto.OpenDurationSeconds = (int)cb.OpenDuration.TotalSeconds;
            dto.SuccessThreshold = cb.SuccessThresholdInHalfOpen;
        }
        else if (policy is RetryPolicy retry)
        {
            dto.MaxRetries = retry.MaxRetries;
            dto.InitialDelayMs = (int)retry.InitialDelay.TotalMilliseconds;
            dto.Strategy = retry.Strategy.ToString();
            dto.BackoffMultiplier = retry.BackoffMultiplier;
        }
        else if (policy is TimeoutPolicy timeout)
        {
            dto.TimeoutSeconds = (int)timeout.Timeout.TotalSeconds;
        }
        else if (policy is BulkheadPolicy bulkhead)
        {
            dto.MaxParallelization = bulkhead.MaxParallelization;
            dto.MaxQueueLength = bulkhead.MaxQueueLength;
        }

        return dto;
    }
}

/// <summary>
/// JSON representation of a policy for serialization.
/// </summary>
public sealed class PolicyJson
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
    public bool IsEnabled { get; set; }
    public DateTime CreatedAt { get; set; }

    // CircuitBreaker
    public int? FailureThreshold { get; set; }
    public int? OpenDurationSeconds { get; set; }
    public int? SuccessThreshold { get; set; }

    // Retry
    public int? MaxRetries { get; set; }
    public int? InitialDelayMs { get; set; }
    public string? Strategy { get; set; }
    public double? BackoffMultiplier { get; set; }

    // Timeout
    public int? TimeoutSeconds { get; set; }

    // Bulkhead
    public int? MaxParallelization { get; set; }
    public int? MaxQueueLength { get; set; }

    public ResiliencyPolicy? ToPolicy()
    {
        return Type switch
        {
            "CircuitBreakerPolicy" => new CircuitBreakerPolicy(Name)
            {
                Id = Id,
                FailureThreshold = FailureThreshold ?? 5,
                OpenDuration = TimeSpan.FromSeconds(OpenDurationSeconds ?? 30),
                SuccessThresholdInHalfOpen = SuccessThreshold ?? 2,
                IsEnabled = IsEnabled
            },
            "RetryPolicy" => new RetryPolicy(Name)
            {
                Id = Id,
                MaxRetries = MaxRetries ?? 3,
                InitialDelay = TimeSpan.FromMilliseconds(InitialDelayMs ?? 100),
                IsEnabled = IsEnabled
            },
            "TimeoutPolicy" => new TimeoutPolicy(Name)
            {
                Id = Id,
                Timeout = TimeSpan.FromSeconds(TimeoutSeconds ?? 10),
                IsEnabled = IsEnabled
            },
            "BulkheadPolicy" => new BulkheadPolicy(Name)
            {
                Id = Id,
                MaxParallelization = MaxParallelization ?? 10,
                MaxQueueLength = MaxQueueLength ?? 50,
                IsEnabled = IsEnabled
            },
            "FallbackPolicy" => new FallbackPolicy(Name) { Id = Id, IsEnabled = IsEnabled },
            _ => null
        };
    }
}
