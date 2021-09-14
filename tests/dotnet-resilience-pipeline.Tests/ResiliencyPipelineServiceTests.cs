#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class ResiliencyPipelineServiceTests
{
    [Fact]
    public void RegisterPolicy_ShouldAddPolicyToPipeline()
    {
        // Arrange
        var service = new ResiliencyPipelineService();
        var policy = new RetryPolicy("test-retry");

        // Act
        service.RegisterPolicy(policy);

        // Assert
        service.GetAllPolicies().Should().ContainSingle(p => p.Id == policy.Id);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnSuccess_WhenOperationSucceeds()
    {
        // Arrange
        var service = new ResiliencyPipelineService();

        // Act
        var result = await service.ExecuteAsync(async _ => await Task.FromResult("success"));

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("success");
    }

    [Fact]
    public async Task ExecuteAsync_ShouldTrackExecutionStats()
    {
        // Arrange
        var service = new ResiliencyPipelineService();

        // Act
        await service.ExecuteAsync(async _ => await Task.FromResult(1));
        await service.ExecuteAsync(async _ => throw new InvalidOperationException("failed"));

        // Assert
        var stats = service.GetStats();
        stats.TotalExecutions.Should().Be(2);
        stats.SuccessfulExecutions.Should().Be(1);
        stats.FailedExecutions.Should().Be(1);
    }

    [Fact]
    public async Task ExecuteAsync_ShouldReturnFailure_WhenOperationFailsAndNoFallback()
    {
        // Arrange
        var service = new ResiliencyPipelineService();

        // Act
        var result = await service.ExecuteAsync<string>(async _ => throw new InvalidOperationException("error"));

        // Assert
        result.IsSuccess.Should().BeFalse();
        result.Exception.Should().BeOfType<InvalidOperationException>();
    }

    [Fact]
    public async Task ExecuteAsync_ShouldUseFallback_WhenOperationFails()
    {
        // Arrange
        var service = new ResiliencyPipelineService();
        var fallbackPolicy = new FallbackPolicy("test-fallback")
        {
            IsEnabled = true
        };
        fallbackPolicy.SetFallbackAction(async _ => await Task.FromResult("fallback-result"));
        fallbackPolicy.AddFallbackTrigger(typeof(InvalidOperationException));

        // Act
        var result = await service.ExecuteAsync(
            async _ => await Task.FromException<string>(new InvalidOperationException("error")),
            fallback: fallbackPolicy);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("fallback-result");
        result.Metadata.Should().ContainKey("FallbackUsed");
    }
}
