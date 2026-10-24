using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
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
    public async Task ExecuteAsync_UnknownCommand_ThrowsValidationException()
    {
        // Arrange
        var handler = CreateHandler();
        var options = new CommandOptions { Command = "unknownCommand" };

        // Act
        Func<Task> act = async () => await handler.ExecuteAsync(options);

        // Assert: the validator rejects unknown commands before routing (documented as ValidationException)
        await act.Should().ThrowAsync<ValidationException>()
            .WithMessage("*Command validation failed*");
    }

    [Fact]
    public async Task ExecuteAsync_CreatePolicy_ValidOptions_ReturnsSuccess()
    {
        // Arrange
        var pipelineService = new ResiliencyPipelineService();
        var policyRepository = new PolicyRepository();
        var handler = CreateHandler(pipelineService, policyRepository);

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

        // The policy must be registered with the pipeline and persisted to the repository.
        pipelineService.GetAllPolicies().Should().ContainSingle(p => p is RetryPolicy && p.Name == "myPolicy");
        policyRepository.GetByName("myPolicy").Should().NotBeNull();
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
    public async Task ExecuteAsync_NullOptions_ThrowsArgumentNullException()
    {
        // Arrange
        var handler = CreateHandler();

        // Act
        Func<Task> act = async () => await handler.ExecuteAsync(null!);

        // Assert
        await act.Should().ThrowAsync<ArgumentNullException>();
    }
}
