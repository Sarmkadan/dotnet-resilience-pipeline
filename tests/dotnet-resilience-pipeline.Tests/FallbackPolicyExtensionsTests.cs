#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Contains unit tests for the <see cref="FallbackPolicyExtensions"/> helper methods.
/// </summary>
public sealed class FallbackPolicyExtensionsTests
{
    [Fact]
    public void AddFallbackTriggers_ValidTypes_AddsDistinctTypesInOrder()
    {
        var policy = new FallbackPolicy();

        policy.AddFallbackTriggers(new[] { typeof(TimeoutException), typeof(InvalidOperationException), typeof(TimeoutException) });

        policy.FallbackTriggerExceptions.Should().Equal(typeof(TimeoutException), typeof(InvalidOperationException));
    }

    [Fact]
    public void AddFallbackTriggers_NullArguments_ThrowsArgumentNullException()
    {
        var policy = new FallbackPolicy();

        Action nullPolicy = () => FallbackPolicyExtensions.AddFallbackTriggers(null!, new[] { typeof(TimeoutException) });
        Action nullTypes = () => policy.AddFallbackTriggers(null!);

        nullPolicy.Should().Throw<ArgumentNullException>().WithParameterName("policy");
        nullTypes.Should().Throw<ArgumentNullException>().WithParameterName("exceptionTypes");
    }

    [Fact]
    public void AddFallbackTriggers_EmptyCollection_ThrowsArgumentExceptionAndAddsNothing()
    {
        var policy = new FallbackPolicy();

        Action act = () => policy.AddFallbackTriggers(Array.Empty<Type>());

        act.Should().Throw<ArgumentException>().WithParameterName("exceptionTypes");
        policy.FallbackTriggerExceptions.Should().BeEmpty();
    }

    [Fact]
    public void AddFallbackTriggers_NonExceptionType_ThrowsArgumentException()
    {
        var policy = new FallbackPolicy();

        Action act = () => policy.AddFallbackTriggers(new[] { typeof(string) });

        act.Should().Throw<ArgumentException>().WithMessage("*String is not an Exception type*");
    }

    [Fact]
    public void IsFallbackHealthy_NoInvocations_ReturnsTrue()
    {
        var policy = new FallbackPolicy();

        policy.IsFallbackHealthy(1.0).Should().BeTrue();
    }

    [Fact]
    public void IsFallbackHealthy_AllFailures_ReturnsFalse_AndAllSuccesses_ReturnsTrue()
    {
        var failing = new FallbackPolicy();
        failing.RecordFailedFallback(new InvalidOperationException("boom"), 5);
        failing.RecordFailedFallback(new InvalidOperationException("boom"), 5);

        var healthy = new FallbackPolicy();
        healthy.RecordSuccessfulFallback(5);
        healthy.RecordSuccessfulFallback(5);

        failing.IsFallbackHealthy().Should().BeFalse();
        healthy.IsFallbackHealthy().Should().BeTrue();
    }

    [Fact]
    public void GetExecutionSummary_RecordedInvocations_ReportsCounts()
    {
        var policy = new FallbackPolicy();

        policy.GetExecutionSummary().Should().StartWith("Fallback Statistics:")
            .And.Contain("Invocations: 0, Successes: 0, Failures: 0");

        policy.RecordSuccessfulFallback(10);
        policy.RecordFailedFallback(new TimeoutException(), 20);

        policy.GetExecutionSummary().Should().Contain("Invocations: 2, Successes: 1, Failures: 1")
            .And.Contain("Avg Execution Time: 15");
    }

    [Fact]
    public void IsFallbackHealthy_AndGetExecutionSummary_NullPolicy_ThrowsArgumentNullException()
    {
        FallbackPolicy? missing = null;

        Action health = () => FallbackPolicyExtensions.IsFallbackHealthy(missing!);
        Action summary = () => FallbackPolicyExtensions.GetExecutionSummary(missing!);

        health.Should().Throw<ArgumentNullException>().WithParameterName("policy");
        summary.Should().Throw<ArgumentNullException>().WithParameterName("policy");
    }
}
