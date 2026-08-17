#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using System.Text.Json;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Contains unit tests for the <see cref="BulkheadPolicyJsonExtensions"/> class.
/// </summary>
public sealed class BulkheadPolicyJsonExtensionsTests
{
    /// <summary>
    /// Tests that ToJson throws ArgumentNullException when value is null.
    /// </summary>
    [Fact]
    public void ToJson_NullValue_ThrowsArgumentNullException()
    {
        // Arrange
        BulkheadPolicy? policy = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => policy!.ToJson());
    }

    /// <summary>
    /// Tests that ToJson returns valid JSON for a BulkheadPolicy instance.
    /// </summary>
    [Fact]
    public void ToJson_ValidPolicy_ReturnsValidJson()
    {
        // Arrange
        var policy = new BulkheadPolicy("test-policy")
        {
            MaxParallelization = 5,
            MaxQueueLength = 10
        };

        // Act
        string json = policy.ToJson();

        // Assert
        Assert.False(string.IsNullOrWhiteSpace(json));
        Assert.StartsWith("{", json);
        Assert.EndsWith("}", json);

        // Verify it's valid JSON that can be deserialized
        var deserialized = JsonSerializer.Deserialize<BulkheadPolicy>(json);
        Assert.NotNull(deserialized);
        Assert.Equal("test-policy", deserialized?.Name);
        Assert.Equal(5, deserialized?.MaxParallelization);
        Assert.Equal(10, deserialized?.MaxQueueLength);
    }

    /// <summary>
    /// Tests that ToJson respects the indented parameter.
    /// </summary>
    [Fact]
    public void ToJson_IndentedFlag_RespectsWriteIndentedSetting()
    {
        // Arrange
        var policy = new BulkheadPolicy("indent-test");

        // Act
        string jsonIndented = policy.ToJson(indented: true);
        string jsonCompact = policy.ToJson(indented: false);

        // Assert
        Assert.NotEqual(jsonIndented, jsonCompact);
        Assert.Contains(Environment.NewLine, jsonIndented);
        Assert.DoesNotContain(Environment.NewLine, jsonCompact);
    }

    /// <summary>
    /// Tests that FromJson returns null for empty or whitespace input.
    /// </summary>
    [Fact]
    public void FromJson_EmptyOrWhiteSpace_ReturnsNull()
    {
        // Arrange
        string empty = "";
        string whitespace = "   ";
        string newline = "\n\t";

        // Act
        var resultEmpty = BulkheadPolicyJsonExtensions.FromJson(empty);
        var resultWhite = BulkheadPolicyJsonExtensions.FromJson(whitespace);
        var resultNewline = BulkheadPolicyJsonExtensions.FromJson(newline);

        // Assert
        Assert.Null(resultEmpty);
        Assert.Null(resultWhite);
        Assert.Null(resultNewline);
    }

    /// <summary>
    /// Tests that FromJson throws ArgumentNullException when input is null.
    /// </summary>
    [Fact]
    public void FromJson_NullInput_ThrowsArgumentNullException()
    {
        // Arrange
        string? json = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BulkheadPolicyJsonExtensions.FromJson(json!));
    }

    /// <summary>
    /// Tests that FromJson successfully deserializes valid JSON.
    /// </summary>
    [Fact]
    public void FromJson_ValidJson_ReturnsInstance()
    {
        // Arrange
        var original = new BulkheadPolicy("fromjson-test")
        {
            MaxParallelization = 8,
            MaxQueueLength = 20,
            MaxQueueWaitTimeout = TimeSpan.FromSeconds(15)
        };
        string json = original.ToJson();

        // Act
        var deserialized = BulkheadPolicyJsonExtensions.FromJson(json);

        // Assert
        Assert.NotNull(deserialized);
        Assert.IsType<BulkheadPolicy>(deserialized);
        Assert.Equal("fromjson-test", deserialized?.Name);
        Assert.Equal(8, deserialized?.MaxParallelization);
        Assert.Equal(20, deserialized?.MaxQueueLength);
        Assert.Equal(TimeSpan.FromSeconds(15), deserialized?.MaxQueueWaitTimeout);
    }

    /// <summary>
    /// Tests that FromJson returns null for invalid JSON.
    /// </summary>
    [Fact]
    public void FromJson_InvalidJson_ReturnsNull()
    {
        // Arrange
        string invalidJson = "{ this is not valid json }";

        // Act
        var result = BulkheadPolicyJsonExtensions.FromJson(invalidJson);

        // Assert
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that TryFromJson throws ArgumentNullException when json is null.
    /// </summary>
    [Fact]
    public void TryFromJson_NullJson_ThrowsArgumentNullException()
    {
        // Arrange
        string? json = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(() => BulkheadPolicyJsonExtensions.TryFromJson(json!, out _));
    }

    /// <summary>
    /// Tests that TryFromJson returns false for empty or whitespace input.
    /// </summary>
    [Fact]
    public void TryFromJson_EmptyOrWhiteSpace_ReturnsFalse()
    {
        // Arrange
        string empty = "";
        string whitespace = "   ";

        // Act
        bool successEmpty = BulkheadPolicyJsonExtensions.TryFromJson(empty, out var resultEmpty);
        bool successWhite = BulkheadPolicyJsonExtensions.TryFromJson(whitespace, out var resultWhite);

        // Assert
        Assert.False(successEmpty);
        Assert.False(successWhite);
        Assert.Null(resultEmpty);
        Assert.Null(resultWhite);
    }

    /// <summary>
    /// Tests that TryFromJson returns false for invalid JSON.
    /// </summary>
    [Fact]
    public void TryFromJson_InvalidJson_ReturnsFalse()
    {
        // Arrange
        string invalidJson = "{ invalid json }";

        // Act
        bool success = BulkheadPolicyJsonExtensions.TryFromJson(invalidJson, out var result);

        // Assert
        Assert.False(success);
        Assert.Null(result);
    }

    /// <summary>
    /// Tests that TryFromJson returns true and populates output for valid JSON.
    /// </summary>
    [Fact]
    public void TryFromJson_ValidJson_ReturnsTrueAndInstance()
    {
        // Arrange
        var original = new BulkheadPolicy("tryfromjson-test")
        {
            MaxParallelization = 3,
            MaxQueueLength = 7
        };
        string json = original.ToJson();

        // Act
        bool success = BulkheadPolicyJsonExtensions.TryFromJson(json, out var result);

        // Assert
        Assert.True(success);
        Assert.NotNull(result);
        Assert.IsType<BulkheadPolicy>(result);
        Assert.Equal("tryfromjson-test", result?.Name);
        Assert.Equal(3, result?.MaxParallelization);
        Assert.Equal(7, result?.MaxQueueLength);
    }
}