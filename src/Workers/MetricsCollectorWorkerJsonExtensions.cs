using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetResiliencePipeline.Workers;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="MetricsCollectorWorker"/>.
/// </summary>
public static class MetricsCollectorWorkerJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonSerializerOptions = new(JsonSerializerDefaults.General)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false
	};

	/// <summary>
	/// Serializes the specified <see cref="MetricsCollectorWorker"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The <see cref="MetricsCollectorWorker"/> instance to serialize.</param>
	/// <param name="indented">Indicates whether the JSON output should be indented.</param>
	/// <returns>A JSON string representation of the <paramref name="value"/>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="value"/> is <c>null</c>.</exception>
	public static string ToJson(this MetricsCollectorWorker value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = new JsonSerializerOptions(_jsonSerializerOptions)
		{
			WriteIndented = indented
		};
		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes the specified JSON string into a <see cref="MetricsCollectorWorker"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized <see cref="MetricsCollectorWorker"/> instance.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
	/// <exception cref="JsonException">The JSON is invalid or cannot be deserialized to a <see cref="MetricsCollectorWorker"/>.</exception>
	public static MetricsCollectorWorker FromJson(string json)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		return JsonSerializer.Deserialize<MetricsCollectorWorker>(json, _jsonSerializerOptions)
			?? throw new JsonException("Deserialization returned null for non-nullable type MetricsCollectorWorker.");
	}

	/// <summary>
	/// Attempts to deserialize the specified JSON string into a <see cref="MetricsCollectorWorker"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">When this method returns, contains the deserialized <see cref="MetricsCollectorWorker"/> instance if successful; otherwise, <c>null</c>.</param>
	/// <returns><c>true</c> if deserialization succeeded; otherwise, <c>false</c>.</returns>
	/// <exception cref="ArgumentNullException"><paramref name="json"/> is <c>null</c>.</exception>
	public static bool TryFromJson(string json, out MetricsCollectorWorker? value)
	{
		ArgumentException.ThrowIfNullOrEmpty(json);

		try
		{
			value = JsonSerializer.Deserialize<MetricsCollectorWorker>(json, _jsonSerializerOptions);
			return true;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}