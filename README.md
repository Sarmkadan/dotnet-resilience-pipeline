// ... existing content ...

## MetricsExporterTests

The `MetricsExporterTests` class provides comprehensive unit tests for the `MetricsExporter` class, verifying its functionality in exporting metrics snapshots to JSON, CSV, and Prometheus formats. These tests cover various scenarios, including valid snapshots, null inputs, and expected output formats.

Here's a realistic usage example based on its real public members:

```csharp
using DotNetResiliencePipeline.Formatters;
using DotNetResiliencePipeline.Tests;
using DotNetResiliencePipeline.Domain;

class Program
{
    static void Main()
    {
        var exporter = new MetricsExporter();
        var snapshot = MetricsExporterTests.BuildSnapshot();

        // JSON export
        var json = exporter.ExportJson(snapshot);
        Console.WriteLine(json);

        // CSV export
        var csv = exporter.ExportCsv(snapshot);
        Console.WriteLine(csv);

        // Prometheus export
        var prom = exporter.ExportPrometheus(snapshot);
        Console.WriteLine(prom);

        // Error handling
        try
        {
            exporter.ExportJson(null);
        }
        catch (ArgumentNullException ex)
        {
            Console.WriteLine(ex.Message);
        }
    }
}
```

This example demonstrates how to create an instance of the `MetricsExporter` class and use it to export a `PipelineMetricsSnapshot` to different formats. It also showcases error handling for null input scenarios.

// ... rest of existing content
