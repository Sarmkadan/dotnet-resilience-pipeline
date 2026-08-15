#nullable enable

using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Provides a shared <see cref="JsonSerializerOptions"/> instance used by JSON extension helpers
/// to ensure a consistent serialization contract across the library.
/// </summary>
internal static class JsonSerializerOptionsProvider
{
    /// <summary>
    /// The shared serializer options configured for camel‑case naming, null‑ignoring,
    /// reference handling, case‑insensitive property names, and enum‑as‑string conversion.
    /// </summary>
    internal static readonly JsonSerializerOptions SharedOptions = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        WriteIndented = false,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        PropertyNameCaseInsensitive = true,
        Converters = { new JsonStringEnumConverter(JsonNamingPolicy.CamelCase) }
    };
}
