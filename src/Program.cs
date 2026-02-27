// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Data;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline;

/// <summary>
/// Entry point demonstrating resilience pipeline usage with various patterns.
/// </summary>
class Program
{
    static async Task Main(string[] args)
    {
        Console.WriteLine("========================================");
        Console.WriteLine("  DotNet Resilience Pipeline - Demo");
        Console.WriteLine("========================================\n");

        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddResiliencePipeline(builder =>
        {
            builder
                .WithCircuitBreaker("payment-circuit", policy =>
                {
                    policy.FailureThreshold = 5;
                    policy.OpenDuration = TimeSpan.FromSeconds(30);
                })
                .WithRetry("api-retry", policy =>
                {
                    policy.MaxRetries = 3;
                    policy.InitialDelay = TimeSpan.FromMilliseconds(100);
                    policy.Strategy = RetryPolicy.BackoffStrategy.Exponential;
                })
                .WithTimeout("operation-timeout", TimeSpan.FromSeconds(10))
                .WithBulkhead("resource-bulkhead", 10, 50)
                .WithFallback("graceful-fallback", policy =>
                {
                    policy.FallbackOnAnyException = true;
                    policy.FallbackTimeout = TimeSpan.FromSeconds(5);
                });
        });

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();
        var history = provider.GetRequiredService<ExecutionHistoryRepository>();

        // Example 1: Simple successful operation
        Console.WriteLine("Example 1: Simple Operation with Retry Policy");
        Console.WriteLine("=============================================\n");
        await RunSimpleOperation(pipeline, history);

        // Example 2: Circuit breaker demonstration
        Console.WriteLine("\nExample 2: Circuit Breaker Pattern");
        Console.WriteLine("==================================\n");
        await RunCircuitBreakerDemo(pipeline, history);

        // Example 3: Timeout demonstration
        Console.WriteLine("\nExample 3: Timeout Policy");
        Console.WriteLine("========================\n");
        await RunTimeoutDemo(pipeline, history);

        // Example 4: Bulkhead isolation
        Console.WriteLine("\nExample 4: Bulkhead Pattern");
        Console.WriteLine("===========================\n");
        await RunBulkheadDemo(pipeline, history);

        // Display statistics
        Console.WriteLine("\nPipeline Statistics");
        Console.WriteLine("===================");
        var stats = pipeline.GetStatistics();
        Console.WriteLine($"Total Executions: {stats.TotalExecutions}");
        Console.WriteLine($"Successful: {stats.SuccessfulExecutions}");
        Console.WriteLine($"Failed: {stats.FailedExecutions}");
        Console.WriteLine($"Success Rate: {stats.SuccessRate:F2}%");
        Console.WriteLine($"Registered Policies: {stats.PolicyCount}");

        // Health report
        Console.WriteLine("\nPipeline Health Report");
        Console.WriteLine("======================");
        var healthReport = ResiliencyHelper.GenerateHealthReport(pipeline, history);
        Console.WriteLine($"Health Status: {healthReport.HealthStatus}");
        Console.WriteLine($"Report Generated: {healthReport.ReportGeneratedAt:O}");
    }

    static async Task RunSimpleOperation(ResiliencyPipelineService pipeline, ExecutionHistoryRepository history)
    {
        var retryPolicy = pipeline.GetPolicyByName("api-retry") as RetryPolicy;

        var result = await pipeline.ExecuteAsync(
            async ct =>
            {
                await Task.Delay(50);
                return "Success: Data retrieved successfully";
            },
            retry: retryPolicy);

        Console.WriteLine($"Result: {result.Data}");
        Console.WriteLine($"Success: {result.IsSuccess}");
        Console.WriteLine($"Execution Time: {result.ExecutionTimeMs}ms");

        // Record to history
        var record = new ExecutionRecord
        {
            PolicyName = "simple-operation",
            PolicyId = retryPolicy?.Id ?? "",
            IsSuccess = result.IsSuccess,
            ExecutionTimeMs = result.ExecutionTimeMs
        };
        history.Record(record);
    }

    static async Task RunCircuitBreakerDemo(ResiliencyPipelineService pipeline, ExecutionHistoryRepository history)
    {
        var cbPolicy = pipeline.GetPolicyByName("payment-circuit") as CircuitBreakerPolicy;
        int failureCount = 0;

        for (int i = 0; i < 8; i++)
        {
            try
            {
                await pipeline.ExecuteAsync(
                    async ct =>
                    {
                        // Simulate failures for first 5 attempts
                        if (failureCount < 5)
                        {
                            failureCount++;
                            throw new Exception("Simulated payment failure");
                        }
                        await Task.Delay(10);
                        return true;
                    },
                    circuitBreaker: cbPolicy);

                Console.WriteLine($"Attempt {i + 1}: Success - Circuit state: {cbPolicy?.CurrentState}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Attempt {i + 1}: Failed - {ex.GetType().Name}");
            }
        }
    }

    static async Task RunTimeoutDemo(ResiliencyPipelineService pipeline, ExecutionHistoryRepository history)
    {
        var timeoutPolicy = pipeline.GetPolicyByName("operation-timeout") as TimeoutPolicy;

        // Fast operation
        try
        {
            var result = await pipeline.ExecuteAsync(
                async ct =>
                {
                    await Task.Delay(500, ct);
                    return "Completed";
                },
                timeout: timeoutPolicy);

            Console.WriteLine($"Fast Operation: {result.Data} in {result.ExecutionTimeMs}ms");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Fast Operation: {ex.GetType().Name} - {ex.Message}");
        }

        // Slow operation (will timeout)
        try
        {
            var result = await pipeline.ExecuteAsync(
                async ct =>
                {
                    await Task.Delay(15000, ct);
                    return "Completed";
                },
                timeout: timeoutPolicy);

            Console.WriteLine($"Slow Operation: {result.Data}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Slow Operation: {ex.GetType().Name} - {ex.Message}");
        }
    }

    static async Task RunBulkheadDemo(ResiliencyPipelineService pipeline, ExecutionHistoryRepository history)
    {
        var bulkheadPolicy = pipeline.GetPolicyByName("resource-bulkhead") as BulkheadPolicy;

        Console.WriteLine($"Bulkhead Capacity: {bulkheadPolicy?.MaxParallelization}/{bulkheadPolicy?.MaxQueueLength}");

        var tasks = new List<Task>();
        for (int i = 0; i < 15; i++)
        {
            int index = i;
            tasks.Add(Task.Run(async () =>
            {
                try
                {
                    var result = await pipeline.ExecuteAsync(
                        async ct =>
                        {
                            await Task.Delay(200, ct);
                            return $"Task {index} completed";
                        },
                        bulkhead: bulkheadPolicy);

                    Console.WriteLine($"  {result.Data}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"  Task {index} failed: {ex.GetType().Name}");
                }
            }));
        }

        await Task.WhenAll(tasks);
        Console.WriteLine($"Final Bulkhead State - Active: {bulkheadPolicy?.ActiveExecutions}, Queued: {bulkheadPolicy?.QueuedRequests}");
    }
}
