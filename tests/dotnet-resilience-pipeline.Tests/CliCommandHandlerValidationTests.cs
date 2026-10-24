using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using DotNetResiliencePipeline.Cli;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Data;

namespace DotNetResiliencePipeline.Tests;

public class CliCommandHandlerValidationTests
{
    private static CliCommandHandler CreateHandler(
        ResiliencyPipelineService? pipelineService = null,
        PolicyRepository? policyRepository = null,
        ExecutionHistoryRepository? historyRepository = null)
    {
        // Sealed services are used directly; they cannot be mocked. The optional circuit-breaker service can be omitted.
        return new CliCommandHandler(
            pipelineService ?? new ResiliencyPipelineService(),
            policyRepository ?? new PolicyRepository(),
            historyRepository ?? new ExecutionHistoryRepository(),
            null);
    }

    [Fact]
    public void Validate_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        CliCommandHandler? handler = null;

        // Act
        Action act = () => handler!.Validate();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Fact]
    public void Validate_WithValidHandler_ReturnsEmptyList()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = handler.Validate();

        // Assert
        result.Should().NotBeNull();
        result.Should().BeEmpty();
        result.Should().BeOfType<List<string>>();
    }

    [Fact]
    public void IsValid_WithNullHandler_ReturnsFalse()
    {
        // Arrange
        CliCommandHandler? handler = null;

        // Act
        var result = handler.IsValid();

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WithValidHandler_ReturnsTrue()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        var result = handler.IsValid();

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void EnsureValid_WithNullHandler_ThrowsArgumentNullException()
    {
        // Arrange
        CliCommandHandler? handler = null;

        // Act
        Action act = () => handler!.EnsureValid();

        // Assert
        act.Should().Throw<ArgumentNullException>()
            .WithParameterName("value");
    }

    [Fact]
    public void EnsureValid_WithValidHandler_DoesNotThrow()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        Action act = () => handler.EnsureValid();

        // Assert
        act.Should().NotThrow();
    }
}