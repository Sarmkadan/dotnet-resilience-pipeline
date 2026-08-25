using DotNetResiliencePipeline.Exceptions;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Unit tests for the webhook exception types exposed by the
/// <c>DotNetResiliencePipeline.Exceptions</c> namespace. Covers constructor
/// parameter handling, property assignment, generated message formatting,
/// defaults for optional arguments, and the inheritance hierarchy rooted at
/// <see cref="ResiliencyException"/>.
/// </summary>
public class WebhookExceptionTests
{
    /// <summary>
    /// Verifies that creating a <see cref="WebhookException"/> with only a message
    /// stores that message and leaves <see cref="WebhookException.WebhookId"/>,
    /// <see cref="WebhookException.WebhookUrl"/> and the inner exception unset.
    /// </summary>
    [Fact]
    public void WebhookException_WithMessageOnly_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Test webhook exception message";

        // Act
        var exception = new WebhookException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.WebhookId.Should().BeNull();
        exception.WebhookUrl.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    /// <summary>
    /// Verifies that creating a <see cref="WebhookException"/> with a message,
    /// webhook identifier and webhook URL assigns each value to its matching
    /// property and leaves the inner exception unset.
    /// </summary>
    [Fact]
    public void WebhookException_WithMessageAndWebhookIdAndUrl_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Test webhook exception message";
        var webhookId = "wh-12345";
        var webhookUrl = "https://example.com/webhooks/test";

        // Act
        var exception = new WebhookException(message, webhookId, webhookUrl);

