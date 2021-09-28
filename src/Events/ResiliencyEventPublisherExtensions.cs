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
        public static async Task PublishWithHistoryAsync(
            this ResiliencyEventPublisher publisher,
            ResiliencyEvent eventData)
        {
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

            if (eventData == null)
            {
                throw new ArgumentNullException(nameof(eventData));
            }

            await publisher.PublishAsync(eventData);

            // Ensure history doesn't exceed max size
            if (publisher.GetEventHistory().Count >= publisher.MaxHistorySize)
            {
                publisher.ClearHistory();
            }
        }

        /// <summary>
        /// Gets the most recent event of type T from the history, or null if none exists.
        /// </summary>
        /// <typeparam name="T">The event type to retrieve</typeparam>
        /// <param name="publisher">The event publisher instance</param>
        /// <returns>The most recent event of type T, or null if none exists</returns>
        public static T? GetLastEvent<T>(this ResiliencyEventPublisher publisher) where T : ResiliencyEvent
        {
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

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
        public static async Task PublishExceptionAsync(
            this ResiliencyEventPublisher publisher,
            Exception exception,
            string policyName,
            long durationMs = 0)
        {
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

            if (exception == null)
            {
                throw new ArgumentNullException(nameof(exception));
            }

            if (string.IsNullOrWhiteSpace(policyName))
            {
                throw new ArgumentException("Policy name cannot be null or whitespace.", nameof(policyName));
            }

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
        public static int GetSubscriberCount(
            this ResiliencyEventPublisher publisher,
            string eventType)
        {
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

            if (string.IsNullOrWhiteSpace(eventType))
            {
                throw new ArgumentException("Event type cannot be null or whitespace.", nameof(eventType));
            }

            return publisher.GetSubscriberCount(eventType);
        }

        /// <summary>
        /// Gets the count of subscribers for events of type T.
        /// </summary>
        /// <typeparam name="T">The event type to check</typeparam>
        /// <param name="publisher">The event publisher instance</param>
        /// <returns>The number of subscribers for type T</returns>
        public static int GetSubscriberCount<T>(this ResiliencyEventPublisher publisher) where T : ResiliencyEvent
        {
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

            var eventType = typeof(T).Name;
            return publisher.GetSubscriberCount(eventType);
        }

        /// <summary>
        /// Clears all event history and resets the publisher state.
        /// </summary>
        /// <param name="publisher">The event publisher instance</param>
        /// <param name="clearSubscribers">Whether to also clear all subscribers</param>
        public static void Reset(this ResiliencyEventPublisher publisher, bool clearSubscribers = false)
        {
            if (publisher == null)
            {
                throw new ArgumentNullException(nameof(publisher));
            }

            publisher.ClearHistory();
        }
    }
}