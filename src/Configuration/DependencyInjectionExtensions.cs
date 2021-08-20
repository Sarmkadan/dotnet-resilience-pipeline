// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetResiliencePipeline.Data;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Configuration;

/// <summary>
/// Extension methods for integrating resilience pipeline into dependency injection containers.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Adds resilience pipeline services to the DI container.
    /// </summary>
    public static IServiceCollection AddResiliencePipeline(
        this IServiceCollection services,
        Action<ResiliencyPipelineBuilder>? configureBuilder = null)
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        // Register repositories
        services.AddSingleton<PolicyRepository>();
        services.AddSingleton<ExecutionHistoryRepository>();

        // Register services
        services.AddSingleton<CircuitBreakerService>();
        services.AddSingleton<RetryService>();
        services.AddSingleton<TimeoutService>();
        services.AddSingleton<BulkheadService>();
        services.AddSingleton<FallbackService>();

        // Register pipeline service
        services.AddSingleton(provider =>
        {
            var builder = new ResiliencyPipelineBuilder();
            configureBuilder?.Invoke(builder);
            return builder.Build();
        });

        return services;
    }

    /// <summary>
    /// Adds resilience pipeline with custom configuration.
    /// </summary>
    public static IServiceCollection AddResiliencePipeline<TConfig>(
        this IServiceCollection services,
        TConfig config,
        Action<TConfig, ResiliencyPipelineBuilder> configureBuilder)
        where TConfig : class
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (config == null)
            throw new ArgumentNullException(nameof(config));

        if (configureBuilder == null)
            throw new ArgumentNullException(nameof(configureBuilder));

        // Register repositories
        services.AddSingleton<PolicyRepository>();
        services.AddSingleton<ExecutionHistoryRepository>();

        // Register services
        services.AddSingleton<CircuitBreakerService>();
        services.AddSingleton<RetryService>();
        services.AddSingleton<TimeoutService>();
        services.AddSingleton<BulkheadService>();
        services.AddSingleton<FallbackService>();

        // Register configuration
        services.AddSingleton(config);

        // Register pipeline service with config
        services.AddSingleton(provider =>
        {
            var builder = new ResiliencyPipelineBuilder();
            configureBuilder(config, builder);
            return builder.Build();
        });

        return services;
    }

    /// <summary>
    /// Registers a specific policy type in the container.
    /// </summary>
    public static IServiceCollection AddPolicy<TPolicy>(
        this IServiceCollection services,
        Func<IServiceProvider, TPolicy> factory)
        where TPolicy : ResiliencyPolicy
    {
        if (services == null)
            throw new ArgumentNullException(nameof(services));

        if (factory == null)
            throw new ArgumentNullException(nameof(factory));

        services.AddSingleton(provider =>
        {
            var policy = factory(provider);
            var pipeline = provider.GetRequiredService<ResiliencyPipelineService>();
            pipeline.RegisterPolicy(policy);
            return policy;
        });

        return services;
    }
}