        // Assert
        exception.Message.Should().Be(message);
        exception.WebhookId.Should().Be(webhookId);
        exception.WebhookUrl.Should().Be(webhookUrl);
        exception.InnerException.Should().BeNull();
    }

    /// <summary>
    /// Verifies that creating a <see cref="WebhookException"/> with a message,
    /// inner exception, webhook identifier and webhook URL populates every
    /// property, including the supplied inner exception.
    /// </summary>
    [Fact]
    public void WebhookException_WithMessageInnerExceptionAndWebhookIdAndUrl_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Test webhook exception message";
        var innerException = new InvalidOperationException("Inner error");
        var webhookId = "wh-67890";
        var webhookUrl = "https://example.com/webhooks/prod";

        // Act
        var exception = new WebhookException(message, innerException, webhookId, webhookUrl);

        // Assert
        exception.Message.Should().Be(message);
        exception.WebhookId.Should().Be(webhookId);
        exception.WebhookUrl.Should().Be(webhookUrl);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    /// <summary>
    /// Verifies that an empty message is accepted by <see cref="WebhookException"/>,
    /// yielding an empty <see cref="WebhookException.Message"/> with no webhook
    /// identifier or URL.
    /// </summary>
    [Fact]
    public void WebhookException_WithEmptyMessage_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = string.Empty;

        // Act
        var exception = new WebhookException(message);

        // Assert
        exception.Message.Should().BeEmpty();
        exception.WebhookId.Should().BeNull();
        exception.WebhookUrl.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a <see cref="WebhookDeliveryFailedException"/> built from a
    /// webhook identifier, URL, event type, attempt count and inner exception
    /// stores every value and formats its message as
    /// "Webhook delivery failed after {attemptCount} attempt(s) for event
    /// '{eventType}' to {webhookUrl}".
    /// </summary>
    [Fact]
    public void WebhookDeliveryFailedException_WithAllParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var webhookId = "wh-delivery-001";
        var webhookUrl = "https://api.example.com/webhooks/events";
        var eventType = "user.created";
        var attemptCount = 5;
        var innerException = new HttpRequestException("Failed to deliver webhook");

        // Act
        var exception = new WebhookDeliveryFailedException(webhookId, webhookUrl, eventType, attemptCount, innerException);

        // Assert
        exception.Message.Should().Be("Webhook delivery failed after 5 attempt(s) for event 'user.created' to https://api.example.com/webhooks/events");
        exception.WebhookId.Should().Be(webhookId);
        exception.WebhookUrl.Should().Be(webhookUrl);
        exception.EventType.Should().Be(eventType);
        exception.AttemptCount.Should().Be(attemptCount);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    /// <summary>
    /// Verifies that a <see cref="WebhookDeliveryFailedException"/> created without
    /// an inner exception still produces a non-empty message and records the
    /// webhook identifier, URL, event type and attempt count.
    /// </summary>
    [Fact]
    public void WebhookDeliveryFailedException_WithMinimumParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var webhookId = "wh-minimal";
        var webhookUrl = "https://minimal.example.com/webhook";
        var eventType = "order.placed";
        var attemptCount = 1;

        // Act
        var exception = new WebhookDeliveryFailedException(webhookId, webhookUrl, eventType, attemptCount, null);

        exception.Message.Should().NotBeNullOrEmpty();
        exception.WebhookId.Should().Be(webhookId);
        exception.WebhookUrl.Should().Be(webhookUrl);
        exception.EventType.Should().Be(eventType);
        exception.AttemptCount.Should().Be(attemptCount);
        exception.InnerException.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a zero attempt count passed to
    /// <see cref="WebhookDeliveryFailedException"/> is preserved on
    /// <see cref="WebhookDeliveryFailedException.AttemptCount"/>.
    /// </summary>
    [Fact]
    public void WebhookDeliveryFailedException_WithZeroAttemptCount_SetsPropertiesCorrectly()
    {
        // Arrange
        var webhookId = "wh-zero-attempts";
        var webhookUrl = "https://zero.example.com/webhook";
        var eventType = "test.event";
        var attemptCount = 0;

        // Act
        var exception = new WebhookDeliveryFailedException(webhookId, webhookUrl, eventType, attemptCount, null);

        // Assert
        exception.AttemptCount.Should().Be(0);
    }

    /// <summary>
    /// Verifies that a large attempt count (1000) passed to
    /// <see cref="WebhookDeliveryFailedException"/> is preserved on
    /// <see cref="WebhookDeliveryFailedException.AttemptCount"/>.
    /// </summary>
    [Fact]
    public void WebhookDeliveryFailedException_WithLargeAttemptCount_SetsPropertiesCorrectly()
    {
        // Arrange
        var webhookId = "wh-large-attempts";
        var webhookUrl = "https://large.example.com/webhook";
        var eventType = "critical.event";
        var attemptCount = 1000;

        // Act
        var exception = new WebhookDeliveryFailedException(webhookId, webhookUrl, eventType, attemptCount, null);

        // Assert
        exception.AttemptCount.Should().Be(1000);
    }

    /// <summary>
    /// Verifies that creating a <see cref="WebhookRegistrationException"/> with
    /// only a message leaves <see cref="WebhookRegistrationException.WebhookUrl"/>
    /// and the inner exception unset.
    /// </summary>
    [Fact]
    public void WebhookRegistrationException_WithMessageOnly_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Webhook registration failed";

        // Act
        var exception = new WebhookRegistrationException(message);

        // Assert
        exception.Message.Should().Be(message);
        exception.WebhookUrl.Should().BeNull();
        exception.InnerException.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a message and webhook URL passed to
    /// <see cref="WebhookRegistrationException"/> are stored on their respective
    /// properties and the inner exception remains unset.
    /// </summary>
    [Fact]
    public void WebhookRegistrationException_WithMessageAndWebhookUrl_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Webhook registration failed due to invalid signature";
        var webhookUrl = "https://secure.example.com/webhooks/register";

        // Act
        var exception = new WebhookRegistrationException(message, webhookUrl);

        // Assert
        exception.Message.Should().Be(message);
        exception.WebhookUrl.Should().Be(webhookUrl);
        exception.InnerException.Should().BeNull();
    }

    /// <summary>
    /// Verifies that a message, inner exception and webhook URL passed to
    /// <see cref="WebhookRegistrationException"/> are all recorded, including the
    /// reference to the supplied inner exception.
    /// </summary>
    [Fact]
    public void WebhookRegistrationException_WithMessageAndInnerExceptionAndWebhookUrl_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Webhook registration failed";
        var innerException = new UnauthorizedAccessException("Invalid credentials");
        var webhookUrl = "https://auth.example.com/webhooks";

        // Act
        var exception = new WebhookRegistrationException(message, innerException, webhookUrl);

        // Assert
        exception.Message.Should().Be(message);
        exception.WebhookUrl.Should().Be(webhookUrl);
        exception.InnerException.Should().BeSameAs(innerException);
    }

    /// <summary>
    /// Verifies that a message, webhook identifier and webhook URL passed to
    /// <see cref="InvalidWebhookException"/> are assigned to their matching
    /// properties and the inner exception remains unset.
    /// </summary>
    [Fact]
    public void InvalidWebhookException_WithAllParameters_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = "Invalid webhook subscription: URL mismatch";
        var webhookId = "wh-invalid-001";
        var webhookUrl = "https://expected.example.com/webhooks";

        // Act
        var exception = new InvalidWebhookException(message, webhookId, webhookUrl);

        // Assert
        exception.Message.Should().Be(message);
        exception.WebhookId.Should().Be(webhookId);
        exception.WebhookUrl.Should().Be(webhookUrl);
        exception.InnerException.Should().BeNull();
    }

    /// <summary>
    /// Verifies that an empty message passed to <see cref="InvalidWebhookException"/>
    /// produces an empty <see cref="WebhookException.Message"/> while the webhook
    /// identifier and URL are still stored.
    /// </summary>
    [Fact]
    public void InvalidWebhookException_WithEmptyMessage_SetsPropertiesCorrectly()
    {
        // Arrange
        var message = string.Empty;
        var webhookId = "wh-empty";
        var webhookUrl = "https://empty.example.com/webhook";

        // Act
        var exception = new InvalidWebhookException(message, webhookId, webhookUrl);

        // Assert
        exception.Message.Should().BeEmpty();
        exception.WebhookId.Should().Be(webhookId);
        exception.WebhookUrl.Should().Be(webhookUrl);
    }

    /// <summary>
    /// Verifies that <see cref="WebhookException"/> is assignable to
    /// <see cref="ResiliencyException"/>.
    /// </summary>
    [Fact]
    public void WebhookException_InheritsFromResiliencyException()
    {
        // Arrange
        var message = "Test exception";

        // Act
        var exception = new WebhookException(message);

        // Assert
        exception.Should().BeAssignableTo<ResiliencyException>();
    }

    /// <summary>
    /// Verifies that <see cref="WebhookDeliveryFailedException"/> is assignable to
    /// both <see cref="WebhookException"/> and <see cref="ResiliencyException"/>.
    /// </summary>
    [Fact]
    public void WebhookDeliveryFailedException_InheritsFromWebhookException()
    {
        // Arrange
        var webhookId = "wh-test";
        var webhookUrl = "https://test.com/webhook";
        var eventType = "test.event";
        var attemptCount = 3;
        var innerException = new Exception("Inner");

        // Act
        var exception = new WebhookDeliveryFailedException(webhookId, webhookUrl, eventType, attemptCount, innerException);

        // Assert
        exception.Should().BeAssignableTo<WebhookException>();
        exception.Should().BeAssignableTo<ResiliencyException>();
    }

    /// <summary>
    /// Verifies that <see cref="WebhookRegistrationException"/> is assignable to
    /// both <see cref="WebhookException"/> and <see cref="ResiliencyException"/>.
    /// </summary>
    [Fact]
    public void WebhookRegistrationException_InheritsFromWebhookException()
    {
        // Arrange
        var message = "Test registration";

        // Act
        var exception = new WebhookRegistrationException(message);

        // Assert
        exception.Should().BeAssignableTo<WebhookException>();
        exception.Should().BeAssignableTo<ResiliencyException>();
    }

    /// <summary>
    /// Verifies that <see cref="InvalidWebhookException"/> is assignable to both
    /// <see cref="WebhookException"/> and <see cref="ResiliencyException"/>.
    /// </summary>
    [Fact]
    public void InvalidWebhookException_InheritsFromWebhookException()
    {
        // Arrange
        var message = "Test invalid";
        var webhookId = "wh-123";
        var webhookUrl = "https://test.com/webhook";

        // Act
        var exception = new InvalidWebhookException(message, webhookId, webhookUrl);

        // Assert
        exception.Should().BeAssignableTo<WebhookException>();
        exception.Should().BeAssignableTo<ResiliencyException>();
    }

    /// <summary>
    /// Verifies that <see cref="WebhookDeliveryFailedException.AttemptCount"/>
    /// exposes the attempt count supplied at construction (7).
    /// </summary>
    [Fact]
    public void WebhookDeliveryFailedException_HasCorrectAttemptCountProperty()
    {
        // Arrange
        var webhookId = "wh-count-test";
        var webhookUrl = "https://count.example.com/webhook";
        var eventType = "count.test";
        var attemptCount = 7;

        // Act
        var exception = new WebhookDeliveryFailedException(webhookId, webhookUrl, eventType, attemptCount, null);

        // Assert
        exception.AttemptCount.Should().Be(7);
    }

    /// <summary>
    /// Verifies that <see cref="WebhookDeliveryFailedException.EventType"/> exposes
    /// the event type supplied at construction.
    /// </summary>
    [Fact]
    public void WebhookDeliveryFailedException_HasCorrectEventTypeProperty()
    {
        // Arrange
        var webhookId = "wh-type-test";
        var webhookUrl = "https://type.example.com/webhook";
        var eventType = "specific.event.type";
        var attemptCount = 2;

        // Act
        var exception = new WebhookDeliveryFailedException(webhookId, webhookUrl, eventType, attemptCount, null);

        // Assert
        exception.EventType.Should().Be(eventType);
    }

    /// <summary>
    /// Verifies that <see cref="WebhookRegistrationException.WebhookUrl"/> exposes
    /// the webhook URL supplied at construction.
    /// </summary>
    [Fact]
    public void WebhookRegistrationException_HasCorrectWebhookUrlProperty()
    {
        // Arrange
        var message = "Test url property";
        var webhookUrl = "https://url-test.example.com/webhook";

        // Act
        var exception = new WebhookRegistrationException(message, webhookUrl);

        // Assert
        exception.WebhookUrl.Should().Be(webhookUrl);
    }

    /// <summary>
    /// Verifies that <see cref="InvalidWebhookException.WebhookId"/> exposes the
    /// webhook identifier supplied at construction.
    /// </summary>
    [Fact]
    public void InvalidWebhookException_HasCorrectWebhookIdProperty()
    {
        // Arrange
        var message = "Test id property";
        var webhookId = "wh-id-property";
        var webhookUrl = "https://id-test.example.com/webhook";

        // Act
        var exception = new InvalidWebhookException(message, webhookId, webhookUrl);

        // Assert
        exception.WebhookId.Should().Be(webhookId);
    }
}
