#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ===================================================================
using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using Xunit;

/// <summary>
/// Tests for the RetryPolicy class.
/// </summary>
public sealed class RetryPolicyTests
{
    /// <summary>
    /// Verifies that the CalculateDelay method returns the same delay for every attempt when using the Fixed strategy.
    /// </summary>
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

    /// <summary>
    /// Verifies that the CalculateDelay method returns a delay that grows with each attempt when using the Exponential strategy.
    /// </summary>
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

    /// <summary>
    /// Verifies that the CalculateDelay method throws an ArgumentOutOfRangeException when the attempt number is equal to the maximum retries.
    /// </summary>
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

    /// <summary>
    /// Verifies that the IsValidConfiguration method returns false when the maximum delay is less than the initial delay.
    /// </summary>
    [Fact]
    public void IsValidConfiguration_WhenMaxDelayIsLessThanInitialDelay_ReturnsFalseWithError()
    {
        // Arrange
        var policy = new RetryPolicy("search-retry")
        {
            InitialDelay = TimeSpan.FromSeconds(10),
            MaxDelay = TimeSpan.FromSeconds(5) // less than InitialDelay — invalid
        };

        // Act
        var isValid = policy.IsValidConfiguration(out var error);

        // Assert
        isValid.Should().BeFalse();
        error.Should().Contain("MaxDelay");
    }

    /// <summary>
    /// Verifies that the IsRetryable method returns false when the exception is null.
    /// </summary>
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

