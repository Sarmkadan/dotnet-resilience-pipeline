#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;
using System.Threading;

namespace DotNetResiliencePipeline.Events;

/// <summary>
/// Publisher for resilience pipeline events using pub-sub pattern.
/// Enables decoupled event notification across the system.
/// </summary>
public sealed class ResiliencyEventPublisher
{
    private readonly ConcurrentDictionary<string, List<Delegate>> _subscribers = new();
    private readonly List<ResiliencyEvent> _eventHistory = new();
    private readonly object _lockObj = new object();
    private long _totalEventsEmitted;
    private long _successfulExecutions;
    private long _failedExecutions;
    private long _circuitBreakerChanges;
    private long _bulkheadRejections;
    private long _timeouts;
    private long _fallbacksTriggered;
    private long _policyHealthChanged;
    public int MaxHistorySize { get; set; } = 1000;

    /// <summary>
    /// Subscribes to an event type.
    /// </summary>
    public void Subscribe<T>(string eventType, Action<T> handler) where T : ResiliencyEvent
    {
        var subscribers = _subscribers.GetOrAdd(eventType, _ => new List<Delegate>());
        lock (subscribers)
        {
            subscribers.Add(handler);
        }
    }

    /// <summary>
    /// Unsubscribes from an event type.
    /// </summary>
    public bool Unsubscribe<T>(string eventType, Action<T> handler) where T : ResiliencyEvent
    {
        if (_subscribers.TryGetValue(eventType, out var subscribers))
        {
            lock (subscribers)
            {
                return subscribers.Remove(handler);
            }
        }

        return false;
    }

