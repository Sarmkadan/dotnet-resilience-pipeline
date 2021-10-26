using System.Text.Json;
using System.Text.Json.Serialization;

namespace DotNetResiliencePipeline.Workers;

public static class MetricsCollectorWorkerJsonExtensions
{
    private static readonly JsonSerializerOptions _jsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public static string ToJson(this MetricsCollectorWorker value, bool indented = false)
    {
        return JsonSerializer.Serialize(value, _jsonSerializerOptions);
    }

    public static MetricsCollectorWorker? FromJson(string json)
    {
        return JsonSerializer.Deserialize<MetricsCollectorWorker>(json, _jsonSerializerOptions);
    }

    public static bool TryFromJson(string json, out MetricsCollectorWorker? value)
    {
        try
        {
            value = FromJson(json);
            return true;
        }
        catch (JsonException)
        {
            value = null;
            return false;
        }
    }
}
