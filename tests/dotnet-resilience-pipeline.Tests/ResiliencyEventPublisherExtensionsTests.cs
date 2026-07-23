#nullable enable
using DotNetResiliencePipeline.Events;
using FluentAssertions;
using System;
using System.Threading.Tasks;
using Xunit;

/// <summary>
/// Tests for the ResiliencyEventPublisherExtensions class.
/// </summary>
public sealed class ResiliencyEventPublisherExtensionsTests
{
    /// <summary>
    /// Creates a new ResiliencyEventPublisher instance for testing.
    /// </summary>
    private static ResiliencyEventPublisher CreatePublisher() => new();

    /// <summary>
    /// Verifies that PublishWithHistoryAsync successfully publishes an event.
    /// </summary>
    [Fact]
    public async Task PublishWithHistoryAsync_PublishesEventSuccessfully()
    {
        // Arrange
        var publisher = CreatePublisher();
        var eventData = new PolicyExecutedSuccessfullyEvent
        {
            PolicyName = "TestPolicy",
            DurationMs = 100,
            AttemptNumber = 1
        };

        // Act
        await publisher.PublishWithHistoryAsync(eventData);

        // Assert
        var history = publisher.GetEventHistory();
        history.Should().ContainSingle(e => e.Id == eventData.Id);
    }