    /// <summary>
    /// Verifies that MaxRetries property limits the number of retry attempts.
    /// </summary>
    [Fact]
    public void MaxRetries_LimitsRetryAttempts()
    {
        // Arrange
        var policy = new RetryPolicy("test-retry")
        {
            MaxRetries = 2,
            UseJitter = false
        };

        // Act & Assert
        policy.CalculateDelay(0).Should().NotBe(TimeSpan.Zero);
        policy.CalculateDelay(1).Should().NotBe(TimeSpan.Zero);

        // MaxRetries is the maximum number of retry attempts, so attemptNumber must be < MaxRetries
        Action act = () => policy.CalculateDelay(2);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Verifies that MaxRetries of 0 allows no retry attempts (zero-retry passthrough).
    /// </summary>
    [Fact]
    public void MaxRetries_Zero_AllowsNoRetryAttempts()
    {
        // Arrange
        var policy = new RetryPolicy("no-retry")
        {
            MaxRetries = 0,
            UseJitter = false
        };

        // Act & Assert - with MaxRetries = 0, only attempt 0 is valid (0 < 0 is false, so no attempts allowed)
        // Actually, looking at the code, MaxRetries = 0 means attemptNumber must be < 0, which is never true
        // So no CalculateDelay calls should succeed
        Action act = () => policy.CalculateDelay(0);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Verifies that MaxRetries of 1 allows exactly one retry attempt.
    /// </summary>
    [Fact]
    public void MaxRetries_One_AllowsOneRetryAttempt()
    {
        // Arrange
        var policy = new RetryPolicy("single-retry")
        {
            MaxRetries = 1,
            UseJitter = false
        };

        // Act & Assert - attempt 0 is valid (0 < 1), attempt 1 is not (1 < 1 is false)
        policy.CalculateDelay(0).Should().NotBe(TimeSpan.Zero);

        Action act = () => policy.CalculateDelay(1);
        act.Should().Throw<ArgumentOutOfRangeException>();
    }

    /// <summary>
    /// Verifies that backoff grows exponentially with each attempt.
    /// </summary>
    [Fact]
    public void CalculateDelay_ExponentialStrategy_DelayGrowsExponentially()
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

        // Act - attempt 0: 100ms, attempt 1: 200ms, attempt 2: 400ms, attempt 3: 800ms
        var delay0 = policy.CalculateDelay(0).TotalMilliseconds;
        var delay1 = policy.CalculateDelay(1).TotalMilliseconds;
        var delay2 = policy.CalculateDelay(2).TotalMilliseconds;
        var delay3 = policy.CalculateDelay(3).TotalMilliseconds;

        // Assert
        delay0.Should().BeApproximately(100, 0.1);
        delay1.Should().BeApproximately(200, 0.1);
        delay2.Should().BeApproximately(400, 0.1);
        delay3.Should().BeApproximately(800, 0.1);
    }

    /// <summary>
    /// Verifies that backoff grows linearly with each attempt.
    /// </summary>
    [Fact]
    public void CalculateDelay_LinearStrategy_DelayGrowsLinearly()
    {
        // Arrange
        var policy = new RetryPolicy("cache-retry")
        {
            Strategy = RetryPolicy.BackoffStrategy.Linear,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            MaxRetries = 4,
            UseJitter = false
        };

        // Act - attempt 0: 100ms, attempt 1: 200ms, attempt 2: 300ms
        var delay0 = policy.CalculateDelay(0).TotalMilliseconds;
        var delay1 = policy.CalculateDelay(1).TotalMilliseconds;
        var delay2 = policy.CalculateDelay(2).TotalMilliseconds;

        // Assert
        delay0.Should().BeApproximately(100, 0.1);
        delay1.Should().BeApproximately(200, 0.1);
        delay2.Should().BeApproximately(300, 0.1);
    }

    /// <summary>
    /// Verifies that backoff is capped at MaxDelay.
    /// </summary>
    [Fact]
    public void CalculateDelay_ExponentialStrategy_CapsAtMaxDelay()
    {
        // Arrange
        var policy = new RetryPolicy("capped-retry")
        {
            Strategy = RetryPolicy.BackoffStrategy.Exponential,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            BackoffMultiplier = 10.0, // Very aggressive growth
            MaxRetries = 10,
            UseJitter = false,
            MaxDelay = TimeSpan.FromMilliseconds(500) // Cap at 500ms
        };

        // Act - attempt 0: 100ms, attempt 1: 1000ms (capped to 500ms)
        var delay0 = policy.CalculateDelay(0).TotalMilliseconds;
        var delay1 = policy.CalculateDelay(1).TotalMilliseconds;

        // Assert
        delay0.Should().BeApproximately(100, 0.1);
        delay1.Should().BeApproximately(500, 0.1); // Capped at MaxDelay
    }

    /// <summary>
    /// Verifies that IsRetryable returns true for default retryable exceptions (TimeoutException, HttpRequestException).
    /// </summary>
    [Fact]
    public void IsRetryable_DefaultRetryableExceptions_ReturnsTrue()
    {
        // Arrange
        var policy = new RetryPolicy("default-retry");

        // Act & Assert
        policy.IsRetryable(new TimeoutException()).Should().BeTrue();
        policy.IsRetryable(new HttpRequestException()).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsRetryable returns false for non-retryable exceptions when RetryableExceptions is empty.
    /// </summary>
    [Fact]
    public void IsRetryable_NoRetryableExceptionsConfigured_ReturnsTrueForAll()
    {
        // Arrange
        var policy = new RetryPolicy("all-retry");
        policy.RetryableExceptions.Clear(); // Clear default exceptions

        // Act & Assert
        policy.IsRetryable(new TimeoutException()).Should().BeTrue();
        policy.IsRetryable(new HttpRequestException()).Should().BeTrue();
        policy.IsRetryable(new InvalidOperationException()).Should().BeTrue();
        policy.IsRetryable(new ArgumentException()).Should().BeTrue();
    }

    /// <summary>
    /// Verifies that IsRetryable returns false for exceptions not in RetryableExceptions list.
    /// </summary>
    [Fact]
    public void IsRetryable_NonRetryableException_ReturnsFalse()
    {
        // Arrange
        var policy = new RetryPolicy("selective-retry");
        policy.RetryableExceptions.Clear();
        policy.RetryableExceptions.Add(typeof(TimeoutException));

        // Act & Assert
        policy.IsRetryable(new TimeoutException()).Should().BeTrue();
        policy.IsRetryable(new HttpRequestException()).Should().BeFalse();
        policy.IsRetryable(new InvalidOperationException()).Should().BeFalse();
    }

    /// <summary>
    /// Verifies that RecordRetryAttempt increments TotalRetryAttempts counter.
    /// </summary>
    [Fact]
    public void RecordRetryAttempt_IncrementsCounter()
    {
        // Arrange
        var policy = new RetryPolicy("counter-test")
        {
            MaxRetries = 3
        };

        // Act
        policy.RecordRetryAttempt();
        policy.RecordRetryAttempt();

        // Assert
        policy.TotalRetryAttempts.Should().Be(2);
    }

    /// <summary>
    /// Verifies that GetNextDelayMs returns correct delay in milliseconds.
    /// </summary>
    [Fact]
    public void GetNextDelayMs_ReturnsCorrectMilliseconds()
    {
        // Arrange
        var policy = new RetryPolicy("ms-test")
        {
            Strategy = RetryPolicy.BackoffStrategy.Exponential,
            InitialDelay = TimeSpan.FromMilliseconds(100),
            BackoffMultiplier = 2.0,
            MaxRetries = 3,
            UseJitter = false
        };

        // Act
        var delay0 = policy.GetNextDelayMs(0);
        var delay1 = policy.GetNextDelayMs(1);

        // Assert
        delay0.Should().Be(100);
        delay1.Should().Be(200);
    }

    /// <summary>
    /// Verifies that TotalRetryAttempts is initially zero.
    /// </summary>
    [Fact]
    public void TotalRetryAttempts_InitiallyZero()
    {
        // Arrange
        var policy = new RetryPolicy("initial-zero");

        // Assert
        policy.TotalRetryAttempts.Should().Be(0);
    }
}
