#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Data;
using DotNetResiliencePipeline.Services;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetResiliencePipeline.Examples;

/// <summary>
/// Bulkhead pattern example demonstrating resource isolation
/// </summary>
public sealed class BulkheadPatternExample
{
    public static async Task Main()
    {
        Console.WriteLine("=== Bulkhead Pattern - Resource Isolation Example ===\n");

        // Setup with bulkhead policy
        var services = new ServiceCollection();
        services.AddResiliencePipeline(builder =>
        {
            builder.WithBulkhead("database", maxParallelization: 5, maxQueueLength: 20);
            builder.WithBulkhead("api", maxParallelization: 10, maxQueueLength: 50);
        });

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();
        var policyRepository = provider.GetRequiredService<PolicyRepository>();

        var dbBulkhead = policyRepository.GetPolicy<BulkheadPolicy>("database");
        var apiBulkhead = policyRepository.GetPolicy<BulkheadPolicy>("api");

        Console.WriteLine("Bulkhead Configuration:");
        Console.WriteLine($"  Database: max={dbBulkhead?.MaxParallelization}, queue={dbBulkhead?.MaxQueueLength}");
        Console.WriteLine($"  API: max={apiBulkhead?.MaxParallelization}, queue={apiBulkhead?.MaxQueueLength}\n");

        // Simulate concurrent database operations
        Console.WriteLine("--- Database Operations (Limited to 5 concurrent) ---");
        var dbTasks = new List<Task>();
        for (int i = 1; i <= 15; i++)
        {
            var taskId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    var result = await pipeline.ExecuteAsync(
                        async ct => await PerformDatabaseQuery(taskId, ct),
                        bulkhead: dbBulkhead
                    );

                    if (result.IsSuccess)
                        Console.WriteLine($"  Task {taskId}: ✓ Completed in {result.Duration.TotalMilliseconds}ms");
                    else
                        Console.WriteLine($"  Task {taskId}: ✗ Failed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Task {taskId}: ⚠ {ex.GetType().Name}");
                }
            });
            dbTasks.Add(task);

            // Stagger task submissions
            await Task.Delay(50);
        }

        await Task.WhenAll(dbTasks);

        Console.WriteLine("\n--- API Requests (Limited to 10 concurrent) ---");
        var apiTasks = new List<Task>();
        for (int i = 1; i <= 25; i++)
        {
            var taskId = i;
            var task = Task.Run(async () =>
            {
                try
                {
                    var result = await pipeline.ExecuteAsync(
                        async ct => await CallExternalApi(taskId, ct),
                        bulkhead: apiBulkhead
                    );

                    if (result.IsSuccess)
                        Console.WriteLine($"  Request {taskId}: ✓ Completed in {result.Duration.TotalMilliseconds}ms");
                    else
                        Console.WriteLine($"  Request {taskId}: ✗ Failed");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Request {taskId}: ⚠ {ex.GetType().Name}");
                }
            });
            apiTasks.Add(task);

            // Stagger task submissions
            await Task.Delay(30);
        }

        await Task.WhenAll(apiTasks);

        // Display final statistics
        Console.WriteLine("\n--- Final Bulkhead State ---");
        Console.WriteLine($"Database - Current: {dbBulkhead?.CurrentExecutions}/{dbBulkhead?.MaxParallelization}, Queue: {dbBulkhead?.CurrentQueueLength}");
        Console.WriteLine($"API - Current: {apiBulkhead?.CurrentExecutions}/{apiBulkhead?.MaxParallelization}, Queue: {apiBulkhead?.CurrentQueueLength}");
    }

    private static async Task<string> PerformDatabaseQuery(int queryId, CancellationToken ct)
    {
        // Simulate database operation taking 200-300ms
        await Task.Delay(Random.Shared.Next(200, 300), ct);
        return $"Query {queryId} Result";
    }

    private static async Task<string> CallExternalApi(int requestId, CancellationToken ct)
    {
        // Simulate API call taking 100-200ms
        await Task.Delay(Random.Shared.Next(100, 200), ct);
        return $"API {requestId} Response";
    }
}
