#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetResiliencePipeline.Events;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="ResiliencyEventPublisher"/>.
/// </summary>
public static class ResiliencyEventPublisherJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };

    /// <summary>
    /// Serializes the <see cref="ResiliencyEventPublisher"/> to a JSON string.
    /// </summary>
    /// <param name="value">The publisher instance to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the publisher.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this ResiliencyEventPublisher value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);

        var options = indented
            ? new JsonSerializerOptions(_jsonSerializerOptions) { WriteIndented = true }
            : _jsonSerializerOptions;

        return JsonSerializer.Serialize(value, options);
    }

    /// <summary>
    /// Deserializes a JSON string to a <see cref="ResiliencyEventPublisher"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized publisher instance.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static ResiliencyEventPublisher FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);
        ArgumentException.ThrowIfNullOrWhiteSpace(json);

        return JsonSerializer.Deserialize<ResiliencyEventPublisher>(json, _jsonSerializerOptions)
            ?? throw new JsonException("Deserialization returned null for non-null input.");
    }

    /// <summary>
    /// Attempts to deserialize a JSON string to a <see cref="ResiliencyEventPublisher"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized publisher instance if successful.</param>
    /// <returns>True if deserialization succeeded; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
    public static bool TryFromJson(string json, out ResiliencyEventPublisher? value)
    {
        ArgumentNullException.ThrowIfNull(json);

        try
        {
            value = string.IsNullOrWhiteSpace(json)
                ? null
                : JsonSerializer.Deserialize<ResiliencyEventPublisher>(json, _jsonSerializerOptions);

            return value is not null;
        }
        catch
        {
            value = null;
            return false;
        }
    }
}