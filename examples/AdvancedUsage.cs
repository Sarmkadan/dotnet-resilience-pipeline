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
/// Advanced usage example demonstrating fluent configuration, custom fallback actions,
/// and complex bulkhead/circuit breaker scenarios.
/// </summary>
public sealed class AdvancedUsageExample
{
    public static async Task Main()
    {
        Console.WriteLine("=== DotNet Resilience Pipeline - Advanced Usage Example ===\n");

        // Setup dependency injection
        var services = new ServiceCollection();
        
        // Use the fluent builder with advanced configurations
        services.AddResiliencePipeline(builder =>
        {
            // Circuit Breaker with advanced failure threshold
            builder.WithCircuitBreaker("advanced-api", policy =>
            {
                policy.FailureThreshold = 5;
                policy.OpenDuration = TimeSpan.FromSeconds(30);
            });

            // Retry with exponential backoff and max delay
            builder.WithRetry("advanced-api", policy =>
            {
                policy.MaxRetries = 3;
                policy.InitialDelay = TimeSpan.FromMilliseconds(200);
                policy.Strategy = RetryPolicy.BackoffStrategy.Exponential;
                policy.MaxDelay = TimeSpan.FromSeconds(5);
            });

            // Bulkhead for resource isolation
            builder.WithBulkhead("advanced-api", maxParallelization: 10, maxQueueLength: 20);

            // Fallback policy configured with an asynchronous action
            builder.WithFallback("advanced-api");
            builder.WithFallbackAction<string>(async ct => 
            {
                Console.WriteLine("  [Fallback] Executing fallback logic...");
                await Task.Delay(50, ct); // Simulate fallback delay
                return "Fallback Result";
            });
        });

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();
        var policyRepository = provider.GetRequiredService<PolicyRepository>();

        // Retrieve configured policies
        var cbPolicy = policyRepository.GetPolicy<CircuitBreakerPolicy>("advanced-api");
        var fallbackPolicy = policyRepository.GetPolicy<FallbackPolicy>("advanced-api");

        Console.WriteLine("--- Executing Advanced Scenario ---");

        // Execute operation with all policies, including fallback
        var result = await pipeline.ExecuteAsync(
            async ct => await SimulateFragileApiCallAsync(ct),
            circuitBreaker: cbPolicy,
            fallback: fallbackPolicy
        );

        Console.WriteLine($"\nResult: {result.Value}");
        Console.WriteLine($"Success: {result.IsSuccess}");
        Console.WriteLine($"Error: {result.Error?.Message ?? "None"}");
    }

    private static async Task<string> SimulateFragileApiCallAsync(CancellationToken ct)
    {
        await Task.Delay(50, ct);
        // Randomly throw an exception to trigger retry/fallback/circuit breaker
        if (Random.Shared.Next(0, 3) == 0)
        {
            throw new Exception("Critical service failure!");
        }
        return "Successful Result";
    }
}
