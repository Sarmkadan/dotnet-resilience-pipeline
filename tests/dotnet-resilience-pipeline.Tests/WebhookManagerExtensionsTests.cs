using DotNetResiliencePipeline.Integration;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public class WebhookManagerExtensionsTests
{
    private readonly WebhookManager _manager = new();

    [Fact]
    public void HasWebhookRegistered_WithValidId_ReturnsTrue()
    {
        // Arrange
        var url = "http://example.com";
        var events = new[] { "test.event" };
        var id = _manager.RegisterWebhook(url, events);

        // Act
        var result = _manager.HasWebhookRegistered(id);

        // Assert
        result.Should().BeTrue();
    }

    [Fact]
    public void HasWebhookRegistered_WithInvalidId_ReturnsFalse()
    {
        // Act
        var result = _manager.HasWebhookRegistered("non-existent");

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void HasWebhookRegistered_NullManager_ThrowsNullReferenceException()
    {
        // Act
        Action act = () => WebhookManagerExtensions.HasWebhookRegistered(null!, "test");

        // Assert
        act.Should().Throw<NullReferenceException>();
    }

    [Fact]
    public void GetWebhooksByEvent_WithMatchingEvent_ReturnsSubscription()
    {
        // Arrange
        var url = "http://example.com";
        var events = new[] { "test.event" };
        _manager.RegisterWebhook(url, events);

        // Act
        var result = _manager.GetWebhooksByEvent("test.event");

        // Assert
        result.Should().ContainSingle();
    }

    [Fact]
    public void GetWebhooksByEvent_WithNoMatchingEvent_ReturnsEmpty()
    {
        // Act
        var result = _manager.GetWebhooksByEvent("non-existent");

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetDeliveryHistoryForWebhook_WithValidRange_ReturnsEmptyWhenNoHistory()
    {
        // Arrange
        var url = "http://example.com";
        var events = new[] { "test.event" };
        var id = _manager.RegisterWebhook(url, events);
        var startTime = DateTime.UtcNow.AddMinutes(-1);
        var endTime = DateTime.UtcNow.AddMinutes(1);

        // Act
        var result = _manager.GetDeliveryHistoryForWebhook(id, startTime, endTime);

        // Assert
        result.Should().BeEmpty();
    }

    [Fact]
    public void GetDeliveryHistoryForWebhook_InvalidTimeRange_ThrowsArgumentException()
    {
        // Arrange
        var id = "wh-123";
        var startTime = DateTime.UtcNow.AddMinutes(1);
        var endTime = DateTime.UtcNow;

        // Act
        Action act = () => _manager.GetDeliveryHistoryForWebhook(id, startTime, endTime);

        // Assert
        act.Should().Throw<ArgumentException>();
    }
}
