# RetryPolicy

The `RetryPolicy` class defines a configurable retry strategy for transient fault handling. It encapsulates the logic for determining whether an operation should be retried, calculating the delay between retries, and tracking retry attempts. The policy supports multiple backoff strategies, jitter, and a configurable list of exception types that trigger a retry.

## API

### `public int MaxRetries`
Gets or sets the maximum number of retry attempts. A value of zero means no retries are performed.  
**Returns:** The maximum retry count.  
**Throws:** Nothing (property access).

### `public TimeSpan InitialDelay`
Gets or sets the initial delay before the first retry.  
**Returns:** The base delay duration.  
**Throws:** Nothing (property access).

### `public BackoffStrategy Strategy`
Gets or sets the backoff strategy used to compute delays. Typical values include `Linear`, `Exponential`, or `Constant`.  
**Returns:** The current backoff strategy.  
**Throws:** Nothing (property access).

### `public TimeSpan MaxDelay`
Gets or sets the maximum allowed delay for any single retry. Delays calculated by the policy are capped at this value.  
**Returns:** The maximum delay.  
**Throws:** Nothing (property access).

### `public double BackoffMultiplier`
Gets or sets the multiplier applied to the delay in exponential backoff strategies.  
**Returns:** The multiplier value.  
**Throws:** Nothing (property access).

### `public bool UseJitter`
Gets or sets whether a random jitter is added to the calculated delay.  
**Returns:** `true` if jitter is enabled; otherwise `false`.  
**Throws:** Nothing (property access).

### `public double JitterFactor`
Gets or sets the factor that controls the magnitude of the jitter. The actual jitter is a random value between zero and `InitialDelay * JitterFactor`.  
**Returns:** The jitter factor (typically between 0.0 and 1.0).  
**Throws:** Nothing (property access).

### `public List<Type> RetryableExceptions`
Gets or sets the list of exception types that are considered retryable. If an exception of a type in this list is thrown, the policy will attempt a retry (subject to `MaxRetries`).  
**Returns:** A mutable list of `System.Type` objects.  
**Throws:** Nothing (property access).

### `public long TotalRetryAttempts`
Gets the total number of retry attempts recorded across all invocations of this policy. This value is incremented each time `RecordRetryAttempt` is called.  
**Returns:** The cumulative retry count.  
**Throws:** Nothing (property access).

### `public RetryPolicy(string name)`
Initializes a new instance of the `RetryPolicy` class with the specified policy name. The base class constructor is invoked.  
**Parameters:**  
- `name` – A string identifier for the policy.  
**Throws:** `ArgumentNullException` if `name` is `null`.

### `public bool IsRetryable`
Gets a value indicating whether the policy is configured to perform retries. Returns `true` when `MaxRetries > 0` and `RetryableExceptions` contains at least one type.  
**Returns:** `true` if retries are possible; otherwise `false`.  
**Throws:** Nothing (property access).

### `public TimeSpan CalculateDelay()`
Calculates the delay for the next retry attempt based on the current configuration and internal retry count. The result is subject to `MaxDelay` and jitter settings.  
**Returns:** A `TimeSpan` representing the delay before the next retry.  
**Throws:** `InvalidOperationException` if the policy configuration is invalid (e.g., `MaxRetries` is negative).

### `public void RecordRetryAttempt()`
Records a retry attempt by incrementing the internal retry counter. This method should be called immediately before executing a retry.  
**Parameters:** None.  
**Returns:** Nothing.  
**Throws:** Nothing.

### `public long GetNextDelayMs()`
Calculates the delay for the next retry attempt in milliseconds. This is equivalent to `CalculateDelay().TotalMilliseconds` but returns a `long` value.  
**Returns:** The delay in milliseconds as a 64-bit integer.  
**Throws:** `InvalidOperationException` if the policy configuration is invalid.

