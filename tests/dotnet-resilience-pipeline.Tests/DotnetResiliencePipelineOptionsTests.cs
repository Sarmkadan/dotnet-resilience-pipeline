using System.ComponentModel.DataAnnotations;
using DotNetResiliencePipeline.Configuration;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public class DotnetResiliencePipelineOptionsTests
{
    [Fact]
    public void Validate_ShouldReturnTrue_WhenDefaultOptionsAreUsed()
    {
        // Arrange
        var options = new DotnetResiliencePipelineOptions();

        // Act
        var isValid = options.Validate(out var results);

        // Assert
        isValid.Should().BeTrue();
        results.Should().BeEmpty();
    }

    [Fact]
    public void CircuitBreakerOptions_ToPolicy_ShouldCreateCorrectPolicy()
    {
        // Arrange
        var options = new DotnetResiliencePipelineOptions.CircuitBreakerOptions
        {
            FailureThreshold = 10,
            OpenDurationSeconds = 60,
            SuccessThresholdInHalfOpen = 5
        };

        // Act
        var policy = options.ToPolicy("test-cb");

        // Assert
        policy.Name.Should().Be("test-cb");
        policy.FailureThreshold.Should().Be(10);
        policy.OpenDuration.Should().Be(TimeSpan.FromSeconds(60));
        policy.SuccessThresholdInHalfOpen.Should().Be(5);
    }

    [Fact]
    public void RetryOptions_ToPolicy_ShouldCreateCorrectPolicy()
    {
        // Arrange
        var options = new DotnetResiliencePipelineOptions.RetryOptions
        {
            MaxRetries = 5,
            InitialDelayMs = 200,
            MaxDelayMs = 50000,
            BackoffMultiplier = 3.0,
            UseJitter = false
        };

        // Act
        var policy = options.ToPolicy("test-retry");

        // Assert
        policy.Name.Should().Be("test-retry");
        policy.MaxRetries.Should().Be(5);
        policy.InitialDelay.Should().Be(TimeSpan.FromMilliseconds(200));
        policy.MaxDelay.Should().Be(TimeSpan.FromMilliseconds(50000));
        policy.BackoffMultiplier.Should().Be(3.0);
        policy.UseJitter.Should().BeFalse();
    }

    [Fact]
    public void TimeWindow_ToTimeOnly_ShouldConvertCorrectly()
    {
        // Arrange
        var options = new DotnetResiliencePipelineOptions.FailureInjectionTimeWindowOptions
        {
            StartTime = "09:30",
            EndTime = "18:45"
        };

        // Act
        var (start, end) = options.ToTimeOnly();

        // Assert
        start.Should().Be(new TimeOnly(9, 30));
        end.Should().Be(new TimeOnly(18, 45));
    }

    [Fact]
    public void TimeWindow_ToTimeOnly_ShouldThrow_WhenFormatIsInvalid()
    {
        // Arrange
        var options = new DotnetResiliencePipelineOptions.FailureInjectionTimeWindowOptions
        {
            StartTime = "25:00" // Invalid time
        };

        // Act
        var action = () => options.ToTimeOnly();

        // Assert
        action.Should().Throw<Exception>(); // Specifically, it will be an ArgumentOutOfRangeException from TimeOnly constructor
    }
}
