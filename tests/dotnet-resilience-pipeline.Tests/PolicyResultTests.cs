#nullable enable
using DotNetResiliencePipeline.Domain;
using DotNetResiliencePipeline.Domain.Policies;
using FluentAssertions;
using Xunit;

namespace DotNetResiliencePipeline.Tests;

public sealed class PolicyResultTests
{
    [Fact]
    public void Success_SetsIsSuccessTrueWithData()
    {
        var result = PolicyResult<string>.Success("hello", "my-policy", 42, attempts: 1);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("hello");
        result.PolicyName.Should().Be("my-policy");
        result.ExecutionTimeMs.Should().Be(42);
        result.AttemptCount.Should().Be(1);
        result.Exception.Should().BeNull();
    }

    [Fact]
    public void Failure_SetsIsSuccessFalseWithException()
    {
        var ex = new InvalidOperationException("boom");
        var result = PolicyResult<string>.Failure(ex, "fail-policy", 100, attempts: 2);

        result.IsSuccess.Should().BeFalse();
        result.Data.Should().BeNull();
        result.Exception.Should().BeSameAs(ex);
        result.PolicyName.Should().Be("fail-policy");
        result.AttemptCount.Should().Be(2);
    }

    [Fact]
    public void Fallback_SetsIsSuccessTrueAndFallbackMetadata()
    {
        var primaryEx = new TimeoutException("primary");
        var result = PolicyResult<string>.Fallback("fallback-value", primaryEx, "fallback-policy", 200);

        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("fallback-value");
        result.Exception.Should().BeSameAs(primaryEx);
        result.Metadata.Should().ContainKey("FallbackUsed");
        result.Metadata["FallbackUsed"].Should().Be(true);
    }

    [Fact]
    public void OnSuccess_CalledWhenSuccess()
    {
        var result = PolicyResult<int>.Success(99, "policy", 10);
        int captured = 0;

        result.OnSuccess(v => captured = v);

        captured.Should().Be(99);
    }

    [Fact]
    public void OnSuccess_NotCalledWhenFailure()
    {
        var result = PolicyResult<int>.Failure(new Exception("err"), "policy", 10);
        bool called = false;

        result.OnSuccess(_ => called = true);

        called.Should().BeFalse();
    }

    [Fact]
    public void OnFailure_CalledWhenFailure()
    {
        var ex = new ArgumentException("bad");
        var result = PolicyResult<int>.Failure(ex, "policy", 10);
        Exception? captured = null;

        result.OnFailure(e => captured = e);

        captured.Should().BeSameAs(ex);
    }

    [Fact]
    public void OnFailure_NotCalledWhenSuccess()
    {
        var result = PolicyResult<int>.Success(1, "policy", 10);
        bool called = false;

        result.OnFailure(_ => called = true);

        called.Should().BeFalse();
    }

    [Fact]
    public void Map_OnSuccess_TransformsData()
    {
        var result = PolicyResult<int>.Success(5, "policy", 20);

        var mapped = result.Map(v => v.ToString());

        mapped.IsSuccess.Should().BeTrue();
        mapped.Data.Should().Be("5");
        mapped.PolicyName.Should().Be("policy");
        mapped.ExecutionTimeMs.Should().Be(20);
    }

    [Fact]
    public void Map_OnFailure_PropagatesFailure()
    {
        var ex = new Exception("fail");
        var result = PolicyResult<int>.Failure(ex, "policy", 30);

        var mapped = result.Map(v => v.ToString());

        mapped.IsSuccess.Should().BeFalse();
        mapped.Exception.Should().BeSameAs(ex);
    }

    [Fact]
    public void Success_HasUniqueExecutionId()
    {
        var r1 = PolicyResult<string>.Success("a", "p", 1);
        var r2 = PolicyResult<string>.Success("b", "p", 1);

        r1.ExecutionId.Should().NotBe(r2.ExecutionId);
    }

    [Fact]
    public void Success_ExecutedAtIsRecentUtc()
    {
        var before = DateTime.UtcNow.AddSeconds(-1);
        var result = PolicyResult<string>.Success("x", "p", 0);
        var after = DateTime.UtcNow.AddSeconds(1);

        result.ExecutedAt.Should().BeAfter(before);
        result.ExecutedAt.Should().BeBefore(after);
    }

    [Fact]
    public void Failure_DefaultAttemptCountIsOne()
    {
        var result = PolicyResult<string>.Failure(new Exception(), "p", 0);

        result.AttemptCount.Should().Be(1);
    }
}
