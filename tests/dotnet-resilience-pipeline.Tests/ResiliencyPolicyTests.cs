using System;
using System.Collections.Generic;
using FluentAssertions;
using Xunit;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Tests;

public class ResiliencyPolicyTests
{
    [Fact]
    public void Constructor_WithValidName_CreatesInstance()
    {
        // Act
        var policy = new TestResiliencyPolicy("TestPolicy");

        // Assert
        policy.Should().NotBeNull();
        policy.Name.Should().Be("TestPolicy");
        policy.Id.Should().NotBeNullOrEmpty();
        policy.IsEnabled.Should().BeTrue();
        policy.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        policy.ModifiedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        policy.TotalExecutions.Should().Be(0);
        policy.SuccessfulExecutions.Should().Be(0);
        policy.FailedExecutions.Should().Be(0);
        policy.Tags.Should().NotBeNull().And.BeEmpty();
        policy.Metadata.Should().NotBeNull().And.BeEmpty();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    [InlineData("   ")]
    public void Constructor_WithInvalidName_ThrowsArgumentException(string invalidName)
    {
        // Act
        Action act = () => new TestResiliencyPolicy(invalidName);

        // Assert
        act.Should().Throw<ArgumentException>()
            .WithMessage("*Policy name cannot be empty*");
    }

    [Fact]
    public void Id_IsGeneratedGuid_WhenNotSet()
    {
        // Arrange
        var policy = new TestResiliencyPolicy("TestPolicy");

        // Act - Id is auto-generated in constructor
        var id1 = policy.Id;

        // Assert
        id1.Should().NotBeNullOrEmpty();
        Guid.TryParse(id1, out _).Should().BeTrue();
    }


    [Fact]
    public void RecordSuccess_IncrementsCounters()
    {
        // Arrange
        var policy = new TestResiliencyPolicy("TestPolicy");
        var modifiedBefore = policy.ModifiedAt;

        // Act
        policy.RecordSuccess();

        // Assert
        policy.TotalExecutions.Should().Be(1);
        policy.SuccessfulExecutions.Should().Be(1);
        policy.FailedExecutions.Should().Be(0);
        policy.ModifiedAt.Should().BeAfter(modifiedBefore);
    }

    [Fact]
    public void RecordFailure_IncrementsCounters()
    {
        // Arrange
        var policy = new TestResiliencyPolicy("TestPolicy");
        var modifiedBefore = policy.ModifiedAt;

        // Act
        policy.RecordFailure();

        // Assert
        policy.TotalExecutions.Should().Be(1);
        policy.SuccessfulExecutions.Should().Be(0);
        policy.FailedExecutions.Should().Be(1);
        policy.ModifiedAt.Should().BeAfter(modifiedBefore);
    }

    [Fact]
    public void RecordSuccess_AndFailure_UpdatesCountersCorrectly()
    {
        // Arrange
        var policy = new TestResiliencyPolicy("TestPolicy");

        // Act
        policy.RecordSuccess();
        policy.RecordFailure();
        policy.RecordSuccess();
        policy.RecordFailure();
        policy.RecordFailure();

        // Assert
        policy.TotalExecutions.Should().Be(5);
        policy.SuccessfulExecutions.Should().Be(2);
        policy.FailedExecutions.Should().Be(3);
    }



    [Fact]
    public void GetSuccessRate_WithMixedResults_CalculatesCorrectly()
    {
        // Arrange
        var policy = new TestResiliencyPolicy("TestPolicy");
        policy.RecordSuccess();
        policy.RecordFailure();
        policy.RecordSuccess();
        policy.RecordFailure();
        policy.RecordFailure();

        // Act
        var successRate = policy.GetSuccessRate();

        // Assert
        successRate.Should().Be(40);
    }

    [Fact]
    public void ResetStatistics_ResetsAllCounters()
    {
        // Arrange
        var policy = new TestResiliencyPolicy("TestPolicy");
        policy.RecordSuccess();
        policy.RecordFailure();
        policy.RecordSuccess();
        policy.TotalExecutions.Should().Be(3);
        policy.SuccessfulExecutions.Should().Be(2);
        policy.FailedExecutions.Should().Be(1);

        // Act
        policy.ResetStatistics();

        // Assert
        policy.TotalExecutions.Should().Be(0);
        policy.SuccessfulExecutions.Should().Be(0);
        policy.FailedExecutions.Should().Be(0);
        policy.ModifiedAt.Should().BeAfter(policy.CreatedAt);
    }

    [Fact]
    public void ResetStatistics_MultipleTimes_WorksCorrectly()
    {
        // Arrange
        var policy = new TestResiliencyPolicy("TestPolicy");
        policy.RecordSuccess();

        // Act
        policy.ResetStatistics();
        policy.ResetStatistics();

        // Assert
        policy.TotalExecutions.Should().Be(0);
        policy.SuccessfulExecutions.Should().Be(0);
        policy.FailedExecutions.Should().Be(0);
    }

    [Fact]
    public void GetSnapshot_ReturnsCorrectSnapshot()
    {
        // Arrange
        var policy = new TestResiliencyPolicy("TestPolicy");
        policy.RecordSuccess();
        policy.RecordFailure();
        policy.IsEnabled = false;
        policy.Tags.Add("tag1");
        policy.Tags.Add("tag2");
        policy.Metadata.Add("key1", "value1");
        policy.Metadata.Add("key2", 123);

        // Act
        var snapshot = policy.GetSnapshot();

        // Assert
        snapshot.PolicyId.Should().Be(policy.Id);
        snapshot.PolicyName.Should().Be("TestPolicy");
        snapshot.PolicyType.Should().Be("TestResiliencyPolicy");
        snapshot.IsEnabled.Should().BeFalse();
        snapshot.TotalExecutions.Should().Be(2);
        snapshot.SuccessfulExecutions.Should().Be(1);
        snapshot.FailedExecutions.Should().Be(1);
        snapshot.SuccessRate.Should().Be(50);
        snapshot.SnapshotTime.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
        snapshot.Metadata.Should().BeNull();
    }


    [Fact]
    public void Tags_CanBeModified()
    {
        // Arrange
        var policy = new TestResiliencyPolicy("TestPolicy");

        // Act
        policy.Tags.Add("production");
        policy.Tags.Add("critical");
        policy.Tags.Remove("critical");

        // Assert
        policy.Tags.Should().HaveCount(1);
        policy.Tags.Should().Contain("production");
    }

    [Fact]
    public void Metadata_CanBeModified()
    {
        // Arrange
        var policy = new TestResiliencyPolicy("TestPolicy");

        // Act
        policy.Metadata.Add("timeout", 5000);
        policy.Metadata.Add("retries", 3);
        policy.Metadata.Remove("retries");
        policy.Metadata["timeout"] = 10000;

        // Assert
        policy.Metadata.Should().HaveCount(1);
        policy.Metadata.Should().ContainKey("timeout").WhoseValue.Should().Be(10000);
    }

    [Fact]
    public void IsEnabled_CanBeToggled()
    {
        // Arrange
        var policy = new TestResiliencyPolicy("TestPolicy");
        policy.IsEnabled.Should().BeTrue();

        // Act
        policy.IsEnabled = false;

        // Assert
        policy.IsEnabled.Should().BeFalse();
    }

    [Fact]
    public void Name_CanBeModified()
    {
        // Arrange
        var policy = new TestResiliencyPolicy("TestPolicy");
        policy.Name.Should().Be("TestPolicy");

        // Act
        policy.Name = "NewPolicyName";

        // Assert
        policy.Name.Should().Be("NewPolicyName");
    }

    // Test class to allow testing the abstract base class
    private sealed class TestResiliencyPolicy : ResiliencyPolicy
    {
        public TestResiliencyPolicy(string name) : base(name)
        {
        }
    }
}