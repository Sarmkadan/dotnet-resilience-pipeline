using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using DotNetResiliencePipeline.Cli;

namespace DotNetResiliencePipeline.Tests;

public class CommandOptionsTests
{
    [Fact]
    public void HasFlag_ReturnsTrue_ForLongAndShortForms()
    {
        // Arrange
        var options = new CommandOptions
        {
            Flags = new List<string> { "--verbose", "-v" }
        };

        // Act & Assert
        options.HasFlag("verbose").Should().BeTrue(); // checks "--verbose" and "-v"
        options.HasFlag("v").Should().BeTrue();       // checks "--v" (not present) and "-v"
    }

    [Fact]
    public void HasFlag_ReturnsFalse_WhenFlagIsMissing()
    {
        // Arrange
        var options = new CommandOptions
        {
            Flags = new List<string> { "--quiet" }
        };

        // Act
        var result = options.HasFlag("verbose");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void GetArgument_ReturnsValueOrDefault_IgnoringCase()
    {
        // Arrange
        var options = new CommandOptions
        {
            Arguments = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                ["timeout"] = "30s"
            }
        };

        // Act & Assert
        options.GetArgument("TIMEOUT").Should().Be("30s");
        options.GetArgument("missing", "default").Should().Be("default");
    }

    [Fact]
    public void Validate_ReturnsNoErrors_ForValidOptions()
    {
        // Arrange
        var options = new CommandOptions
        {
            Command = "policy",
            PolicyName = "myPolicy",
            PolicyType = "retry",
            MaxRetries = 3,
            FailureThreshold = 5
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_ReturnsErrors_ForInvalidConfiguration()
    {
        // Arrange
        var options = new CommandOptions
        {
            Command = "policy",
            PolicyName = null,
            PolicyType = "unknown",
            MaxRetries = -1,
            FailureThreshold = -2
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().Contain("Policy name is required for policy operations");
        errors.Should().Contain("Invalid policy type: unknown");
        errors.Should().Contain("MaxRetries cannot be negative");
        errors.Should().Contain("FailureThreshold cannot be negative");
        errors.Should().HaveCount(4);
    }

    [Fact]
    public void Validate_AllowsMissingPolicyName_WhenCommandIsNotPolicy()
    {
        // Arrange
        var options = new CommandOptions
        {
            Command = "list",
            PolicyName = null,
            PolicyType = null,
            MaxRetries = null,
            FailureThreshold = null
        };

        // Act
        var errors = options.Validate();

        // Assert
        errors.Should().BeEmpty();
    }
}
