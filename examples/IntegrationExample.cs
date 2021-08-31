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
using Microsoft.Extensions.Hosting;

namespace DotNetResiliencePipeline.Examples;

/// <summary>
/// ASP.NET Core integration example demonstrating how to register the pipeline
/// in the dependency injection container and use it in a background service.
/// </summary>
public sealed class IntegrationExample
{
    public static void Main(string[] args)
    {
        var builder = Host.CreateApplicationBuilder(args);

        // 1. Configure the pipeline in DI
        builder.Services.AddResiliencePipeline(pipelineBuilder =>
        {
            pipelineBuilder.WithRetry("api-client", policy =>
            {
                policy.MaxRetries = 3;
            });
            pipelineBuilder.WithTimeout("api-client", TimeSpan.FromSeconds(2));
        });

        // 2. Register a service that uses the pipeline
        builder.Services.AddSingleton<MyApiService>();

        var host = builder.Build();
        var myService = host.Services.GetRequiredService<MyApiService>();
        
        Console.WriteLine("Integration Example configured.");
    }
}

/// <summary>
/// Service that depends on ResiliencyPipelineService
/// </summary>
public sealed class MyApiService
{
    private readonly ResiliencyPipelineService _pipeline;
    private readonly PolicyRepository _policyRepository;

    public MyApiService(ResiliencyPipelineService pipeline, PolicyRepository policyRepository)
    {
        _pipeline = pipeline;
        _policyRepository = policyRepository;
    }

    public async Task<string> GetDataAsync(CancellationToken ct)
    {
        var retryPolicy = _policyRepository.GetPolicy<RetryPolicy>("api-client");
        var timeoutPolicy = _policyRepository.GetPolicy<TimeoutPolicy>("api-client");

        var result = await _pipeline.ExecuteAsync(
            async token => 
            {
                // Actual API call logic here
                await Task.Delay(100, token);
                return "Data";
            },
            retry: retryPolicy,
            timeout: timeoutPolicy
        );

        return result.Value ?? "Default";
    }
}
