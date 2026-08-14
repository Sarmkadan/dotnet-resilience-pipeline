using System;
using DotNetResiliencePipeline.Configuration;
using Xunit;
using FluentAssertions;

namespace DotNetResiliencePipeline.Tests;

public class ResiliencyPipelineBuilderExtensionsTests
{
    [Fact]
    public void WithDefaultCircuitBreaker_NullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        ResiliencyPipelineBuilder? builder = null;

        // Act
        Action act = () => ResiliencyPipelineBuilderExtensions.WithDefaultCircuitBreaker(builder!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithDefaultCircuitBreaker_ReturnsSameBuilderInstance()
    {
        // Arrange
        var builder = new ResiliencyPipelineBuilder();

        // Act
        var result = builder.WithDefaultCircuitBreaker();

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithExponentialBackoffRetry_NullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        ResiliencyPipelineBuilder? builder = null;

        // Act
        Action act = () => ResiliencyPipelineBuilderExtensions.WithExponentialBackoffRetry(builder!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithExponentialBackoffRetry_ReturnsSameBuilderInstance()
    {
        // Arrange
        var builder = new ResiliencyPipelineBuilder();

        // Act
        var result = builder.WithExponentialBackoffRetry();

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithIsolatedBulkhead_NullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        ResiliencyPipelineBuilder? builder = null;

        // Act
        Action act = () => ResiliencyPipelineBuilderExtensions.WithIsolatedBulkhead(builder!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithIsolatedBulkhead_ReturnsSameBuilderInstance()
    {
        // Arrange
        var builder = new ResiliencyPipelineBuilder();

        // Act
        var result = builder.WithIsolatedBulkhead();

        // Assert
        result.Should().BeSameAs(builder);
    }

    [Fact]
    public void WithDefaultTimeout_NullBuilder_ThrowsArgumentNullException()
    {
        // Arrange
        ResiliencyPipelineBuilder? builder = null;

        // Act
        Action act = () => ResiliencyPipelineBuilderExtensions.WithDefaultTimeout(builder!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void WithDefaultTimeout_ReturnsSameBuilderInstance()
    {
        // Arrange
        var builder = new ResiliencyPipelineBuilder();

        // Act
        var result = builder.WithDefaultTimeout();

        // Assert
        result.Should().BeSameAs(builder);
    }
}
