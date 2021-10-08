#nullable enable

using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using System;
using System.Collections.Generic;
using System.Globalization;

namespace DotNetResiliencePipeline.Tests;

/// <summary>
/// Provides extension methods for <see cref="TimeoutPolicyTests"/> to facilitate testing scenarios.
/// </summary>
public static class TimeoutPolicyTestsExtensions
{
    /// <summary>
    /// Creates a timeout policy with default configuration for testing purposes.
    /// </summary>
    /// <param name="timeoutMs">The timeout duration in milliseconds.</param>
    /// <returns>A configured <see cref="TimeoutPolicy"/> instance.</returns>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeoutMs is not positive.</exception>
    public static TimeoutPolicy CreateTestPolicy(this TimeoutPolicyTests _, int timeoutMs = 1000)
    {
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeoutMs);

        var policy = new TimeoutPolicy("test-policy")
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs)
        };

        return policy;
    }

    /// <summary>
    /// Creates a timeout policy with the specified name and timeout.
    /// </summary>
    /// <param name="name">The policy name.</param>
    /// <param name="timeoutMs">The timeout duration in milliseconds.</param>
    /// <returns>A configured <see cref="TimeoutPolicy"/> instance.</returns>
    /// <exception cref="ArgumentException">Thrown when name is null or whitespace.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when timeoutMs is not positive.</exception>
    public static TimeoutPolicy CreateTestPolicy(this TimeoutPolicyTests _, string name, int timeoutMs)
    {
        ArgumentException.ThrowIfNullOrEmpty(name);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(timeoutMs);

        var policy = new TimeoutPolicy(name)
        {
            Timeout = TimeSpan.FromMilliseconds(timeoutMs)
        };

        return policy;
    }

    /// <summary>
    /// Records multiple execution times for testing statistical calculations.
    /// </summary>
    /// <param name="policy">The policy instance.</param>
    /// <param name="executionTimesMs">Collection of execution times in milliseconds.</param>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    public static void RecordExecutionTimes(this TimeoutPolicyTests _, TimeoutPolicy policy, IEnumerable<int> executionTimesMs)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(executionTimesMs);

        foreach (var time in executionTimesMs)
        {
            policy.RecordExecutionTime(time);
        }
    }

    /// <summary>
    /// Records multiple timeouts for testing timeout statistics.
    /// </summary>
    /// <param name="policy">The policy instance.</param>
    /// <param name="timeoutDurationsMs">Collection of timeout durations in milliseconds.</param>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    /// <exception cref="ArgumentNullException">Thrown when timeoutDurationsMs is null.</exception>
    public static void RecordTimeouts(this TimeoutPolicyTests _, TimeoutPolicy policy, IEnumerable<int> timeoutDurationsMs)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(timeoutDurationsMs);

        foreach (var duration in timeoutDurationsMs)
        {
            policy.RecordTimeout(duration);
        }
    }

    /// <summary>
    /// Creates a sequence of execution times for testing percentile calculations.
    /// </summary>
    /// <param name="policy">The policy instance.</param>
    /// <param name="count">Number of execution times to record.</param>
    /// <param name="baseTimeMs">Base time value for the sequence.</param>
    /// <returns>Collection of execution times in milliseconds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is not positive.</exception>
    public static IEnumerable<int> CreateExecutionTimeSequence(this TimeoutPolicyTests _, TimeoutPolicy policy, int count, int baseTimeMs = 100)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        for (int i = 0; i < count; i++)
        {
            yield return baseTimeMs + (i * 10);
        }
    }

    /// <summary>
    /// Asserts that the policy has recorded the expected number of timeouts.
    /// </summary>
    /// <param name="policy">The policy instance.</param>
    /// <param name="expectedTimeoutCount">Expected timeout count.</param>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    public static void ShouldHaveTimeoutCount(this TimeoutPolicyTests _, TimeoutPolicy policy, int expectedTimeoutCount)
    {
        ArgumentNullException.ThrowIfNull(policy);

        policy.TimeoutCount.Should().Be(expectedTimeoutCount,
            $"Expected timeout count to be {expectedTimeoutCount}, but was {policy.TimeoutCount}");
    }

    /// <summary>
    /// Asserts that the policy timeout percentage matches the expected value.
    /// </summary>
    /// <param name="policy">The policy instance.</param>
    /// <param name="expectedPercentage">Expected timeout percentage (0-100).</param>
    /// <param name="precision">Allowed precision for floating point comparison.</param>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    public static void ShouldHaveTimeoutPercentage(this TimeoutPolicyTests _, TimeoutPolicy policy, double expectedPercentage, double precision = 0.1)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var actualPercentage = policy.GetTimeoutPercentage();
        actualPercentage.Should().BeApproximately(expectedPercentage, precision,
            $"Expected timeout percentage to be approximately {expectedPercentage}%, but was {actualPercentage}%");
    }

    /// <summary>
    /// Asserts that the policy's execution statistics match expected values.
    /// </summary>
    /// <param name="policy">The policy instance.</param>
    /// <param name="expectedAverage">Expected average execution time in milliseconds.</param>
    /// <param name="expectedMin">Expected minimum execution time in milliseconds.</param>
    /// <param name="expectedMax">Expected maximum execution time in milliseconds.</param>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    public static void ShouldHaveExecutionStats(this TimeoutPolicyTests _, TimeoutPolicy policy,
        long expectedAverage, long expectedMin, long expectedMax)
    {
        ArgumentNullException.ThrowIfNull(policy);

        policy.AverageExecutionTimeMs.Should().Be(expectedAverage,
            $"Expected average execution time to be {expectedAverage}ms, but was {policy.AverageExecutionTimeMs}ms");
        policy.ShortestExecutionTimeMs.Should().Be(expectedMin,
            $"Expected minimum execution time to be {expectedMin}ms, but was {policy.ShortestExecutionTimeMs}ms");
        policy.LongestExecutionTimeMs.Should().Be(expectedMax,
            $"Expected maximum execution time to be {expectedMax}ms, but was {policy.LongestExecutionTimeMs}ms");
    }

    /// <summary>
    /// Asserts that the policy's percentile execution times are within expected ranges.
    /// </summary>
    /// <param name="policy">The policy instance.</param>
    /// <param name="expectedP95">Expected 95th percentile execution time in milliseconds.</param>
    /// <param name="expectedP99">Expected 99th percentile execution time in milliseconds.</param>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    public static void ShouldHavePercentileTimes(this TimeoutPolicyTests _, TimeoutPolicy policy,
        long expectedP95, long expectedP99)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var actualP95 = policy.GetPercentile95ExecutionTime();
        var actualP99 = policy.GetPercentile99ExecutionTime();

        actualP95.Should().Be(expectedP95,
            $"Expected 95th percentile to be {expectedP95}ms, but was {actualP95}ms");
        actualP99.Should().Be(expectedP99,
            $"Expected 99th percentile to be {expectedP99}ms, but was {actualP99}ms");
    }

    /// <summary>
    /// Asserts that the policy configuration is valid.
    /// </summary>
    /// <param name="policy">The policy instance.</param>
    /// <param name="shouldBeValid">Whether the configuration should be valid.</param>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    public static void ShouldHaveValidConfiguration(this TimeoutPolicyTests _, TimeoutPolicy policy, bool shouldBeValid)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var isValid = policy.IsValidConfiguration(out var error);

        if (shouldBeValid)
        {
            isValid.Should().BeTrue("Expected policy configuration to be valid");
            error.Should().BeNull("Expected error message to be null for valid configuration");
        }
        else
        {
            isValid.Should().BeFalse("Expected policy configuration to be invalid");
            error.Should().NotBeNull("Expected error message to be provided for invalid configuration");
        }
    }

    /// <summary>
    /// Creates a policy with a sequence of execution times that follow a normal distribution.
    /// </summary>
    /// <param name="policy">The policy instance.</param>
    /// <param name="mean">Mean execution time in milliseconds.</param>
    /// <param name="stdDev">Standard deviation for the distribution.</param>
    /// <param name="count">Number of samples to generate.</param>
    /// <returns>Collection of normally distributed execution times in milliseconds.</returns>
    /// <exception cref="ArgumentNullException">Thrown when policy is null.</exception>
    /// <exception cref="ArgumentOutOfRangeException">Thrown when count is not positive.</exception>
    public static IEnumerable<int> CreateNormalDistributionExecutionTimes(this TimeoutPolicyTests _, TimeoutPolicy policy,
        int mean, int stdDev, int count)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        var random = new Random();
        for (int i = 0; i < count; i++)
        {
            // Box-Muller transform for normal distribution
            double u1 = 1.0 - random.NextDouble(); // Uniform(0,1] to avoid log(0)
            double u2 = 1.0 - random.NextDouble();
            double randStdNormal = Math.Sqrt(-2.0 * Math.Log(u1)) * Math.Sin(2.0 * Math.PI * u2);
            double randNormal = mean + stdDev * randStdNormal;

            yield return (int)Math.Round(randNormal);
        }
    }
}
