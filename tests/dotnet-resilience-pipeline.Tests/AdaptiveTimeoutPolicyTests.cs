using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Tests;

public class AdaptiveTimeoutPolicyTests
{
    [Fact]
    public void Constructor_SetsDefaults_Correctly()
    {
        // Arrange & Act
        var policy = new AdaptiveTimeoutPolicy("test");

        // Assert
        policy.InitialTimeout.Should().Be(TimeSpan.FromSeconds(10));
        policy.MinTimeout.Should().Be(TimeSpan.FromMilliseconds(200));
        policy.MaxTimeout.Should().Be(TimeSpan.FromSeconds(60));
        policy.CurrentTimeout.Should().Be(policy.InitialTimeout);
        policy.TargetPercentile.Should().Be(95.0);
        policy.HeadroomFactor.Should().Be(1.2);
        policy.WindowSize.Should().Be(100);
        policy.MinSampleSize.Should().Be(10);
        policy.AdjustmentInterval.Should().Be(TimeSpan.FromSeconds(30));
        policy.TotalAdjustments.Should().Be(0);
    }

    [Fact]
    public void IsValidConfiguration_ReturnsTrue_ForDefaultValues()
    {
        // Arrange
        var policy = new AdaptiveTimeoutPolicy("test");

        // Act
        var isValid = policy.IsValidConfiguration(out var error);

        // Assert
        isValid.Should().BeTrue();
        error.Should().BeNull();
    }

    [Fact]
    public void RecordExecutionTime_Negative_ThrowsArgumentException()
    {
        // Arrange
        var policy = new AdaptiveTimeoutPolicy("test");

        // Act
        Action act = () => policy.RecordExecutionTime(-1);

        // Assert
        act.Should().Throw<ArgumentException>()
        .WithMessage("*Execution time cannot be negative*");
    }

    [Fact]
    public void RecordExecutionTime_WithExtremeValues_ClampsToConfiguredBounds()
    {
        // Arrange: configure policy with tight bounds
        var policy = new AdaptiveTimeoutPolicy("extreme-test")
        {
            InitialTimeout = TimeSpan.FromSeconds(1),
            MinTimeout = TimeSpan.FromSeconds(1),
            MaxTimeout = TimeSpan.FromSeconds(5),
            TargetPercentile = 95.0,
            HeadroomFactor = 1.0,
            WindowSize = 10,
            MinSampleSize = 1,
            AdjustmentInterval = TimeSpan.Zero
        };

        // Act & Assert: Provide extreme values that would skew the calculation
        // These values are way beyond reasonable bounds and should be rejected
        Action act1 = () => policy.RecordExecutionTime(-1000);
        act1.Should().Throw<ArgumentException>();

        Action act2 = () => policy.RecordExecutionTime((long)TimeSpan.FromHours(1).TotalMilliseconds);
        act2.Should().Throw<ArgumentException>();

        // Act: Provide values that are within bounds but would produce extreme percentiles
        // Fill window with values that would calculate to 1000 seconds if not clamped
        for (int i = 0; i < 10; i++)
        {
            policy.RecordExecutionTime(1000); // 1 second
        }

        // Assert: CurrentTimeout should remain at InitialTimeout since all values are clamped to MaxTimeout
        policy.CurrentTimeout.Should().Be(TimeSpan.FromSeconds(1));

        // Act: Now test with values that would push timeout up but get clamped
        policy.RecordExecutionTime(6000); // 6 seconds, would be 6000ms * 1.0 = 6000ms
        policy.CurrentTimeout.Should().Be(TimeSpan.FromSeconds(5)); // Clamped to MaxTimeout
    }

    [Fact]
    public void RecordExecutionTime_AdaptTimeout_UpdatesCurrentTimeout()
    {
        // Arrange: configure policy to adapt on every call
        var policy = new AdaptiveTimeoutPolicy("test")
        {
            InitialTimeout = TimeSpan.FromSeconds(1),
            MinTimeout = TimeSpan.FromMilliseconds(100),
            MaxTimeout = TimeSpan.FromSeconds(5),
            TargetPercentile = 50.0, // median
            HeadroomFactor = 1.0,
            WindowSize = 5,
            MinSampleSize = 1,
            AdjustmentInterval = TimeSpan.Zero // no waiting
        };

        // Act: first execution time 200 ms
        policy.RecordExecutionTime(200);
        // After first sample, median = 200 ms, so CurrentTimeout should become 200 ms
        policy.CurrentTimeout.Should().Be(TimeSpan.FromMilliseconds(200));

        // Act: second execution time 400 ms (window now 200,400)
        policy.RecordExecutionTime(400);
        // Median of [200,400] = 400 (ceil(2*0.5)-1 = 0? actually index = 0, but our algorithm uses Ceil-1, gives 1 -> 400)
        policy.CurrentTimeout.Should().Be(TimeSpan.FromMilliseconds(400));

        // Act: third execution time 600 ms (window [200,400,600])
        policy.RecordExecutionTime(600);
        // Median = 400 ms, so timeout stays 400 ms
        policy.CurrentTimeout.Should().Be(TimeSpan.FromMilliseconds(400));
    }

    [Fact]
    public void RecordTimeout_IncrementsTimeoutCount_AndUpdatesStatistics()
    {
        // Arrange
        var policy = new AdaptiveTimeoutPolicy("test")
        {
            MinSampleSize = 1,
            AdjustmentInterval = TimeSpan.Zero,
            TargetPercentile = 50.0,
            HeadroomFactor = 1.0
        };

        // Act
        policy.RecordTimeout(1234); // first timeout
        policy.RecordTimeout(5678); // second timeout

        // Assert
        policy.TimeoutCount.Should().Be(2);
        policy.GetTimeoutPercentage().Should().BeGreaterThan(0);
    }

    [Fact]
    public void ResetStatistics_RevertsToInitialState()
    {
        // Arrange
        var policy = new AdaptiveTimeoutPolicy("test")
        {
            InitialTimeout = TimeSpan.FromSeconds(2),
            MinSampleSize = 1,
            AdjustmentInterval = TimeSpan.Zero,
            TargetPercentile = 50.0,
            HeadroomFactor = 1.0
        };

        // cause some state changes
        policy.RecordExecutionTime(300);
        policy.RecordTimeout(500);
        policy.CurrentTimeout.Should().NotBe(policy.InitialTimeout);
        policy.TotalAdjustments.Should().BeGreaterThan(0);
        policy.TimeoutCount.Should().Be(1);

        // Act
        policy.ResetStatistics();

        // Assert
        policy.CurrentTimeout.Should().Be(policy.InitialTimeout);
        policy.TotalAdjustments.Should().Be(0);
        policy.TimeoutCount.Should().Be(0);
        policy.GetTimeoutPercentage().Should().Be(0);
    }

    [Fact]
    public void IsValidConfiguration_ReturnsFalse_WhenSettingsAreInconsistent()
    {
        // Arrange
        var policy = new AdaptiveTimeoutPolicy("test")
        {
            MinTimeout = TimeSpan.FromSeconds(10),
            MaxTimeout = TimeSpan.FromSeconds(5) // Min > Max
        };

        // Act
        var isValid = policy.IsValidConfiguration(out var error);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Contain("MinTimeout cannot exceed MaxTimeout");
    }
}