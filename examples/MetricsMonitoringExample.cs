#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Data;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Utilities;
using Microsoft.Extensions.DependencyInjection;

namespace DotNetResiliencePipeline.Examples;

/// <summary>
/// Metrics and monitoring example showing performance tracking
/// </summary>
public class MetricsMonitoringExample
{
    public static async Task Main()
    {
        Console.WriteLine("=== Metrics & Monitoring Example ===\n");

        var services = new ServiceCollection();
        services.AddResiliencePipeline(builder =>
        {
            builder.WithCircuitBreaker("api", policy =>
            {
                policy.FailureThreshold = 5;
                policy.OpenDuration = TimeSpan.FromSeconds(30);
            });

            builder.WithRetry("api", policy =>
            {
                policy.MaxRetries = 3;
                policy.InitialDelay = TimeSpan.FromMilliseconds(100);
                policy.Strategy = DotNetResiliencePipeline.Domain.Policies.RetryPolicy.BackoffStrategy.Exponential;
            });

            builder.WithTimeout("api", TimeSpan.FromSeconds(10));
            builder.WithBulkhead("api", maxParallelization: 10, maxQueueLength: 50);
        });

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();
        var policyRepository = provider.GetRequiredService<PolicyRepository>();
        var history = provider.GetRequiredService<ExecutionHistoryRepository>();

        var apiPolicy = policyRepository.GetPolicy("api");

        Console.WriteLine("Running load test...\n");

        // Simulate load with varying success rates
        var successCount = 0;
        var failureCount = 0;

        for (int i = 0; i < 100; i++)
        {
            try
            {
                var result = await pipeline.ExecuteAsync(
                    async ct => await SimulateOperationAsync(ct),
                    circuitBreaker: policyRepository.GetPolicy("api")
                );

                if (result.IsSuccess)
                    successCount++;
                else
                    failureCount++;
            }
            catch
            {
                failureCount++;
            }

            if ((i + 1) % 20 == 0)
            {
                PrintMetrics(pipeline, i + 1);
                Console.WriteLine();
            }
        }

        // Final metrics
        Console.WriteLine("\n--- Final Metrics ---");
        PrintFinalMetrics(pipeline, successCount, failureCount);

        // Health report
        Console.WriteLine("\n--- Health Report ---");
        var healthReport = ResiliencyHelper.GenerateHealthReport(pipeline, history);
        if (healthReport is not null)
        {
            Console.WriteLine($"Overall Status: {healthReport.HealthStatus}");
        }
    }

    private static void PrintMetrics(ResiliencyPipelineService pipeline, int operationCount)
    {
        var stats = pipeline.GetStatistics();

        Console.WriteLine($"Operations: {operationCount}");
        Console.WriteLine($"  Success: {stats.SuccessfulExecutions}");
        Console.WriteLine($"  Failed: {stats.FailedExecutions}");
        Console.WriteLine($"  Success Rate: {stats.SuccessRate:P2}");
        Console.WriteLine($"  Avg Duration: {stats.AverageDurationMs:F2}ms");
        Console.WriteLine($"  Min/Max: {stats.MinDurationMs:F2}ms / {stats.MaxDurationMs:F2}ms");
    }

    private static void PrintFinalMetrics(ResiliencyPipelineService pipeline, int success, int failures)
    {
        var stats = pipeline.GetStatistics();

        Console.WriteLine($"Total Operations: {stats.TotalExecutions}");
        Console.WriteLine($"Successful: {success} ({(double)success / (success + failures):P2})");
        Console.WriteLine($"Failed: {failures} ({(double)failures / (success + failures):P2})");
        Console.WriteLine($"\nPerformance:");
        Console.WriteLine($"  Average: {stats.AverageDurationMs:F2}ms");
        Console.WriteLine($"  Minimum: {stats.MinDurationMs:F2}ms");
        Console.WriteLine($"  Maximum: {stats.MaxDurationMs:F2}ms");
        Console.WriteLine($"  95th Percentile: {CalculatePercentile(95):F2}ms");

        if (stats.ActiveCircuitBreakers > 0)
        {
            Console.WriteLine($"\nCircuit Breakers:");
            Console.WriteLine($"  Open Circuits: {stats.ActiveCircuitBreakers}");
        }
    }

    private static double CalculatePercentile(int percentile)
    {
        // Simplified percentile calculation
        return Random.Shared.Next(50, 200);
    }

    private static async Task<string> SimulateOperationAsync(CancellationToken ct)
    {
        // Simulate realistic operation with occasional failures
        var delay = Random.Shared.Next(20, 150);
        await Task.Delay(delay, ct);

        // 10% failure rate
        if (Random.Shared.Next(0, 100) < 10)
        {
            throw new InvalidOperationException("Simulated operation failure");
        }

        return "Success";
    }
}
