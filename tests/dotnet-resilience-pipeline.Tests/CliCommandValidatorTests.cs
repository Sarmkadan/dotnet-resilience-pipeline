using System;
using System.IO;
using DotNetResiliencePipeline.Cli;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public class CliCommandValidatorTests
{
    private readonly CliCommandValidator _validator = new();

    [Fact]
    public void Validate_WithValidPolicyCreateCommand_ReturnsValidResult()
    {
        // Arrange
        var options = new CommandOptions
        {
            Command = "policy",
            Subcommand = "create",
            PolicyName = "my-policy",
            PolicyType = "retry",
            MaxRetries = 3,
            FailureThreshold = 5,
            MaxParallelization = 10,
            Timeout = TimeSpan.FromSeconds(30),
            OpenDuration = TimeSpan.FromSeconds(10)
        };

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Errors.Should().BeEmpty();
        result.Warnings.Should().BeEmpty();
        result.ToString().Should().Contain("✓ Validation Passed");
    }

    [Fact]
    public void Validate_WithInvalidCommand_AddsError()
    {
        // Arrange
        var options = new CommandOptions
        {
            Command = "unknown",
            Subcommand = "create"
        };

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("Invalid command"));
        result.Warnings.Should().BeEmpty();
        result.ToString().Should().Contain("❌ Validation Failed");
    }

    [Fact]
    public void Validate_WithHighMaxRetries_AddsWarning()
    {
        // Arrange
        var options = new CommandOptions
        {
            Command = "policy",
            Subcommand = "create",
            PolicyName = "high-retries",
            PolicyType = "retry",
            MaxRetries = 200 // above the 100 threshold
        };

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeTrue();
        result.Warnings.Should().ContainSingle(w => w.Contains("MaxRetries is very high"));
        result.Errors.Should().BeEmpty();
    }

    [Fact]
    public void Validate_WithNonExistingOutputDirectory_AddsError()
    {
        // Arrange
        var nonExistingDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        var options = new CommandOptions
        {
            Command = "policy",
            Subcommand = "create",
            PolicyName = "test",
            PolicyType = "retry",
            OutputFile = Path.Combine(nonExistingDir, "output.json")
        };

        // Act
        var result = _validator.Validate(options);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.Contains("Output directory does not exist"));
    }

    [Fact]
    public void Validate_NullOptions_ThrowsNullReferenceException()
    {
        // Act
        Action act = () => _validator.Validate(null!);

        // Assert
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void ValidationResult_ToString_IncludesErrorsAndWarnings()
    {
        // Arrange
        var result = new ValidationResult
        {
            IsValid = false,
            Errors = new() { "Error one", "Error two" },
            Warnings = new() { "Warning one" }
        };

        // Act
        var output = result.ToString();

        // Assert
        output.Should().Contain("❌ Validation Failed");
        output.Should().Contain("ERROR: Error one");
        output.Should().Contain("ERROR: Error two");
        output.Should().Contain("⚠ Warnings:");
        output.Should().Contain("WARNING: Warning one");
    }
}
