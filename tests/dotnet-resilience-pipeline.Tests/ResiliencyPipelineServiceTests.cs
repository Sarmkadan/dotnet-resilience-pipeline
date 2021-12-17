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

/// <summary>
/// Provides unit tests for the <see cref="ResiliencyPipelineService"/> class.
/// </summary>
public sealed class ResiliencyPipelineServiceTests
{
    /// <summary>
    /// Verifies that registering a policy adds it to the pipeline.
    /// </summary>
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

    /// <summary>
    /// Verifies that executing an asynchronous operation returns a successful result when the operation succeeds.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that executing asynchronous operations tracks execution statistics.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that executing an asynchronous operation returns a failure result when the operation fails and no fallback is provided.
    /// </summary>
    /// <returns>A task representing the asynchronous operation.</returns>
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

    /// <summary>
    /// Verifies that executing an asynchronous operation uses a fallback policy when the operation fails.
    /// </summary>
    /// <param name="fallbackPolicy">The fallback policy to use.</param>
    /// <returns>A task representing the asynchronous operation.</returns>
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
