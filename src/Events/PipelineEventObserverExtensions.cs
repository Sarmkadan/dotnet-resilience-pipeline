#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Text;

namespace DotNetResiliencePipeline.Events;

/// <summary>
/// Extension methods for PipelineEventObserver providing additional functionality
/// for monitoring, filtering, and managing event handlers.
/// </summary>
public static class PipelineEventObserverExtensions
{
    /// <summary>
    /// Gets the number of active handlers currently registered.
    /// </summary>
    /// <param name="observer">The observer instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
    /// <returns>Count of active handlers</returns>
    public static int GetActiveHandlersCount(this PipelineEventObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return observer.GetHandlers().Count(h => h.IsActive);
    }

    /// <summary>
    /// Gets the number of inactive handlers currently registered.
    /// </summary>
    /// <param name="observer">The observer instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
    /// <returns>Count of inactive handlers</returns>
    public static int GetInactiveHandlersCount(this PipelineEventObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return observer.GetHandlers().Count(h => !h.IsActive);
    }

    /// <summary>
    /// Finds a handler by its ID.
    /// </summary>
    /// <param name="observer">The observer instance</param>
    /// <param name="handlerId">The handler ID to find</param>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="handlerId"/> is null or whitespace.</exception>
    /// <returns>The handler if found, otherwise null</returns>
    public static PipelineEventHandler? FindHandler(this PipelineEventObserver observer, string handlerId)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentException.ThrowIfNullOrWhiteSpace(handlerId);

        return observer.GetHandlers().FirstOrDefault(h => h.Id.Equals(handlerId, StringComparison.Ordinal));
    }

    /// <summary>
    /// Gets statistics formatted as a human-readable string.
    /// </summary>
    /// <param name="observer">The observer instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
    /// <returns>Formatted statistics string</returns>
    public static string GetStatisticsFormatted(this PipelineEventObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        var stats = observer.GetStatistics();
        var sb = new StringBuilder();

        sb.AppendLine("Event Statistics");
        sb.AppendLine("================");
        sb.AppendLine($"Total Events: {stats.TotalEventsEmitted}");
        sb.AppendLine($"Successful Executions: {stats.SuccessfulExecutions} ({stats.SuccessfulExecutions * 100.0 / Math.Max(1, stats.TotalEventsEmitted):F2}%)");
        sb.AppendLine($"Failed Executions: {stats.FailedExecutions} ({stats.FailureRate:F2}% failure rate)");
        sb.AppendLine($"Circuit Breaker Changes: {stats.CircuitBreakerChanges}");
        sb.AppendLine($"Bulkhead Rejections: {stats.BulkheadRejections}");
        sb.AppendLine($"Timeouts: {stats.Timeouts}");
        sb.AppendLine($"Fallbacks Triggered: {stats.FallbacksTriggered}");

        return sb.ToString();
    }

    /// <summary>
    /// Checks if any handlers are currently active.
    /// </summary>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
    /// <returns>True if at least one handler is active, otherwise false</returns>
    public static bool HasActiveHandlers(this PipelineEventObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        return observer.GetHandlers().Any(h => h.IsActive);
    }

    /// <summary>
    /// Gets handlers filtered by event type.
    /// </summary>
    /// <param name="observer">The observer instance</param>
    /// <param name="eventType">The event type to filter by</param>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
    /// <exception cref="ArgumentException"><paramref name="eventType"/> is null or whitespace.</exception>
    /// <returns>List of handlers matching the event type</returns>
    public static List<PipelineEventHandler> GetHandlersByEventType(this PipelineEventObserver observer, string eventType)
    {
        ArgumentNullException.ThrowIfNull(observer);
        ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

        return observer.GetHandlers()
            .Where(h => h.EventType.Equals(eventType, StringComparison.Ordinal))
            .ToList();
    }

    /// <summary>
    /// Toggles the active state of a handler.
    /// </summary>
    /// <param name="observer">The observer instance</param>
    /// <param name="handlerId">The handler ID to toggle</param>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
    /// <returns>True if the handler was found and state was toggled, otherwise false</returns>
    public static bool ToggleHandlerActive(this PipelineEventObserver observer, string handlerId)
    {
        ArgumentNullException.ThrowIfNull(observer);

        var handler = observer.FindHandler(handlerId);
        if (handler != null)
        {
            return observer.SetHandlerActive(handlerId, !handler.IsActive);
        }

        return false;
    }

    /// <summary>
    /// Gets a summary of all handlers with their status.
    /// </summary>
    /// <param name="observer">The observer instance</param>
    /// <exception cref="ArgumentNullException"><paramref name="observer"/> is null.</exception>
    /// <returns>Formatted string with handler summary</returns>
    public static string GetHandlersSummary(this PipelineEventObserver observer)
    {
        ArgumentNullException.ThrowIfNull(observer);

        var handlers = observer.GetHandlers();
        var activeCount = handlers.Count(h => h.IsActive);
        var inactiveCount = handlers.Count - activeCount;

        var sb = new StringBuilder();
        sb.AppendLine("Handler Summary");
        sb.AppendLine("==============");
        sb.AppendLine($"Total Handlers: {handlers.Count}");
        sb.AppendLine($"Active: {activeCount}");
        sb.AppendLine($"Inactive: {inactiveCount}");
        sb.AppendLine();

        if (handlers.Count > 0)
        {
            sb.AppendLine("Handlers:");
            foreach (var handler in handlers.OrderBy(h => h.CreatedAt))
            {
                sb.AppendLine($" [{(handler.IsActive ? "✓" : "✗")}] {handler.Id} - {handler.EventType} (Created: {handler.CreatedAt:yyyy-MM-dd})");
            }
        }

        return sb.ToString();
    }
}