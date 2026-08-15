using System.Text.Json;
using DotNetResiliencePipeline.Middleware;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public class RateLimitingMiddlewareJsonExtensionsTests
{
    [Fact]
    public void ToJson_ValidMiddleware_ReturnsJsonString()
    {
        var middleware = new RateLimitingMiddleware();
        middleware.ConfigureLimits(10, 100);
        
        var json = middleware.ToJson();
        
        Assert.NotNull(json);
        Assert.Contains("defaultRequestsPerSecond", json);
        Assert.Contains("10", json);
    }

    [Fact]
    public void ToJson_NullMiddleware_ThrowsArgumentNullException()
    {
        RateLimitingMiddleware? middleware = null;
        Assert.Throws<ArgumentNullException>(() => RateLimitingMiddlewareJsonExtensions.ToJson(middleware!));
    }

    [Fact]
    public void FromJson_ValidJson_ReturnsMiddlewareInstance()
    {
        var middleware = new RateLimitingMiddleware();
        middleware.ConfigureLimits(5, 50);
        var json = middleware.ToJson();
        
        var result = RateLimitingMiddlewareJsonExtensions.FromJson(json);
        
        Assert.NotNull(result);
        Assert.True(result!.IsRequestAllowed("client1", 1));
    }

    [Fact]
    public void FromJson_EmptyJson_ReturnsNull()
    {
        var result = RateLimitingMiddlewareJsonExtensions.FromJson("");
        Assert.Null(result);
    }

    [Fact]
    public void FromJson_InvalidJson_ThrowsJsonException()
    {
        Assert.Throws<JsonException>(() => RateLimitingMiddlewareJsonExtensions.FromJson("{invalid}"));
    }

    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndMiddlewareInstance()
    {
        var middleware = new RateLimitingMiddleware();
        middleware.ConfigureLimits(20, 200);
        var json = middleware.ToJson();
        
        bool success = RateLimitingMiddlewareJsonExtensions.TryFromJson(json, out var result);
        
        Assert.True(success);
        Assert.NotNull(result);
        Assert.True(result!.IsRequestAllowed("client1", 1));
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalseAndNull()
    {
        bool success = RateLimitingMiddlewareJsonExtensions.TryFromJson("{invalid}", out var result);
        
        Assert.False(success);
        Assert.Null(result);
    }
}
