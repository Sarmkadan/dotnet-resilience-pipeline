#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Provides JSON serialization extensions for <see cref="PipelineStatistics"/> instances.
/// </summary>
public static class PipelineStatisticsJsonExtensions
{
    /// <summary>
    /// Serializes pipeline statistics to a JSON string using the shared serializer options.
    /// </summary>
    /// <param name="statistics">The pipeline statistics to serialize.</param>
    /// <returns>A JSON string representation of the pipeline statistics.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="statistics"/> is null.</exception>
    public static string ToJson(this PipelineStatistics statistics)
    {
        ArgumentNullException.ThrowIfNull(statistics);

        return JsonSerializer.Serialize(statistics, JsonSerializerOptionsProvider.SharedOptions);
    }
}
