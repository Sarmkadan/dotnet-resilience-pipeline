// ... existing content ...

## PolicyResultTests

The `PolicyResultTests` class provides unit tests for the `PolicyResult` class, verifying its behavior when handling policy execution outcomes, including success, failure, and fallback scenarios. These tests cover various properties and methods of the `PolicyResult` class, ensuring its correctness and reliability.

Here's a realistic usage example based on its public members:

```csharp
using DotNetResiliencePipeline.Domain;

public class PolicyResultUsage
{
    public void RunExamples()
    {
        // Verifies: Success_SetsIsSuccessTrueWithData
        var result = PolicyResult<string>.Success("hello", "my-policy", 42, attempts: 1);
        result.IsSuccess.Should().BeTrue();
        result.Data.Should().Be("hello");
        result.PolicyName.Should().Be("my-policy");
        result.ExecutionTimeMs.Should().Be(42);
        result.AttemptCount.Should().Be(1);
        result.Exception.Should().BeNull();

        // Verifies: Failure_SetsIsSuccessFalseWithException
        var ex = new InvalidOperationException("boom");
        var failureResult = PolicyResult<string>.Failure(ex, "fail-policy", 100, attempts: 2);
        failureResult.IsSuccess.Should().BeFalse();
        failureResult.Data.Should().BeNull();
        failureResult.Exception.Should().BeSameAs(ex);
        failureResult.PolicyName.Should().Be("fail-policy");
        failureResult.AttemptCount.Should().Be(2);

        // Verifies: Fallback_SetsIsSuccessTrueAndFallbackMetadata
        var primaryEx = new TimeoutException("primary");
        var fallbackResult = PolicyResult<string>.Fallback("fallback-value", primaryEx, "fallback-policy", 200);
        fallbackResult.IsSuccess.Should().BeTrue();
        fallbackResult.Data.Should().Be("fallback-value");
        fallbackResult.Exception.Should().BeSameAs(primaryEx);
        fallbackResult.Metadata.Should().ContainKey("FallbackUsed");
        fallbackResult.Metadata["FallbackUsed"].Should().Be(true);
    }
}
```

// ... rest of existing content
```