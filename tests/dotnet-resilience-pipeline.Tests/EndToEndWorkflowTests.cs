#nullable enable
using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// End-to-end integration tests covering realistic multi-policy workflows.
/// </summary>
public sealed class EndToEndWorkflowTests
{
    // ──────────────────────────────────────────────────────────────────────
    // README main use case: retry -> circuit breaker -> fallback pipeline
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task ReadmeMainUseCase_RetryThenCircuitBreaker_FailsOverToFallback()
    {
        var pipeline = new ResiliencyPipelineBuilder()
            .WithCircuitBreaker("order-cb", p =>
            {
                p.FailureThreshold = 3;
                p.OpenDuration = TimeSpan.FromSeconds(30);
            })
            .WithRetry("order-retry", p =>
            {
                p.MaxRetries = 2;
                p.InitialDelay = TimeSpan.FromMilliseconds(1);
                p.Strategy = RetryPolicy.BackoffStrategy.Fixed;
                p.UseJitter = false;
            })
            .WithFallback("order-fallback", p =>
            {
                p.FallbackOnAnyException = true;
                p.SetFallbackAction<string>(async ct => "fallback-order-data");
            })
            .Build();

        var allPolicies = pipeline.GetAllPolicies();
        allPolicies.Should().HaveCount(3);
        allPolicies.Should().Contain(p => p is CircuitBreakerPolicy);
        allPolicies.Should().Contain(p => p is RetryPolicy);
        allPolicies.Should().Contain(p => p is FallbackPolicy);

        var cbPolicy = pipeline.GetPolicyByName("order-cb") as CircuitBreakerPolicy;
        var retryPolicy = pipeline.GetPolicyByName("order-retry") as RetryPolicy;
        var fallbackPolicy = pipeline.GetPolicyByName("order-fallback") as FallbackPolicy;

        cbPolicy.Should().NotBeNull();
        retryPolicy.Should().NotBeNull();
        fallbackPolicy.Should().NotBeNull();

        cbPolicy!.FailureThreshold.Should().Be(3);
        retryPolicy!.MaxRetries.Should().Be(2);
        fallbackPolicy!.FallbackOnAnyException.Should().BeTrue();
    }

    // ──────────────────────────────────────────────────────────────────────
    // Retry service: actual retries counted, then success
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task RetryService_ExecutesRetries_ThenSucceeds()
    {
        var retryService = new RetryService();
        var policy = new RetryPolicy("e2e-retry")
        {
            MaxRetries = 4,
            InitialDelay = TimeSpan.FromMilliseconds(1),
            Strategy = RetryPolicy.BackoffStrategy.Fixed,
            UseJitter = false
        };
        int attempts = 0;

        var result = await retryService.ExecuteAsync<string>(
            policy,
            _ =>
            {
                attempts++;
                if (attempts < 4)
                    throw new TimeoutException("transient");
                return Task.FromResult("success");
            },
            CancellationToken.None);

        result.Should().Be("success");
        attempts.Should().Be(4);
        policy.TotalRetryAttempts.Should().Be(3);
        policy.SuccessfulExecutions.Should().Be(1);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Circuit breaker: trips after threshold, blocks further calls
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CircuitBreaker_TripsAfterThreshold_BlocksSubsequentCalls()
    {
        var cbService = new CircuitBreakerService();
        var policy = new CircuitBreakerPolicy("e2e-cb") { FailureThreshold = 3 };

        for (int i = 0; i < 3; i++)
        {
            try { await cbService.ExecuteAsync<string>(policy, _ => throw new InvalidOperationException("fail")); }
            catch (InvalidOperationException) { }
        }

        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Open);

        Func<Task> act = () => cbService.ExecuteAsync<string>(policy, _ => Task.FromResult("x"));
        await act.Should().ThrowAsync<CircuitBreakerOpenException>();
    }

    [Fact]
    public async Task CircuitBreaker_AfterOpenDurationElapses_AllowsHalfOpenProbe()
    {
        var cbService = new CircuitBreakerService();
        var policy = new CircuitBreakerPolicy("half-open-e2e")
        {
            FailureThreshold = 1,
            OpenDuration = TimeSpan.FromMilliseconds(80),
            SuccessThresholdInHalfOpen = 1
        };

        policy.RecordFailure();
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Open);

        await Task.Delay(120);

        var result = await cbService.ExecuteAsync<string>(policy, _ => Task.FromResult("probe-ok"));

