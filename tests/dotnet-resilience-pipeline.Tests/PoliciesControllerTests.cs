using System.Collections.Generic;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Api.Controllers;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public class PoliciesControllerTests
{
    // The controller only needs its dependencies for some methods.
    // For validation‑only tests we can safely pass null because the
    // code validates the request before touching the dependencies.
    private readonly PoliciesController _controller = new(null!, null!);

    [Fact]
    public async Task CreatePolicyAsync_ReturnsError_WhenNameIsMissing()
    {
        // Arrange
        var request = new CreatePolicyRequest
        {
            Name = "",               // missing name
            Type = "circuitbreaker"
        };

        // Act
        var result = await _controller.CreatePolicyAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Name is required", result.Message);
    }

    [Fact]
    public async Task CreatePolicyAsync_ReturnsError_WhenTypeIsInvalid()
    {
        // Arrange
        var request = new CreatePolicyRequest
        {
            Name = "MyPolicy",
            Type = "unknowntype"
        };

        // Act
        var result = await _controller.CreatePolicyAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.Contains("Invalid policy type", result.Message);
    }

    [Fact]
    public async Task ValidatePolicyAsync_ReturnsInvalid_WhenNameMissing()
    {
        // Arrange
        var request = new ValidatePolicyRequest
        {
            Name = "",               // missing name
            Type = "retry"
        };

        // Act
        var result = await _controller.ValidatePolicyAsync(request);

        // Assert
        Assert.False(result.Success);
        Assert.False(result.Data!.IsValid);
        Assert.Contains("Name is required", result.Data.Errors);
    }

    [Fact]
    public async Task ValidatePolicyAsync_ReturnsValid_WhenAllFieldsCorrect()
    {
        // Arrange
        var request = new ValidatePolicyRequest
        {
            Name = "ValidPolicy",
            Type = "retry",
            MaxRetries = 3
        };

        // Act
        var result = await _controller.ValidatePolicyAsync(request);

        // Assert
        Assert.True(result.Success);
        Assert.True(result.Data!.IsValid);
        Assert.Empty(result.Data.Errors);
    }

    [Fact]
    public async Task GetPolicyAsync_ReturnsError_WhenPolicyNotFound()
    {
        // Arrange
        // The controller will call the pipeline service, which we passed as null.
        // It will throw a NullReferenceException; we verify that the method
        // catches the exception and returns a failed ApiResponse.
        var result = await _controller.GetPolicyAsync("nonexistent-id");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Message);
    }

    [Fact]
    public async Task DeletePolicyAsync_ReturnsError_WhenExceptionOccurs()
    {
        // Arrange
        // With a null pipeline service the call to RemovePolicy will throw.
        var result = await _controller.DeletePolicyAsync("any-id");

        // Assert
        Assert.False(result.Success);
        Assert.NotNull(result.Message);
    }
}
