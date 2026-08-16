#nullable enable
using DotNetResiliencePipeline.Formatters;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests.Formatters;

/// <summary>
/// Tests for the CsvReportFormatterJsonExtensions class.
/// </summary>
public sealed class CsvReportFormatterJsonExtensionsTests
{
    // CsvReportFormatter is a sealed class with a parameterless constructor (implicit)
    private readonly CsvReportFormatter _formatter = new();

    [Fact]
    public void ToJson_WithValidFormatter_ReturnsJsonString()
    {
        // Act
        var json = _formatter.ToJson();

        // Assert
        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("{}"); // Empty object because CsvReportFormatter has no properties to serialize
    }

    [Fact]
    public void ToJson_WithNullFormatter_ThrowsArgumentNullException()
    {
        // Arrange
        CsvReportFormatter? formatter = null;

        // Act
        Action act = () => formatter!.ToJson();

        // Assert
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FromJson_WithValidJson_ReturnsFormatterInstance()
    {
        // Arrange
        var json = "{}";

        // Act
        var formatter = CsvReportFormatterJsonExtensions.FromJson(json);

        // Assert
        formatter.Should().NotBeNull();
    }

    [Fact]
    public void FromJson_WithInvalidJson_ThrowsJsonException()
    {
        // Arrange
        var json = "{ invalid }";

        // Act
        Action act = () => CsvReportFormatterJsonExtensions.FromJson(json);

        // Assert
        act.Should().Throw<System.Text.Json.JsonException>();
    }

    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndInstance()
    {
        // Arrange
        var json = "{}";

        // Act
        var success = CsvReportFormatterJsonExtensions.TryFromJson(json, out var formatter);

        // Assert
        success.Should().BeTrue();
        formatter.Should().NotBeNull();
    }

    [Fact]
    public void TryFromJson_WithInvalidJson_ReturnsFalseAndNull()
    {
        // Arrange
        var json = "{ invalid }";

        // Act
        var success = CsvReportFormatterJsonExtensions.TryFromJson(json, out var formatter);

        // Assert
        success.Should().BeFalse();
        formatter.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_WithNullOrEmptyJson_ThrowsArgumentException()
    {
        // Act
        Action act1 = () => CsvReportFormatterJsonExtensions.TryFromJson(string.Empty, out _);
        Action act2 = () => CsvReportFormatterJsonExtensions.TryFromJson(null!, out _);

        // Assert
        act1.Should().Throw<ArgumentException>();
        act2.Should().Throw<ArgumentException>();
    }
}
