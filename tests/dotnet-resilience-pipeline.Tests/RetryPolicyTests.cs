#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;

namespace DotNetResiliencePipeline.Tests;

public sealed class RetryPolicyTests
{
    [Fact]
    public void CalculateDelay_FixedStrategy_ReturnsSameDelayForEveryAttempt()
    {
        // Arrange
        var policy = new RetryPolicy("api-retry")
        {
            Strategy = RetryPolicy.BackoffStrategy.Fixed,
            InitialDelay = TimeSpan.FromMilliseconds(200),
            MaxRetries = 4,
            UseJitter = false
        };

        // Act
        var delay0 = policy.CalculateDelay(0);
        var delay1 = policy.CalculateDelay(1);
        var delay2 = policy.CalculateDelay(2);

        // Assert
        delay0.Should().Be(TimeSpan.FromMilliseconds(200));
        delay1.Should().Be(TimeSpan.FromMilliseconds(200));
        delay2.Should().Be(TimeSpan.FromMilliseconds(200));
    }

    [Fact]
    public void CalculateDelay_ExponentialStrategy_DelayGrowsWithEachAttempt()
    {
        // Arrange
        var policy = new RetryPolicy("db-retry")
        {
            Strategy = RetryPolicy.BackoffStrategy.Exponential,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            BackoffMultiplier = 2.0,
            MaxRetries = 5,
            UseJitter = false,
            MaxDelay = TimeSpan.FromSeconds(60)
        };

        // Act – attempt 0: 100ms, attempt 1: 200ms, attempt 2: 400ms
        var delay0 = policy.CalculateDelay(0).TotalMilliseconds;
        var delay1 = policy.CalculateDelay(1).TotalMilliseconds;
        var delay2 = policy.CalculateDelay(2).TotalMilliseconds;

        // Assert
        delay1.Should().BeGreaterThan(delay0);
        delay2.Should().BeGreaterThan(delay1);
        delay1.Should().BeApproximately(delay0 * 2, 1.0);
        delay2.Should().BeApproximately(delay1 * 2, 1.0);
    }

    [Fact]
    public void CalculateDelay_AttemptEqualToMaxRetries_ThrowsArgumentOutOfRangeException()
    {
        // Arrange
        var policy = new RetryPolicy("cache-retry") { MaxRetries = 3 };

        // Act
        Action act = () => policy.CalculateDelay(3); // attemptNumber must be < MaxRetries

        // Assert
        act.Should().Throw<ArgumentOutOfRangeException>()
            .WithParameterName("attemptNumber");
    }

    [Fact]
    public void IsValidConfiguration_WhenMaxDelayIsLessThanInitialDelay_ReturnsFalseWithError()
    {
        // Arrange
        var policy = new RetryPolicy("search-retry")
        {
            InitialDelay = TimeSpan.FromSeconds(10),
            MaxDelay = TimeSpan.FromSeconds(5)   // less than InitialDelay — invalid
        };

        // Act
        var isValid = policy.IsValidConfiguration(out var error);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Contain("MaxDelay");
    }

    [Fact]
    public void IsRetryable_NullException_ReturnsFalse()
    {
        // Arrange
        var policy = new RetryPolicy("null-check-retry");

        // Act
        var result = policy.IsRetryable(null!);

        // Assert
        result.Should().BeFalse();
    }
}
