using DotNetResiliencePipeline.Integration;
using DotNetResiliencePipeline.Services;
using System.Net.Http;
using System.Threading;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public class HttpClientFactoryTests
{
    private readonly ResiliencyPipelineService _pipelineService;

    public HttpClientFactoryTests()
    {
        _pipelineService = new ResiliencyPipelineService();
    }

    [Fact]
    public void CreateClient_HappyPath_ReturnsClient()
    {
        // Arrange
        var factory = new HttpClientFactory(_pipelineService);

        // Act
        var client = factory.CreateClient("test-client");

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void GetClient_HappyPath_ReturnsClient()
    {
        // Arrange
        var factory = new HttpClientFactory(_pipelineService);
        factory.CreateClient("test-client");

        // Act
        var client = factory.GetClient("test-client");

        // Assert
        Assert.NotNull(client);
    }

    [Fact]
    public void RemoveClient_HappyPath_RemovesClient()
    {
        // Arrange
        var factory = new HttpClientFactory(_pipelineService);
        factory.CreateClient("test-client");

        // Act
        var removed = factory.RemoveClient("test-client");

        // Assert
        Assert.True(removed);
    }

    [Fact]
    public async Task GetAsync_HappyPath_ReturnsResponse()
    {
        // Arrange
        var factory = new HttpClientFactory(_pipelineService);
        var client = factory.CreateClient("test-client");

        // Act
        var response = await factory.GetAsync("test-client", "https://example.com");

        // Assert
        Assert.NotNull(response);
    }

    [Fact]
    public async Task PostAsync_HappyPath_ReturnsResponse()
    {
        // Arrange
        var factory = new HttpClientFactory(_pipelineService);
        var client = factory.CreateClient("test-client");
        var content = new StringContent("test-content");

        // Act
        var response = await factory.PostAsync("test-client", "https://example.com", content);

        // Assert
        Assert.NotNull(response);
    }

    [Fact]
    public void GetClientNames_HappyPath_ReturnsClientNames()
    {
        // Arrange
        var factory = new HttpClientFactory(_pipelineService);
        factory.CreateClient("test-client1");
        factory.CreateClient("test-client2");

        // Act
        var clientNames = factory.GetClientNames();

        // Assert
        Assert.Equal(2, clientNames.Count);
    }

    [Fact]
    public void Dispose_HappyPath_DisposesClients()
    {
        // Arrange
        var factory = new HttpClientFactory(_pipelineService);
        factory.CreateClient("test-client");

        // Act
        factory.Dispose();

        // Assert
        // No exception thrown
    }
}
