#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Domain;

/// <summary>
/// Provides JSON serialization extensions for <see cref="PipelineMetricsSnapshot"/>.
/// </summary>
public static class PipelineMetricsSnapshotJsonExtensions
{
    /// <summary>
    /// Serializes a <see cref="PipelineMetricsSnapshot"/> instance to a JSON string.
    /// </summary>
    /// <param name="snapshot">The pipeline metrics snapshot to serialize.</param>
    /// <returns>A JSON string representation of the pipeline metrics snapshot.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="snapshot"/> is <see langword="null"/>.</exception>
    public static string ToJson(this PipelineMetricsSnapshot snapshot)
    {
        ArgumentNullException.ThrowIfNull(snapshot);
        return JsonSerializer.Serialize(snapshot, JsonSerializerOptionsProvider.SharedOptions);
    }
}