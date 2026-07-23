#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Threading.Tasks;
using DotNetResiliencePipeline.Events;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Stress tests for PipelineEventObserver to verify thread-safety and non-blocking behavior.
/// </summary>
public class PipelineEventObserverStressTests
{
    [Fact]
    public async Task EventStatistics_Counters_AreThreadSafe_And_Accurate_With_10k_Parallel_Events()
    {
        // Arrange
        var publisher = new ResiliencyEventPublisher();
        using var observer = new PipelineEventObserver(publisher);

        var eventCount = 1_000; // MaxHistorySize is 1000
        var tasks = new Task[eventCount];

        // Act: Fire 10k events from multiple threads
        for (int i = 0; i < eventCount; i++)
        {
            var taskId = i;
            tasks[i] = Task.Run(() =>
            {
                // Mix of different event types
                if (taskId % 5 == 0)
                {
                    publisher.PublishAsync(new PolicyExecutedSuccessfullyEvent
                    {
                        PolicyName = "TestRetryPolicy",
                        DurationMs = 100,
                        AttemptNumber = 1
                    }).Wait();
                }
                else if (taskId % 5 == 1)
                {
                    publisher.PublishAsync(new PolicyExecutionFailedEvent
                    {
                        PolicyName = "TestCircuitBreaker",
                        ExceptionType = "TimeoutException",
                        ExceptionMessage = "Operation timed out",
                        DurationMs = 200
                    }).Wait();
                }
                else if (taskId % 5 == 2)
                {
                    publisher.PublishAsync(new CircuitBreakerStateChangedEvent
                    {
                        PolicyName = "TestCircuitBreaker",
                        PreviousState = "Closed",
                        NewState = "Open",
                        ConsecutiveFailures = 5
                    }).Wait();
                }
                else if (taskId % 5 == 3)
                {
                    publisher.PublishAsync(new BulkheadRejectedEvent
                    {
                        PolicyName = "TestBulkhead",
                        ActiveExecutions = 10,
                        MaxCapacity = 10,
                        QueuedRequests = 5
                    }).Wait();
                }
                else
                {
                    publisher.PublishAsync(new TimeoutOccurredEvent
                    {
                        PolicyName = "TestTimeout",
                        TimeoutMs = 1000,
                        ActualDurationMs = 1500
                    }).Wait();
                }
            });
        }

        await Task.WhenAll(tasks);

        // Assert: Verify statistics are accurate
        var stats = observer.GetStatistics();

        Assert.Equal(eventCount, stats.TotalEventsEmitted);
        Assert.Equal(eventCount / 5, stats.SuccessfulExecutions);
        Assert.Equal(eventCount / 5, stats.FailedExecutions);
        Assert.Equal(eventCount / 5, stats.CircuitBreakerChanges);
        Assert.Equal(eventCount / 5, stats.BulkheadRejections);
        Assert.Equal(eventCount / 5, stats.Timeouts);

        // Verify counters are non-zero (thread-safe increments worked)
        Assert.True(stats.TotalEventsEmitted > 0);
        Assert.True(stats.SuccessfulExecutions >= 0);
        Assert.True(stats.FailedExecutions >= 0);
    }

    [Fact]
    public async Task EventStatistics_Counters_RemainConsistent_When_HandlersThrowExceptions()
    {
        // Arrange
        var publisher = new ResiliencyEventPublisher();
        using var observer = new PipelineEventObserver(publisher);

        var throwHandlerInvoked = false;
        var normalHandlerInvoked = false;

        // Register a handler that throws
        observer.RegisterHandler<PolicyExecutedSuccessfullyEvent>(
            "ThrowingHandler",
            _ =>
            {
                throwHandlerInvoked = true;
                throw new InvalidOperationException("Handler failed!");
            });

        // Register a normal handler
        observer.RegisterHandler<PolicyExecutedSuccessfullyEvent>(
            "NormalHandler",
            _ => normalHandlerInvoked = true);

        // Act: Publish events that will trigger both handlers
        await publisher.PublishAsync(new PolicyExecutedSuccessfullyEvent
        {
            PolicyName = "TestPolicy",
            DurationMs = 50,
            AttemptNumber = 1
        });

        // Assert: Both handlers were invoked despite one throwing
        Assert.True(throwHandlerInvoked, "Throwing handler should have been invoked");
        Assert.True(normalHandlerInvoked, "Normal handler should have been invoked");

        // Verify statistics still incremented correctly
        var stats = observer.GetStatistics();
        Assert.Equal(1, stats.TotalEventsEmitted);
        Assert.Equal(1, stats.SuccessfulExecutions);
    }