    /// <summary>
    /// Unsubscribes all handlers for a specific event type.
    /// </summary>
    /// <param name="eventType">The event type to clear</param>
    /// <returns>True if subscribers were found and removed, otherwise false</returns>
    public bool UnsubscribeAll(string eventType)
    {
        if (_subscribers.TryGetValue(eventType, out var subscribers))
        {
            lock (subscribers)
            {
                subscribers.Clear();
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Publishes an event to all subscribers.
    /// </summary>
    /// <param name="eventData">The event to publish</param>
    /// <exception cref="ArgumentNullException"><paramref name="eventData"/> is null.</exception>
    public async Task PublishAsync<T>(T eventData) where T : ResiliencyEvent
    {
        ArgumentNullException.ThrowIfNull(eventData);

        eventData.Timestamp = DateTime.UtcNow;

        // Increment thread-safe counters
        Interlocked.Increment(ref _totalEventsEmitted);

        switch (eventData)
        {
            case PolicyExecutedSuccessfullyEvent _:
                Interlocked.Increment(ref _successfulExecutions);
                break;
            case PolicyExecutionFailedEvent _:
                Interlocked.Increment(ref _failedExecutions);
                break;
            case CircuitBreakerStateChangedEvent _:
                Interlocked.Increment(ref _circuitBreakerChanges);
                break;
            case BulkheadRejectedEvent _:
                Interlocked.Increment(ref _bulkheadRejections);
                break;
            case TimeoutOccurredEvent _:
                Interlocked.Increment(ref _timeouts);
                break;
            case FallbackTriggeredEvent _:
                Interlocked.Increment(ref _fallbacksTriggered);
                break;
            case PolicyHealthChangedEvent _:
                Interlocked.Increment(ref _policyHealthChanged);
                break;
        }

        // Record in history
        lock (_lockObj)
        {
            _eventHistory.Add(eventData);
            if (_eventHistory.Count > MaxHistorySize)
                _eventHistory.RemoveAt(0);
        }

        var eventType = eventData.GetType().Name;

        if (_subscribers.TryGetValue(eventType, out var subscribers))
        {
            var tasks = new List<Task>();

            lock (subscribers)
            {
                foreach (var handler in subscribers.OfType<Delegate>())
                {
                    try
                    {
                        if (handler is Action<T> typedHandler)
                            typedHandler(eventData);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error in event handler: {ex.Message}");
                    }
                }
            }

            if (tasks.Count > 0)
                await Task.WhenAll(tasks);
        }
    }

    /// <summary>
    /// Gets event history.
    /// </summary>
    public List<ResiliencyEvent> GetEventHistory(int limit = 100)
    {
        lock (_lockObj)
        {
            return _eventHistory.TakeLast(limit).ToList();
        }
    }

    /// <summary>
    /// Gets events of a specific type.
    /// </summary>
    public List<T> GetEvents<T>(int limit = 100) where T : ResiliencyEvent
    {
        lock (_lockObj)
        {
            return _eventHistory.OfType<T>().TakeLast(limit).ToList();
        }
    }

    /// <summary>
    /// Gets subscriber count for an event type.
    /// </summary>
    public int GetSubscriberCount(string eventType)
    {
        if (!_subscribers.TryGetValue(eventType, out var subscribers))
            return 0;

        lock (subscribers)
        {
            return subscribers.Count;
        }
    }

    /// <summary>
    /// Clears event history.
    /// </summary>
    public void ClearHistory()
    {
        lock (_lockObj)
        {
            _eventHistory.Clear();
        }
    }

    /// <summary>
    /// Removes all subscribers for all event types.
    /// </summary>
    public void ClearSubscribers()
    {
        foreach (var eventType in _subscribers.Keys.ToList())
        {
            if (_subscribers.TryGetValue(eventType, out var subscribers))
            {
                lock (subscribers)
                {
                    subscribers.Clear();
                }
            }
        }
    }

    /// <summary>
    /// Gets thread-safe event statistics.
    /// </summary>
    /// <returns>Event statistics with atomic counters</returns>
    public EventStatistics GetStatistics()
    {
        return new EventStatistics
        {
            TotalEventsEmitted = Volatile.Read(ref _totalEventsEmitted),
            SuccessfulExecutions = Volatile.Read(ref _successfulExecutions),
            FailedExecutions = Volatile.Read(ref _failedExecutions),
            CircuitBreakerChanges = Volatile.Read(ref _circuitBreakerChanges),
            BulkheadRejections = Volatile.Read(ref _bulkheadRejections),
            Timeouts = Volatile.Read(ref _timeouts),
            FallbacksTriggered = Volatile.Read(ref _fallbacksTriggered),
            PolicyHealthChanged = Volatile.Read(ref _policyHealthChanged)
        };
    }

    /// <summary>
    /// Resets all statistics counters to zero.
    /// </summary>
    public void ResetStatistics()
    {
        Interlocked.Exchange(ref _totalEventsEmitted, 0);
        Interlocked.Exchange(ref _successfulExecutions, 0);
        Interlocked.Exchange(ref _failedExecutions, 0);
        Interlocked.Exchange(ref _circuitBreakerChanges, 0);
        Interlocked.Exchange(ref _bulkheadRejections, 0);
        Interlocked.Exchange(ref _timeouts, 0);
        Interlocked.Exchange(ref _fallbacksTriggered, 0);
        Interlocked.Exchange(ref _policyHealthChanged, 0);
    }
}

/// <summary>
/// Base class for all resilience events.
/// </summary>
public abstract class ResiliencyEvent
{
    public string Id { get; set; } = Guid.NewGuid().ToString();
    public DateTime Timestamp { get; set; } = DateTime.UtcNow;
    public string SourcePolicy { get; set; } = string.Empty;
}

/// <summary>
/// Event raised when a policy executes successfully.
/// </summary>
public sealed class PolicyExecutedSuccessfullyEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public int AttemptNumber { get; set; }
}

/// <summary>
/// Event raised when a policy execution fails.
/// </summary>
public sealed class PolicyExecutionFailedEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public long DurationMs { get; set; }
}

/// <summary>
/// Event raised when a circuit breaker state changes.
/// </summary>
public sealed class CircuitBreakerStateChangedEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public string PreviousState { get; set; } = string.Empty;
    public string NewState { get; set; } = string.Empty;
    public int ConsecutiveFailures { get; set; }
}

/// <summary>
/// Event raised when bulkhead capacity is exceeded.
/// </summary>
public sealed class BulkheadRejectedEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public int ActiveExecutions { get; set; }
    public int MaxCapacity { get; set; }
    public int QueuedRequests { get; set; }
}

/// <summary>
/// Event raised when timeout occurs.
/// </summary>
public sealed class TimeoutOccurredEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public long TimeoutMs { get; set; }
    public long ActualDurationMs { get; set; }
}

/// <summary>
/// Event raised when fallback is triggered.
/// </summary>
public sealed class FallbackTriggeredEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool FallbackSucceeded { get; set; }
}

/// <summary>
/// Event raised when policy health changes.
/// </summary>
public sealed class PolicyHealthChangedEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public string PreviousHealth { get; set; } = string.Empty;
    public string NewHealth { get; set; } = string.Empty;
    public double SuccessRate { get; set; }
}
