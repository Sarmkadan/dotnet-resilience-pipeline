#nullable enable

using DotNetResiliencePipeline.Events;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Unit tests for PipelineEventObserverExtensions to verify extension method behavior.
/// </summary>
public sealed class PipelineEventObserverExtensionsTests
{
    private readonly PipelineEventObserver _observer;

    public PipelineEventObserverExtensionsTests()
    {
        var publisher = new ResiliencyEventPublisher();
        _observer = new PipelineEventObserver(publisher);
    }

    [Fact]
    public void GetActiveHandlersCount_WithNullObserver_ThrowsArgumentNullException()
    {
        PipelineEventObserver? nullObserver = null;

        Action act = () => nullObserver!.GetActiveHandlersCount();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetActiveHandlersCount_WithNoHandlers_ReturnsZero()
    {
        var result = _observer.GetActiveHandlersCount();

        result.Should().Be(0);
    }

    [Fact]
    public void GetActiveHandlersCount_WithActiveAndInactiveHandlers_ReturnsOnlyActiveCount()
    {
        // Register some handlers
        _observer.RegisterHandler("handler1", (ResiliencyEvent e) => { }, "TestEvent");
        _observer.RegisterHandler("handler2", (ResiliencyEvent e) => { }, "TestEvent");

        // Deactivate one handler
        _observer.SetHandlerActive("handler2", false);

        var result = _observer.GetActiveHandlersCount();

        result.Should().Be(1);
    }

    [Fact]
    public void GetInactiveHandlersCount_WithNullObserver_ThrowsArgumentNullException()
    {
        PipelineEventObserver? nullObserver = null;

        Action act = () => nullObserver!.GetInactiveHandlersCount();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetInactiveHandlersCount_WithNoHandlers_ReturnsZero()
    {
        var result = _observer.GetInactiveHandlersCount();

        result.Should().Be(0);
    }

    [Fact]
    public void GetInactiveHandlersCount_WithActiveAndInactiveHandlers_ReturnsOnlyInactiveCount()
    {
        // Register some handlers
        _observer.RegisterHandler("handler1", (ResiliencyEvent e) => { }, "TestEvent");
        _observer.RegisterHandler("handler2", (ResiliencyEvent e) => { }, "TestEvent");
        _observer.RegisterHandler("handler3", (ResiliencyEvent e) => { }, "TestEvent");

        // Deactivate two handlers
        _observer.SetHandlerActive("handler2", false);
        _observer.SetHandlerActive("handler3", false);

        var result = _observer.GetInactiveHandlersCount();

        result.Should().Be(2);
    }

    [Fact]
    public void FindHandler_WithNullObserver_ThrowsArgumentNullException()
    {
        PipelineEventObserver? nullObserver = null;

        Action act = () => nullObserver!.FindHandler("handler1");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FindHandler_WithNullHandlerId_ThrowsArgumentNullException()
    {
        Action act = () => _observer.FindHandler(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void FindHandler_WithEmptyHandlerId_ThrowsArgumentException()
    {
        Action act = () => _observer.FindHandler("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FindHandler_WithWhitespaceHandlerId_ThrowsArgumentException()
    {
        Action act = () => _observer.FindHandler("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void FindHandler_WithNonExistentHandler_ReturnsNull()
    {
        var result = _observer.FindHandler("non-existent-handler");

        result.Should().BeNull();
    }

    [Fact]
    public void FindHandler_WithExistingHandler_ReturnsHandler()
    {
        // Register a handler
        _observer.RegisterHandler("test-handler", (ResiliencyEvent e) => { }, "TestEvent");

        var result = _observer.FindHandler("test-handler");

        result.Should().NotBeNull();
        result!.Id.Should().Be("test-handler");
        result.EventType.Should().Be("TestEvent");
    }

    [Fact]
    public void GetStatisticsFormatted_WithNullObserver_ThrowsArgumentNullException()
    {
        PipelineEventObserver? nullObserver = null;

        Action act = () => nullObserver!.GetStatisticsFormatted();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetStatisticsFormatted_WithNoEvents_ReturnsFormattedStatistics()
    {
        var result = _observer.GetStatisticsFormatted();

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Event Statistics");
        result.Should().Contain("Total Events: 0");
        result.Should().Contain("Successful Executions: 0");
        result.Should().Contain("Failed Executions: 0");
        result.Should().Contain("Circuit Breaker Changes: 0");
        result.Should().Contain("Bulkhead Rejections: 0");
        result.Should().Contain("Timeouts: 0");
        result.Should().Contain("Fallbacks Triggered: 0");
    }

    [Fact]
    public void HasActiveHandlers_WithNullObserver_ThrowsArgumentNullException()
    {
        PipelineEventObserver? nullObserver = null;

        Action act = () => nullObserver!.HasActiveHandlers();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void HasActiveHandlers_WithNoHandlers_ReturnsFalse()
    {
        var result = _observer.HasActiveHandlers();

        result.Should().BeFalse();
    }

    [Fact]
    public void HasActiveHandlers_WithActiveHandlers_ReturnsTrue()
    {
        // Register a handler
        _observer.RegisterHandler("test-handler", (ResiliencyEvent e) => { }, "TestEvent");

        var result = _observer.HasActiveHandlers();

        result.Should().BeTrue();
    }

    [Fact]
    public void HasActiveHandlers_WithOnlyInactiveHandlers_ReturnsFalse()
    {
        // Register and deactivate a handler
        _observer.RegisterHandler("test-handler", (ResiliencyEvent e) => { }, "TestEvent");
        _observer.SetHandlerActive("test-handler", false);

        var result = _observer.HasActiveHandlers();

        result.Should().BeFalse();
    }

    [Fact]
    public void GetHandlersByEventType_WithNullObserver_ThrowsArgumentNullException()
    {
        PipelineEventObserver? nullObserver = null;

        Action act = () => nullObserver!.GetHandlersByEventType("TestEvent");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetHandlersByEventType_WithNullEventType_ThrowsArgumentNullException()
    {
        Action act = () => _observer.GetHandlersByEventType(null!);
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetHandlersByEventType_WithEmptyEventType_ThrowsArgumentException()
    {
        Action act = () => _observer.GetHandlersByEventType("");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetHandlersByEventType_WithWhitespaceEventType_ThrowsArgumentException()
    {
        Action act = () => _observer.GetHandlersByEventType("   ");
        act.Should().Throw<ArgumentException>();
    }

    [Fact]
    public void GetHandlersByEventType_WithNoHandlersForEventType_ReturnsEmptyList()
    {
        var result = _observer.GetHandlersByEventType("NonExistentEventType");

        result.Should().BeEmpty();
    }

    [Fact]
    public void GetHandlersByEventType_WithHandlersForEventType_ReturnsMatchingHandlers()
    {
        // Register handlers for different event types
        _observer.RegisterHandler("handler1", (ResiliencyEvent e) => { }, "EventType1");
        _observer.RegisterHandler("handler2", (ResiliencyEvent e) => { }, "EventType2");
        _observer.RegisterHandler("handler3", (ResiliencyEvent e) => { }, "EventType1");

        var result = _observer.GetHandlersByEventType("EventType1");

        result.Should().HaveCount(2);
        result.Should().AllSatisfy(h => h.EventType.Should().Be("EventType1"));
        result.Select(h => h.Id).Should().BeEquivalentTo(new[] { "handler1", "handler3" });
    }

    [Fact]
    public void ToggleHandlerActive_WithNullObserver_ThrowsArgumentNullException()
    {
        PipelineEventObserver? nullObserver = null;

        Action act = () => nullObserver!.ToggleHandlerActive("handler1");
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void ToggleHandlerActive_WithNonExistentHandler_ReturnsFalse()
    {
        var result = _observer.ToggleHandlerActive("non-existent-handler");

        result.Should().BeFalse();
    }

    [Fact]
    public void ToggleHandlerActive_WithExistingHandler_TogglesStateAndReturnsTrue()
    {
        // Register a handler
        _observer.RegisterHandler("test-handler", (ResiliencyEvent e) => { }, "TestEvent");

        // Initially active
        var handlerBefore = _observer.FindHandler("test-handler");
        handlerBefore.Should().NotBeNull();
        handlerBefore!.IsActive.Should().BeTrue();

        // Toggle to inactive
        var result1 = _observer.ToggleHandlerActive("test-handler");
        result1.Should().BeTrue();

        var handlerAfterToggle1 = _observer.FindHandler("test-handler");
        handlerAfterToggle1.Should().NotBeNull();
        handlerAfterToggle1!.IsActive.Should().BeFalse();

        // Toggle back to active
        var result2 = _observer.ToggleHandlerActive("test-handler");
        result2.Should().BeTrue();

        var handlerAfterToggle2 = _observer.FindHandler("test-handler");
        handlerAfterToggle2.Should().NotBeNull();
        handlerAfterToggle2!.IsActive.Should().BeTrue();
    }

    [Fact]
    public void GetHandlersSummary_WithNullObserver_ThrowsArgumentNullException()
    {
        PipelineEventObserver? nullObserver = null;

        Action act = () => nullObserver!.GetHandlersSummary();
        act.Should().Throw<ArgumentNullException>();
    }

    [Fact]
    public void GetHandlersSummary_WithNoHandlers_ReturnsSummaryWithZeroCounts()
    {
        var result = _observer.GetHandlersSummary();

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Handler Summary");
        result.Should().Contain("Total Handlers: 0");
        result.Should().Contain("Active: 0");
        result.Should().Contain("Inactive: 0");
    }

    [Fact]
    public void GetHandlersSummary_WithMultipleHandlers_ReturnsDetailedSummary()
    {
        // Register multiple handlers with different states
        _observer.RegisterHandler("handler1", (ResiliencyEvent e) => { }, "EventType1");
        _observer.RegisterHandler("handler2", (ResiliencyEvent e) => { }, "EventType2");
        _observer.RegisterHandler("handler3", (ResiliencyEvent e) => { }, "EventType1");

        // Deactivate one handler
        _observer.SetHandlerActive("handler2", false);

        var result = _observer.GetHandlersSummary();

        result.Should().NotBeNullOrEmpty();
        result.Should().Contain("Handler Summary");
        result.Should().Contain("Total Handlers: 3");
        result.Should().Contain("Active: 2");
        result.Should().Contain("Inactive: 1");
        result.Should().Contain("handler1");
        result.Should().Contain("handler2");
        result.Should().Contain("handler3");
    }
}
