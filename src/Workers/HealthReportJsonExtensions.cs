#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Workers;

/// <summary>
/// Provides JSON serialization extensions for <see cref="HealthReport"/>.
/// </summary>
public static class HealthReportJsonExtensions
{
    /// <summary>
    /// Serializes the specified <see cref="HealthReport"/> to a JSON string.
    /// </summary>
    /// <param name="report">The health report to serialize.</param>
    /// <returns>A JSON string representation of the health report.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="report"/> is null.</exception>
    public static string ToJson(this HealthReport report)
    {
        ArgumentNullException.ThrowIfNull(report);
        return JsonSerializer.Serialize(report, JsonSerializerOptionsProvider.SharedOptions);
    }
}