### `public bool IsValidConfiguration()`
Validates the current configuration of the policy. Checks that `MaxRetries` is non-negative, `InitialDelay` and `MaxDelay` are non-negative, `BackoffMultiplier` is positive, and `JitterFactor` is between 0.0 and 1.0.  
**Returns:** `true` if the configuration is valid; otherwise `false`.  
**Throws:** Nothing.

### `public override PolicySnapshot GetSnapshot()`
Creates a snapshot of the current state of the policy, including configuration values and the current retry count.  
**Returns:** A `PolicySnapshot` object containing a copy of the policy’s state.  
**Throws:** Nothing.

## Usage

### Example 1: Basic retry loop with exponential backoff

```csharp
var policy = new RetryPolicy("MyRetryPolicy")
{
    MaxRetries = 3,
    InitialDelay = TimeSpan.FromSeconds(1),
    Strategy = BackoffStrategy.Exponential,
    MaxDelay = TimeSpan.FromSeconds(10),
    BackoffMultiplier = 2.0,
    UseJitter = false,
    RetryableExceptions = new List<Type> { typeof(HttpRequestException) }
};

int attempt = 0;
while (attempt <= policy.MaxRetries)
{
    try
    {
        // Perform the operation
        PerformOperation();
        break; // success
    }
    catch (Exception ex) when (policy.RetryableExceptions.Contains(ex.GetType()))
    {
        attempt++;
        if (attempt > policy.MaxRetries)
            throw; // exhausted retries

        policy.RecordRetryAttempt();
        TimeSpan delay = policy.CalculateDelay();
        Thread.Sleep(delay);
    }
}
```

### Example 2: Using jitter and validating configuration

```csharp
var policy = new RetryPolicy("JitteredPolicy")
{
    MaxRetries = 5,
    InitialDelay = TimeSpan.FromMilliseconds(500),
    Strategy = BackoffStrategy.Linear,
    MaxDelay = TimeSpan.FromSeconds(5),
    UseJitter = true,
    JitterFactor = 0.3,
    RetryableExceptions = new List<Type> { typeof(TimeoutException) }
};

if (!policy.IsValidConfiguration())
{
    Console.WriteLine("Policy configuration is invalid.");
    return;
}

for (int i = 0; i <= policy.MaxRetries; i++)
{
    try
    {
        await PerformAsyncOperation();
        break;
    }
    catch (TimeoutException) when (i < policy.MaxRetries)
    {
        policy.RecordRetryAttempt();
        long delayMs = policy.GetNextDelayMs();
        await Task.Delay((int)delayMs);
    }
}

Console.WriteLine($"Total retry attempts: {policy.TotalRetryAttempts}");
```

## Notes

- **Configuration validation:** The `IsValidConfiguration` method checks basic constraints, but it does not verify that `MaxDelay` is greater than or equal to `InitialDelay`. Setting `MaxDelay` smaller than `InitialDelay` will cause all delays to be capped at `MaxDelay`, which may be unintended.
- **Negative values:** Setting `MaxRetries` to a negative value will cause `IsValidConfiguration` to return `false` and methods like `CalculateDelay` and `GetNextDelayMs` to throw `InvalidOperationException`.
- **Jitter behavior:** When `UseJitter` is `true`, the jitter is added as a random offset between zero and `InitialDelay * JitterFactor`. The jitter is applied *after* the base delay calculation and before the `MaxDelay` cap.
- **Thread safety:** The `RetryPolicy` instance is **not thread-safe**. Properties such as `MaxRetries`, `RetryableExceptions`, and the internal retry counter (`TotalRetryAttempts`) are mutable. Concurrent reads and writes from multiple threads can lead to inconsistent state. If the policy must be shared across threads, external synchronization is required or a new instance should be created per thread.
- **Snapshot isolation:** `GetSnapshot` returns a copy of the current state, so it is safe to call from any thread. However, the snapshot reflects the state at the moment of the call and does not update automatically.
- **Exception type matching:** The `RetryableExceptions` list uses reference equality for `System.Type`. Subclasses of a listed exception type are **not** automatically considered retryable unless explicitly added.
