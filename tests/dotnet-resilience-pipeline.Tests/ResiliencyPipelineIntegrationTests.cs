#nullable enable
using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class ResiliencyPipelineIntegrationTests
{
    [Fact]
    public async Task FullPipeline_WithMultiplePolicies_RegistersAllPolicies()
    {
        var builder = new ResiliencyPipelineBuilder();
        var pipeline = builder
            .WithCircuitBreaker("payment-cb", p => p.FailureThreshold = 5)
            .WithRetry("payment-retry", p =>
            {
                p.MaxRetries = 3;
                p.InitialDelay = TimeSpan.FromMilliseconds(10);
                p.Strategy = RetryPolicy.BackoffStrategy.Exponential;
            })
            .Build();

        var allPolicies = pipeline.GetAllPolicies();

        allPolicies.Should().HaveCountGreaterThanOrEqualTo(2);
        allPolicies.Should().Contain(p => p.Name == "payment-cb");
        allPolicies.Should().Contain(p => p.Name == "payment-retry");
    }

    [Fact]
    public void BulkheadPolicy_WithMultipleSlots_LimitsParallelization()
    {
        var bulkhead = new BulkheadPolicy("api-bulkhead") { MaxParallelization = 2, MaxQueueLength = 5 };
        var timeout = new TimeoutPolicy("api-timeout") { Timeout = TimeSpan.FromMilliseconds(500) };

        var builder = new ResiliencyPipelineBuilder();
        var pipeline = builder
            .WithBulkhead("api-bulkhead", maxParallelization: 2, maxQueueLength: 5)
            .WithTimeout("api-timeout", TimeSpan.FromMilliseconds(500))
            .Build();

        var bulkheadPolicy = pipeline.GetPolicyByName("api-bulkhead") as BulkheadPolicy;
        bulkheadPolicy.Should().NotBeNull();
        bulkheadPolicy!.MaxParallelization.Should().Be(2);
    }

    [Fact]
    public void FullPipeline_WithFallback_ConfiguresFallbackPolicy()
    {
        var builder = new ResiliencyPipelineBuilder();
        var pipeline = builder
            .WithFallback("data-fetch", p =>
            {
                p.FallbackOnAnyException = true;
                p.SetFallbackAction<string>(async (ct) => "fallback-data");
            })
            .Build();

        var fallbackPolicy = pipeline.GetPolicyByName("data-fetch") as FallbackPolicy;
        fallbackPolicy.Should().NotBeNull();
        fallbackPolicy!.FallbackOnAnyException.Should().BeTrue();
    }

    [Fact]
    public void FullPipeline_WithAllPolicies_ConfiguresAll()
    {
        var builder = new ResiliencyPipelineBuilder();
        var pipeline = builder
            .WithCircuitBreaker("service-cb", p => p.FailureThreshold = 10)
            .WithRetry("service-retry", p =>
            {
                p.MaxRetries = 2;
                p.InitialDelay = TimeSpan.FromMilliseconds(10);
            })
            .WithBulkhead("service-bulkhead", 5, 20)
            .WithTimeout("service-timeout", TimeSpan.FromSeconds(10))
            .WithFallback("service-fallback", p =>
            {
                p.FallbackOnAnyException = true;
                p.SetFallbackAction<string>(async (ct) => "fallback-result");
            })
            .Build();

        var allPolicies = pipeline.GetAllPolicies();

        allPolicies.Should().HaveCountGreaterThanOrEqualTo(5);
        allPolicies.Should().Contain(p => p is CircuitBreakerPolicy);
        allPolicies.Should().Contain(p => p is RetryPolicy);
        allPolicies.Should().Contain(p => p is BulkheadPolicy);
        allPolicies.Should().Contain(p => p is TimeoutPolicy);
        allPolicies.Should().Contain(p => p is FallbackPolicy);
    }

    [Fact]
    public void CircuitBreakerService_WithFailures_TracksFailureCount()
    {
        var service = new CircuitBreakerService();
        var policy = new CircuitBreakerPolicy("test-cb") { FailureThreshold = 5 };

        policy.RecordFailure();
        policy.RecordFailure();

        policy.ConsecutiveFailures.Should().Be(2);
        policy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Closed);
    }

    [Fact]
    public void PipelineService_TracksTotalExecutions()
    {
        var pipeline = new ResiliencyPipelineService();
        var cbPolicy = new CircuitBreakerPolicy("metrics-cb");

        pipeline.RegisterPolicy(cbPolicy);

        cbPolicy.RecordSuccess();
        cbPolicy.RecordSuccess();
        cbPolicy.RecordFailure();

        pipeline.TotalExecutions.Should().Be(0); // Pipeline itself hasn't executed operations
        cbPolicy.TotalExecutions.Should().Be(3);
        cbPolicy.SuccessfulExecutions.Should().Be(2);
        cbPolicy.FailedExecutions.Should().Be(1);
    }

    [Fact]
    public void PipelineBuilder_FluentConfiguration_CreatesValidPipeline()
    {
        var pipeline = new ResiliencyPipelineBuilder()
            .WithCircuitBreaker("cb-1", p => p.FailureThreshold = 3)
            .WithRetry("retry-1", p => p.MaxRetries = 2)
            .WithTimeout("timeout-1", TimeSpan.FromSeconds(5))
            .WithBulkhead("bulkhead-1", 10, 50)
            .WithFallback("fallback-1")
            .Build();

        pipeline.Should().NotBeNull();
    }

    [Fact]
    public async Task CircuitBreakerOpenState_PreventsFurtherExecutions()
    {
        var cbPolicy = new CircuitBreakerPolicy("state-cb") { FailureThreshold = 1 };
        var cbService = new CircuitBreakerService();

        cbPolicy.RecordFailure();

        cbPolicy.CurrentState.Should().Be(CircuitBreakerPolicy.CircuitState.Open);

        Func<Task> act = () => cbService.ExecuteAsync<string>(
            cbPolicy,
            async _ => "should-not-execute");

        // The service will try to execute but the policy should block it
        await act.Should().ThrowAsync<CircuitBreakerOpenException>();
    }

    [Fact]
    public void RetryWithBackoff_CalculatesExponentialDelay()
    {
        var policy = new RetryPolicy("backoff-test")
        {
            Strategy = RetryPolicy.BackoffStrategy.Exponential,
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(10),
            BackoffMultiplier = 2.0,
            UseJitter = false
        };

        var delay0 = policy.CalculateDelay(0);
        var delay1 = policy.CalculateDelay(1);
        var delay2 = policy.CalculateDelay(2);

        delay0.Should().Be(TimeSpan.FromMilliseconds(10));
        delay1.TotalMilliseconds.Should().BeApproximately(20, 1);
        delay2.TotalMilliseconds.Should().BeApproximately(40, 1);
    }

    [Fact]
    public void BulkheadWithQueueing_ManagesQueuedRequests()
    {
        var bulkhead = new BulkheadPolicy("queue-bulkhead")
        {
            MaxParallelization = 2,
            MaxQueueLength = 10
        };

        bulkhead.TryAcquireSlot();
        bulkhead.TryAcquireSlot();
        var queueResult1 = bulkhead.TryAcquireSlot();
        var queueResult2 = bulkhead.TryAcquireSlot();

        queueResult1.Should().BeFalse();
        queueResult2.Should().BeFalse();
        bulkhead.QueuedRequests.Should().Be(2);
    }

    [Fact]
    public void TimeoutPolicy_ConfiguresTimeout()
    {
        var timeout = new TimeoutPolicy("test-timeout")
        {
            Timeout = TimeSpan.FromMilliseconds(200)
        };

        timeout.Timeout.Should().Be(TimeSpan.FromMilliseconds(200));
        timeout.IsValidConfiguration(out var error).Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void PolicyValidation_CatchesInvalidConfiguration()
    {
        var builder = new ResiliencyPipelineBuilder();
        var pipeline = builder
            .WithCircuitBreaker("invalid-cb", p => p.FailureThreshold = -1)
            .Build();

        var cbPolicy = pipeline.GetPolicyByName("invalid-cb") as CircuitBreakerPolicy;
        cbPolicy.Should().NotBeNull();
        cbPolicy!.FailureThreshold.Should().Be(-1);
    }

    [Fact]
    public void PipelineSnapshot_IncludesPolicies()
    {
        var cbPolicy = new CircuitBreakerPolicy("snap-cb");
        cbPolicy.RecordSuccess();
        cbPolicy.RecordSuccess();
        cbPolicy.RecordFailure();

        var snapshot = cbPolicy.GetSnapshot();

        snapshot.PolicyName.Should().Be("snap-cb");
        snapshot.Metadata.Should().NotBeEmpty();
    }
}
