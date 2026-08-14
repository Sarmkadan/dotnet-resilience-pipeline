#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Data;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Xunit;
using static DotNetResiliencePipeline.Configuration.DotnetResiliencePipelineOptions;

/// <summary>
/// Tests for the DependencyInjectionExtensions class.
/// </summary>
public sealed class DependencyInjectionExtensionsTests
{
    [Fact]
    public void AddResiliencePipeline_RegistersServices_Successfully()
    {
        var services = new ServiceCollection();
        
        // Register required dependency for ResiliencyPipelineService
        services.AddSingleton<ResiliencyPipelineService>();

        services.AddResiliencePipeline(builder => 
        {
            builder.WithTimeout("test-timeout", TimeSpan.FromSeconds(1));
        });
        
        var provider = services.BuildServiceProvider();
        
        provider.GetService<PolicyRepository>().Should().NotBeNull();
        provider.GetService<CircuitBreakerService>().Should().NotBeNull();
        provider.GetService<ResiliencyPipelineService>().Should().NotBeNull();
    }

    [Fact]
    public void AddResiliencePipeline_WithNullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        
        Action act = () => services!.AddResiliencePipeline();
        
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddResiliencePipelineWithOptions_RegistersServices_Successfully()
    {
        var services = new ServiceCollection();
        
        // Register required dependency for ResiliencyPipelineService
        services.AddSingleton<ResiliencyPipelineService>();

        services.AddResiliencePipelineWithOptions(options =>
        {
            options.Timeout.TimeoutSeconds = 10;
            // Initialize other options to avoid potential null issues in ToPolicy
            options.CircuitBreaker = new CircuitBreakerOptions();
            options.Retry = new RetryOptions();
            options.Bulkhead = new BulkheadOptions();
            options.Fallback = new FallbackOptions();
        });
        
        var provider = services.BuildServiceProvider();
        
        provider.GetService<PolicyRepository>().Should().NotBeNull();
        provider.GetService<CircuitBreakerService>().Should().NotBeNull();
        provider.GetService<ResiliencyPipelineService>().Should().NotBeNull();
    }

    [Fact]
    public void AddResiliencePipelineWithOptions_WithNullServices_ThrowsArgumentNullException()
    {
        IServiceCollection? services = null;
        
        Action act = () => services!.AddResiliencePipelineWithOptions(_ => {});
        
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void AddResiliencePipelineTConfig_RegistersServices_Successfully()
    {
        var services = new ServiceCollection();
        var config = new object();
        
        // Register required dependency for ResiliencyPipelineService
        services.AddSingleton<ResiliencyPipelineService>();

        services.AddResiliencePipeline(config, (c, builder) => 
        {
            builder.WithTimeout("test-timeout", TimeSpan.FromSeconds(1));
        });
        
        var provider = services.BuildServiceProvider();
        
        provider.GetService<PolicyRepository>().Should().NotBeNull();
        provider.GetService<ResiliencyPipelineService>().Should().NotBeNull();
        provider.GetService<object>().Should().Be(config);
    }

    [Fact]
    public void AddPolicy_RegistersPolicy_Successfully()
    {
        var services = new ServiceCollection();
        services.AddSingleton<ResiliencyPipelineService>(); // Need this for AddPolicy
        
        // Use a dummy policy type for testing
        var policy = new TestPolicy("test-policy");
        
        services.AddPolicy<TestPolicy>(_ => policy);
        
        var provider = services.BuildServiceProvider();
        var registeredPolicy = provider.GetService<TestPolicy>();
        
        registeredPolicy.Should().NotBeNull();
        registeredPolicy.Should().Be(policy);
    }

    private class TestPolicy : ResiliencyPolicy
    {
        public TestPolicy(string name) : base(name) { }
    }
}
