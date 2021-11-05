# ThrottlingHelper

The `ThrottlingHelper` type provides a lightweight, thread‑safe mechanism for applying rate‑limiting (throttling) to asynchronous operations. It maintains a collection of named throttles, each tracking request counts, allowed versus throttled calls, and token‑bucket statistics, and exposes helper methods to evaluate whether a call should be throttled, retrieve statistics, and execute work under a throttling policy.

## API

### Throttle GetOrCreateThrottle()
Returns the `Throttle` instance associated with the helper. If no throttle has been created yet, a default throttle is instantiated and stored.  
**Return value:** A `Throttle` object that can be used to query state or invoke throttling logic.  
**Throws:** `InvalidOperationException` if the helper has been disposed or is in an invalid state.

### bool ShouldThrottle()
Evaluates whether the next call should be throttled based on the current token bucket state.  
**Return value:** `true` if the call would exceed the allowed rate and should be throttled; otherwise `false`.  
**Throws:** `ObjectDisposedException` if the helper has been disposed.

### ThrottleStatistics GetStatistics()
Retrieves the statistics for the default throttle managed by this helper.  
**Return value:** A `ThrottleStatistics` snapshot containing request counts, rates, and token availability.  
**Throws:** `InvalidOperationException` if no throttle has been created or the helper is disposed.

### Dictionary<string, ThrottleStatistics> GetAllStatistics()
Returns a read‑only dictionary mapping throttle identifiers to their respective `ThrottleStatistics` objects.  
**Return value:** A dictionary where each key is a throttle name and each value is a statistics snapshot.  
**Throws:** `ObjectDisposedException` if the helper has been disposed.

### void ResetThrottle()
Resets the internal state of the default throttle to its initial configuration (token bucket refilled, counters cleared).  
**Return value:** None.  
**Throws:** `ObjectDisposedException` if the helper has been disposed.

### void Clear()
Removes all throttles maintained by the helper and clears internal counters. After calling this method, subsequent calls to `GetOrCreateThrottle` will create new throttles.  
**Return value:** None.  
**Throws:** `ObjectDisposedException` if the helper has been disposed.

### Throttle Throttle { get; }
Gets the `Throttle` instance currently associated with the helper. This property is equivalent to calling `GetOrCreateThrottle()` but does not create a new throttle if none exists.  
**Return value:** The existing `Throttle` object, or `null` if no throttle has been created.  
**Throws:** None.

### bool IsAllowed { get; }
Indicates whether the most recent evaluation (`ShouldThrottle`) determined that the call was allowed.  
**Return value:** `true` if the last check permitted the call; otherwise `false`.  
**Throws:** `ObjectDisposedException` if the helper has been disposed.

### ThrottleStatistics GetStatistics()
(Duplicate entry – identical behavior to the earlier `GetStatistics` method.)  
**Return value:** A `ThrottleStatistics` snapshot for the default throttle.  
**Throws:** `InvalidOperationException` if no throttle exists or the helper is disposed.

### string? PolicyName { get; }
Gets the optional name assigned to the throttling policy represented by this helper.  
**Return value:** The policy name, or `null` if no name was set.  
**Throws:** None.

### int MaxRate { get; }
Gets the maximum number of requests permitted per time unit (e.g., per second) for the throttle.  
**Return value:** An integer representing the configured maximum rate.  
**Throws:** None.

### long TotalRequests { get; }
Gets the cumulative count of all requests that have been evaluated by the throttle since its creation or last reset.  
**Return value:** A 64‑bit integer total request count.  
**Throws:** None.

### long AllowedRequests { get; }
Gets the count of requests that were allowed to proceed (i.e., not throttled).  
**Return value:** A 64‑bit integer of allowed requests.  
**Throws:** None.

### long ThrottledRequests { get; }
Gets the count of requests that were blocked due to exceeding the rate limit.  
**Return value:** A 64‑bit integer of throttled requests.  
**Throws:** None.

### double ThrottleRate { get; }
Gets the current effective throttling rate, calculated as the ratio of allowed requests to total requests over the measurement window.  
**Return value:** A double between 0 and 1 representing the observed throttle rate.  
**Throws:** None.

### int AvailableTokens { get; }
Gets the number of tokens currently available in the token bucket, indicating how many additional requests can be allowed without throttling.  
**Return value:** An integer token count.  
**Throws:** None.

### int BurstCapacity { get; }
Gets the maximum burst capacity of the token bucket – the maximum number of tokens that can be accumulated.  
**Return value:** An integer representing the burst limit.  
**Throws:** None.

### static async Task<T> ExecuteWithThrottlingAsync<T>(Func<Task<T>> operation)
Executes the supplied asynchronous operation only if the throttle permits it; otherwise, the operation is delayed until tokens become available.  
**Parameters:**  
- `operation`: A delegate returning a `Task<T>` that represents the work to be throttled.  
**Return value:** A `Task<T>` that completes with the result of `operation`.  
**Throws:**  
- `ObjectDisposedException` if the helper has been disposed.  
- Any exception thrown by `operation` is propagated unchanged.

### static async Task ExecuteWithThrottlingAsync(Func<Task> operation)
Executes the supplied asynchronous operation under the same throttling rules as the generic overload, but without a return value.  
**Parameters:**  
- `operation`: A delegate returning a `Task` that represents the work to be throttled.  
**Return value:** A `Task` that completes when `operation` finishes.  
**Throws:**  
- `ObjectDisposedException` if the helper has been disposed.  
- Any exception thrown by `operation` is propagated unchanged.

## Usage

### Example 1: Manual throttle checks
```csharp
var throttlingHelper = new ThrottlingHelper(); // assumes default constructor

// Obtain or create a throttle for a specific policy (name could be set via constructor)
Throttle throttle = throttlingHelper.GetOrCreateThrottle();

if (!throttlingHelper.ShouldThrottle())
{
    // Proceed with the work because capacity is available
    DoWork();
}
else
{
    // Handle throttling – e.g., log, retry later, or reject the request
    LogThrottleEvent();
}
```

### Example 2: Using the static execution helper
```csharp
async Task ProcessMessageAsync()
{
    // Simulate some asynchronous work
    await Task.Delay(10);
    Console.WriteLine("Message processed");
}

// Execute the work with throttling applied globally
await ThrottlingHelper.ExecuteWithThrottlingAsync(ProcessMessageAsync);
```

## Notes
- All instance members of `ThrottlingHelper` are thread‑safe; concurrent calls from multiple threads will not corrupt internal state.  
- The static `ExecuteWithThrottlingAsync` methods create a temporary throttling scope; they do not retain state beyond the execution of the supplied delegate.  
- Calling `ResetThrottle` while other threads are evaluating `ShouldThrottle` may cause a brief window where token counts are reset mid‑evaluation, potentially allowing a burst that exceeds the configured rate. For strict rate limiting, ensure no throttling checks are in flight when resetting.  
- The `GetAllStatistics` method returns a snapshot; modifications to the returned dictionary do not affect the helper’s internal state.  
- If the helper is disposed, any attempt to access properties or invoke methods will throw `ObjectDisposedException`.  
- The `PolicyName` property may be `null` if the helper was instantiated without an explicit name; in such cases, throttling statistics are still tracked but cannot be distinguished by name in `GetAllStatistics`.  
- The `AvailableTokens` and `BurstCapacity` values reflect the token‑bucket algorithm; they are updated atomically on each throttling decision.  
- Throttling statistics (`TotalRequests`, `AllowedRequests`, `ThrottledRequests`) are monotonic counters and only decrease when `ResetThrottle` or `Clear` is invoked.
