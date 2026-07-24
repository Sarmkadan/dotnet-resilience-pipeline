using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using DotNetResiliencePipeline.Cli;

namespace DotNetResiliencePipeline.Tests;

public class CommandParserTests
{
    [Fact]
    public void Constructor_DoesNotThrow_WithValidArguments()
    {
        // Act
        Action act = () => new CommandParser(new string[0]);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void Parse_HappyPath_ReturnsCommandOptions()
    {
        // Arrange
        var parser = new CommandParser(new[] { "policy", "create", "--name", "myPolicy", "--type", "retry" });

        // Act
        var options = parser.Parse();

        // Assert
        options.Command.Should().Be("policy");
        options.Subcommand.Should().Be("create");
        options.PolicyName.Should().Be("myPolicy");
        options.PolicyType.Should().Be("retry");
    }

    [Fact]
    public void Parse_EmptyArguments_ReturnsDefaultCommandOptions()
    {
        // Arrange
        var parser = new CommandParser(new string[0]);

        // Act
        var options = parser.Parse();

        // Assert
        options.Command.Should().BeNull();
        options.Subcommand.Should().BeNull();
    }

    [Fact]
    public void GetHelpText_ReturnsHelpText()
    {
        // Act
        var helpText = CommandParser.GetHelpText();

        // Assert
        helpText.Should().NotBeNullOrEmpty();
    }

    [Fact]
    public void Parse_NullArguments_ThrowsArgumentNullException()
    {
        // Act
        Action act = () => new CommandParser(null!);

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }
}
