#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Middleware;

/// <summary>
/// Provides JSON serialization extensions for <see cref="RateLimitStatus"/>.
/// </summary>
public static class RateLimitStatusJsonExtensions
{
	/// <summary>
	/// Serializes the specified rate limit status to a JSON string.
	/// </summary>
	/// <param name="status">The rate limit status to serialize.</param>
	/// <returns>A JSON string representation of <paramref name="status"/>.</returns>
	/// <exception cref="ArgumentNullException">Thrown when <paramref name="status"/> is <see langword="null"/>.</exception>
	public static string ToJson(this RateLimitStatus status)
	{
		ArgumentNullException.ThrowIfNull(status);

		return JsonSerializer.Serialize(status, JsonSerializerOptionsProvider.SharedOptions);
	}
}
