// ... existing content ...


## RetryPolicyTests

The `RetryPolicyTests` class provides comprehensive unit tests for the `RetryPolicy` class, verifying its retry functionality, delay calculations, and configuration validation.

Here's a realistic usage example based on its real public members:

```csharp
using DotNetResiliencePipeline.Domain.Policies;

// Create a retry policy with fixed delay and limited retries
var retryPolicy = new RetryPolicy("order-processing-retry")
{
    Strategy = RetryPolicy.BackoffStrategy.Fixed,
    InitialDelay = TimeSpan.FromSeconds(1),
    MaxRetries = 3
};

// Test the delay calculation
var delay0 = retryPolicy.CalculateDelay(0);
var delay1 = retryPolicy.CalculateDelay(1);
var delay2 = retryPolicy.CalculateDelay(2);

// Assert delay results
Console.WriteLine($"Retry delay 0: {delay0.TotalSeconds} seconds");
Console.WriteLine($"Retry delay 1: {delay1.TotalSeconds} seconds");
Console.WriteLine($"Retry delay 2: {delay2.TotalSeconds} seconds");

// Validate if a retry should be attempted
bool retryable = retryPolicy.IsRetryable(new TimeoutException());
Console.WriteLine($"Is retryable: {retryable}");


bool isValidConfig = retryPolicy.IsValidConfiguration(out var error);
Console.WriteLine($"Is valid config: {isValidConfig}");

// Check retryable for null exception
bool isNullRetryable = retryPolicy.IsRetryable(null);
Console.WriteLine($"Is null retryable: {isNullRetryable}");
```