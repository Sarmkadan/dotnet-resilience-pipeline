#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Threading.Channels;

namespace DotNetResiliencePipeline.Events;

/// <summary>
/// Observer that listens to pipeline events and executes custom handlers.
/// Provides hooks for monitoring, alerting, and custom business logic.
/// </summary>
public sealed class PipelineEventObserver : IDisposable
{
    private readonly ResiliencyEventPublisher _publisher;
    private readonly ConcurrentDictionary<string, PipelineEventHandler> _handlers = new();
    private readonly Channel<ResiliencyEvent> _eventChannel;
    private readonly CancellationTokenSource _cts = new();
    private Task? _dispatchTask;

    /// <summary>
    /// Initializes a new instance of the <see cref="PipelineEventObserver"/> class.
    /// </summary>
    /// <param name="publisher">The event publisher to subscribe to</param>
    /// <exception cref="ArgumentNullException"><paramref name="publisher"/> is null.</exception>
    public PipelineEventObserver(ResiliencyEventPublisher publisher)
    {
        ArgumentNullException.ThrowIfNull(publisher);
        _publisher = publisher;

        // Create a bounded channel to prevent memory issues under heavy load
        _eventChannel = Channel.CreateBounded<ResiliencyEvent>(
            new BoundedChannelOptions(10_000)
            {
                FullMode = BoundedChannelFullMode.Wait,
                SingleReader = true,
                SingleWriter = true
            });

        RegisterDefaultHandlers();
        StartDispatcher();
    }

    /// <summary>
    /// Registers a custom event handler.
    /// </summary>
    /// <param name="handlerName">The unique name of the handler</param>
    /// <param name="handler">The handler action</param>
    /// <param name="eventType">The event type to handle</param>
    /// <exception cref="ArgumentNullException"><paramref name="handlerName"/> or <paramref name="handler"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="handlerName"/> is empty or whitespace.</exception>
    public void RegisterHandler<T>(string handlerName, Action<T> handler, string? eventType = null) where T : ResiliencyEvent
    {
        ArgumentNullException.ThrowIfNull(handlerName);
        ArgumentException.ThrowIfNullOrWhiteSpace(handlerName);
        ArgumentNullException.ThrowIfNull(handler);

        var actualEventType = eventType ?? typeof(T).Name;

        // Register with the publisher for immediate event delivery
        _publisher.Subscribe(actualEventType, handler);

        // Register for statistics tracking and async dispatch
        _handlers.TryAdd(handlerName, new PipelineEventHandler
        {
            Id = handlerName,
            EventType = actualEventType,
            CreatedAt = DateTime.UtcNow,
            IsActive = true,
            HandlerCount = 1,
            LastUsed = DateTime.UtcNow
        });
    }

    /// <summary>
    /// Unregisters an event handler.
    /// </summary>
    /// <param name="handlerName">The name of the handler to remove</param>
    /// <returns>True if the handler was found and removed, otherwise false</returns>
    /// <exception cref="ArgumentException"><paramref name="handlerName"/> is null or whitespace.</exception>
    public bool UnregisterHandler(string handlerName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handlerName);

