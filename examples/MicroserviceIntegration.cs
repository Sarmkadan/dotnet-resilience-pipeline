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
/// Realistic microservice integration example showing multiple policies
/// </summary>
public sealed class MicroserviceIntegrationExample
{
    private record UserDto(int Id, string Name, string Email);
    private record OrderDto(int Id, int UserId, decimal Amount);

    public static async Task Main()
    {
        Console.WriteLine("=== Microservice Integration Example ===\n");

        // Setup resilience pipeline
        var services = new ServiceCollection();
        services.AddResiliencePipeline(builder =>
        {
            // User Service policies - strict requirements
            builder.WithCircuitBreaker("user-service", policy =>
            {
                policy.FailureThreshold = 5;
                policy.OpenDuration = TimeSpan.FromSeconds(30);
                policy.SuccessThresholdInHalfOpen = 3;
            });

            builder.WithRetry("user-service", policy =>
            {
                policy.MaxRetries = 3;
                policy.InitialDelay = TimeSpan.FromMilliseconds(50);
                policy.Strategy = RetryPolicy.BackoffStrategy.Exponential;
                policy.BackoffMultiplier = 2.0;
                policy.MaxDelay = TimeSpan.FromSeconds(10);
            });

            builder.WithTimeout("user-service", TimeSpan.FromSeconds(10));

            builder.WithBulkhead("user-service", maxParallelization: 20, maxQueueLength: 100);

            // Order Service policies - more lenient
            builder.WithCircuitBreaker("order-service", policy =>
            {
                policy.FailureThreshold = 10;
                policy.OpenDuration = TimeSpan.FromSeconds(60);
            });

            builder.WithRetry("order-service", policy =>
            {
                policy.MaxRetries = 2;
                policy.InitialDelay = TimeSpan.FromMilliseconds(100);
                policy.Strategy = RetryPolicy.BackoffStrategy.Fixed;
            });

            builder.WithTimeout("order-service", TimeSpan.FromSeconds(15));

            builder.WithBulkhead("order-service", maxParallelization: 10, maxQueueLength: 50);

            // Fallback service - low priority
            builder.WithFallback("notification-service");
        });

        var provider = services.BuildServiceProvider();
        var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();
        var policyRepository = provider.GetRequiredService<PolicyRepository>();

        // Get policies for services
        var userServicePolicies = GetPolicies(policyRepository, "user-service");
        var orderServicePolicies = GetPolicies(policyRepository, "order-service");
        var notificationPolicies = GetPolicies(policyRepository, "notification-service");

        // Process user request
        Console.WriteLine("Processing User Request...");
        var user = await pipeline.ExecuteAsync(
            async ct => await FetchUserAsync(1, ct),
            circuitBreaker: userServicePolicies.CircuitBreaker,
            retry: userServicePolicies.Retry,
            timeout: userServicePolicies.Timeout,
            bulkhead: userServicePolicies.Bulkhead
        );

        if (user.IsSuccess)
        {
            Console.WriteLine($"✓ User loaded: {user.Value?.Name} ({user.Value?.Email})");

            // Process order for user
            Console.WriteLine("\nProcessing Order...");
            var order = await pipeline.ExecuteAsync(
                async ct => await FetchOrderAsync(101, user.Value!.Id, ct),
                circuitBreaker: orderServicePolicies.CircuitBreaker,
                retry: orderServicePolicies.Retry,
                timeout: orderServicePolicies.Timeout,
                bulkhead: orderServicePolicies.Bulkhead
            );

            if (order.IsSuccess)
            {
                Console.WriteLine($"✓ Order loaded: #{order.Value?.Id} (${order.Value?.Amount})");

                // Send notification with fallback
                Console.WriteLine("\nSending Notification...");
                var notification = await pipeline.ExecuteAsync(
                    async ct => await SendNotificationAsync(user.Value.Email, ct),
                    fallback: notificationPolicies.Fallback
                );

                if (notification.IsSuccess)
                    Console.WriteLine("✓ Notification sent");
                else
                    Console.WriteLine("✗ Notification failed (fallback may have been used)");
            }
            else
            {
                Console.WriteLine($"✗ Order failed: {order.Error?.Message}");
            }
        }
        else
        {
            Console.WriteLine($"✗ User load failed: {user.Error?.Message}");
        }

        // Display metrics
        Console.WriteLine("\n--- Service Metrics ---");
        PrintServiceMetrics("User Service", pipeline);
        PrintServiceMetrics("Order Service", pipeline);

        // Print policy states
        Console.WriteLine("\n--- Circuit Breaker States ---");
        Console.WriteLine($"User Service: {userServicePolicies.CircuitBreaker?.State ?? "N/A"}");
        Console.WriteLine($"Order Service: {orderServicePolicies.CircuitBreaker?.State ?? "N/A"}");
    }

    private class ServicePolicies
    {
        public CircuitBreakerPolicy? CircuitBreaker { get; set; }
        public RetryPolicy? Retry { get; set; }
        public TimeoutPolicy? Timeout { get; set; }
        public BulkheadPolicy? Bulkhead { get; set; }
        public FallbackPolicy? Fallback { get; set; }

        public override string ToString()
        {
            return $"ServicePolicies {{ CircuitBreaker = {CircuitBreaker?.State ?? "null"}, Retry = {Retry?.GetType().Name ?? "null"}, Timeout = {Timeout?.GetType().Name ?? "null"}, Bulkhead = {Bulkhead?.GetType().Name ?? "null"}, Fallback = {Fallback?.GetType().Name ?? "null"} }}";
        }
    }

    private static ServicePolicies GetPolicies(PolicyRepository repository, string serviceName)
    {
        return new ServicePolicies
        {
            CircuitBreaker = repository.GetPolicy<CircuitBreakerPolicy>(serviceName),
            Retry = repository.GetPolicy<RetryPolicy>(serviceName),
            Timeout = repository.GetPolicy<TimeoutPolicy>(serviceName),
            Bulkhead = repository.GetPolicy<BulkheadPolicy>(serviceName),
            Fallback = repository.GetPolicy<FallbackPolicy>(serviceName)
        };
    }

    private static async Task<UserDto> FetchUserAsync(int userId, CancellationToken ct)
    {
        await Task.Delay(100, ct);
        return new UserDto(userId, "John Doe", "john@example.com");
    }

    private static async Task<OrderDto> FetchOrderAsync(int orderId, int userId, CancellationToken ct)
    {
        await Task.Delay(150, ct);
        return new OrderDto(orderId, userId, 99.99m);
    }

    private static async Task<bool> SendNotificationAsync(string email, CancellationToken ct)
    {
        await Task.Delay(50, ct);
        // Simulate occasional failures
        if (DateTime.UtcNow.Second % 2 == 0)
            throw new InvalidOperationException("Email service unavailable");
        return true;
    }

    private static void PrintServiceMetrics(string serviceName, ResiliencyPipelineService pipeline)
    {
        var stats = pipeline.GetStatistics();
        Console.WriteLine($"{serviceName}:");
        Console.WriteLine($"  Total Executions: {stats.TotalExecutions}");
        Console.WriteLine($"  Success Rate: {stats.SuccessRate:P}");
        Console.WriteLine($"  Avg Duration: {stats.AverageDurationMs}ms");
    }
}
