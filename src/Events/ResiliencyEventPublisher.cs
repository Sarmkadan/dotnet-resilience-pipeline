#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Concurrent;

namespace DotNetResiliencePipeline.Events;

/// <summary>
/// Publisher for resilience pipeline events using pub-sub pattern.
/// Enables decoupled event notification across the system.
/// </summary>
public class ResiliencyEventPublisher
{
    private readonly ConcurrentDictionary<string, List<Delegate>> _subscribers = new();
    private readonly List<ResiliencyEvent> _eventHistory = new();
    private readonly object _lockObj = new object();
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
    /// Publishes an event to all subscribers.
    /// </summary>
    public async Task PublishAsync<T>(T eventData) where T : ResiliencyEvent
    {
        eventData.Timestamp = DateTime.UtcNow;

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
        return _subscribers.TryGetValue(eventType, out var subscribers) ? subscribers.Count : 0;
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
public class PolicyExecutedSuccessfullyEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public long DurationMs { get; set; }
    public int AttemptNumber { get; set; }
}

/// <summary>
/// Event raised when a policy execution fails.
/// </summary>
public class PolicyExecutionFailedEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public string ExceptionType { get; set; } = string.Empty;
    public string ExceptionMessage { get; set; } = string.Empty;
    public long DurationMs { get; set; }
}

/// <summary>
/// Event raised when a circuit breaker state changes.
/// </summary>
public class CircuitBreakerStateChangedEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public string PreviousState { get; set; } = string.Empty;
    public string NewState { get; set; } = string.Empty;
    public int ConsecutiveFailures { get; set; }
}

/// <summary>
/// Event raised when bulkhead capacity is exceeded.
/// </summary>
public class BulkheadRejectedEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public int ActiveExecutions { get; set; }
    public int MaxCapacity { get; set; }
    public int QueuedRequests { get; set; }
}

/// <summary>
/// Event raised when timeout occurs.
/// </summary>
public class TimeoutOccurredEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public long TimeoutMs { get; set; }
    public long ActualDurationMs { get; set; }
}

/// <summary>
/// Event raised when fallback is triggered.
/// </summary>
public class FallbackTriggeredEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public string Reason { get; set; } = string.Empty;
    public bool FallbackSucceeded { get; set; }
}

/// <summary>
/// Event raised when policy health changes.
/// </summary>
public class PolicyHealthChangedEvent : ResiliencyEvent
{
    public string PolicyName { get; set; } = string.Empty;
    public string PreviousHealth { get; set; } = string.Empty;
    public string NewHealth { get; set; } = string.Empty;
    public double SuccessRate { get; set; }
}
