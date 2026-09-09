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
/// Basic usage example demonstrating circuit breaker and retry patterns
/// </summary>
public sealed class BasicUsageExample
{
    /// <summary>
    /// Demonstrates the basic usage of the resilience pipeline, covering the following scenarios:
    /// 1. Successful operations with Circuit Breaker, Retry, and Timeout policies active.
    /// 2. Failed operations that trigger Retry logic with exponential backoff.
    /// 3. Retrieval and display of pipeline execution statistics, including success rates and average duration.
    /// </summary>
    public static async Task Main()
    {
        Console.WriteLine("=== DotNet Resilience Pipeline - Basic Usage Example ===\n");

        // Setup dependency injection
        var services = new ServiceCollection();
        services.AddResiliencePipeline(builder =>
        {
            // Configure circuit breaker for external API calls
            builder.WithCircuitBreaker("external-api", policy =>
            {
                policy.FailureThreshold = 3;
                policy.OpenDuration = TimeSpan.FromSeconds(10);
                policy.SuccessThresholdInHalfOpen = 2;
            });

            // Configure retry with exponential backoff
            builder.WithRetry("external-api", policy =>
            {
                policy.MaxRetries = 2;
                policy.InitialDelay = TimeSpan.FromMilliseconds(100);
                policy.Strategy = RetryPolicy.BackoffStrategy.Exponential;
                policy.BackoffMultiplier = 2.0;
            });

            // Configure timeout for operations
            builder.WithTimeout("external-api", TimeSpan.FromSeconds(5));
        });

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();
        var policyRepository = provider.GetRequiredService<PolicyRepository>();

        // Get configured policies
        var cbPolicy = policyRepository.GetPolicy<CircuitBreakerPolicy>("external-api");
        var retryPolicy = policyRepository.GetPolicy<RetryPolicy>("external-api");
        var timeoutPolicy = policyRepository.GetPolicy<TimeoutPolicy>("external-api");

        Console.WriteLine("Configuration:");
        Console.WriteLine($"  Circuit Breaker: FailureThreshold={cbPolicy?.FailureThreshold}");
        Console.WriteLine($"  Retry: MaxRetries={retryPolicy?.MaxRetries}");
        Console.WriteLine($"  Timeout: {timeoutPolicy?.Timeout.TotalSeconds}s\n");

        // Simulate successful operations
        Console.WriteLine("--- Scenario 1: Successful Operations ---");
        for (int i = 1; i <= 3; i++)
        {
            var result = await pipeline.ExecuteAsync(
                async ct => await SimulateApiCallAsync(success: true, ct),
                circuitBreaker: cbPolicy,
                retry: retryPolicy,
                timeout: timeoutPolicy
            );

            Console.WriteLine($"Attempt {i}: {(result.IsSuccess ? "SUCCESS" : "FAILED")}");
            if (result.IsSuccess)
                Console.WriteLine($"  Duration: {result.Duration.TotalMilliseconds}ms");
        }

        // Simulate failed operations with retry
        Console.WriteLine("\n--- Scenario 2: Operations with Retries ---");
        for (int i = 1; i <= 2; i++)
        {
            var result = await pipeline.ExecuteAsync(
                async ct => await SimulateApiCallAsync(success: false, ct),
                circuitBreaker: cbPolicy,
                retry: retryPolicy,
                timeout: timeoutPolicy
            );

            Console.WriteLine($"Attempt {i}: {(result.IsSuccess ? "SUCCESS" : "FAILED")}");
            Console.WriteLine($"  Retries: {result.RetryCount}");
            Console.WriteLine($"  Duration: {result.Duration.TotalMilliseconds}ms");
            if (!result.IsSuccess)
                Console.WriteLine($"  Error: {result.Error?.Message}");
        }

        // Check pipeline statistics
        Console.WriteLine("\n--- Pipeline Statistics ---");
        var stats = pipeline.GetStatistics();
        Console.WriteLine($"Total Executions: {stats.TotalExecutions}");
        Console.WriteLine($"Successful: {stats.SuccessfulExecutions}");
        Console.WriteLine($"Failed: {stats.FailedExecutions}");
        Console.WriteLine($"Success Rate: {stats.SuccessRate:P}");
        Console.WriteLine($"Average Duration: {stats.AverageDurationMs}ms");
    }

    /// <summary>
    /// Simulates an API call that either succeeds or fails based on the success parameter.
    /// Used to demonstrate how the Retry policy handles transient failures and how the Circuit Breaker monitors error rates.
    /// </summary>
    /// <param name="success">If true, the call succeeds; if false, it throws an exception to trigger retry logic.</param>
    /// <param name="ct">Cancellation token for the operation.</param>
    /// <returns>True if the call was successful.</returns>
    private static async Task<bool> SimulateApiCallAsync(bool success, CancellationToken ct)
    {
        // Simulate network delay
        await Task.Delay(50, ct);

        if (success)
        {
            return true;
        }

        // Simulate temporary failure
        throw new HttpRequestException("Service temporarily unavailable");
    }
}
