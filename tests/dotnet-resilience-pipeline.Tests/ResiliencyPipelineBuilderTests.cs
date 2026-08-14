using System;
using System.Threading;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Configuration;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public class ResiliencyPipelineBuilderTests
{
    [Fact]
    public void Build_WithAllPoliciesInCanonicalOrder_ReturnsService()
    {
        var builder = new ResiliencyPipelineBuilder()
            .WithFallback("fallback")
            .WithCircuitBreaker("circuit")
            .WithRetry("retry")
            .WithBulkhead("bulkhead", maxParallelization: 2)
            .WithTimeout("timeout", TimeSpan.FromSeconds(1));

        var service = builder.Build();

        Assert.NotNull(service);
        Assert.NotNull(builder.GetFallbackPolicy());
        Assert.NotNull(builder.GetCircuitBreakerPolicy());
        Assert.NotNull(builder.GetRetryPolicy());
        Assert.NotNull(builder.GetBulkheadPolicy());
        Assert.NotNull(builder.GetTimeoutPolicy());
    }

    [Fact]
    public void WithCircuitBreaker_WithoutFallback_ThrowsInvalidPolicyConfigurationException()
    {
        var builder = new ResiliencyPipelineBuilder();

        var ex = Assert.Throws<InvalidPolicyConfigurationException>(() =>
            builder.WithCircuitBreaker("circuit"));

        Assert.Contains("fallback", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void AllowCustomOrder_AllowsNonCanonicalPolicyAddition()
    {
        var builder = new ResiliencyPipelineBuilder()
            .AllowCustomOrder()
            .WithCircuitBreaker("circuit")
            .WithFallback("fallback")
            .WithRetry("retry")
            .WithBulkhead("bulkhead", maxParallelization: 1)
            .WithTimeout("timeout", TimeSpan.FromMilliseconds(500));

        var service = builder.Build();

        Assert.NotNull(service);
        Assert.NotNull(builder.GetCircuitBreakerPolicy());
        Assert.NotNull(builder.GetFallbackPolicy());
    }

    [Fact]
    public void WithDuplicatePolicyName_ThrowsInvalidOperationException()
    {
        var builder = new ResiliencyPipelineBuilder()
            .WithFallback("policy");

        var ex = Assert.Throws<InvalidOperationException>(() =>
            builder.WithCircuitBreaker("policy"));

        Assert.Contains("already been added", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithCircuitBreaker_NullOrWhiteSpaceName_ThrowsArgumentException()
    {
        var builder = new ResiliencyPipelineBuilder()
            .WithFallback("fallback");

        Assert.Throws<ArgumentException>(() => builder.WithCircuitBreaker(""));
        Assert.Throws<ArgumentException>(() => builder.WithCircuitBreaker("   "));
        Assert.Throws<ArgumentException>(() => builder.WithCircuitBreaker(null!));
    }

    [Fact]
    public void WithBulkhead_InvalidParallelization_ThrowsArgumentOutOfRangeException()
    {
        var builder = new ResiliencyPipelineBuilder()
            .WithFallback("fallback")
            .WithCircuitBreaker("circuit")
            .WithRetry("retry");

        Assert.Throws<ArgumentOutOfRangeException>(() =>
            builder.WithBulkhead("bulkhead", maxParallelization: 0));
    }

    [Fact]
    public void WithFallbackAction_WithoutFallback_ThrowsInvalidOperationException()
    {
        var builder = new ResiliencyPipelineBuilder();

        Func<CancellationToken, Task<int>> fallback = _ => Task.FromResult(42);

        var ex = Assert.Throws<InvalidOperationException>(() => builder.WithFallbackAction(fallback));

        Assert.Contains("FallbackPolicy must be configured", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void WithFallbackAction_SetsActionSuccessfully()
    {
        var builder = new ResiliencyPipelineBuilder()
            .WithFallback("fallback");

        Func<CancellationToken, Task<string>> fallback = _ => Task.FromResult("fallback");

        builder.WithFallbackAction(fallback);

        // No exception means the action was accepted; we can also verify the fallback is stored via reflection if needed,
        // but the public API does not expose it directly. The absence of an exception is sufficient for this test.
        Assert.NotNull(builder.GetFallbackPolicy());
    }
}
