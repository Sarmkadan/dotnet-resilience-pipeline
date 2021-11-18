# RetryPolicyExtensions

The `RetryPolicyExtensions` static class provides extension methods for the `RetryPolicy` type, enabling convenient configuration of retryable exceptions, execution of actions with automatic retry, and management of policy state. These methods allow developers to fluently build and use retry policies without manually manipulating the underlying exception lists or execution loops.

## API

### `AddRetryableException<TException>`

Adds a specific exception type to the set of exceptions that trigger a retry.

- **Parameters**:  
  `this RetryPolicy policy` – The policy to modify.  
  `TException` is a type parameter constrained to `Exception`.

- **Returns**:  
  The same `RetryPolicy` instance for chaining.

- **Throws**:  
  `ArgumentNullException` if `policy` is `null`.  
  `InvalidOperationException` if the policy is in a state that does not allow modification (e.g., after execution has started).

### `AddRetryableExceptions`

Adds multiple exception types to the set of retryable exceptions.

- **Parameters**:  
  `this RetryPolicy policy` – The policy to modify.  
  `params Type[] exceptionTypes` – One or more exception types to add.

- **Returns**:  
  The same `RetryPolicy` instance for chaining.

- **Throws**:  
  `ArgumentNullException` if `policy` is `null` or `exceptionTypes` is `null`.  
  `ArgumentException` if any type in `exceptionTypes` does not derive from `Exception`.  
  `InvalidOperationException` if the policy cannot be modified.

### `RemoveRetryableException<TException>`

Removes a specific exception type from the set of retryable exceptions.

- **Parameters**:  
  `this RetryPolicy policy` – The policy to modify.  
  `TException` is a type parameter constrained to `Exception`.

- **Returns**:  
  The same `RetryPolicy` instance for chaining.

- **Throws**:  
  `ArgumentNullException` if `policy` is `null`.  
  `InvalidOperationException` if the policy cannot be modified.

### `ClearRetryableExceptions`

Removes all exception types from the set of retryable exceptions, effectively disabling retry on any exception.

- **Parameters**:  
  `this RetryPolicy policy` – The policy to clear.

- **Returns**:  
  The same `RetryPolicy` instance for chaining.

- **Throws**:  
  `ArgumentNullException` if `policy` is `null`.  
  `InvalidOperationException` if the policy cannot be modified.

### `ExecuteWithRetry`

Executes a synchronous action with retry logic defined by the policy.

- **Parameters**:  
  `this RetryPolicy policy` – The policy governing retry behavior.  
  `Action action` – The action to execute.

- **Returns**:  
  `true` if the action succeeded without throwing a retryable exception; `false` if all retries were exhausted and the last exception was retryable.

- **Throws**:  
  `ArgumentNullException` if `policy` or `action` is `null`.  
  Any non‑retryable exception thrown by `action` is propagated immediately.

### `ExecuteWithRetry<T>`

Executes a synchronous function with retry logic and returns its result.

- **Parameters**:  
  `this RetryPolicy policy` – The policy governing retry behavior.  
  `Func<T> func` – The function to execute.

- **Returns**:  
  The value returned by `func` if it succeeds within the retry limits.

- **Throws**:  
  `ArgumentNullException` if `policy` or `func` is `null`.  
  The last retryable exception if all retries are exhausted.  
  Any non‑retryable exception thrown by `func` is propagated immediately.

### `ExecuteWithRetryAsync`

Executes an asynchronous action with retry logic.

- **Parameters**:  
  `this RetryPolicy policy` – The policy governing retry behavior.  
  `Func<Task> asyncAction` – The asynchronous action to execute.

- **Returns**:  
  A `Task<bool>` that completes with `true` if the action succeeded, or `false` if all retries were exhausted.

- **Throws**:  
  `ArgumentNullException` if `policy` or `asyncAction` is `null`.  
  Any non‑retryable exception thrown by `asyncAction` is propagated.

### `ExecuteWithRetryAsync<T>`

Executes an asynchronous function with retry logic and returns its result.

- **Parameters**:  
  `this RetryPolicy policy` – The policy governing retry behavior.  
  `Func<Task<T>> asyncFunc` – The asynchronous function to execute.

