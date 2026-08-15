#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Exceptions;

/// <summary>
/// Provides System.Text.Json serialization and deserialization extensions for <see cref="ResiliencyException"/> and derived types.
/// </summary>
public static class ResiliencyExceptionJsonExtensions
{
    private const string WhitespaceOnlyMessage = "Input string contains only whitespace.";

    /// <summary>
    /// Serializes the <see cref="ResiliencyException"/> to a JSON string.
    /// </summary>
    /// <param name="value">The exception to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation.</param>
    /// <returns>A JSON string representation of the exception.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
    public static string ToJson(this ResiliencyException value, bool indented = false)
    {
        ArgumentNullException.ThrowIfNull(value);
        return JsonSerializer.Serialize(
            value,
            indented
                ? new JsonSerializerOptions(JsonSerializerOptionsProvider.SharedOptions) { WriteIndented = true }
                : JsonSerializerOptionsProvider.SharedOptions);
    }

    /// <summary>
    /// Deserializes a <see cref="ResiliencyException"/> from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized exception, or null if the JSON is invalid.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null, empty, or whitespace.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is malformed.</exception>
    public static ResiliencyException? FromJson(string json)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        if (string.IsNullOrWhiteSpace(json))
        {
            throw new ArgumentException(WhitespaceOnlyMessage, nameof(json));
        }

        try
        {
            return JsonSerializer.Deserialize<ResiliencyException>(json, JsonSerializerOptionsProvider.SharedOptions);
        }
        catch (JsonException ex)
        {
            throw new JsonException("Failed to deserialize ResiliencyException from JSON.", ex);
        }
    }

    /// <summary>
    /// Attempts to deserialize a <see cref="ResiliencyException"/> from a JSON string.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">The deserialized exception, or null if deserialization fails.</param>
    /// <returns>True if deserialization succeeds; otherwise, false.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is null or empty.</exception>
    public static bool TryFromJson(string json, out ResiliencyException? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);
        try
        {
            value = JsonSerializer.Deserialize<ResiliencyException>(json, JsonSerializerOptionsProvider.SharedOptions);
            return value is not null;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
