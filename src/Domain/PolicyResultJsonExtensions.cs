using System.Text.Json;
using System.Text.Json.Serialization.Metadata;

namespace DotNetResiliencePipeline.Domain;

/// <summary>
/// Provides JSON serialization and deserialization extensions for <see cref="PolicyResult"/>.
/// </summary>
public static class PolicyResultJsonExtensions
{
	private static readonly JsonSerializerOptions _options = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
		WriteIndented = false
	};

	private static JsonSerializerOptions GetOptions(bool indented)
	{
		var options = new JsonSerializerOptions(_options)
		{
			WriteIndented = indented
		};
		return options;
	}

	/// <summary>
	/// Serializes a <see cref="PolicyResult"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The policy result to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the policy result.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is <see langword="null"/>.</exception>
	public static string ToJson(this PolicyResult value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		return JsonSerializer.Serialize(value, GetOptions(indented));
	}

	/// <summary>
	/// Deserializes a JSON string to a <see cref="PolicyResult"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized policy result, or <see langword="null"/> if the JSON is empty or whitespace.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
	/// <exception cref="ArgumentException">Thrown when <paramref name="json"/> is empty or whitespace.</exception>
	/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
	public static PolicyResult? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);

		if (string.IsNullOrWhiteSpace(json))
		{
			return null;
		}

		return JsonSerializer.Deserialize<PolicyResult>(json, _options);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string to a <see cref="PolicyResult"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <param name="value">Receives the deserialized policy result if successful.</param>
	/// <returns><see langword="true"/> if deserialization succeeded; otherwise, <see langword="false"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is <see langword="null"/>.</exception>
	public static bool TryFromJson(string json, out PolicyResult? value)
	{
		ArgumentNullException.ThrowIfNull(json);

		try
		{
			if (string.IsNullOrWhiteSpace(json))
			{
				value = null;
				return true;
			}

			value = JsonSerializer.Deserialize<PolicyResult>(json, _options);
			return true;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}