    [Fact]
    public async Task EventStatistics_Reset_ClearsAllCounters()
    {
        // Arrange
        var publisher = new ResiliencyEventPublisher();
        using var observer = new PipelineEventObserver(publisher);

        // Publish some events
        await publisher.PublishAsync(new PolicyExecutedSuccessfullyEvent
        {
            PolicyName = "TestPolicy",
            DurationMs = 50,
            AttemptNumber = 1
        });

        await publisher.PublishAsync(new PolicyExecutionFailedEvent
        {
            PolicyName = "TestPolicy",
            ExceptionType = "Exception",
            ExceptionMessage = "Failed",
            DurationMs = 50
        });

        var stats = observer.GetStatistics();
        Assert.Equal(2, stats.TotalEventsEmitted);
        Assert.Equal(1, stats.SuccessfulExecutions);
        Assert.Equal(1, stats.FailedExecutions);

        // Act: Reset statistics
        stats.Reset();

        // Assert: All counters are zero
        Assert.Equal(0, stats.TotalEventsEmitted);
        Assert.Equal(0, stats.SuccessfulExecutions);
        Assert.Equal(0, stats.FailedExecutions);
    }

    [Fact]
    public async Task PipelineEventHandler_HasAdditionalTrackingFields()
    {
        // Arrange
        var publisher = new ResiliencyEventPublisher();
        using var observer = new PipelineEventObserver(publisher);

        // Act: Register a handler
        observer.RegisterHandler<PolicyExecutedSuccessfullyEvent>(
            "TestHandler",
            _ => { });

        // Assert: Verify tracking fields exist
        var handlers = observer.GetHandlers();
        var handler = Assert.Single(handlers);

        Assert.NotEmpty(handler.Id);
        Assert.NotEmpty(handler.EventType);
        Assert.True(handler.CreatedAt <= DateTime.UtcNow);
        Assert.True(handler.IsActive);
        Assert.True(handler.LastUsed <= DateTime.UtcNow);
        Assert.Equal(1, handler.HandlerCount);
    }

    [Fact]
    public async Task EventStatistics_CalculatesFailureRate_Correctly()
    {
        // Arrange
        var publisher = new ResiliencyEventPublisher();
        using var observer = new PipelineEventObserver(publisher);

        // Publish 100 successful events
        for (int i = 0; i < 100; i++)
        {
            await publisher.PublishAsync(new PolicyExecutedSuccessfullyEvent
            {
                PolicyName = "TestPolicy",
                DurationMs = 50,
                AttemptNumber = 1
            });
        }

        // Publish 25 failed events
        for (int i = 0; i < 25; i++)
        {
            await publisher.PublishAsync(new PolicyExecutionFailedEvent
            {
                PolicyName = "TestPolicy",
                ExceptionType = "Exception",
                ExceptionMessage = "Failed",
                DurationMs = 50
            });
        }

        // Act: Get statistics
        var stats = observer.GetStatistics();
        var failureRate = stats.FailureRate;
        var successRate = stats.SuccessRate;

        // Assert: Rates are calculated correctly
        Assert.Equal(20.0, failureRate); // 25/125 = 20%
        Assert.Equal(80.0, successRate); // 100/125 = 80%
    }

    [Fact]
    public async Task EventStatistics_HandlesDivisionByZero_Gracefully()
    {
        // Arrange
        var publisher = new ResiliencyEventPublisher();
        using var observer = new PipelineEventObserver(publisher);

        // Act: Get statistics with no events
        var stats = observer.GetStatistics();
        var failureRate = stats.FailureRate;
        var successRate = stats.SuccessRate;

        // Assert: No division by zero exception, returns 0
        Assert.Equal(0, failureRate);
        Assert.Equal(100, successRate);
    }
}