    /// <summary>
    /// Verifies that PublishWithHistoryAsync throws ArgumentNullException when publisher is null.
    /// </summary>
    [Fact]
    public async Task PublishWithHistoryAsync_NullPublisher_ThrowsArgumentNullException()
    {
        // Arrange
        ResiliencyEventPublisher? publisher = null;
        var eventData = new PolicyExecutedSuccessfullyEvent();

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => publisher!.PublishWithHistoryAsync(eventData));
    }

    /// <summary>
    /// Verifies that PublishWithHistoryAsync throws ArgumentNullException when eventData is null.
    /// </summary>
    [Fact]
    public async Task PublishWithHistoryAsync_NullEventData_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();
        ResiliencyEvent? eventData = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => publisher.PublishWithHistoryAsync(eventData!));
    }

    /// <summary>
    /// Verifies that GetLastEvent returns the most recent event of type T.
    /// </summary>
    [Fact]
    public async Task GetLastEvent_ReturnsMostRecentEvent()
    {
        // Arrange
        var publisher = CreatePublisher();
        var event1 = new PolicyExecutedSuccessfullyEvent { PolicyName = "Policy1" };
        var event2 = new PolicyExecutedSuccessfullyEvent { PolicyName = "Policy2" };
        var event3 = new PolicyExecutedSuccessfullyEvent { PolicyName = "Policy3" };

        await publisher.PublishAsync(event1);
        await publisher.PublishAsync(event2);
        await publisher.PublishAsync(event3);

        // Act
        var lastEvent = publisher.GetLastEvent<PolicyExecutedSuccessfullyEvent>();

        // Assert
        lastEvent.Should().NotBeNull();
        lastEvent!.PolicyName.Should().Be("Policy3");
    }

    /// <summary>
    /// Verifies that GetLastEvent returns null when no events of type T exist.
    /// </summary>
    [Fact]
    public void GetLastEvent_NoEvents_ReturnsNull()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        var lastEvent = publisher.GetLastEvent<PolicyExecutedSuccessfullyEvent>();

        // Assert
        lastEvent.Should().BeNull();
    }

    /// <summary>
    /// Verifies that GetLastEvent returns null when publisher is null.
    /// </summary>
    [Fact]
    public void GetLastEvent_NullPublisher_ThrowsArgumentNullException()
    {
        // Arrange
        ResiliencyEventPublisher? publisher = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => publisher!.GetLastEvent<PolicyExecutedSuccessfullyEvent>());
    }

    /// <summary>
    /// Verifies that GetLastEvent returns the correct event when multiple event types are mixed.
    /// </summary>
    [Fact]
    public async Task GetLastEvent_MixedEventTypes_ReturnsCorrectType()
    {
        // Arrange
        var publisher = CreatePublisher();
        var circuitBreakerEvent = new CircuitBreakerStateChangedEvent { PolicyName = "CB1" };
        var timeoutEvent = new TimeoutOccurredEvent { PolicyName = "TO1" };
        var successEvent = new PolicyExecutedSuccessfullyEvent { PolicyName = "Success1" };

        await publisher.PublishAsync(circuitBreakerEvent);
        await publisher.PublishAsync(timeoutEvent);
        await publisher.PublishAsync(successEvent);

        // Act
        var lastSuccessEvent = publisher.GetLastEvent<PolicyExecutedSuccessfullyEvent>();

        // Assert
        lastSuccessEvent.Should().NotBeNull();
        lastSuccessEvent!.Should().BeOfType<PolicyExecutedSuccessfullyEvent>();
        lastSuccessEvent.PolicyName.Should().Be("Success1");
    }

    /// <summary>
    /// Verifies that PublishExceptionAsync successfully publishes an exception event.
    /// </summary>
    [Fact]
    public async Task PublishExceptionAsync_PublishesExceptionEvent()
    {
        // Arrange
        var publisher = CreatePublisher();
        var exception = new InvalidOperationException("Test exception");
        const string policyName = "TestPolicy";
        const long durationMs = 250;

        // Act
        await publisher.PublishExceptionAsync(exception, policyName, durationMs);

        // Assert
        var lastEvent = publisher.GetLastEvent<PolicyExecutionFailedEvent>();
        lastEvent.Should().NotBeNull();
        lastEvent!.ExceptionType.Should().Be(typeof(InvalidOperationException).FullName);
        lastEvent.ExceptionMessage.Should().Be("Test exception");
        lastEvent.PolicyName.Should().Be(policyName);
        lastEvent.DurationMs.Should().Be(durationMs);
    }

    /// <summary>
    /// Verifies that PublishExceptionAsync uses default duration of 0 when not specified.
    /// </summary>
    [Fact]
    public async Task PublishExceptionAsync_DefaultDuration_PublishesWithZeroDuration()
    {
        // Arrange
        var publisher = CreatePublisher();
        var exception = new ArgumentException("Invalid argument");
        const string policyName = "ValidationPolicy";

        // Act
        await publisher.PublishExceptionAsync(exception, policyName);

        // Assert
        var lastEvent = publisher.GetLastEvent<PolicyExecutionFailedEvent>();
        lastEvent.Should().NotBeNull();
        lastEvent!.DurationMs.Should().Be(0);
    }

    /// <summary>
    /// Verifies that PublishExceptionAsync throws ArgumentNullException when publisher is null.
    /// </summary>
    [Fact]
    public async Task PublishExceptionAsync_NullPublisher_ThrowsArgumentNullException()
    {
        // Arrange
        ResiliencyEventPublisher? publisher = null;
        var exception = new Exception("Test");
        const string policyName = "Policy";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => publisher!.PublishExceptionAsync(exception, policyName));
    }

    /// <summary>
    /// Verifies that PublishExceptionAsync throws ArgumentNullException when exception is null.
    /// </summary>
    [Fact]
    public async Task PublishExceptionAsync_NullException_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();
        Exception? exception = null;
        const string policyName = "Policy";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => publisher.PublishExceptionAsync(exception!, policyName));
    }

    /// <summary>
    /// Verifies that PublishExceptionAsync throws ArgumentNullException when policyName is null.
    /// </summary>
    [Fact]
    public async Task PublishExceptionAsync_NullPolicyName_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();
        var exception = new Exception("Test");
        string? policyName = null;

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentNullException>(
            () => publisher.PublishExceptionAsync(exception, policyName!));
    }

    /// <summary>
    /// Verifies that PublishExceptionAsync throws ArgumentException when policyName is whitespace.
    /// </summary>
    [Fact]
    public async Task PublishExceptionAsync_WhitespacePolicyName_ThrowsArgumentException()
    {
        // Arrange
        var publisher = CreatePublisher();
        var exception = new Exception("Test");
        const string policyName = "   ";

        // Act & Assert
        await Assert.ThrowsAsync<ArgumentException>(
            () => publisher.PublishExceptionAsync(exception, policyName));
    }

    /// <summary>
    /// Verifies that GetSubscriberCount returns correct count for specific event type.
    /// </summary>
    [Fact]
    public void GetSubscriberCount_ForSpecificEventType_ReturnsCorrectCount()
    {
        // Arrange
        var publisher = CreatePublisher();
        var handler1 = new Action<PolicyExecutedSuccessfullyEvent>(_ => { });
        var handler2 = new Action<PolicyExecutedSuccessfullyEvent>(_ => { });
        var handler3 = new Action<TimeoutOccurredEvent>(_ => { });

        publisher.Subscribe<PolicyExecutedSuccessfullyEvent>("PolicyExecutedSuccessfullyEvent", handler1);
        publisher.Subscribe<PolicyExecutedSuccessfullyEvent>("PolicyExecutedSuccessfullyEvent", handler2);
        publisher.Subscribe<TimeoutOccurredEvent>("TimeoutOccurredEvent", handler3);

        // Act
        var count = publisher.GetSubscriberCount("PolicyExecutedSuccessfullyEvent");

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Verifies that GetSubscriberCount returns 0 when no subscribers exist.
    /// </summary>
    [Fact]
    public void GetSubscriberCount_NoSubscribers_ReturnsZero()
    {
        // Arrange
        var publisher = CreatePublisher();

        // Act
        var count = publisher.GetSubscriberCount("NonExistentEvent");

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Verifies that GetSubscriberCount throws when publisher is null.
    /// </summary>
    [Fact]
    public void GetSubscriberCount_NullPublisher_Throws()
    {
        // Arrange
        ResiliencyEventPublisher? publisher = null;
        const string eventType = "TestEvent";

        // Act & Assert
        Assert.Throws<NullReferenceException>(
            () => publisher!.GetSubscriberCount(eventType));
    }

    /// <summary>
    /// Verifies that GetSubscriberCount throws ArgumentNullException when eventType is null.
    /// </summary>
    [Fact]
    public void GetSubscriberCount_NullEventType_ThrowsArgumentNullException()
    {
        // Arrange
        var publisher = CreatePublisher();
        string? eventType = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => publisher.GetSubscriberCount(eventType!));
    }

    /// <summary>
    /// Verifies that GetSubscriberCount returns 0 for whitespace eventType (no validation in publisher).
    /// </summary>
    [Fact]
    public void GetSubscriberCount_WhitespaceEventType_ReturnsZero()
    {
        // Arrange
        var publisher = CreatePublisher();
        const string eventType = "   ";

        // Act
        var count = publisher.GetSubscriberCount(eventType);

        // Assert
        count.Should().Be(0);
    }

    /// <summary>
    /// Verifies that GetSubscriberCount<T> returns correct count for type T.
    /// </summary>
    [Fact]
    public void GetSubscriberCount_Generic_ReturnsCorrectCount()
    {
        // Arrange
        var publisher = CreatePublisher();
        var handler1 = new Action<PolicyExecutedSuccessfullyEvent>(_ => { });
        var handler2 = new Action<PolicyExecutedSuccessfullyEvent>(_ => { });
        var handler3 = new Action<TimeoutOccurredEvent>(_ => { });

        publisher.Subscribe<PolicyExecutedSuccessfullyEvent>("PolicyExecutedSuccessfullyEvent", handler1);
        publisher.Subscribe<PolicyExecutedSuccessfullyEvent>("PolicyExecutedSuccessfullyEvent", handler2);
        publisher.Subscribe<TimeoutOccurredEvent>("TimeoutOccurredEvent", handler3);

        // Act
        var count = publisher.GetSubscriberCount<PolicyExecutedSuccessfullyEvent>();

        // Assert
        count.Should().Be(2);
    }

    /// <summary>
    /// Verifies that GetSubscriberCount<T> throws ArgumentNullException when publisher is null.
    /// </summary>
    [Fact]
    public void GetSubscriberCountGeneric_NullPublisher_ThrowsArgumentNullException()
    {
        // Arrange
        ResiliencyEventPublisher? publisher = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => publisher!.GetSubscriberCount<PolicyExecutedSuccessfullyEvent>());
    }

    /// <summary>
    /// Verifies that Reset clears event history.
    /// </summary>
    [Fact]
    public async Task Reset_ClearsEventHistory()
    {
        // Arrange
        var publisher = CreatePublisher();
        await publisher.PublishAsync(new PolicyExecutedSuccessfullyEvent());
        await publisher.PublishAsync(new PolicyExecutedSuccessfullyEvent());
        await publisher.PublishAsync(new PolicyExecutedSuccessfullyEvent());

        var historyBefore = publisher.GetEventHistory();
        historyBefore.Should().HaveCount(3);

        // Act
        publisher.Reset();

        // Assert
        var historyAfter = publisher.GetEventHistory();
        historyAfter.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that Reset with clearSubscribers=true clears both history and subscribers.
    /// </summary>
    [Fact]
    public async Task Reset_WithClearSubscribers_ClearsBothHistoryAndSubscribers()
    {
        // Arrange
        var publisher = CreatePublisher();
        var handler = new Action<PolicyExecutedSuccessfullyEvent>(_ => { });

        publisher.Subscribe<PolicyExecutedSuccessfullyEvent>("PolicyExecutedSuccessfullyEvent", handler);
        await publisher.PublishAsync(new PolicyExecutedSuccessfullyEvent());

        var countBefore = publisher.GetSubscriberCount<PolicyExecutedSuccessfullyEvent>();
        countBefore.Should().Be(1);
        var historyBefore = publisher.GetEventHistory();
        historyBefore.Should().HaveCount(1);

        // Act
        publisher.Reset(clearSubscribers: true);

        // Assert
        var countAfter = publisher.GetSubscriberCount<PolicyExecutedSuccessfullyEvent>();
        countAfter.Should().Be(0);
        var historyAfter = publisher.GetEventHistory();
        historyAfter.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that Reset with clearSubscribers=false keeps subscribers.
    /// </summary>
    [Fact]
    public async Task Reset_WithoutClearSubscribers_KeepsSubscribers()
    {
        // Arrange
        var publisher = CreatePublisher();
        var handler = new Action<PolicyExecutedSuccessfullyEvent>(_ => { });

        publisher.Subscribe<PolicyExecutedSuccessfullyEvent>("PolicyExecutedSuccessfullyEvent", handler);
        await publisher.PublishAsync(new PolicyExecutedSuccessfullyEvent());

        var countBefore = publisher.GetSubscriberCount<PolicyExecutedSuccessfullyEvent>();
        countBefore.Should().Be(1);

        // Act
        publisher.Reset(clearSubscribers: false);

        // Assert
        var countAfter = publisher.GetSubscriberCount<PolicyExecutedSuccessfullyEvent>();
        countAfter.Should().Be(1);
        var historyAfter = publisher.GetEventHistory();
        historyAfter.Should().BeEmpty();
    }

    /// <summary>
    /// Verifies that Reset throws ArgumentNullException when publisher is null.
    /// </summary>
    [Fact]
    public void Reset_NullPublisher_ThrowsArgumentNullException()
    {
        // Arrange
        ResiliencyEventPublisher? publisher = null;

        // Act & Assert
        Assert.Throws<ArgumentNullException>(
            () => publisher!.Reset());
    }
}