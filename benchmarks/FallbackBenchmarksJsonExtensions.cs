using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotnetResilience.Pipeline.Benchmarks;

/// <summary>
/// Provides JSON serialization and deserialization helpers for <see cref="FallbackBenchmarks"/>.
/// </summary>
public static class FallbackBenchmarksJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>
    /// Serializes the specified <see cref="FallbackBenchmarks"/> value to a JSON string.
    /// </summary>
    /// <param name="value">The value to serialize.</param>
    /// <param name="indented">Whether to format the JSON with indentation for readability.</param>
    /// <returns>A JSON string representation of the value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
    public static string ToJson(this FallbackBenchmarks value, bool indented = false) =>
        value is null
            ? throw new ArgumentNullException(nameof(value))
            : JsonSerializer.Serialize(value, GetJsonOptions(indented));

    /// <summary>
    /// Deserializes a JSON string into a <see cref="FallbackBenchmarks"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <returns>The deserialized <see cref="FallbackBenchmarks"/> instance, or <see langword="null"/> if the JSON represents a null value.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    /// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
    public static FallbackBenchmarks? FromJson(string json)
    {
        ArgumentNullException.ThrowIfNull(json);

        return JsonSerializer.Deserialize<FallbackBenchmarks>(json, _jsonOptions);
    }

    /// <summary>
    /// Attempts to deserialize a JSON string into a <see cref="FallbackBenchmarks"/> instance.
    /// </summary>
    /// <param name="json">The JSON string to deserialize.</param>
    /// <param name="value">Receives the deserialized value if successful; otherwise, <see langword="null"/>.</param>
    /// <returns><see langword="true"/> if deserialization succeeds; otherwise, <see langword="false"/>.</returns>
    /// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is <see langword="null"/> or empty.</exception>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
    public static bool TryFromJson(string json, out FallbackBenchmarks? value)
    {
        ArgumentException.ThrowIfNullOrEmpty(json);

        try
        {
            value = JsonSerializer.Deserialize<FallbackBenchmarks>(json, _jsonOptions);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }

    private static JsonSerializerOptions GetJsonOptions(bool indented) =>
        indented
            ? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
            : _jsonOptions;
}