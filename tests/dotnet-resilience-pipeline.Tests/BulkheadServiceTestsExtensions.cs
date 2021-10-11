#nullable enable

using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Services;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public static class BulkheadServiceTestsExtensions
{
    /// <summary>
    /// Creates a bulkhead policy with the specified configuration for testing purposes.
    /// </summary>
    /// <param name="service">The bulkhead service instance.</param>
    /// <param name="name">The name of the policy.</param>
    /// <param name="maxParallelization">Maximum parallel executions allowed.</param>
    /// <param name="maxQueueLength">Maximum queue length allowed.</param>
    /// <param name="isEnabled">Whether the policy is enabled.</param>
    /// <returns>A configured bulkhead policy instance.</returns>
    public static BulkheadPolicy CreateTestPolicy(
        this BulkheadService service,
        string name,
        int maxParallelization = 5,
        int maxQueueLength = 10,
        bool isEnabled = true)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return new BulkheadPolicy(name)
        {
            MaxParallelization = maxParallelization,
            MaxQueueLength = maxQueueLength,
            IsEnabled = isEnabled
        };
    }

    /// <summary>
    /// Asserts that a policy's configuration matches expected values.
    /// </summary>
    /// <param name="service">The bulkhead service instance.</param>
    /// <param name="policy">The policy to verify.</param>
    /// <param name="expectedMaxParallelization">Expected max parallelization value.</param>
    /// <param name="expectedMaxQueueLength">Expected max queue length value.</param>
    /// <param name="expectedIsEnabled">Expected enabled state.</param>
    public static void ShouldHaveConfiguration(
        this BulkheadService service,
        BulkheadPolicy policy,
        int expectedMaxParallelization,
        int expectedMaxQueueLength,
        bool expectedIsEnabled)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(policy);

        policy.MaxParallelization.Should().Be(expectedMaxParallelization);
        policy.MaxQueueLength.Should().Be(expectedMaxQueueLength);
        policy.IsEnabled.Should().Be(expectedIsEnabled);
    }

    /// <summary>
    /// Asserts that a policy's utilization percentage matches expected value.
    /// </summary>
    /// <param name="service">The bulkhead service instance.</param>
    /// <param name="policy">The policy to verify.</param>
    /// <param name="expectedUtilization">Expected utilization percentage (0-100).</param>
    public static void ShouldHaveUtilizationPercentage(
        this BulkheadService service,
        BulkheadPolicy policy,
        int expectedUtilization)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(policy);

        var actualUtilization = service.GetUtilizationPercentage(policy);
        actualUtilization.Should().Be(expectedUtilization);
    }

    /// <summary>
    /// Asserts that a policy's state matches expected values for active executions and queued requests.
    /// </summary>
    /// <param name="service">The bulkhead service instance.</param>
    /// <param name="policy">The policy to verify.</param>
    /// <param name="expectedActiveExecutions">Expected number of active executions.</param>
    /// <param name="expectedQueuedRequests">Expected number of queued requests.</param>
    public static void ShouldHaveExecutionState(
        this BulkheadService service,
        BulkheadPolicy policy,
        int expectedActiveExecutions,
        int expectedQueuedRequests)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(policy);

        policy.ActiveExecutions.Should().Be(expectedActiveExecutions);
        policy.QueuedRequests.Should().Be(expectedQueuedRequests);
    }

    /// <summary>
    /// Asserts that a policy's configuration validation returns the expected result.
    /// </summary>
    /// <param name="service">The bulkhead service instance.</param>
    /// <param name="policy">The policy to validate.</param>
    /// <param name="expectedIsValid">Expected validation result.</param>
    /// <param name="expectedErrorSubstring">Optional expected error substring if validation fails.</param>
    public static void ShouldValidateConfiguration(
        this BulkheadService service,
        BulkheadPolicy policy,
        bool expectedIsValid,
        string? expectedErrorSubstring = null)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(policy);

        var isValid = service.IsValidConfiguration(policy, out var error);
        isValid.Should().Be(expectedIsValid);

        if (expectedIsValid)
        {
            error.Should().BeNull();
        }
        else if (expectedErrorSubstring is not null)
        {
            error.Should().NotBeNull()
                .And.Contain(expectedErrorSubstring);
        }
    }

    /// <summary>
    /// Gets the current bulkhead metrics as a read-only dictionary for verification.
    /// </summary>
    /// <param name="service">The bulkhead service instance.</param>
    /// <param name="policy">The policy to get metrics for.</param>
    /// <returns>A read-only dictionary containing current metrics.</returns>
    public static IReadOnlyDictionary<string, object> GetMetrics(
        this BulkheadService service,
        BulkheadPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(policy);

        return new Dictionary<string, object>
        {
            ["ActiveExecutions"] = policy.ActiveExecutions,
            ["QueuedRequests"] = policy.QueuedRequests,
            ["MaxParallelization"] = policy.MaxParallelization,
            ["MaxQueueLength"] = policy.MaxQueueLength,
            ["IsEnabled"] = policy.IsEnabled,
            ["AverageQueueTimeMs"] = policy.AverageQueueTimeMs,
            ["UtilizationPercentage"] = service.GetUtilizationPercentage(policy)
        };
    }

    /// <summary>
    /// Asserts that a policy's metrics match expected values.
    /// </summary>
    /// <param name="service">The bulkhead service instance.</param>
    /// <param name="policy">The policy to verify.</param>
    /// <param name="expectedActiveExecutions">Expected active executions count.</param>
    /// <param name="expectedQueuedRequests">Expected queued requests count.</param>
    /// <param name="expectedUtilizationPercentage">Expected utilization percentage.</param>
    public static void ShouldHaveMetrics(
        this BulkheadService service,
        BulkheadPolicy policy,
        int expectedActiveExecutions,
        int expectedQueuedRequests,
        int expectedUtilizationPercentage)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(policy);

        service.ShouldHaveExecutionState(policy, expectedActiveExecutions, expectedQueuedRequests);
        service.ShouldHaveUtilizationPercentage(policy, expectedUtilizationPercentage);
    }
}