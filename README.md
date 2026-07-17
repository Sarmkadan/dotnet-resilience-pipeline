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

## CircuitBreakerDashboardTests

The `CircuitBreakerDashboardTests` class validates the functionality of the `CircuitBreakerDashboardController`, ensuring accurate reporting and state management of circuit breakers within the resilience pipeline. These tests verify dashboard metrics, breaker status queries, and circuit state transitions, confirming the system correctly handles various operational scenarios.

Here's a realistic usage example illustrating the behavior verified by its public members:

```csharp
using DotNetResiliencePipeline.Api.Controllers;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Domain.Policies;

public class CircuitBreakerDashboardUsage
{
    public async Task RunExamples()
    {
        var pipeline = new ResiliencyPipelineService();
        var controller = new CircuitBreakerDashboardController(pipeline, new CircuitBreakerService());

        // Verifies: GetDashboard_NoPolicies_ReturnsEmptyHealthyDashboard
        var dashboard = await controller.GetDashboardAsync();

        // Verifies: GetDashboard_WithClosedBreaker_ReturnsClosedCount
        pipeline.RegisterPolicy(new CircuitBreakerPolicy("svc-1") { FailureThreshold = 5 });
        var closedDashboard = await controller.GetDashboardAsync();

        // Verifies: GetDashboard_WithOpenBreaker_ReturnsOpenCountAndDegradedHealth
        var p = new CircuitBreakerPolicy("svc-2") { FailureThreshold = 1 };
        pipeline.RegisterPolicy(p);
        p.RecordFailure();
        var openDashboard = await controller.GetDashboardAsync();

        // Verifies: GetBreakerStatus_UnknownName_ReturnsNotFound
        await controller.GetBreakerStatusAsync("unknown");

        // Verifies: GetBreakerStatus_ExistingBreaker_ReturnsCorrectState
        await controller.GetBreakerStatusAsync("svc-1");

        // Verifies: ResetBreaker_OpenCircuit_TransitionsToClosedState
        await controller.ResetBreakerAsync("svc-2");

        // Verifies: GetOpenBreakers_MixedStates_ReturnsOnlyOpenBreakers
        await controller.GetOpenBreakersAsync();

        // Verifies: GetDashboard_TripCountAccumulates_AcrossMultipleTrips
        p.RecordFailure(); 
        await controller.GetDashboardAsync();
    }
}
```

This example demonstrates how to use the `CircuitBreakerDashboardController` to monitor circuit breaker states, handle state transitions, and retrieve dashboard metrics. It directly maps to the scenarios covered in the `CircuitBreakerDashboardTests` suite.

// ... rest of existing content
