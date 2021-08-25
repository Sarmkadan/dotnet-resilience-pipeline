#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotNetResiliencePipeline.Events;

/// <summary>
/// Observer that listens to pipeline events and executes custom handlers.
/// Provides hooks for monitoring, alerting, and custom business logic.
/// </summary>
public class PipelineEventObserver
{
    private readonly ResiliencyEventPublisher _publisher;
    private readonly ConcurrentDictionary<string, EventHandler> _handlers = new();

    public PipelineEventObserver(ResiliencyEventPublisher publisher)
    {
        _publisher = publisher;
        RegisterDefaultHandlers();
    }

    /// <summary>
    /// Registers a custom event handler.
    /// </summary>
    public void RegisterHandler<T>(string handlerName, Action<T> handler) where T : ResiliencyEvent
    {
        var eventType = typeof(T).Name;
        _publisher.Subscribe(eventType, handler);

        _handlers.TryAdd(handlerName, new EventHandler
        {
            Id = handlerName,
            EventType = eventType,
            CreatedAt = DateTime.UtcNow,
            IsActive = true
        });
    }

    /// <summary>
    /// Unregisters an event handler.
    /// </summary>
    public bool UnregisterHandler(string handlerName)
    {
        return _handlers.TryRemove(handlerName, out _);
    }

    /// <summary>
    /// Gets all registered handlers.
    /// </summary>
    public List<EventHandler> GetHandlers()
    {
        return _handlers.Values.ToList();
    }

    /// <summary>
    /// Enables or disables a handler.
    /// </summary>
    public bool SetHandlerActive(string handlerName, bool isActive)
    {
        if (_handlers.TryGetValue(handlerName, out var handler))
        {
            handler.IsActive = isActive;
            return true;
        }

        return false;
    }

    private void RegisterDefaultHandlers()
    {
        // Handler for successful executions
        _publisher.Subscribe<PolicyExecutedSuccessfullyEvent>(
            "PolicyExecutedSuccessfullyEvent",
            evt => HandleSuccessfulExecution(evt));

        // Handler for failed executions
        _publisher.Subscribe<PolicyExecutionFailedEvent>(
            "PolicyExecutionFailedEvent",
            evt => HandleFailedExecution(evt));

        // Handler for circuit breaker changes
        _publisher.Subscribe<CircuitBreakerStateChangedEvent>(
            "CircuitBreakerStateChangedEvent",
            evt => HandleCircuitBreakerChange(evt));

        // Handler for bulkhead rejections
        _publisher.Subscribe<BulkheadRejectedEvent>(
            "BulkheadRejectedEvent",
            evt => HandleBulkheadRejection(evt));

        // Handler for timeouts
        _publisher.Subscribe<TimeoutOccurredEvent>(
            "TimeoutOccurredEvent",
            evt => HandleTimeout(evt));
    }

    private void HandleSuccessfulExecution(PolicyExecutedSuccessfullyEvent evt)
    {
        // Log successful execution
        Console.WriteLine($"[✓] Policy '{evt.PolicyName}' executed successfully in {evt.DurationMs}ms");
    }

    private void HandleFailedExecution(PolicyExecutionFailedEvent evt)
    {
        // Log failed execution
        Console.WriteLine($"[✗] Policy '{evt.PolicyName}' failed: {evt.ExceptionType} - {evt.ExceptionMessage}");
    }

    private void HandleCircuitBreakerChange(CircuitBreakerStateChangedEvent evt)
    {
        // Log circuit breaker state change
        Console.WriteLine($"[⚡] Circuit breaker '{evt.PolicyName}' changed from {evt.PreviousState} to {evt.NewState}");
    }

    private void HandleBulkheadRejection(BulkheadRejectedEvent evt)
    {
        // Log bulkhead rejection
        Console.WriteLine($"[🚫] Bulkhead '{evt.PolicyName}' rejected request (Active: {evt.ActiveExecutions}/{evt.MaxCapacity})");
    }

    private void HandleTimeout(TimeoutOccurredEvent evt)
    {
        // Log timeout
        Console.WriteLine($"[⏱] Timeout in '{evt.PolicyName}': {evt.ActualDurationMs}ms exceeded {evt.TimeoutMs}ms");
    }

    /// <summary>
    /// Gets event statistics.
    /// </summary>
    public EventStatistics GetStatistics()
    {
        var history = _publisher.GetEventHistory(10000);

        return new EventStatistics
        {
            TotalEventsEmitted = history.Count,
            SuccessfulExecutions = history.OfType<PolicyExecutedSuccessfullyEvent>().Count(),
            FailedExecutions = history.OfType<PolicyExecutionFailedEvent>().Count(),
            CircuitBreakerChanges = history.OfType<CircuitBreakerStateChangedEvent>().Count(),
            BulkheadRejections = history.OfType<BulkheadRejectedEvent>().Count(),
            Timeouts = history.OfType<TimeoutOccurredEvent>().Count(),
            FallbacksTriggered = history.OfType<FallbackTriggeredEvent>().Count()
        };
    }
}

/// <summary>
/// Represents a registered event handler.
/// </summary>
public class EventHandler
{
    public string Id { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public bool IsActive { get; set; }
}

/// <summary>
/// Statistics about events emitted.
/// </summary>
public class EventStatistics
{
    public int TotalEventsEmitted { get; set; }
    public int SuccessfulExecutions { get; set; }
    public int FailedExecutions { get; set; }
    public int CircuitBreakerChanges { get; set; }
    public int BulkheadRejections { get; set; }
    public int Timeouts { get; set; }
    public int FallbacksTriggered { get; set; }

    public double FailureRate => TotalEventsEmitted > 0
        ? (FailedExecutions * 100.0) / TotalEventsEmitted
        : 0;
}
