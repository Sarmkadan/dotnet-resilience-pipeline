using DotNetResiliencePipeline.Services;
using System.Text.Json;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public class FallbackServiceJsonExtensionsTests
{
    [Fact]
    public void ToJson_HappyPath_ReturnsJsonString()
    {
        // Arrange
        var fallbackService = new FallbackService();

        // Act
        var json = fallbackService.ToJson();

        // Assert
        Assert.NotEmpty(json);
    }

    [Fact]
    public void FromJson_HappyPath_ReturnsFallbackService()
    {
        // Arrange
        var json = new FallbackService().ToJson();

        // Act
        var fallbackService = FallbackServiceJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(fallbackService);
    }

    [Fact]
    public void FromJson_NullInput_ReturnsNull()
    {
        // Act
        var fallbackService = FallbackServiceJsonExtensions.FromJson(null);

        // Assert
        Assert.Null(fallbackService);
    }

    [Fact]
    public void TryFromJson_HappyPath_ReturnsTrueAndFallbackService()
    {
        // Arrange
        var json = new FallbackService().ToJson();

        // Act
        var success = FallbackServiceJsonExtensions.TryFromJson(json, out var fallbackService);

        // Assert
        Assert.True(success);
        Assert.NotNull(fallbackService);
    }

    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalse()
    {
        // Act
        var success = FallbackServiceJsonExtensions.TryFromJson("invalid json", out _);

        // Assert
        Assert.False(success);
    }
}
