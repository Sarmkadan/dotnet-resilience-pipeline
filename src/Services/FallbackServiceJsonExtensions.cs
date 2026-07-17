#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="FallbackService"/>.
/// </summary>
public static class FallbackServiceJsonExtensions
{
  private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
  {
    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    WriteIndented = false,
    DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    ReferenceHandler = ReferenceHandler.IgnoreCycles
  };

  /// <summary>
  /// Converts a <see cref="FallbackService"/> instance to its JSON representation.
  /// </summary>
  /// <param name="value">The fallback service instance to serialize.</param>
  /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
  /// <returns>A JSON string representing the fallback service.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
  public static string ToJson(this FallbackService value, bool indented = false)
  {
    ArgumentNullException.ThrowIfNull(value);

    var options = indented
      ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
      : _jsonOptions;

    return JsonSerializer.Serialize(value, options);
  }

  /// <summary>
  /// Deserializes a JSON string to a <see cref="FallbackService"/> instance.
  /// </summary>
  /// <param name="json">The JSON string to deserialize.</param>
  /// <returns>The deserialized fallback service instance, or null if the JSON is empty.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
  /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
  public static FallbackService? FromJson(string json)
  {
    ArgumentNullException.ThrowIfNull(json);

    return string.IsNullOrWhiteSpace(json)
      ? null
      : JsonSerializer.Deserialize<FallbackService>(json, _jsonOptions);
  }

  /// <summary>
  /// Attempts to deserialize a JSON string to a <see cref="FallbackService"/> instance.
  /// </summary>
  /// <param name="json">The JSON string to deserialize.</param>
  /// <param name="value">Receives the deserialized fallback service instance if successful.</param>
  /// <returns>True if deserialization succeeded; otherwise, false.</returns>
  /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
  public static bool TryFromJson(string json, out FallbackService? value)
    => TryFromJson(json, _jsonOptions, out value);

  /// <summary>
  /// Attempts to deserialize a JSON string to a <see cref="FallbackService"/> instance using custom options.
  /// </summary>
  /// <param name="json">The JSON string to deserialize.</param>
  /// <param name="options">The JSON serialization options to use.</param>
  /// <param name="value">Receives the deserialized fallback service instance if successful.</param>
  /// <returns>True if deserialization succeeded; otherwise, false.</returns>
  public static bool TryFromJson(string json, JsonSerializerOptions options, out FallbackService? value)
  {
    ArgumentNullException.ThrowIfNull(options);
    value = null;

    if (string.IsNullOrWhiteSpace(json))
    {
      return true;
    }

    try
    {
      value = JsonSerializer.Deserialize<FallbackService>(json, options);
      return true;
    }
    catch (JsonException)
    {
      return false;
    }
  }
}