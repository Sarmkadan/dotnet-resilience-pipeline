using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Integration;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public class WebhookManagerTests
{
    private readonly WebhookManager _manager;

    public WebhookManagerTests()
    {
        _manager = new WebhookManager();
    }

    [Fact]
    public void MaxHistoryEntries_DefaultValue_ReturnsExpected()
    {
        // Arrange & Act
        var maxEntries = _manager.MaxHistoryEntries;

        // Assert
        maxEntries.Should().Be(1000);
    }

    [Fact]
    public void MaxHistoryEntries_DefaultValue_ReturnsExpectedValue()
    {
        // Arrange & Act
        var maxEntries = _manager.MaxHistoryEntries;

        // Assert
        maxEntries.Should().Be(1000);
    }

    [Fact]
    public void RegisterWebhook_WithValidParameters_ReturnsValidId()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = new[] { "test.event", "another.event" };

        // Act
        var webhookId = _manager.RegisterWebhook(url, events);

        // Assert
        webhookId.Should().NotBeNullOrEmpty();
        webhookId.Should().StartWith("wh-");
    }

    [Fact]
    public void RegisterWebhook_WithValidParameters_CreatesActiveSubscription()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = new[] { "test.event" };

        // Act
        var webhookId = _manager.RegisterWebhook(url, events);
        var webhook = _manager.GetWebhook(webhookId);

        // Assert
        webhook.Should().NotBeNull();
        webhook!.Url.Should().Be(url);
        webhook.Events.Should().BeEquivalentTo(events);
        webhook.IsActive.Should().BeTrue();
        webhook.Id.Should().Be(webhookId);
    }

    [Fact]
    public void RegisterWebhook_WithCustomHeaders_IncludesHeadersInSubscription()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = new[] { "test.event" };
        var headers = new Dictionary<string, string> { { "X-Custom-Header", "custom-value" }, { "Authorization", "Bearer token" } };

        // Act
        var webhookId = _manager.RegisterWebhook(url, events, headers);
        var webhook = _manager.GetWebhook(webhookId);

        // Assert
        webhook.Should().NotBeNull();
        webhook!.CustomHeaders.Should().BeEquivalentTo(headers);
    }

    [Fact]
    public void RegisterWebhook_WithNullUrl_ThrowsArgumentNullException()
    {
        // Arrange
        string url = null!;
        var events = new[] { "test.event" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _manager.RegisterWebhook(url, events));
        exception.ParamName.Should().Be("url");
    }

    [Fact]
    public void RegisterWebhook_WithEmptyUrl_ThrowsArgumentNullException()
    {
        // Arrange
        var url = "   ";
        var events = new[] { "test.event" };

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _manager.RegisterWebhook(url, events));
        exception.ParamName.Should().Be("url");
    }

    [Fact]
    public void RegisterWebhook_WithNullEvents_ThrowsArgumentNullException()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        string[] events = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _manager.RegisterWebhook(url, events));
        exception.ParamName.Should().Be("events");
    }

    [Fact]
    public void RegisterWebhook_WithEmptyEvents_ThrowsArgumentNullException()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = Array.Empty<string>();

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _manager.RegisterWebhook(url, events));
        exception.ParamName.Should().Be("events");
    }

    [Fact]
    public void RegisterWebhook_WithInvalidUrl_ThrowsWebhookRegistrationException()
    {
        // Arrange
        var url = "not-a-url";
        var events = new[] { "test.event" };

        // Act & Assert
        Assert.Throws<WebhookRegistrationException>(() => _manager.RegisterWebhook(url, events));
    }

    [Fact]
    public void RegisterWebhook_WithRelativeUrl_ThrowsWebhookRegistrationException()
    {
        // Arrange
        var url = "/relative/path";
        var events = new[] { "test.event" };

        // Act & Assert
        Assert.Throws<WebhookRegistrationException>(() => _manager.RegisterWebhook(url, events));
    }

    [Fact]
    public void RegisterWebhook_WithNonHttpUrl_ThrowsWebhookRegistrationException()
    {
        // Arrange
        var url = "ftp://example.com/webhooks/test";
        var events = new[] { "test.event" };

        // Act & Assert
        Assert.Throws<WebhookRegistrationException>(() => _manager.RegisterWebhook(url, events));
    }

    [Fact]
    public void UnregisterWebhook_WithValidId_ReturnsTrueAndRemovesSubscription()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = new[] { "test.event" };
        var webhookId = _manager.RegisterWebhook(url, events);
        var initialCount = _manager.GetAllWebhooks().Count;

        // Act
        var result = _manager.UnregisterWebhook(webhookId);
        var afterCount = _manager.GetAllWebhooks().Count;

        // Assert
        result.Should().BeTrue();
        afterCount.Should().Be(initialCount - 1);
        _manager.GetWebhook(webhookId).Should().BeNull();
    }

    [Fact]
    public void UnregisterWebhook_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var invalidId = "non-existent-id";

        // Act
        var result = _manager.UnregisterWebhook(invalidId);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void UnregisterWebhook_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        string webhookId = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _manager.UnregisterWebhook(webhookId));
        exception.ParamName.Should().Be("webhookId");
    }

    [Fact]
    public void UnregisterWebhook_WithEmptyId_ThrowsArgumentNullException()
    {
        // Arrange
        var webhookId = "   ";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _manager.UnregisterWebhook(webhookId));
        exception.ParamName.Should().Be("webhookId");
    }

    [Fact]
    public async Task TriggerEventAsync_WithNoSubscriptions_DoesNotThrow()
    {
        // Arrange
        var eventType = "test.event";
        var eventData = new { test = "data" };

        // Act & Assert
        await _manager.TriggerEventAsync(eventType, eventData);
        // Should not throw even with no subscriptions
    }

    [Fact]
    public async Task TriggerEventAsync_WithActiveSubscription_CallsWebhook()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = new[] { "test.event" };
        var webhookId = _manager.RegisterWebhook(url, events);
        var eventType = "test.event";
        var eventData = new { test = "data" };

        // Act
        await _manager.TriggerEventAsync(eventType, eventData);

        // Assert - delivery should be recorded
        var history = _manager.GetDeliveryHistory(webhookId, limit: 1);
        history.Should().HaveCount(1);
        history[0].EventType.Should().Be(eventType);
        history[0].WebhookId.Should().Be(webhookId);
        history[0].Success.Should().BeTrue();
    }

    [Fact]
    public async Task TriggerEventAsync_WithInactiveSubscription_DoesNotDeliver()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = new[] { "test.event" };
        var webhookId = _manager.RegisterWebhook(url, events);
        _manager.SetWebhookActive(webhookId, false);
        var eventType = "test.event";
        var eventData = new { test = "data" };

        // Act
        await _manager.TriggerEventAsync(eventType, eventData);

        // Assert - no delivery should be recorded
        var history = _manager.GetDeliveryHistory(webhookId);
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task TriggerEventAsync_WithMultipleEvents_OnlyDeliversToMatchingSubscriptions()
    {
        // Arrange
        var url1 = "https://example.com/webhooks/test1";
        var url2 = "https://example.com/webhooks/test2";
        var events1 = new[] { "event.a", "event.b" };
        var events2 = new[] { "event.b", "event.c" };
        var webhookId1 = _manager.RegisterWebhook(url1, events1);
        var webhookId2 = _manager.RegisterWebhook(url2, events2);
        var eventType = "event.b";
        var eventData = new { test = "data" };

        // Act
        await _manager.TriggerEventAsync(eventType, eventData);

        // Assert - both webhooks should receive the event
        var history1 = _manager.GetDeliveryHistory(webhookId1, limit: 1);
        var history2 = _manager.GetDeliveryHistory(webhookId2, limit: 1);

        history1.Should().HaveCount(1);
        history1[0].EventType.Should().Be(eventType);
        history2.Should().HaveCount(1);
        history2[0].EventType.Should().Be(eventType);
    }

    [Fact]
    public async Task TriggerEventAsync_WithNullEventType_ThrowsArgumentNullException()
    {
        // Arrange
        string eventType = null!;
        var eventData = new { test = "data" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _manager.TriggerEventAsync(eventType, eventData));
        exception.ParamName.Should().Be("eventType");
    }

    [Fact]
    public async Task TriggerEventAsync_WithEmptyEventType_ThrowsArgumentNullException()
    {
        // Arrange
        var eventType = "   ";
        var eventData = new { test = "data" };

        // Act & Assert
        var exception = await Assert.ThrowsAsync<ArgumentNullException>(() => _manager.TriggerEventAsync(eventType, eventData));
        exception.ParamName.Should().Be("eventType");
    }

    [Fact]
    public void GetAllWebhooks_WithNoSubscriptions_ReturnsEmptyList()
    {
        // Arrange
        // No subscriptions registered

        // Act
        var webhooks = _manager.GetAllWebhooks();

        // Assert
        webhooks.Should().BeEmpty();
    }

    [Fact]
    public void GetAllWebhooks_WithMultipleSubscriptions_ReturnsAll()
    {
        // Arrange
        var url1 = "https://example.com/webhooks/test1";
        var url2 = "https://example.com/webhooks/test2";
        var events1 = new[] { "test.event" };
        var events2 = new[] { "test.event" };
        var webhookId1 = _manager.RegisterWebhook(url1, events1);
        var webhookId2 = _manager.RegisterWebhook(url2, events2);

        // Act
        var webhooks = _manager.GetAllWebhooks();

        // Assert
        webhooks.Should().HaveCount(2);
        webhooks.Should().Contain(w => w.Id == webhookId1);
        webhooks.Should().Contain(w => w.Id == webhookId2);
    }

    [Fact]
    public void GetWebhook_WithValidId_ReturnsSubscription()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = new[] { "test.event" };
        var webhookId = _manager.RegisterWebhook(url, events);

        // Act
        var webhook = _manager.GetWebhook(webhookId);

        // Assert
        webhook.Should().NotBeNull();
        webhook!.Id.Should().Be(webhookId);
        webhook.Url.Should().Be(url);
    }

    [Fact]
    public void GetWebhook_WithInvalidId_ReturnsNull()
    {
        // Arrange
        var invalidId = "non-existent-id";

        // Act
        var webhook = _manager.GetWebhook(invalidId);

        // Assert
        webhook.Should().BeNull();
    }

    [Fact]
    public void GetWebhook_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        string webhookId = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _manager.GetWebhook(webhookId));
        exception.ParamName.Should().Be("webhookId");
    }

    [Fact]
    public void GetWebhook_WithEmptyId_ThrowsArgumentNullException()
    {
        // Arrange
        var webhookId = "   ";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _manager.GetWebhook(webhookId));
        exception.ParamName.Should().Be("webhookId");
    }

    [Fact]
    public void SetWebhookActive_WithValidId_SetsActiveStatus()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = new[] { "test.event" };
        var webhookId = _manager.RegisterWebhook(url, events);
        var webhook = _manager.GetWebhook(webhookId);
        webhook!.IsActive.Should().BeTrue();

        // Act
        var result = _manager.SetWebhookActive(webhookId, false);

        // Assert
        result.Should().BeTrue();
        webhook = _manager.GetWebhook(webhookId);
        webhook!.IsActive.Should().BeFalse();
    }

    [Fact]
    public void SetWebhookActive_WithInvalidId_ReturnsFalse()
    {
        // Arrange
        var invalidId = "non-existent-id";

        // Act
        var result = _manager.SetWebhookActive(invalidId, true);

        // Assert
        result.Should().BeFalse();
    }

    [Fact]
    public void SetWebhookActive_WithNullId_ThrowsArgumentNullException()
    {
        // Arrange
        string webhookId = null!;

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _manager.SetWebhookActive(webhookId, true));
        exception.ParamName.Should().Be("webhookId");
    }

    [Fact]
    public void SetWebhookActive_WithEmptyId_ThrowsArgumentNullException()
    {
        // Arrange
        var webhookId = "   ";

        // Act & Assert
        var exception = Assert.Throws<ArgumentNullException>(() => _manager.SetWebhookActive(webhookId, true));
        exception.ParamName.Should().Be("webhookId");
    }

    [Fact]
    public void GetDeliveryHistory_WithNoDeliveries_ReturnsEmptyList()
    {
        // Arrange
        // No deliveries made

        // Act
        var history = _manager.GetDeliveryHistory();

        // Assert
        history.Should().BeEmpty();
    }

    [Fact]
    public async Task GetDeliveryHistory_WithLimit_ReturnsLimitedResults()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = new[] { "test.event" };
        var webhookId = _manager.RegisterWebhook(url, events);

        // Make multiple deliveries
        for (int i = 0; i < 5; i++)
        {
            await _manager.TriggerEventAsync("test.event", new { counter = i });
        }

        // Act
        var history = _manager.GetDeliveryHistory(limit: 2);

        // Assert
        history.Should().HaveCount(2);
    }

    [Fact]
    public void GetDeliveryHistory_WithZeroLimit_ThrowsArgumentException()
    {
        // Arrange
        // No setup needed

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.GetDeliveryHistory(limit: 0));
        exception.ParamName.Should().Be("limit");
    }

    [Fact]
    public void GetDeliveryHistory_WithNegativeLimit_ThrowsArgumentException()
    {
        // Arrange
        // No setup needed

        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => _manager.GetDeliveryHistory(limit: -1));
        exception.ParamName.Should().Be("limit");
    }

    [Fact]
    public async Task GetDeliveryHistory_WithWebhookId_FiltersByWebhook()
    {
        // Arrange
        var url1 = "https://example.com/webhooks/test1";
        var url2 = "https://example.com/webhooks/test2";
        var events = new[] { "test.event" };
        var webhookId1 = _manager.RegisterWebhook(url1, events);
        var webhookId2 = _manager.RegisterWebhook(url2, events);

        // Make deliveries to both
        await _manager.TriggerEventAsync("test.event", new { source = "webhook1" });
        await _manager.TriggerEventAsync("test.event", new { source = "webhook2" });

        // Act
        var history1 = _manager.GetDeliveryHistory(webhookId1);
        var history2 = _manager.GetDeliveryHistory(webhookId2);

        // Assert
        history1.Should().HaveCount(1);
        history1[0].WebhookId.Should().Be(webhookId1);
        history2.Should().HaveCount(1);
        history2[0].WebhookId.Should().Be(webhookId2);
    }

    [Fact]
    public void GetDeliveryHistory_WithNullWebhookId_ReturnsAllDeliveries()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = new[] { "test.event" };
        var webhookId = _manager.RegisterWebhook(url, events);
        _manager.TriggerEventAsync("test.event", new { test = "data" }).Wait();

        // Act
        var history = _manager.GetDeliveryHistory(webhookId: null);

        // Assert
        history.Should().HaveCount(1);
    }

    [Fact]
    public void GetStatistics_WithNoDeliveries_ReturnsZeroStatistics()
    {
        // Arrange
        // No deliveries made

        // Act
        var stats = _manager.GetStatistics();

        // Assert
        stats.TotalDeliveries.Should().Be(0);
        stats.SuccessfulDeliveries.Should().Be(0);
        stats.FailedDeliveries.Should().Be(0);
        stats.SuccessRate.Should().Be(0);
        stats.ActiveSubscriptions.Should().Be(0);
    }

    [Fact]
    public async Task GetStatistics_WithDeliveries_ReturnsCorrectStatistics()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = new[] { "test.event" };
        var webhookId = _manager.RegisterWebhook(url, events);

        // Make successful delivery
        await _manager.TriggerEventAsync("test.event", new { test = "success" });

        // Make failed delivery (will fail since URL doesn't exist)
        try
        {
            _manager.RegisterWebhook("https://invalid-url-that-does-not-exist-12345.com/webhooks/fail", events);
            await _manager.TriggerEventAsync("test.event", new { test = "fail" });
        }
        catch { /* Expected to fail */ }

        // Act
        var stats = _manager.GetStatistics();

        // Assert
        stats.TotalDeliveries.Should().BeGreaterThan(0);
        stats.SuccessfulDeliveries.Should().BeGreaterThan(0);
        stats.ActiveSubscriptions.Should().Be(1); // Only the valid one is active
    }

    [Fact]
    public void MaxHistoryEntries_WhenSet_ChangesHistoryLimit()
    {
        // Arrange
        var initialMax = _manager.MaxHistoryEntries;

        // Act
        _manager.MaxHistoryEntries = 500;
        var newMax = _manager.MaxHistoryEntries;

        // Assert
        newMax.Should().Be(500);
        newMax.Should().NotBe(initialMax);
    }

    [Fact]
    public async Task GetDeliveryHistory_RespectsMaxHistoryEntries()
    {
        // Arrange
        var url = "https://example.com/webhooks/test";
        var events = new[] { "test.event" };
        var webhookId = _manager.RegisterWebhook(url, events);

        // Set low max history to test boundary
        _manager.MaxHistoryEntries = 3;

        // Make multiple deliveries to exceed max
        for (int i = 0; i < 5; i++)
        {
            await _manager.TriggerEventAsync("test.event", new { counter = i });
        }

        // Act
        var history = _manager.GetDeliveryHistory();

        // Assert
        history.Should().HaveCount(3); // Should be limited to MaxHistoryEntries
    }
}