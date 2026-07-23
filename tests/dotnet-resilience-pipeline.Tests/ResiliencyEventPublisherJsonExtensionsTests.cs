#nullable enable
using DotNetResiliencePipeline.Events;
using FluentAssertions;
using System;
using System.Text.Json;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class ResiliencyEventPublisherJsonExtensionsTests
{
    [Fact]
    public void ToJson_WithValidPublisher_ReturnsNonEmptyJson()
    {
        var publisher = new ResiliencyEventPublisher();
        var json = publisher.ToJson();

        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("maxHistorySize");
    }

    [Fact]
    public void ToJson_WithIndentedTrue_ReturnsFormattedJson()
    {
        var publisher = new ResiliencyEventPublisher();
        var json = publisher.ToJson(indented: true);

        json.Should().NotBeNullOrEmpty();
        json.Should().Contain("{\n");
        json.Should().Contain("  ");
    }

    [Fact]
    public void ToJson_WithIndentedFalse_ReturnsCompactJson()
    {
        var publisher = new ResiliencyEventPublisher();
        var json = publisher.ToJson(indented: false);

        json.Should().NotBeNullOrEmpty();
        json.Should().NotContain("\n");
    }

    [Fact]
    public void ToJson_WithNullPublisher_ThrowsArgumentNullException()
    {
        ResiliencyEventPublisher? publisher = null;
        Assert.Throws<ArgumentNullException>(() => publisher!.ToJson());
    }

    [Fact]
    public void FromJson_WithValidJson_ReturnsDeserializedPublisher()
    {
        var json = "{\"maxHistorySize\":1000}";
        var publisher = ResiliencyEventPublisherJsonExtensions.FromJson(json);

        publisher.Should().NotBeNull();
        publisher.MaxHistorySize.Should().Be(1000);
    }

    [Fact]
    public void FromJson_WithValidJson_PreservesMaxHistorySize()
    {
        var json = "{\"maxHistorySize\":500}";
        var publisher = ResiliencyEventPublisherJsonExtensions.FromJson(json);

        publisher.MaxHistorySize.Should().Be(500);
    }

    [Fact]
    public void FromJson_WithNullJson_ThrowsArgumentNullException()
    {
        string? json = null;
        Assert.Throws<ArgumentNullException>(() => ResiliencyEventPublisherJsonExtensions.FromJson(json!));
    }

    [Fact]
    public void FromJson_WithEmptyJson_ThrowsArgumentException()
    {
        var json = "   ";
        Assert.Throws<ArgumentException>(() => ResiliencyEventPublisherJsonExtensions.FromJson(json));
    }

    [Fact]
    public void FromJson_WithWhitespaceJson_ThrowsArgumentException()
    {
        var json = "\t\n  \r";
        Assert.Throws<ArgumentException>(() => ResiliencyEventPublisherJsonExtensions.FromJson(json));
    }

    [Fact]
    public void FromJson_WithInvalidJson_ThrowsJsonException()
    {
        var json = "{ invalid json";
        Assert.Throws<JsonException>(() => ResiliencyEventPublisherJsonExtensions.FromJson(json));
    }

    [Fact]
    public void TryFromJson_WithValidJson_ReturnsTrueAndDeserializedPublisher()
    {
        var json = "{\"maxHistorySize\":2000}";
        var result = ResiliencyEventPublisherJsonExtensions.TryFromJson(json, out var publisher);

        result.Should().BeTrue();
        publisher.Should().NotBeNull();
        publisher!.MaxHistorySize.Should().Be(2000);
    }

    [Fact]
    public void TryFromJson_WithValidJson_SetsPublisherValue()
    {
        var json = "{\"maxHistorySize\":1500}";
        ResiliencyEventPublisher? publisher = null;
        var result = ResiliencyEventPublisherJsonExtensions.TryFromJson(json, out publisher);

        result.Should().BeTrue();
        publisher.Should().NotBeNull();
    }

    [Fact]
    public void TryFromJson_WithNullJson_ThrowsArgumentNullException()
    {
        string? json = null;
        ResiliencyEventPublisher? publisher = null;
        Assert.Throws<ArgumentNullException>(
            () => ResiliencyEventPublisherJsonExtensions.TryFromJson(json!, out publisher!)
        );
    }

    [Fact]
    public void TryFromJson_WithEmptyJson_ReturnsFalseAndNullPublisher()
    {
        var json = "   ";
        ResiliencyEventPublisher? publisher = new();
        var result = ResiliencyEventPublisherJsonExtensions.TryFromJson(json, out publisher);

        result.Should().BeFalse();
        publisher.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_WithWhitespaceJson_ReturnsFalseAndNullPublisher()
    {
        var json = "\t\n  \r";
        ResiliencyEventPublisher? publisher = new();
        var result = ResiliencyEventPublisherJsonExtensions.TryFromJson(json, out publisher);

        result.Should().BeFalse();
        publisher.Should().BeNull();
    }

    [Fact]
    public void TryFromJson_WithInvalidJson_ReturnsFalseAndNullPublisher()
    {
        var json = "{ invalid json";
        ResiliencyEventPublisher? publisher = new();
        var result = ResiliencyEventPublisherJsonExtensions.TryFromJson(json, out publisher);

        result.Should().BeFalse();
        publisher.Should().BeNull();
    }

    [Fact]
    public void RoundTripSerialization_PreservesMaxHistorySize()
    {
        var originalPublisher = new ResiliencyEventPublisher { MaxHistorySize = 750 };
        var json = originalPublisher.ToJson();
        var deserializedPublisher = ResiliencyEventPublisherJsonExtensions.FromJson(json);

        deserializedPublisher.MaxHistorySize.Should().Be(originalPublisher.MaxHistorySize);
    }

    [Fact]
    public void JsonFormat_UsesCamelCaseNaming()
    {
        var publisher = new ResiliencyEventPublisher { MaxHistorySize = 123 };
        var json = publisher.ToJson();

        json.Should().Contain("maxHistorySize");
        json.Should().NotContain("MaxHistorySize");
    }

    [Fact]
    public void DefaultMaxHistorySize_IsSerialized()
    {
        var publisher = new ResiliencyEventPublisher();
        var json = publisher.ToJson();

        json.Should().Contain("1000");
    }

    [Fact]
    public void CustomMaxHistorySize_IsSerialized()
    {
        var publisher = new ResiliencyEventPublisher { MaxHistorySize = 5000 };
        var json = publisher.ToJson();

        json.Should().Contain("5000");
    }

    [Fact]
    public void FromJson_WithZeroMaxHistorySize_SetsProperty()
    {
        var json = "{\"maxHistorySize\":0}";
        var publisher = ResiliencyEventPublisherJsonExtensions.FromJson(json);

        publisher.MaxHistorySize.Should().Be(0);
    }

    [Fact]
    public void FromJson_WithLargeMaxHistorySize_SetsProperty()
    {
        var json = "{\"maxHistorySize\":1000000}";
        var publisher = ResiliencyEventPublisherJsonExtensions.FromJson(json);

        publisher.MaxHistorySize.Should().Be(1000000);
    }
}