- **Returns**:  
  A `Task<T>` that completes with the result of `asyncFunc` if it succeeds within retry limits.

- **Throws**:  
  `ArgumentNullException` if `policy` or `asyncFunc` is `null`.  
  The last retryable exception if all retries are exhausted.  
  Any non‑retryable exception thrown by `asyncFunc` is propagated.

### `GetConfigurationSummary`

Returns a human‑readable string describing the current retry policy configuration (e.g., retry count, delay, list of retryable exceptions).

- **Parameters**:  
  `this RetryPolicy policy` – The policy to inspect.

- **Returns**:  
  A `string` containing the summary.

- **Throws**:  
  `ArgumentNullException` if `policy` is `null`.

### `Clone`

Creates a deep copy of the retry policy, including its exception list and statistics.

- **Parameters**:  
  `this RetryPolicy policy` – The policy to clone.

- **Returns**:  
  A new `RetryPolicy` instance with the same configuration and current statistics.

- **Throws**:  
  `ArgumentNullException` if `policy` is `null`.

### `ResetStatistics`

Resets all runtime statistics (e.g., retry count, success/failure counters) to their initial values.

- **Parameters**:  
  `this RetryPolicy policy` – The policy whose statistics are to be reset.

- **Returns**:  
  The same `RetryPolicy` instance for chaining.

- **Throws**:  
  `ArgumentNullException` if `policy` is `null`.

## Usage

### Example 1: Configuring and using a retry policy for a database operation

```csharp
using ResiliencePipeline;

var policy = new RetryPolicy
{
    RetryCount = 3,
    RetryDelay = TimeSpan.FromMilliseconds(200)
};

// Configure retryable exceptions
policy
    .AddRetryableException<SqlException>()
    .AddRetryableException<TimeoutException>();

// Execute a database query with retry
bool success = policy.ExecuteWithRetry(() =>
{
    using var connection = new SqlConnection(connectionString);
    connection.Open();
    // perform query
});

if (!success)
{
    Console.WriteLine("Operation failed after all retries.");
}
```

### Example 2: Async execution with result and cloning

```csharp
using ResiliencePipeline;

var basePolicy = new RetryPolicy
{
    RetryCount = 5,
    RetryDelay = TimeSpan.FromSeconds(1)
};

// Clone the policy for a specific call to avoid shared statistics
var clonedPolicy = basePolicy.Clone();
clonedPolicy.AddRetryableException<HttpRequestException>();

try
{
    var result = await clonedPolicy.ExecuteWithRetryAsync(async () =>
    {
        var response = await httpClient.GetAsync("https://api.example.com/data");
        response.EnsureSuccessStatusCode();
        return await response.Content.ReadAsStringAsync();
    });
    Console.WriteLine($"Result: {result}");
}
catch (Exception ex)
{
    Console.WriteLine($"Non‑retryable exception: {ex.Message}");
}

// Reset statistics for reuse
clonedPolicy.ResetStatistics();
```

## Notes

- **Thread safety**: `RetryPolicy` instances are not inherently thread‑safe. Concurrent modification (e.g., calling `AddRetryableException` while another thread executes `ExecuteWithRetry`) may lead to undefined behavior. Use synchronization or clone the policy for each execution context if concurrent access is required.
- **Modification after execution**: Once a policy has been used for execution, some implementations may disallow further configuration changes. The `InvalidOperationException` thrown by configuration methods reflects this constraint. Always configure the policy before first use, or clone it to obtain a mutable copy.
- **Exception type matching**: When adding or removing exception types, the policy typically matches the exact type or derived types. For example, adding `Exception` would make all exceptions retryable. Use `ClearRetryableExceptions` to reset the list and then add only the desired types.
- **Statistics reset**: `ResetStatistics` does not affect the policy configuration (retry count, delay, exception list). It only resets runtime counters such as total retries performed or last exception. This is useful when reusing a policy across independent operations.
- **Cloning semantics**: `Clone` creates a deep copy, including the list of retryable exceptions and any internal statistics. The cloned policy is independent of the original and can be modified or executed without affecting the source.
