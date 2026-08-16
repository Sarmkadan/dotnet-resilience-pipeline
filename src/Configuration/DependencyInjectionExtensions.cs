#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using DotNetResiliencePipeline.Api.Controllers;
using DotNetResiliencePipeline.Data;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Formatters;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Configuration;

/// <summary>
/// Extension methods for integrating resilience pipeline into dependency injection containers.
/// </summary>
public static class DependencyInjectionExtensions
{
    /// <summary>
    /// Initializes static members of the <see cref="DependencyInjectionExtensions"/> class.
    /// </summary>
    static DependencyInjectionExtensions()
    {
        // Ensure static initialization if needed
    }

    /// <summary>
    /// Adds resilience pipeline services to the DI container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureBuilder">Optional configuration action for the pipeline builder.</param>
    /// <returns>The <see cref="IServiceCollection"/> so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddResiliencePipeline(
        this IServiceCollection services,
        Action<ResiliencyPipelineBuilder>? configureBuilder = null)
    {
        ArgumentNullException.ThrowIfNull(services);

        // Register repositories
        services.AddSingleton<PolicyRepository>();
        services.AddSingleton<ExecutionHistoryRepository>();

        // Register services
        services.AddSingleton<CircuitBreakerService>();
        services.AddSingleton<IRetryService, RetryService>();
        services.AddSingleton<TimeoutService>();
        services.AddSingleton<BulkheadService>();
        services.AddSingleton<FallbackService>();
        services.AddSingleton<FailureInjectionService>();

        // Register formatters / exporters
        services.AddSingleton<MetricsExporter>();

        // Register API controllers
        services.AddSingleton<CircuitBreakerDashboardController>();

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
    /// Adds resilience pipeline with configuration options using IOptions pattern.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="configureOptions">Configuration action for the pipeline options.</param>
    /// <returns>The <see cref="IServiceCollection"/> so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="configureOptions"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddResiliencePipelineWithOptions(
        this IServiceCollection services,
        Action<DotnetResiliencePipelineOptions> configureOptions)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configureOptions);

        // Configure options
        services.Configure(configureOptions);

        // Register options validator
        services.AddSingleton<IValidateOptions<DotnetResiliencePipelineOptions>, DotnetResiliencePipelineOptionsValidator>();

        // Register repositories
        services.AddSingleton<PolicyRepository>();
        services.AddSingleton<ExecutionHistoryRepository>();

        // Register services
        services.AddSingleton<CircuitBreakerService>();
        services.AddSingleton<IRetryService, RetryService>();
        services.AddSingleton<TimeoutService>();
        services.AddSingleton<BulkheadService>();
        services.AddSingleton<FallbackService>();
        services.AddSingleton<FailureInjectionService>();

        // Register formatters / exporters
        services.AddSingleton<MetricsExporter>();

        // Register API controllers
        services.AddSingleton<CircuitBreakerDashboardController>();

        // Register pipeline service with options
        services.AddSingleton(provider =>
        {
            var options = provider.GetRequiredService<IOptions<DotnetResiliencePipelineOptions>>().Value;
            var builder = new ResiliencyPipelineBuilder();

            // Add policies from options
            builder.WithCircuitBreaker("default-circuit-breaker", policy =>
            {
                var configured = options.CircuitBreaker.ToPolicy(policy.Name);
                policy.FailureThreshold = configured.FailureThreshold;
                policy.OpenDuration = configured.OpenDuration;
                policy.SuccessThresholdInHalfOpen = configured.SuccessThresholdInHalfOpen;
            });

            builder.WithRetry("default-retry", policy =>
            {
                var configured = options.Retry.ToPolicy(policy.Name);
                policy.MaxRetries = configured.MaxRetries;
                policy.InitialDelay = configured.InitialDelay;
                policy.Strategy = configured.Strategy;
                policy.MaxDelay = configured.MaxDelay;
                policy.BackoffMultiplier = configured.BackoffMultiplier;
                policy.UseJitter = configured.UseJitter;
                policy.JitterFactor = configured.JitterFactor;
            });

            builder.WithTimeout("default-timeout", TimeSpan.FromSeconds(options.Timeout.TimeoutSeconds));
            builder.WithBulkhead("default-bulkhead", options.Bulkhead.MaxParallelization, options.Bulkhead.MaxQueueLength);

            builder.WithFallback("default-fallback", policy =>
            {
                var configured = options.Fallback.ToPolicy(policy.Name);
                policy.FallbackOnAnyException = configured.FallbackOnAnyException;
                policy.FallbackTimeout = configured.FallbackTimeout;
            });

            return builder.Build();
        });

        return services;
    }

    /// <summary>
    /// Adds resilience pipeline with custom configuration.
    /// </summary>
    /// <typeparam name="TConfig">The type of configuration object.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="config">The configuration object.</param>
    /// <param name="configureBuilder">Configuration action that receives both config and builder.</param>
    /// <returns>The <see cref="IServiceCollection"/> so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/>, <paramref name="config"/>, or <paramref name="configureBuilder"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddResiliencePipeline<TConfig>(
        this IServiceCollection services,
        TConfig config,
        Action<TConfig, ResiliencyPipelineBuilder> configureBuilder)
        where TConfig : class
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(config);
        ArgumentNullException.ThrowIfNull(configureBuilder);

        // Register repositories
        services.AddSingleton<PolicyRepository>();
        services.AddSingleton<ExecutionHistoryRepository>();

        // Register services
        services.AddSingleton<CircuitBreakerService>();
        services.AddSingleton<IRetryService, RetryService>();
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
    /// <typeparam name="TPolicy">The type of policy to register. Must derive from <see cref="ResiliencyPolicy"/>.</typeparam>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="factory">Factory function to create the policy instance.</param>
    /// <returns>The <see cref="IServiceCollection"/> so calls can be chained.</returns>
    /// <exception cref="ArgumentNullException">
    /// Thrown when <paramref name="services"/> or <paramref name="factory"/> is <see langword="null"/>.
    /// </exception>
    public static IServiceCollection AddPolicy<TPolicy>(
        this IServiceCollection services,
        Func<IServiceProvider, TPolicy> factory)
        where TPolicy : ResiliencyPolicy
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(factory);

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
