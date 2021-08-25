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
/// Fallback pattern example demonstrating graceful degradation
/// </summary>
public class FallbackPatternExample
{
    private record UserProfile(int Id, string Name, string Status);

    public static async Task Main()
    {
        Console.WriteLine("=== Fallback Pattern - Graceful Degradation ===\n");

        var services = new ServiceCollection();
        services.AddResiliencePipeline(builder =>
        {
            builder.WithFallback("user-profile");
            builder.WithCircuitBreaker("primary-service", policy =>
            {
                policy.FailureThreshold = 2;
                policy.OpenDuration = TimeSpan.FromSeconds(10);
            });
            builder.WithRetry("primary-service", policy =>
            {
                policy.MaxRetries = 2;
                policy.InitialDelay = TimeSpan.FromMilliseconds(50);
            });
        });

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();
        var policyRepository = provider.GetRequiredService<PolicyRepository>();

        var fallbackPolicy = policyRepository.GetPolicy<FallbackPolicy>("user-profile");
        var cbPolicy = policyRepository.GetPolicy<CircuitBreakerPolicy>("primary-service");
        var retryPolicy = policyRepository.GetPolicy<RetryPolicy>("primary-service");

        // Scenario 1: Primary service works
        Console.WriteLine("--- Scenario 1: Primary Service Available ---");
        var profile1 = await ExecuteWithFallback(
            pipeline,
            async ct => await GetUserProfileAsync(userId: 1, useFallback: false, ct),
            async ct => await GetCachedUserProfileAsync(userId: 1, ct),
            cbPolicy,
            retryPolicy,
            fallbackPolicy
        );
        Console.WriteLine($"Result: {profile1?.Name} (Status: {profile1?.Status})\n");

        // Scenario 2: Primary service fails, fallback succeeds
        Console.WriteLine("--- Scenario 2: Primary Fails, Fallback Succeeds ---");
        var profile2 = await ExecuteWithFallback(
            pipeline,
            async ct => await GetUserProfileAsync(userId: 2, useFallback: true, ct),
            async ct => await GetCachedUserProfileAsync(userId: 2, ct),
            cbPolicy,
            retryPolicy,
            fallbackPolicy
        );
        Console.WriteLine($"Result: {profile2?.Name} (Status: {profile2?.Status})\n");

        // Scenario 3: Multiple primary failures
        Console.WriteLine("--- Scenario 3: Multiple Primary Failures ---");
        for (int i = 1; i <= 3; i++)
        {
            Console.WriteLine($"Attempt {i}:");
            var profile = await ExecuteWithFallback(
                pipeline,
                async ct => await GetUserProfileAsync(userId: 3, useFallback: true, ct),
                async ct => await GetCachedUserProfileAsync(userId: 3, ct),
                cbPolicy,
                retryPolicy,
                fallbackPolicy
            );

            if (profile is not null)
                Console.WriteLine($"  ✓ {profile.Name} (Status: {profile.Status})");
            else
                Console.WriteLine($"  ✗ Failed to retrieve profile");

            await Task.Delay(200);
        }

        // Scenario 4: Circuit breaker open, using fallback
        Console.WriteLine($"\n--- Scenario 4: Circuit Breaker Opened ---");
        Console.WriteLine($"Circuit State: {cbPolicy?.State}");
        var profile4 = await ExecuteWithFallback(
            pipeline,
            async ct => await GetUserProfileAsync(userId: 4, useFallback: true, ct),
            async ct => await GetCachedUserProfileAsync(userId: 4, ct),
            cbPolicy,
            retryPolicy,
            fallbackPolicy
        );

        if (profile4 is not null)
        {
            Console.WriteLine($"✓ Using Fallback: {profile4.Name} (Status: {profile4.Status})");
        }
    }

    private static async Task<UserProfile?> ExecuteWithFallback(
        ResiliencyPipelineService pipeline,
        Func<CancellationToken, Task<UserProfile>> primary,
        Func<CancellationToken, Task<UserProfile>> fallback,
        CircuitBreakerPolicy? cbPolicy,
        RetryPolicy? retryPolicy,
        FallbackPolicy? fallbackPolicy)
    {
        try
        {
            var result = await pipeline.ExecuteAsync(
                primary,
                circuitBreaker: cbPolicy,
                retry: retryPolicy,
                fallback: fallbackPolicy
            );

            if (result.IsSuccess)
            {
                Console.WriteLine($"  ✓ Primary service succeeded");
                return result.Value;
            }
            else
            {
                Console.WriteLine($"  ⚠ Primary service failed, trying fallback");
                // Fallback would be automatically tried
                return null;
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"  ⚠ {ex.GetType().Name}: {ex.Message}");
            Console.WriteLine($"  → Attempting fallback");

            try
            {
                return await fallback(CancellationToken.None);
            }
            catch
            {
                return null;
            }
        }
    }

    private static async Task<UserProfile> GetUserProfileAsync(
        int userId,
        bool useFallback,
        CancellationToken ct)
    {
        await Task.Delay(50, ct);

        if (useFallback && Random.Shared.Next(0, 2) == 0)
        {
            throw new HttpRequestException("Primary service unavailable");
        }

        return new UserProfile(userId, $"User {userId}", "Active");
    }

    private static async Task<UserProfile> GetCachedUserProfileAsync(int userId, CancellationToken ct)
    {
        // Return stale but available data from cache
        await Task.Delay(10, ct);
        return new UserProfile(userId, $"User {userId}", "Stale-Data");
    }
}
