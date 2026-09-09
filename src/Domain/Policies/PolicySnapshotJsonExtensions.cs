#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Provides JSON serialization extensions for <see cref="PolicySnapshot"/> instances.
/// </summary>
public static class PolicySnapshotJsonExtensions
{
    /// <summary>
    /// Serializes a <see cref="PolicySnapshot"/> instance to a JSON string.
    /// </summary>
    /// <param name="snapshot">The policy snapshot to serialize.</param>
    /// <returns>A JSON string representation of the policy snapshot.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public static string ToJson(this PolicySnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);

        return JsonSerializer.Serialize(snapshot, JsonSerializerOptionsProvider.SharedOptions);
    }
}