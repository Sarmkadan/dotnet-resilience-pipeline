#nullable enable
using System;
using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public class TimeoutPolicyValidationTests
{
    private const string UninitializedShortestError = "ShortestExecutionTimeMs has not been initialized with actual execution times.";
    private const string NonPositiveTimeoutError = "Timeout must be a positive time span.";

    [Fact]
    public void Validate_NullPolicy_ThrowsArgumentNullException()
    {
        TimeoutPolicy? nullPolicy = null;

        Action validate = () => nullPolicy!.Validate();
        Action isValid = () => nullPolicy!.IsValid();
        Action ensureValid = () => nullPolicy!.EnsureValid();

        validate.Should().Throw<ArgumentNullException>().WithParameterName("value");
        isValid.Should().Throw<ArgumentNullException>().WithParameterName("value");
        ensureValid.Should().Throw<ArgumentNullException>().WithParameterName("value");
    }

    [Fact]
    public void Validate_FreshPolicy_ReportsOnlyUninitializedShortest()
    {
        // A new policy has a positive default timeout but no recorded executions yet.
        var policy = new TimeoutPolicy("fresh");

        var errors = policy.Validate();

        errors.Should().Equal(UninitializedShortestError);
        policy.IsValid().Should().BeFalse();
    }

    [Fact]
    public void Validate_FreshPolicyWithZeroTimeout_ReturnsBothErrorsInOrder()
    {
        var policy = new TimeoutPolicy("zero") { Timeout = TimeSpan.Zero };

        var errors = policy.Validate();

        errors.Should().HaveCount(2);
        errors.Should().ContainInOrder(NonPositiveTimeoutError, UninitializedShortestError);
    }

    [Fact]
    public void Validate_PolicyWithRecordedExecutions_ReturnsNoErrors()
    {
        var policy = new TimeoutPolicy("valid") { Timeout = TimeSpan.FromSeconds(5) };
        policy.RecordExecutionTime(100);
        policy.RecordExecutionTime(300);

        policy.Validate().Should().BeEmpty();
        policy.IsValid().Should().BeTrue();
        Action ensureValid = () => policy.EnsureValid();
        ensureValid.Should().NotThrow();
    }

    [Fact]
    public void Validate_ZeroDurationExecution_IsAccepted()
    {
        // Zero is the lower boundary for execution times: it is recorded, not rejected.
        var policy = new TimeoutPolicy("zero-duration");
        policy.RecordExecutionTime(0);

        policy.Validate().Should().BeEmpty();
        policy.IsValid().Should().BeTrue();
    }

    [Theory]
    [InlineData(0L)]
    [InlineData(-1L)]
    public void Validate_NonPositiveTimeout_ReportsTimeoutError(long ticks)
    {
        var policy = new TimeoutPolicy("bad-timeout") { Timeout = TimeSpan.FromTicks(ticks) };
        policy.RecordExecutionTime(50);

        var errors = policy.Validate();

        errors.Should().ContainSingle().Which.Should().Be(NonPositiveTimeoutError);
        policy.IsValid().Should().BeFalse();
    }

    [Fact]
    public void EnsureValid_InvalidPolicy_ThrowsArgumentExceptionListingAllErrors()
    {
        var policy = new TimeoutPolicy("invalid") { Timeout = TimeSpan.Zero };

        Action ensureValid = () => policy.EnsureValid();

        var exception = ensureValid.Should().Throw<ArgumentException>().Which;
        exception.Message.Should().StartWith("TimeoutPolicy validation failed:");
        exception.Message.Should().Contain(NonPositiveTimeoutError);
        exception.Message.Should().Contain(UninitializedShortestError);
    }
}
