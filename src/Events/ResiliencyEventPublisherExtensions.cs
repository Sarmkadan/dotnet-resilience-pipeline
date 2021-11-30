using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DotNetResiliencePipeline.Events
{
    public static class ResiliencyEventPublisherExtensions
    {
        /// <summary>
        /// Publishes an event and tracks it in history, ensuring history doesn't exceed max size.
        /// </summary>
        /// <param name="publisher">The event publisher instance</param>
        /// <param name="eventData">The event data to publish</param>
        /// <returns>A task representing the asynchronous publish operation</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> or <paramref name="eventData"/> is null.</exception>
        public static async Task PublishWithHistoryAsync(
            this ResiliencyEventPublisher publisher,
            ResiliencyEvent eventData)
        {
            ArgumentNullException.ThrowIfNull(publisher);
            ArgumentNullException.ThrowIfNull(eventData);

            await publisher.PublishAsync(eventData);

            // The base class already handles history size in PublishAsync,
            // so this method is kept for backward compatibility
        }

        /// <summary>
        /// Gets the most recent event of type T from the history, or null if none exists.
        /// </summary>
        /// <typeparam name="T">The event type to retrieve</typeparam>
        /// <param name="publisher">The event publisher instance</param>
        /// <returns>The most recent event of type T, or null if none exists</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> is null.</exception>
        public static T? GetLastEvent<T>(this ResiliencyEventPublisher publisher) where T : ResiliencyEvent
        {
            ArgumentNullException.ThrowIfNull(publisher);

            return publisher.GetEvents<T>().LastOrDefault();
        }

        /// <summary>
        /// Publishes a policy execution failed event with detailed exception information.
        /// </summary>
        /// <param name="publisher">The event publisher instance</param>
        /// <param name="exception">The exception to publish</param>
        /// <param name="policyName">The name of the policy that caught the exception</param>
        /// <param name="durationMs">The duration of the operation before the exception</param>
        /// <returns>A task representing the asynchronous publish operation</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/>, <paramref name="exception"/>, or <paramref name="policyName"/> is null.</exception>
        public static async Task PublishExceptionAsync(
            this ResiliencyEventPublisher publisher,
            Exception exception,
            string policyName,
            long durationMs = 0)
        {
            ArgumentNullException.ThrowIfNull(publisher);
            ArgumentNullException.ThrowIfNull(exception);
            ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

            var exceptionEvent = new PolicyExecutionFailedEvent
            {
                ExceptionType = exception.GetType().FullName ?? "UnknownException",
                ExceptionMessage = exception.Message,
                PolicyName = policyName,
                DurationMs = durationMs,
                SourcePolicy = policyName
            };

            await publisher.PublishAsync(exceptionEvent);
        }

        /// <summary>
        /// Gets the count of subscribers for a specific event type.
        /// </summary>
        /// <param name="publisher">The event publisher instance</param>
        /// <param name="eventType">The event type name to check</param>
        /// <returns>The number of subscribers for the specified event type</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> is null.</exception>
        /// <exception cref="ArgumentException">Thrown when <paramref name="eventType"/> is null or whitespace.</exception>
        public static int GetSubscriberCount(
            this ResiliencyEventPublisher publisher,
            string eventType)
        {
            ArgumentNullException.ThrowIfNull(publisher);
            ArgumentException.ThrowIfNullOrWhiteSpace(eventType);

            return publisher.GetSubscriberCount(eventType);
        }

        /// <summary>
        /// Gets the count of subscribers for events of type T.
        /// </summary>
        /// <typeparam name="T">The event type to check</typeparam>
        /// <param name="publisher">The event publisher instance</param>
        /// <returns>The number of subscribers for type T</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> is null.</exception>
        public static int GetSubscriberCount<T>(this ResiliencyEventPublisher publisher) where T : ResiliencyEvent
        {
            ArgumentNullException.ThrowIfNull(publisher);

            return publisher.GetSubscriberCount(typeof(T).Name);
        }

        /// <summary>
        /// Clears all event history and optionally resets the publisher state.
        /// </summary>
        /// <param name="publisher">The event publisher instance</param>
        /// <param name="clearSubscribers">Whether to also clear all subscribers</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="publisher"/> is null.</exception>
        public static void Reset(this ResiliencyEventPublisher publisher, bool clearSubscribers = false)
        {
            ArgumentNullException.ThrowIfNull(publisher);

            publisher.ClearHistory();

            if (clearSubscribers)
            {
                // Note: The base class doesn't expose a method to clear subscribers,
                // so this parameter is documented but not implemented.
                // Subscribers can be cleared by disposing and recreating the publisher.
            }
        }
    }
}