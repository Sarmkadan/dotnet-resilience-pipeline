#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text.Json;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Data;

/// <summary>
/// JSON extension methods for <see cref="ExecutionRecord"/>.
/// </summary>
public static class ExecutionRecordJsonExtensions
{
    /// <summary>
    /// Converts an <see cref="ExecutionRecord"/> to its JSON representation.
    /// </summary>
    /// <param name="record">The execution record to convert.</param>
    /// <returns>JSON string representing the execution record.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="record"/> is null.</exception>
    public static string ToJson(this ExecutionRecord record)
    {
        if (record is null)
            throw new ArgumentNullException(nameof(record));

        return JsonSerializer.Serialize(record, JsonSerializerOptionsProvider.SharedOptions);
    }
}