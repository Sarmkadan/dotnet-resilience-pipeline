#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.DependencyInjection;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;

namespace DotNetResiliencePipeline.Configuration;

/// <summary>
/// Extension methods for registering adaptive timeout services and policies in the DI container.
/// </summary>
public static class AdaptiveTimeoutExtensions
{
    /// <summary>
    /// Registers <see cref="AdaptiveTimeoutService"/> as a singleton in the DI container.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    public static IServiceCollection AddAdaptiveTimeout(this IServiceCollection services)
    {
        ArgumentNullException.ThrowIfNull(services);

        services.AddSingleton<AdaptiveTimeoutService>();
        return services;
    }

    /// <summary>
    /// Registers <see cref="AdaptiveTimeoutService"/> and a named <see cref="AdaptiveTimeoutPolicy"/> as singletons,
    /// and wires the policy into the existing <see cref="ResiliencyPipelineService"/> if one is registered.
    /// </summary>
    /// <param name="services">The <see cref="IServiceCollection"/> to add services to.</param>
    /// <param name="policyName">Unique name for the policy.</param>
    /// <param name="initialTimeout">Timeout applied before enough observations are collected.</param>
    /// <param name="configure">Optional delegate for fine-grained policy configuration.</param>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="services"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="policyName"/> is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when <paramref name="initialTimeout"/> is not positive.</exception>
    public static IServiceCollection AddAdaptiveTimeout(
        this IServiceCollection services,
        string policyName,
        TimeSpan initialTimeout,
        Action<AdaptiveTimeoutPolicy>? configure = null)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(initialTimeout, TimeSpan.Zero);

        services.AddSingleton<AdaptiveTimeoutService>();

        services.AddSingleton(provider =>
        {
            var policy = new AdaptiveTimeoutPolicy(policyName) { InitialTimeout = initialTimeout };

            configure?.Invoke(policy);

            // Re-synchronize CurrentTimeout with InitialTimeout in case configure changed InitialTimeout.
            policy.ResetStatistics();

            provider.GetService<ResiliencyPipelineService>()?.RegisterPolicy(policy);

            return policy;
        });

        return services;
    }
}