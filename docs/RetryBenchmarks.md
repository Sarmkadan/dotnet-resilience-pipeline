# RetryBenchmarks

The `RetryBenchmarks` class provides a set of benchmark methods for measuring the performance of common operations exposed by the `RetryPolicy` class. It is designed to be used with benchmarking frameworks such as BenchmarkDotNet. The benchmarks cover delay calculation for fixed, exponential, and exponential-with-jitter backoff strategies, retryability checks, and retrieval of policy configuration values. This class is intended for performance evaluation and should not be used in production code paths.

## API

### `public void Setup()`

Initializes the internal `RetryPolicy` instance and any required state before benchmarks are executed. This method must be called once before any other member of the class is invoked.

- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: None.

### `public void RetryPolicy_Fixed_Strategy()`

Benchmarks the execution of a retry operation that uses a fixed backoff strategy. Measures the overhead of applying the policy to a delegate that succeeds after a configurable number of attempts.

- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: None.

### `public void RetryPolicy_Exponential_Strategy()`

Benchmarks the execution of a retry operation that uses an exponential backoff strategy. Measures the overhead of applying the policy to a delegate that succeeds after a configurable number of attempts.

- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: None.

### `public void RetryPolicy_ExponentialWithJitter_Strategy()`

Benchmarks the execution of a retry operation that uses an exponential backoff strategy with added jitter. Measures the overhead of applying the policy to a delegate that succeeds after a configurable number of attempts.

- **Parameters**: None.
- **Return value**: `void`.
- **Exceptions**: None.

### `public long RetryPolicy_CalculateDelay_Fixed()`

Calculates the delay for the next retry attempt using the fixed backoff strategy and returns the computed delay value. This benchmark isolates the delay calculation logic from the full retry loop.

- **Parameters**: None.
- **Return value**: `long` – The calculated delay in milliseconds.
- **Exceptions**: None.

### `public long RetryPolicy_CalculateDelay_Exponential()`

Calculates the delay for the next retry attempt using the exponential backoff strategy and returns the computed delay value.

- **Parameters**: None.
- **Return value**: `long` – The calculated delay in milliseconds.
- **Exceptions**: None.

### `public long RetryPolicy_CalculateDelay_ExponentialWithJitter()`

Calculates the delay for the next retry attempt using the exponential backoff strategy with jitter and returns the computed delay value.

- **Parameters**: None.
- **Return value**: `long` – The calculated delay in milliseconds.
- **Exceptions**: None.

### `public bool RetryPolicy_IsRetryable()`

Determines whether the current exception or result is considered retryable according to the configured policy. This benchmark measures the cost of the retryability check.

- **Parameters**: None.
- **Return value**: `bool` – `true` if the operation should be retried; otherwise `false`.
- **Exceptions**: None.

### `public RetryPolicy.BackoffStrategy RetryPolicy_Get_Strategy()`

Returns the backoff strategy configured for the internal `RetryPolicy` instance. This benchmark measures the cost of accessing the strategy property.

- **Parameters**: None.
- **Return value**: `RetryPolicy.BackoffStrategy` – The current backoff strategy (e.g., `Fixed`, `Exponential`, `ExponentialWithJitter`).
- **Exceptions**: None.

### `public int RetryPolicy_Get_MaxRetries()`

Returns the maximum number of retry attempts configured for the internal `RetryPolicy` instance. This benchmark measures the cost of accessing the maximum retries property.

- **Parameters**: None.
- **Return value**: `int` – The maximum number of retry attempts.
- **Exceptions**: None.

## Usage

The following examples demonstrate how to use the `RetryBenchmarks` class in a benchmarking context and for manual verification.

### Example 1: Running benchmarks with BenchmarkDotNet

```csharp
using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Running;
using DotNetResiliencePipeline;

public class Program
{
    public static void Main(string[] args)
    {
        var summary = BenchmarkRunner.Run<RetryBenchmarks>();
    }
}
```

### Example 2: Manual invocation for verification

```csharp
using DotNetResiliencePipeline;

public class RetryBenchmarksVerification
{
    public static void Verify()
    {
        var benchmarks = new RetryBenchmarks();
        benchmarks.Setup();

        // Calculate delays
        long fixedDelay = benchmarks.RetryPolicy_CalculateDelay_Fixed();
        long expDelay = benchmarks.RetryPolicy_CalculateDelay_Exponential();
        long expJitterDelay = benchmarks.RetryPolicy_CalculateDelay_ExponentialWithJitter();

        // Check retryability
        bool canRetry = benchmarks.RetryPolicy_IsRetryable();

        // Retrieve configuration
        var strategy = benchmarks.RetryPolicy_Get_Strategy();
        int maxRetries = benchmarks.RetryPolicy_Get_MaxRetries();

        Console.WriteLine($"Fixed delay: {fixedDelay} ms");
        Console.WriteLine($"Exponential delay: {expDelay} ms");
        Console.WriteLine($"Exponential with jitter delay: {expJitterDelay} ms");
        Console.WriteLine($"Is retryable: {canRetry}");
        Console.WriteLine($"Strategy: {strategy}");
        Console.WriteLine($"Max retries: {maxRetries}");
    }
}
```

## Notes

- **Setup requirement**: The `Setup` method must be called exactly once before any other member is used. Calling other methods without prior setup will result in undefined behavior (likely a `NullReferenceException` if the internal policy is not initialized).
- **Thread safety**: Instances of `RetryBenchmarks` are not thread-safe. Each benchmark or verification should be performed on a single thread. Concurrent access to the same instance may produce incorrect results or exceptions.
- **Edge cases in delay calculation**: The `CalculateDelay` methods may return zero for the first retry attempt if the policy defines no initial delay. For exponential strategies, the delay can grow large; the returned value is subject to any configured maximum delay cap.
- **Retryability check**: The `RetryPolicy_IsRetryable` method evaluates the last captured exception or result. If no operation has been attempted, the behavior is undefined. In a benchmark context, the internal state is typically set up to simulate a retryable failure.
- **Benchmark isolation**: The benchmark methods (`RetryPolicy_Fixed_Strategy`, `RetryPolicy_Exponential_Strategy`, `RetryPolicy_ExponentialWithJitter_Strategy`) execute a full retry loop and are intended for measuring end-to-end overhead. They should not be used in production logic.
