#nullable enable
using DotNetResiliencePipeline.Middleware;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Unit tests for <see cref="RateLimitingMiddleware"/>.
/// </summary>
public sealed class RateLimitingMiddlewareTests
{
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        var middleware = new RateLimitingMiddleware();
        
        middleware.Should().NotBeNull();
        middleware.GetAllStatus().Should().BeEmpty();
    }

    [Fact]
    public void ConfigureLimits_UpdatesLimitsForNewClients()
    {
        var middleware = new RateLimitingMiddleware();
        middleware.ConfigureLimits(50, 2000);
        
        // This will create a new limiter with these limits
        middleware.IsRequestAllowed("client1");
        
        var status = middleware.GetStatus("client1");
        status.RequestsPerSecond.Should().Be(50);
        status.RequestsPerMinute.Should().Be(2000);
    }

    [Fact]
    public void IsRequestAllowed_ReturnsTrueForNewClient()
    {
        var middleware = new RateLimitingMiddleware();
        
        var allowed = middleware.IsRequestAllowed("client1");
        
        allowed.Should().BeTrue();
    }

    [Fact]
    public void IsRequestAllowed_ConsumesTokens()
    {
        var middleware = new RateLimitingMiddleware();
        middleware.ConfigureLimits(1, 10);
        
        middleware.IsRequestAllowed("client1", 1);
        
        var status = middleware.GetStatus("client1");
        status.RemainingTokensPerSecond.Should().Be(0);
    }

    [Fact]
    public void GetStatus_ReturnsDefaultForUnknownClient()
    {
        var middleware = new RateLimitingMiddleware();
        
        var status = middleware.GetStatus("unknown");
        
        status.ClientId.Should().Be("unknown");
        status.RequestsPerSecond.Should().Be(100);
    }

    [Fact]
    public void ResetClient_RemovesLimiter()
    {
        var middleware = new RateLimitingMiddleware();
        middleware.IsRequestAllowed("client1");
        
        middleware.ResetClient("client1");
        
        middleware.GetAllStatus().Should().NotContainKey("client1");
    }

    [Fact]
    public void ClearAll_RemovesAllLimiters()
    {
        var middleware = new RateLimitingMiddleware();
        middleware.IsRequestAllowed("client1");
        middleware.IsRequestAllowed("client2");
        
        middleware.ClearAll();
        
        middleware.GetAllStatus().Should().BeEmpty();
    }

    [Fact]
    public void TryConsumeTokens_HandlesExceedingLimits()
    {
        var middleware = new RateLimitingMiddleware();
        middleware.ConfigureLimits(1, 10);
        
        // Consume 1 token, 0 left per second
        middleware.IsRequestAllowed("client1", 1).Should().BeTrue();
        
        // Try consuming another one, should fail
        middleware.IsRequestAllowed("client1", 1).Should().BeFalse();
    }
}
