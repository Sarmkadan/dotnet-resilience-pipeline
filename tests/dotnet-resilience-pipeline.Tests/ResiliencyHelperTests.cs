#nullable enable
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Utilities;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Provides unit tests for the <see cref="ResiliencyHelper"/> class.
/// Tests various helper methods for determining pipeline health status and validating/exorting resiliency policies.
/// </summary>
public sealed class ResiliencyHelperTests
{
    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.DeterminePipelineHealth"/> returns <see cref="HealthStatus.Healthy"/> when the success rate is 95% or higher.
    /// </summary>
    [Fact]
    public void DeterminePipelineHealth_SuccessRateAbove95_ReturnsHealthy()
    {
        ResiliencyHelper.DeterminePipelineHealth(97).Should().Be(HealthStatus.Healthy);
        ResiliencyHelper.DeterminePipelineHealth(95).Should().Be(HealthStatus.Healthy);
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.DeterminePipelineHealth"/> returns <see cref="HealthStatus.Degraded"/> when the success rate is between 80% and 94%.
    /// </summary>
    [Fact]
    public void DeterminePipelineHealth_SuccessRateBetween80And94_ReturnsDegraded()
    {
        ResiliencyHelper.DeterminePipelineHealth(80).Should().Be(HealthStatus.Degraded);
        ResiliencyHelper.DeterminePipelineHealth(90).Should().Be(HealthStatus.Degraded);
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.DeterminePipelineHealth"/> returns <see cref="HealthStatus.Unhealthy"/> when the success rate is between 50% and 79%.
    /// </summary>
    [Fact]
    public void DeterminePipelineHealth_SuccessRateBetween50And79_ReturnsUnhealthy()
    {
        ResiliencyHelper.DeterminePipelineHealth(50).Should().Be(HealthStatus.Unhealthy);
        ResiliencyHelper.DeterminePipelineHealth(70).Should().Be(HealthStatus.Unhealthy);
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.DeterminePipelineHealth"/> returns <see cref="HealthStatus.Critical"/> when the success rate is below 50%.
    /// </summary>
    [Fact]
    public void DeterminePipelineHealth_SuccessRateBelow50_ReturnsCritical()
    {
        ResiliencyHelper.DeterminePipelineHealth(0).Should().Be(HealthStatus.Critical);
        ResiliencyHelper.DeterminePipelineHealth(49).Should().Be(HealthStatus.Critical);
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ExportPolicyConfig"/> exports a circuit breaker policy configuration with all required base fields.
    /// </summary>
    [Fact]
    public void ExportPolicyConfig_CircuitBreaker_ContainsAllBaseFields()
    {
        var policy = new CircuitBreakerPolicy("export-cb") { IsEnabled = true };

        var config = ResiliencyHelper.ExportPolicyConfig(policy);

        config.Should().ContainKey("Id");
        config.Should().ContainKey("Name");
        config.Should().ContainKey("Type");
        config.Should().ContainKey("IsEnabled");
        config.Should().ContainKey("CreatedAt");
        config.Should().ContainKey("ModifiedAt");
        config.Should().ContainKey("Tags");
        config.Should().ContainKey("Metadata");
        config["Name"].Should().Be("export-cb");
        config["Type"].Should().Be("CircuitBreakerPolicy");
        config["IsEnabled"].Should().Be(true);
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ExportPolicyConfig"/> throws an <see cref="ArgumentNullException"/> when passed a null policy.
    /// </summary>
    [Fact]
    public void ExportPolicyConfig_NullPolicy_ThrowsArgumentNullException()
    {
        Action act = () => ResiliencyHelper.ExportPolicyConfig(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ValidatePolicy"/> throws an <see cref="ArgumentNullException"/> when passed a null policy.
    /// </summary>
    [Fact]
    public void ValidatePolicy_NullPolicy_ThrowsArgumentNullException()
    {
        Action act = () => ResiliencyHelper.ValidatePolicy(null!);

        act.Should().Throw<ArgumentNullException>();
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ValidatePolicy"/> returns an empty collection when validating a valid circuit breaker policy.
    /// </summary>
    [Fact]
    public void ValidatePolicy_ValidCircuitBreaker_ReturnsEmptyErrors()
    {
        var policy = new CircuitBreakerPolicy("valid-cb")
        {
            FailureThreshold = 5,
            OpenDuration = TimeSpan.FromSeconds(30)
        };

        var errors = ResiliencyHelper.ValidatePolicy(policy);

        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ValidatePolicy"/> returns validation errors when a circuit breaker policy has a zero failure threshold.
    /// </summary>
    [Fact]
    public void ValidatePolicy_CircuitBreakerWithZeroThreshold_ReturnsErrors()
    {
        var policy = new CircuitBreakerPolicy("bad-cb") { FailureThreshold = 0 };

        var errors = ResiliencyHelper.ValidatePolicy(policy);

        errors.Should().NotBeEmpty();
        errors.Should().ContainMatch("*threshold*");
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ValidatePolicy"/> returns validation errors when a circuit breaker policy has a zero open duration.
    /// </summary>
    [Fact]
    public void ValidatePolicy_CircuitBreakerWithZeroOpenDuration_ReturnsErrors()
    {
        var policy = new CircuitBreakerPolicy("bad-duration-cb") { OpenDuration = TimeSpan.Zero };

        var errors = ResiliencyHelper.ValidatePolicy(policy);

        errors.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ValidatePolicy"/> returns an empty collection when validating a valid retry policy.
    /// </summary>
    [Fact]
    public void ValidatePolicy_ValidRetryPolicy_ReturnsEmptyErrors()
    {
        var policy = new RetryPolicy("valid-retry")
        {
            MaxRetries = 3,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            MaxDelay = TimeSpan.FromSeconds(30)
        };

        var errors = ResiliencyHelper.ValidatePolicy(policy);

        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ValidatePolicy"/> returns validation errors when a retry policy has an invalid max retries value.
    /// </summary>
    [Fact]
    public void ValidatePolicy_InvalidRetryPolicy_ReturnsErrors()
    {
        var policy = new RetryPolicy("bad-retry") { MaxRetries = -1 };

        var errors = ResiliencyHelper.ValidatePolicy(policy);

        errors.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ValidatePolicy"/> returns an empty collection when validating a valid timeout policy.
    /// </summary>
    [Fact]
    public void ValidatePolicy_ValidTimeoutPolicy_ReturnsEmptyErrors()
    {
        var policy = new TimeoutPolicy("valid-timeout") { Timeout = TimeSpan.FromSeconds(5) };

        var errors = ResiliencyHelper.ValidatePolicy(policy);

        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ValidatePolicy"/> returns validation errors when a timeout policy has a zero timeout value.
    /// </summary>
    [Fact]
    public void ValidatePolicy_InvalidTimeoutPolicy_ReturnsErrors()
    {
        var policy = new TimeoutPolicy("zero-timeout") { Timeout = TimeSpan.Zero };

        var errors = ResiliencyHelper.ValidatePolicy(policy);

        errors.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ValidatePolicy"/> returns an empty collection when validating a valid bulkhead policy.
    /// </summary>
    [Fact]
    public void ValidatePolicy_ValidBulkheadPolicy_ReturnsEmptyErrors()
    {
        var policy = new BulkheadPolicy("valid-bulkhead") { MaxParallelization = 10 };

        var errors = ResiliencyHelper.ValidatePolicy(policy);

        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ValidatePolicy"/> returns validation errors when a bulkhead policy has a zero max parallelization value.
    /// </summary>
    [Fact]
    public void ValidatePolicy_BulkheadWithZeroParallelization_ReturnsErrors()
    {
        var policy = new BulkheadPolicy("zero-bulkhead") { MaxParallelization = 0 };

        var errors = ResiliencyHelper.ValidatePolicy(policy);

        errors.Should().NotBeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ValidatePolicy"/> returns an empty collection when validating a valid fallback policy.
    /// </summary>
    [Fact]
    public void ValidatePolicy_ValidFallbackPolicy_ReturnsEmptyErrors()
    {
        var policy = new FallbackPolicy("valid-fallback") { FallbackOnAnyException = true };
        policy.SetFallbackAction<string>(async ct => "fallback");

        var errors = ResiliencyHelper.ValidatePolicy(policy);

        errors.Should().BeEmpty();
    }

    /// <summary>
    /// Tests that <see cref="ResiliencyHelper.ExportPolicyConfig"/> includes tags in the exported policy configuration.
    /// </summary>
    [Fact]
    public void ExportPolicyConfig_WithTags_IncludesTags()
    {
        var policy = new RetryPolicy("tagged-retry");
        policy.Tags.Add("production");
        policy.Tags.Add("critical");

        var config = ResiliencyHelper.ExportPolicyConfig(policy);

        var tags = config["Tags"] as List<string>;
        tags.Should().NotBeNull();
        tags!.Should().Contain("production");
        tags.Should().Contain("critical");
    }
}
