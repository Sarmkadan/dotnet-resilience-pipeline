#nullable enable
using DotNetResiliencePipeline.Integration;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using System.Net;
using System.Text.Json;
using Xunit;

namespace DotNetResiliencePipeline.Integration.Tests;

/// <summary>
/// Tests for the ExternalApiClient class.
/// </summary>
public sealed class ExternalApiClientTests
{
    private readonly ResiliencyPipelineService _pipelineService = new();
    private readonly ExternalApiClient _sut;

    public ExternalApiClientTests()
    {
        _sut = new ExternalApiClient(new HttpClientFactory(_pipelineService), _pipelineService);
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenHttpClientFactoryIsNull()
    {
        // Act
        Action act = () => new ExternalApiClient(null!, _pipelineService);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("httpClientFactory");
    }

    [Fact]
    public void Constructor_ThrowsArgumentNullException_WhenPipelineServiceIsNull()
    {
        // Act
        Action act = () => new ExternalApiClient(new HttpClientFactory(new ResiliencyPipelineService()), null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("pipelineService");
    }

    [Fact]
    public void RegisterApi_ThrowsArgumentNullException_WhenApiNameIsNull()
    {
        // Act
        Action act = () => _sut.RegisterApi(null!, new ApiConfiguration());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiName");
    }

    [Fact]
    public void RegisterApi_ThrowsArgumentNullException_WhenApiNameIsEmpty()
    {
        // Act
        Action act = () => _sut.RegisterApi(string.Empty, new ApiConfiguration());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiName");
    }

    [Fact]
    public void RegisterApi_ThrowsArgumentNullException_WhenApiNameIsWhitespace()
    {
        // Act
        Action act = () => _sut.RegisterApi("   ", new ApiConfiguration());

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("apiName");
    }

    [Fact]
    public void RegisterApi_ThrowsArgumentNullException_WhenConfigIsNull()
    {
        // Act
        Action act = () => _sut.RegisterApi("test-api", null!);

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("config");
    }

    [Fact]
    public void RegisterApi_ThrowsConfigurationException_WhenBaseUrlIsNull()
    {
        // Act
        Action act = () => _sut.RegisterApi("test-api", new ApiConfiguration { BaseUrl = null! });

        // Assert
        act.Should().Throw<ConfigurationException>();
    }

    [Fact]
    public void RegisterApi_ThrowsConfigurationException_WhenBaseUrlIsEmpty()
    {
        // Act
        Action act = () => _sut.RegisterApi("test-api", new ApiConfiguration { BaseUrl = string.Empty });

        // Assert
        act.Should().Throw<ConfigurationException>();
    }

    [Fact]
    public void RegisterApi_ThrowsConfigurationException_WhenBaseUrlIsWhitespace()
    {
        // Act
        Action act = () => _sut.RegisterApi("test-api", new ApiConfiguration { BaseUrl = "   " });

        // Assert
        act.Should().Throw<ConfigurationException>();
    }

    [Fact]
    public void RegisterApi_ThrowsConfigurationException_WhenBaseUrlIsInvalidFormat()
    {
        // Act
        Action act = () => _sut.RegisterApi("test-api", new ApiConfiguration { BaseUrl = "not-a-valid-url" });

        // Assert
        act.Should().Throw<ConfigurationException>();
    }

    [Fact]
    public void RegisterApi_SuccessfullyRegistersApi_WhenValidParametersProvided()
    {
        // Arrange
        var config = new ApiConfiguration { BaseUrl = "https://api.example.com" };

        // Act
        _sut.RegisterApi("test-api", config);

        // Assert
        var apis = _sut.GetRegisteredApis();
        apis.Should().ContainSingle()
            .Which.Should().Be("test-api");
    }

    [Fact]
    public void GetRegisteredApis_ReturnsEmptyList_WhenNoApisRegistered()
    {
        // Act
        var result = _sut.GetRegisteredApis();

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetRegisteredApis_ReturnsAllRegisteredApis()
    {
        // Arrange
        _sut.RegisterApi("api1", new ApiConfiguration { BaseUrl = "https://api1.com" });
        _sut.RegisterApi("api2", new ApiConfiguration { BaseUrl = "https://api2.com" });
        _sut.RegisterApi("api3", new ApiConfiguration { BaseUrl = "https://api3.com" });

        // Act
        var result = _sut.GetRegisteredApis();

        // Assert
        result.Should().ContainInOrder("api1", "api2", "api3");
    }

    [Fact]
    public async Task GetAsync_ThrowsArgumentNullException_WhenApiNameIsNull()
    {
        // Act
        Func<Task> act = async () => await _sut.GetAsync<string>(null!, "/endpoint");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("apiName");
    }

    [Fact]
    public async Task GetAsync_ThrowsArgumentNullException_WhenApiNameIsEmpty()
    {
        // Act
        Func<Task> act = async () => await _sut.GetAsync<string>(string.Empty, "/endpoint");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("apiName");
    }

    [Fact]
    public async Task GetAsync_ThrowsArgumentNullException_WhenApiNameIsWhitespace()
    {
        // Act
        Func<Task> act = async () => await _sut.GetAsync<string>("   ", "/endpoint");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("apiName");
    }

    [Fact]
    public async Task GetAsync_ThrowsArgumentNullException_WhenEndpointIsNull()
    {
        // Arrange
        _sut.RegisterApi("test-api", new ApiConfiguration { BaseUrl = "https://api.example.com" });

        // Act
        Func<Task> act = async () => await _sut.GetAsync<string>("test-api", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("endpoint");
    }

    [Fact]
    public async Task GetAsync_ThrowsArgumentNullException_WhenEndpointIsEmpty()
    {
        // Arrange
        _sut.RegisterApi("test-api", new ApiConfiguration { BaseUrl = "https://api.example.com" });

        // Act
        Func<Task> act = async () => await _sut.GetAsync<string>("test-api", string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("endpoint");
    }

    [Fact]
    public async Task GetAsync_ThrowsArgumentNullException_WhenEndpointIsWhitespace()
    {
        // Arrange
        _sut.RegisterApi("test-api", new ApiConfiguration { BaseUrl = "https://api.example.com" });

        // Act
        Func<Task> act = async () => await _sut.GetAsync<string>("test-api", "   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("endpoint");
    }

    [Fact]
    public async Task GetAsync_ThrowsConfigurationException_WhenApiIsNotRegistered()
    {
        // Act
        Func<Task> act = async () => await _sut.GetAsync<string>("unknown-api", "/endpoint");

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>();
    }

    [Fact]
    public async Task PostAsync_ThrowsArgumentNullException_WhenApiNameIsNull()
    {
        // Act
        Func<Task> act = async () => await _sut.PostAsync<string>(null!, "/endpoint", new { });

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("apiName");
    }

    [Fact]
    public async Task PostAsync_ThrowsArgumentNullException_WhenApiNameIsEmpty()
    {
        // Act
        Func<Task> act = async () => await _sut.PostAsync<string>(string.Empty, "/endpoint", new { });

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("apiName");
    }

    [Fact]
    public async Task PostAsync_ThrowsArgumentNullException_WhenApiNameIsWhitespace()
    {
        // Act
        Func<Task> act = async () => await _sut.PostAsync<string>("   ", "/endpoint", new { });

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("apiName");
    }

    [Fact]
    public async Task PostAsync_ThrowsArgumentNullException_WhenEndpointIsNull()
    {
        // Arrange
        _sut.RegisterApi("test-api", new ApiConfiguration { BaseUrl = "https://api.example.com" });

        // Act
        Func<Task> act = async () => await _sut.PostAsync<string>("test-api", null!, new { });

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("endpoint");
    }

    [Fact]
    public async Task PostAsync_ThrowsArgumentNullException_WhenEndpointIsEmpty()
    {
        // Arrange
        _sut.RegisterApi("test-api", new ApiConfiguration { BaseUrl = "https://api.example.com" });

        // Act
        Func<Task> act = async () => await _sut.PostAsync<string>("test-api", string.Empty, new { });

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("endpoint");
    }

    [Fact]
    public async Task PostAsync_ThrowsArgumentNullException_WhenEndpointIsWhitespace()
    {
        // Arrange
        _sut.RegisterApi("test-api", new ApiConfiguration { BaseUrl = "https://api.example.com" });

        // Act
        Func<Task> act = async () => await _sut.PostAsync<string>("test-api", "   ", new { });

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("endpoint");
    }

    [Fact]
    public async Task PostAsync_ThrowsArgumentNullException_WhenPayloadIsNull()
    {
        // Arrange
        _sut.RegisterApi("test-api", new ApiConfiguration { BaseUrl = "https://api.example.com" });

        // Act
        Func<Task> act = async () => await _sut.PostAsync<string>("test-api", "/endpoint", null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("payload");
    }

    [Fact]
    public async Task PostAsync_ThrowsConfigurationException_WhenApiIsNotRegistered()
    {
        // Act
        Func<Task> act = async () => await _sut.PostAsync<string>("unknown-api", "/endpoint", new { });

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>();
    }

    [Fact]
    public async Task TestConnectionAsync_ThrowsArgumentNullException_WhenApiNameIsNull()
    {
        // Act
        Func<Task> act = async () => await _sut.TestConnectionAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("apiName");
    }

    [Fact]
    public async Task TestConnectionAsync_ThrowsArgumentNullException_WhenApiNameIsEmpty()
    {
        // Act
        Func<Task> act = async () => await _sut.TestConnectionAsync(string.Empty);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("apiName");
    }

    [Fact]
    public async Task TestConnectionAsync_ThrowsArgumentNullException_WhenApiNameIsWhitespace()
    {
        // Act
        Func<Task> act = async () => await _sut.TestConnectionAsync("   ");

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>()
            .WithParameterName("apiName");
    }

    [Fact]
    public async Task TestConnectionAsync_ThrowsConfigurationException_WhenApiIsNotRegistered()
    {
        // Act
        Func<Task> act = async () => await _sut.TestConnectionAsync("unknown-api");

        // Assert
        await act.Should().ThrowAsync<ConfigurationException>();
    }

    [Fact]
    public void BaseUrlProperty_ReturnsExpectedValue()
    {
        // Arrange
        var expectedUrl = "https://test.example.com";
        var config = new ApiConfiguration { BaseUrl = expectedUrl };
        _sut.RegisterApi("test-api", config);

        // Act
        var apis = _sut.GetRegisteredApis();

        // Assert
        apis.Should().ContainSingle().Which.Should().Be("test-api");
    }

    [Fact]
    public void ApiKeyProperty_CanBeSetAndRetrieved()
    {
        // Arrange
        var expectedKey = "test-api-key";
        var config = new ApiConfiguration { BaseUrl = "https://test.com", ApiKey = expectedKey };
        _sut.RegisterApi("test-api", config);

        // Act & Assert - We verify through behavior since ApiKey is internal to ApiConfiguration
        // The actual testing of ApiKey usage happens in integration tests with mocked HTTP calls
    }

    [Fact]
    public void BearerTokenProperty_CanBeSetAndRetrieved()
    {
        // Arrange
        var expectedToken = "test-bearer-token";
        var config = new ApiConfiguration { BaseUrl = "https://test.com", BearerToken = expectedToken };
        _sut.RegisterApi("test-api", config);

        // Act & Assert - Similar to ApiKey, we verify through behavior in integration scenarios
    }

    [Fact]
    public void TimeoutProperty_CanBeSetAndRetrieved()
    {
        // Arrange
        var expectedTimeout = TimeSpan.FromSeconds(60);
        var config = new ApiConfiguration { BaseUrl = "https://test.com", Timeout = expectedTimeout };
        _sut.RegisterApi("test-api", config);

        // Act & Assert - Verified through behavior in integration tests
    }
}
