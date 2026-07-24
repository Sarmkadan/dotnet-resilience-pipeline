using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Xunit;
using DotNetResiliencePipeline.Cli;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Data;

namespace DotNetResiliencePipeline.Tests;

public class CliCommandHandlerTests
{
    private static CliCommandHandler CreateHandler(
        Mock<ResiliencyPipelineService>? pipelineMock = null,
        Mock<PolicyRepository>? policyRepoMock = null,
        Mock<ExecutionHistoryRepository>? historyRepoMock = null)
    {
        // Use simple mocks if none supplied
        pipelineMock ??= new Mock<ResiliencyPipelineService>();
        policyRepoMock ??= new Mock<PolicyRepository>();
        historyRepoMock ??= new Mock<ExecutionHistoryRepository>();

        // The constructor requires non‑null arguments; the optional circuit‑breaker service can be omitted.
        return new CliCommandHandler(
            pipelineMock.Object,
            policyRepoMock.Object,
            historyRepoMock.Object,
            null);
    }

    [Fact]
    public async Task ExecuteAsync_HelpCommand_ReturnsHelpMessage()
    {
        // Arrange
        var handler = CreateHandler();
        var options = new CommandOptions { Command = "help" };

        // Act
        var result = await handler.ExecuteAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        result.ExitCode.Should().Be(0);
        result.Message.Should().NotBeNullOrWhiteSpace();
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_UnknownCommand_ReturnsErrorResult()
    {
        // Arrange
        var handler = CreateHandler();
        var options = new CommandOptions { Command = "unknownCommand" };

        // Act
        var result = await handler.ExecuteAsync(options);

        // Assert
        result.Success.Should().BeFalse();
        result.ExitCode.Should().Be(1);
        result.Message.Should().Contain("Unknown command");
        result.Error.Should().BeNull();
    }

    [Fact]
    public async Task ExecuteAsync_CreatePolicy_ValidOptions_ReturnsSuccess()
    {
        // Arrange
        var pipelineMock = new Mock<ResiliencyPipelineService>();
        var policyRepoMock = new Mock<PolicyRepository>();
        var historyRepoMock = new Mock<ExecutionHistoryRepository>();

        // RegisterPolicy is void – just verify it was called.
        pipelineMock.Setup(p => p.RegisterPolicy(It.IsAny<ResiliencyPolicy>())).Verifiable();

        // SaveAsync returns a completed task.
        policyRepoMock.Setup(r => r.SaveAsync(It.IsAny<ResiliencyPolicy>()))
                      .Returns(Task.CompletedTask)
                      .Verifiable();

        var handler = CreateHandler(pipelineMock, policyRepoMock, historyRepoMock);

        var options = new CommandOptions
        {
            Command = "policy",
            Subcommand = "create",
            PolicyName = "myPolicy",
            PolicyType = "retry",
            MaxRetries = 2
        };

        // Act
        var result = await handler.ExecuteAsync(options);

        // Assert
        result.Success.Should().BeTrue();
        result.ExitCode.Should().Be(0);
        result.Message.Should().Contain("created successfully");
        result.Error.Should().BeNull();

        // Verify that the service and repository were invoked.
        pipelineMock.Verify(p => p.RegisterPolicy(It.IsAny<ResiliencyPolicy>()), Times.Once);
        policyRepoMock.Verify(r => r.SaveAsync(It.IsAny<ResiliencyPolicy>()), Times.Once);
    }

    [Fact]
    public async Task ExecuteAsync_CreatePolicy_MissingPolicyName_ThrowsValidationException()
    {
        // Arrange
        var handler = CreateHandler();
        var options = new CommandOptions
        {
            Command = "policy",
            Subcommand = "create",
            // PolicyName omitted on purpose
            PolicyType = "retry",
            MaxRetries = 1
        };

        // Act
        Func<Task> act = async () => await handler.ExecuteAsync(options);

        // Assert
        await act.Should().ThrowAsync<ValidationException>()
                 .WithMessage("*Policy name is required*");
    }

    [Fact]
    public async Task ExecuteAsync_NullOptions_ThrowsNullReferenceException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        Func<Task> act = async () => await handler.ExecuteAsync(null!);

        // Assert
        await act.Should().ThrowAsync<NullReferenceException>();
    }
}