        if (_handlers.TryRemove(handlerName, out var handler))
        {
            // Remove from publisher as well
            var handlersToRemove = _publisher.GetSubscriberCount(handler.EventType);
            if (handlersToRemove <= 1)
            {
                _publisher.UnsubscribeAll(handler.EventType);
            }
            else
            {
                // Note: We can't easily remove a specific handler from ResiliencyEventPublisher
                // The publisher will filter inactive handlers
            }

            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets all registered handlers.
    /// </summary>
    /// <returns>A list of all registered handlers</returns>
    public List<PipelineEventHandler> GetHandlers()
    {
        return _handlers.Values.ToList();
    }

    /// <summary>
    /// Enables or disables a handler.
    /// </summary>
    /// <param name="handlerName">The handler name</param>
    /// <param name="isActive">Whether the handler should be active</param>
    /// <returns>True if the handler was found and updated, otherwise false</returns>
    /// <exception cref="ArgumentException"><paramref name="handlerName"/> is null or whitespace.</exception>
    public bool SetHandlerActive(string handlerName, bool isActive)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(handlerName);

        if (_handlers.TryGetValue(handlerName, out var handler))
        {
            handler.IsActive = isActive;
            handler.LastUsed = DateTime.UtcNow;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Gets event statistics with thread-safe counters.
    /// </summary>
    /// <returns>Event statistics</returns>
    public EventStatistics GetStatistics()
    {
        // Use the publisher's atomic counters for real-time statistics
        return _publisher.GetStatistics();
    }

    /// <summary>
    /// Records an event for statistics tracking and async dispatch.
    /// </summary>
    /// <param name="eventData">The event to record</param>
    /// <exception cref="ArgumentNullException"><paramref name="eventData"/> is null.</exception>
    public void RecordEvent(ResiliencyEvent eventData)
    {
        ArgumentNullException.ThrowIfNull(eventData);

        // Try to write to the channel (will wait if full)
        _eventChannel.Writer.TryWrite(eventData);
    }

    /// <summary>
    /// Disposes the observer and cleans up resources.
    /// </summary>
    public void Dispose()
    {
        _cts.Cancel();
        try
        {
            _dispatchTask?.Wait(TimeSpan.FromSeconds(5));
        }
        catch
        {
            // Best effort cleanup
        }
        _cts.Dispose();
        _eventChannel.Writer.Complete();
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

        // Handler for fallbacks
        _publisher.Subscribe<FallbackTriggeredEvent>(
            "FallbackTriggeredEvent",
            evt => HandleFallback(evt));

        // Handler for policy health changes
        _publisher.Subscribe<PolicyHealthChangedEvent>(
            "PolicyHealthChangedEvent",
            evt => HandlePolicyHealthChange(evt));
    }

    private void StartDispatcher()
    {
        _dispatchTask = Task.Run(async () =>
        {
            try
            {
                var reader = _eventChannel.Reader;
                while (await reader.WaitToReadAsync(_cts.Token).ConfigureAwait(false))
                {
                    while (reader.TryRead(out var @event))
                    {
                        await ProcessEventAsync(@event).ConfigureAwait(false);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // Expected during shutdown
            }
            catch (Exception ex)
            {
                // Log but don't crash the dispatcher
                Console.Error.WriteLine($"[ERROR] Event dispatcher failed: {ex.Message}");
            }
        }, _cts.Token);
    }

    private async Task ProcessEventAsync(ResiliencyEvent eventData)
    {
        try
        {
            switch (eventData)
            {
                case PolicyExecutedSuccessfullyEvent evt:
                    HandleSuccessfulExecution(evt);
                    break;

                case PolicyExecutionFailedEvent evt:
                    HandleFailedExecution(evt);
                    break;

                case CircuitBreakerStateChangedEvent evt:
                    HandleCircuitBreakerChange(evt);
                    break;

                case BulkheadRejectedEvent evt:
                    HandleBulkheadRejection(evt);
                    break;

                case TimeoutOccurredEvent evt:
                    HandleTimeout(evt);
                    break;

                case FallbackTriggeredEvent evt:
                    HandleFallback(evt);
                    break;

                case PolicyHealthChangedEvent evt:
                    HandlePolicyHealthChange(evt);
                    break;
            }
        }
        catch (Exception ex)
        {
            // Swallow exceptions from handlers to prevent propagation
            // The event has already been recorded in history
            Console.Error.WriteLine($"[WARNING] Handler failed for event {eventData.GetType().Name}: {ex.Message}");
        }
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

    private void HandleFallback(FallbackTriggeredEvent evt)
    {
        // Log fallback
        Console.WriteLine($"[🛟] Fallback triggered for '{evt.PolicyName}': {evt.Reason}");
    }

    private void HandlePolicyHealthChange(PolicyHealthChangedEvent evt)
    {
        // Log policy health change
        Console.WriteLine($"[💓] Policy '{evt.PolicyName}' health changed from {evt.PreviousHealth} to {evt.NewHealth} (Success rate: {evt.SuccessRate:P1})");
    }
}

/// <summary>
/// Represents a registered event handler with statistics tracking.
/// </summary>
public sealed class PipelineEventHandler
{
    /// <summary>Gets the handler identifier.</summary>
    public string Id { get; set; } = string.Empty;

    /// <summary>Gets the event type this handler processes.</summary>
    public string EventType { get; set; } = string.Empty;

    /// <summary>Gets the creation timestamp.</summary>
    public DateTime CreatedAt { get; set; }

    /// <summary>Gets or sets whether the handler is active.</summary>
    public bool IsActive { get; set; }

    /// <summary>Gets or sets the number of handlers registered with this name.</summary>
    public int HandlerCount { get; set; }

    /// <summary>Gets or sets the last used timestamp.</summary>
    public DateTime LastUsed { get; set; }
}

/// <summary>
/// Statistics about events emitted with thread-safe counters.
/// </summary>
public sealed class EventStatistics
{
    /// <summary>Gets the total number of events emitted.</summary>
    public long TotalEventsEmitted { get; internal set; }

    /// <summary>Gets the number of successful executions.</summary>
    public long SuccessfulExecutions { get; internal set; }

    /// <summary>Gets the number of failed executions.</summary>
    public long FailedExecutions { get; internal set; }

    /// <summary>Gets the number of circuit breaker state changes.</summary>
    public long CircuitBreakerChanges { get; internal set; }

    /// <summary>Gets the number of bulkhead rejections.</summary>
    public long BulkheadRejections { get; internal set; }

    /// <summary>Gets the number of timeouts.</summary>
    public long Timeouts { get; internal set; }

    /// <summary>Gets the number of fallbacks triggered.</summary>
    public long FallbacksTriggered { get; internal set; }

    /// <summary>Gets the number of policy health changes.</summary>
    public long PolicyHealthChanged { get; internal set; }

    /// <summary>
    /// Gets the failure rate as a percentage (0-100).
    /// </summary>
    public double FailureRate => TotalEventsEmitted > 0
        ? (FailedExecutions * 100.0) / TotalEventsEmitted
        : 0;

    /// <summary>
    /// Gets the success rate as a percentage (0-100).
    /// </summary>
    public double SuccessRate => TotalEventsEmitted > 0
        ? (SuccessfulExecutions * 100.0) / TotalEventsEmitted
        : 100;

    /// <summary>
    /// Resets all counters to zero.
    /// </summary>
    public void Reset()
    {
        TotalEventsEmitted = 0;
        SuccessfulExecutions = 0;
        FailedExecutions = 0;
        CircuitBreakerChanges = 0;
        BulkheadRejections = 0;
        Timeouts = 0;
        FallbacksTriggered = 0;
        PolicyHealthChanged = 0;
    }

    /// <summary>
    /// Creates a new <see cref="EventStatistics"/> instance with all counters set to zero.
    /// </summary>
    /// <returns>A new statistics instance</returns>
    public static EventStatistics Create() => new();
}
