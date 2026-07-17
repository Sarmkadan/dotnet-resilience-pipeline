using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetResiliencePipeline.Workers;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="MetricsCollectorWorker"/>.
/// </summary>
public static class MetricsCollectorWorkerJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    /// <summary>
    /// Serializes the specified <see cref="MetricsCollectorWorker"/> instance to a JSON string.
    /// </summary>
    /// <param name="value">The <see cref="MetricsCollectorWorker"/> instance to serialize.</param>
    /// <param name="indented">Indicates whether the JSON output should be indented. Currently unused.</param>
    /// <returns>A JSON string representation of the <paramref name="value"/>.</returns>
    public static string ToJson(this MetricsCollectorWorker value, bool indented = false)
    {
        return JsonSerializer.Serialize(value, _jsonSerializerOptions);
    }

    /// <summary>
    /// Deserializes the specified JSON string into a <see cref="MetricsCollectorWorker"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="MetricsCollectorWorker"/> instance, or <c>null</c> if deserialization fails.</returns>
    public static MetricsCollectorWorker? FromJson(string json)
    {
        return JsonSerializer.Deserialize<MetricsCollectorWorker>(json, _jsonSerializerOptions);
    }

    /// <summary>
    /// Attempts to deserialize the specified JSON string into a <see cref="MetricsCollectorWorker"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">When this method returns, contains the deserialized <see cref="MetricsCollectorWorker"/> instance if successful; otherwise, <c>null</c>.</param>
    /// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
    public static bool TryFromJson(string json, out MetricsCollectorWorker? value)
    {
        try
        {
            value = FromJson(json);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
