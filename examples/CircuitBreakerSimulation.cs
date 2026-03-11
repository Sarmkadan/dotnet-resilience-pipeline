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
/// Circuit breaker pattern simulation showing state transitions
/// </summary>
public class CircuitBreakerSimulationExample
{
    public static async Task Main()
    {
        Console.WriteLine("=== Circuit Breaker State Transitions ===\n");

        // Setup circuit breaker with short timeouts for demo
        var services = new ServiceCollection();
        services.AddResiliencePipeline(builder =>
        {
            builder.WithCircuitBreaker("payment-service", policy =>
            {
                policy.FailureThreshold = 3;
                policy.OpenDuration = TimeSpan.FromSeconds(5);
                policy.SuccessThresholdInHalfOpen = 2;
            });
        });

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();
        var policyRepository = provider.GetRequiredService<PolicyRepository>();
        var cbPolicy = policyRepository.GetPolicy<CircuitBreakerPolicy>("payment-service");

        Console.WriteLine("Configuration:");
        Console.WriteLine($"  Failure Threshold: {cbPolicy?.FailureThreshold}");
        Console.WriteLine($"  Open Duration: {cbPolicy?.OpenDuration.TotalSeconds}s");
        Console.WriteLine($"  Success Threshold (Half-Open): {cbPolicy?.SuccessThresholdInHalfOpen}\n");

        var failureCount = 0;
        var operationCount = 0;

        // Phase 1: Closed state with increasing failures
        Console.WriteLine("--- Phase 1: CLOSED State ---");
        for (int i = 0; i < 4; i++)
        {
            operationCount++;
            Console.WriteLine($"Operation {operationCount}: {cbPolicy?.State}");

            try
            {
                var result = await pipeline.ExecuteAsync(
                    async ct => await CallPaymentServiceAsync(shouldFail: true, ct),
                    circuitBreaker: cbPolicy
                );
            }
            catch (HttpRequestException ex)
            {
                failureCount++;
                Console.WriteLine($"  ✗ Failed (failure count: {failureCount})");
            }

            if (cbPolicy?.IsOpen() ?? false)
            {
                Console.WriteLine($"  ⚠ Circuit breaker OPENED!");
                break;
            }

            await Task.Delay(200);
        }

        // Phase 2: Open state - fast failures
        Console.WriteLine("\n--- Phase 2: OPEN State (Fast Fail) ---");
        for (int i = 0; i < 3; i++)
        {
            operationCount++;
            Console.WriteLine($"Operation {operationCount}: {cbPolicy?.State}");

            try
            {
                var result = await pipeline.ExecuteAsync(
                    async ct => await CallPaymentServiceAsync(shouldFail: true, ct),
                    circuitBreaker: cbPolicy
                );
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ {ex.GetType().Name}: Circuit is open, request rejected");
            }

            await Task.Delay(500);
        }

        // Phase 3: Wait for open duration to expire
        Console.WriteLine("\n--- Phase 3: Waiting for Open Duration to Expire ---");
        Console.WriteLine($"Waiting {cbPolicy?.OpenDuration.TotalSeconds}s...");
        await Task.Delay((int)cbPolicy!.OpenDuration.TotalMilliseconds);

        // Phase 4: Half-open state - test recovery
        Console.WriteLine("\n--- Phase 4: HALF-OPEN State (Testing Recovery) ---");
        Console.WriteLine("Circuit transitioned to HALF-OPEN automatically");

        // Try successful operation
        for (int i = 0; i < 3; i++)
        {
            operationCount++;
            Console.WriteLine($"Operation {operationCount}: {cbPolicy.State}");

            try
            {
                var result = await pipeline.ExecuteAsync(
                    async ct => await CallPaymentServiceAsync(shouldFail: false, ct),
                    circuitBreaker: cbPolicy
                );

                if (result.IsSuccess)
                {
                    Console.WriteLine($"  ✓ Success (success count: {i + 1})");

                    if (cbPolicy.IsOpen())
                    {
                        Console.WriteLine("  Circuit CLOSED (recovered)");
                        break;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ {ex.GetType().Name}");
                break;
            }

            await Task.Delay(200);
        }

        // Phase 5: Back to closed state
        Console.WriteLine("\n--- Phase 5: CLOSED State (Normal Operation) ---");
        for (int i = 0; i < 3; i++)
        {
            operationCount++;
            Console.WriteLine($"Operation {operationCount}: {cbPolicy.State}");

            try
            {
                var result = await pipeline.ExecuteAsync(
                    async ct => await CallPaymentServiceAsync(shouldFail: false, ct),
                    circuitBreaker: cbPolicy
                );

                if (result.IsSuccess)
                    Console.WriteLine($"  ✓ Success");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"  ✗ {ex.GetType().Name}");
            }

            await Task.Delay(200);
        }

        // Summary
        Console.WriteLine("\n--- Summary ---");
        Console.WriteLine($"Total Operations: {operationCount}");
        Console.WriteLine($"Final State: {cbPolicy.State}");
        Console.WriteLine($"Consecutive Failures: {cbPolicy.ConsecutiveFailures}");
    }

    private static async Task<bool> CallPaymentServiceAsync(bool shouldFail, CancellationToken ct)
    {
        await Task.Delay(50, ct);

        if (shouldFail)
        {
            throw new HttpRequestException("Payment service unavailable");
        }

        return true;
    }
}
