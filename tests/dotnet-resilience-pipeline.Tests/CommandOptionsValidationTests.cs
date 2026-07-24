#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using FluentAssertions;
using Xunit;
using DotNetResiliencePipeline.Cli;

namespace DotNetResiliencePipeline.Tests;

public class CommandOptionsValidationTests
{
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
        var errors = CommandOptionsValidation.Validate(options);

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
        var errors = CommandOptionsValidation.Validate(options);

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
        var errors = CommandOptionsValidation.Validate(options);

        // Assert
        errors.Should().BeEmpty();
    }

    [Fact]
    public void IsValid_ReturnsTrue_ForValidOptions()
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
        var isValid = CommandOptionsValidation.IsValid(options);

        // Assert
        isValid.Should().BeTrue();
    }

    [Fact]
    public void IsValid_ReturnsFalse_ForInvalidOptions()
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
        var isValid = CommandOptionsValidation.IsValid(options);

        // Assert
        isValid.Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_DoesNotThrow_ForValidOptions()
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
        Action act = () => CommandOptionsValidation.EnsureValid(options);

        // Assert
        act.Should().NotThrow();
    }

    [Fact]
    public void EnsureValid_ThrowsArgumentException_ForInvalidOptions()
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
        Action act = () => CommandOptionsValidation.EnsureValid(options);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
