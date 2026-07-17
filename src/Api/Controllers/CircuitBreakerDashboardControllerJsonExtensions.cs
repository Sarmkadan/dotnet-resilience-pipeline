#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =====================================================================

using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace DotNetResiliencePipeline.Api.Controllers;

/// <summary>
/// Provides System.Text.Json serialization extensions for <see cref="CircuitBreakerDashboardController"/>
/// and its related DTO types.
/// </summary>
public static class CircuitBreakerDashboardControllerJsonExtensions
{
	private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web)
	{
		PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
		WriteIndented = false,
		TypeInfoResolver = new DefaultJsonTypeInfoResolver(),
		ReferenceHandler = ReferenceHandler.IgnoreCycles,
		NumberHandling = JsonNumberHandling.AllowNamedFloatingPointLiterals
	};

	/// <summary>
	/// Serializes the <see cref="CircuitBreakerDashboardController"/> instance to a JSON string.
	/// </summary>
	/// <param name="value">The controller instance to serialize.</param>
	/// <param name="indented">Whether to format the JSON with indentation for readability.</param>
	/// <returns>A JSON string representation of the controller.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="value"/> is null.</exception>
	public static string ToJson(this CircuitBreakerDashboardController value, bool indented = false)
	{
		ArgumentNullException.ThrowIfNull(value);

		var options = indented
			? new JsonSerializerOptions(_jsonOptions) { WriteIndented = true }
			: _jsonOptions;

		return JsonSerializer.Serialize(value, options);
	}

	/// <summary>
	/// Deserializes a JSON string into a <see cref="CircuitBreakerDashboardController"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize.</param>
	/// <returns>The deserialized controller instance, or null if deserialization fails.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	/// <exception cref="JsonException">Thrown when the JSON is invalid or cannot be deserialized.</exception>
	public static CircuitBreakerDashboardController? FromJson(string json)
	{
		ArgumentNullException.ThrowIfNull(json);

		return JsonSerializer.Deserialize<CircuitBreakerDashboardController>(json, _jsonOptions);
	}

	/// <summary>
	/// Attempts to deserialize a JSON string into a <see cref="CircuitBreakerDashboardController"/> instance.
	/// </summary>
	/// <param name="json">The JSON string to deserialize. Must not be null or whitespace.</param>
	/// <param name="value">Receives the deserialized controller instance if successful; otherwise, null.</param>
	/// <returns>True if deserialization succeeds; otherwise, false.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="json"/> is null.</exception>
	public static bool TryFromJson(string json, out CircuitBreakerDashboardController? value)
	{
		ArgumentNullException.ThrowIfNull(json);

		try
		{
			value = JsonSerializer.Deserialize<CircuitBreakerDashboardController>(json, _jsonOptions);
			return true;
		}
		catch (JsonException)
		{
			value = null;
			return false;
		}
	}
}