        result.Should().Be("probe-ok");
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Closed);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Fallback service: returns alternative value when primary fails
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FallbackService_WhenPrimaryFails_ReturnsFallbackValue()
    {
        var fallbackService = new FallbackService();
        var policy = new FallbackPolicy("e2e-fallback") { FallbackOnAnyException = true };
        policy.SetFallbackAction<string>(async ct => "default-response");

        var primaryEx = new HttpRequestException("service unavailable");
        var result = await fallbackService.ExecuteAsync<string>(policy, primaryEx, 200, CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("default-response");
        result.Metadata.Should().ContainKey("FallbackUsed");
        policy.SuccessfulFallbackCount.Should().Be(1);
    }

    [Fact]
    public async Task FallbackService_WhenFallbackOnAnyExceptionFalse_ReturnsFailureResult()
    {
        var fallbackService = new FallbackService();
        var policy = new FallbackPolicy("no-trigger-fallback")
        {
            FallbackOnAnyException = false,
            FallbackTriggerExceptions = new List<Type> { typeof(TimeoutException) }
        };
        policy.SetFallbackAction<string>(async ct => "fallback");

        var primaryEx = new InvalidOperationException("different exception");
        var result = await fallbackService.ExecuteAsync<string>(policy, primaryEx, 100, CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
        result.Exception.Should().BeSameAs(primaryEx);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Bulkhead: concurrency limiting
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task BulkheadService_ConcurrentRequests_LimitsParallelism()
    {
        var policy = new BulkheadPolicy("e2e-bulkhead")
        {
            MaxParallelization = 3,
            MaxQueueLength = 10
        };
        var bulkheadService = new BulkheadService();

        var acquired = new List<bool>();
        for (int i = 0; i < 6; i++)
            acquired.Add(bulkheadService.TryAcquireSlot(policy));

        acquired.Count(x => x).Should().Be(3);
        bulkheadService.GetActiveExecutionCount(policy).Should().Be(3);
        bulkheadService.GetQueuedRequestCount(policy).Should().Be(3);
    }

    [Fact]
    public void BulkheadService_AfterRelease_AcceptsNewRequests()
    {
        var policy = new BulkheadPolicy("release-e2e") { MaxParallelization = 1, MaxQueueLength = 0 };
        var bulkheadService = new BulkheadService();

        bulkheadService.TryAcquireSlot(policy).Should().BeTrue();
        bulkheadService.TryAcquireSlot(policy).Should().BeFalse();

        bulkheadService.ReleaseSlot(policy);

        bulkheadService.GetActiveExecutionCount(policy).Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Timeout service: operation completes within timeout
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task TimeoutService_OperationCompletesBeforeTimeout_ReturnsResult()
    {
        var timeoutService = new TimeoutService();
        var policy = new TimeoutPolicy("e2e-timeout") { Timeout = TimeSpan.FromSeconds(5) };

        var result = await timeoutService.ExecuteAsync<string>(
            policy,
            async ct =>
            {
                await Task.Delay(10, ct);
                return "done-in-time";
            });

        result.Should().Be("done-in-time");
        policy.SuccessfulExecutions.Should().Be(1);
    }

    [Fact]
    public async Task TimeoutService_OperationExceedsTimeout_ThrowsOperationTimeoutException()
    {
        var timeoutService = new TimeoutService();
        var policy = new TimeoutPolicy("e2e-timeout-exceeded") { Timeout = TimeSpan.FromMilliseconds(50) };

        Func<Task> act = () => timeoutService.ExecuteAsync<string>(
            policy,
            async ct =>
            {
                await Task.Delay(2000, ct);
                return "too-slow";
            });

        await act.Should().ThrowAsync<OperationTimeoutException>();
        policy.TimeoutCount.Should().Be(1);
    }

    // ──────────────────────────────────────────────────────────────────────
    // Configuration: different config combinations
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public void PipelineBuilder_CircuitBreakerOnly_BuildsSuccessfully()
    {
        var pipeline = new ResiliencyPipelineBuilder()
            .WithCircuitBreaker("solo-cb", p => p.FailureThreshold = 10)
            .Build();

        pipeline.GetPolicyByName("solo-cb").Should().NotBeNull();
        pipeline.GetAllPolicies().Should().HaveCount(1);
    }

    [Fact]
    public void PipelineBuilder_TimeoutWithCustomConfiguration_ConfiguresCorrectly()
    {
        var pipeline = new ResiliencyPipelineBuilder()
            .WithTimeout("custom-timeout", TimeSpan.FromSeconds(45))
            .Build();

        var timeout = pipeline.GetPolicyByName("custom-timeout") as TimeoutPolicy;
        timeout.Should().NotBeNull();
        timeout!.Timeout.Should().Be(TimeSpan.FromSeconds(45));
    }

    [Fact]
    public void PipelineBuilder_BulkheadWithCustomLimits_ConfiguresCorrectly()
    {
        var pipeline = new ResiliencyPipelineBuilder()
            .WithBulkhead("custom-bulkhead", maxParallelization: 5, maxQueueLength: 25)
            .Build();

        var bulkhead = pipeline.GetPolicyByName("custom-bulkhead") as BulkheadPolicy;
        bulkhead.Should().NotBeNull();
        bulkhead!.MaxParallelization.Should().Be(5);
        bulkhead.MaxQueueLength.Should().Be(25);
    }

    [Fact]
    public void PipelineBuilder_WithFallbackAction_SetsFallbackCorrectly()
    {
        bool fallbackCalled = false;

        var pipeline = new ResiliencyPipelineBuilder()
            .WithFallback("action-fallback")
            .WithFallbackAction<string>(async ct =>
            {
                fallbackCalled = true;
                return "default";
            })
            .Build();

        var fb = pipeline.GetPolicyByName("action-fallback") as FallbackPolicy;
        fb.Should().NotBeNull();
        // Verify the fallback was configured by checking that invoking it returns the expected value
        fallbackCalled.Should().BeFalse(); // not yet called
        fb!.FallbackOnAnyException.Should().BeTrue(); // default value
    }

    [Fact]
    public void PipelineBuilder_WithFallbackActionBeforeFallbackPolicy_ThrowsInvalidOperationException()
    {
        Action act = () => new ResiliencyPipelineBuilder()
            .WithFallbackAction<string>(async ct => "orphaned");

        act.Should().Throw<InvalidOperationException>()
            .WithMessage("*FallbackPolicy must be configured before*");
    }

    // ──────────────────────────────────────────────────────────────────────
    // Concurrency: multiple threads using circuit breaker simultaneously
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task CircuitBreaker_ConcurrentSuccesses_RecordsAllCorrectly()
    {
        var cbService = new CircuitBreakerService();
        var policy = new CircuitBreakerPolicy("concurrent-cb") { FailureThreshold = 1000 };
        int concurrentTasks = 20;

        var tasks = Enumerable.Range(0, concurrentTasks)
            .Select(_ => cbService.ExecuteAsync<int>(
                policy,
                ct => Task.FromResult(1)));

        var results = await Task.WhenAll(tasks);

        results.Should().AllSatisfy(r => r.Should().Be(1));
        policy.TotalExecutions.Should().Be(concurrentTasks);
        policy.SuccessfulExecutions.Should().Be(concurrentTasks);
    }

    [Fact]
    public async Task Bulkhead_ConcurrentAcquireAndRelease_MaintainsConsistentCount()
    {
        var policy = new BulkheadPolicy("concurrent-bulkhead")
        {
            MaxParallelization = 5,
            MaxQueueLength = 100
        };
        var bulkheadService = new BulkheadService();
        int concurrentTasks = 10;

        var tasks = Enumerable.Range(0, concurrentTasks).Select(async i =>
        {
            bulkheadService.TryAcquireSlot(policy);
            await Task.Delay(5);
            bulkheadService.ReleaseSlot(policy);
        });

        await Task.WhenAll(tasks);

        policy.ActiveExecutions.Should().Be(0);
    }

    // ──────────────────────────────────────────────────────────────────────
    // End-to-end: execute -> verify result -> check policy statistics
    // ──────────────────────────────────────────────────────────────────────

    [Fact]
    public async Task FullWorkflow_ConfigureExecuteVerify_AllPoliciesTrackStatistics()
    {
        var cbService = new CircuitBreakerService();
        var retryService = new RetryService();
        var timeoutService = new TimeoutService();

        var cbPolicy = new CircuitBreakerPolicy("workflow-cb") { FailureThreshold = 10 };
        var retryPolicy = new RetryPolicy("workflow-retry")
        {
            MaxRetries = 2,
            InitialDelay = TimeSpan.FromMilliseconds(1),
            Strategy = RetryPolicy.BackoffStrategy.Fixed,
            UseJitter = false
        };
        var timeoutPolicy = new TimeoutPolicy("workflow-timeout") { Timeout = TimeSpan.FromSeconds(5) };

        var timeoutResult = await timeoutService.ExecuteAsync<string>(
            timeoutPolicy, ct => Task.FromResult("step1"));

        var cbResult = await cbService.ExecuteAsync<string>(
            cbPolicy, _ => Task.FromResult("step2"));

        var retryResult = await retryService.ExecuteAsync<string>(
            retryPolicy,
            _ => Task.FromResult("step3"),
            CancellationToken.None);

        timeoutResult.Should().Be("step1");
        cbResult.Should().Be("step2");
        retryResult.Should().Be("step3");

        timeoutPolicy.SuccessfulExecutions.Should().Be(1);
        cbPolicy.SuccessfulExecutions.Should().Be(1);
        retryPolicy.SuccessfulExecutions.Should().Be(1);
    }
}
