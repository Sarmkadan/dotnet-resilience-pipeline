# ExceptionExtensions

Provides general-purpose extension methods for any `Exception` instance. The current API is a single helper, `GetInnermost`, which walks the `InnerException` chain to find the root cause of a failure. This is useful when a resilience policy (retry, circuit breaker, timeout, bulkhead, fallback) or a service layer wraps the original error in one or more outer exceptions and you need the underlying exception for logging or classification.

## API

### GetInnermost

```csharp
public static Exception GetInnermost(this Exception exception)
```

Walks the `InnerException` chain of the given exception and returns the deepest exception in it. The method follows `InnerException` only; it does not traverse the `InnerExceptions` collection of an `AggregateException`.

**Parameters:**
- `exception` — The exception to traverse. This is the starting point of the walk and is also the return value when it has no inner exception.

**Return Value:** The innermost exception in the chain, meaning the last exception whose `InnerException` is `null`. If `exception` has no inner exception, the method returns `exception` itself.

**Throws:** `ArgumentNullException` if `exception` is `null`.

## Usage

### Example 1: Logging the Root Cause of a Wrapped Failure

```csharp
try
{
    await SendOrderAsync(order, cancellationToken);
}
catch (Exception ex)
{
    // Outer exceptions often only describe the failed operation;
    // the root cause is usually the most useful line in the log.
    var root = ex.GetInnermost();

    logger.LogError(
        "Operation failed. Root cause: {RootType}: {RootMessage}",
        root.GetType().Name,
        root.Message);

    throw;
}
```

### Example 2: Classifying a Failure by Its Root Cause

```csharp
catch (Exception ex)
{
    var root = ex.GetInnermost();

    // Inspect the root cause rather than the outer wrapper type.
    if (root is HttpRequestException or TimeoutException)
    {
        logger.LogWarning("Transient failure, will be retried: {Message}", root.Message);
        throw;
    }

    logger.LogError(root, "Non-transient failure");
    throw;
}
```

## Notes

- `GetInnermost` never returns `null` for a non-null input. If there is no inner exception, the original exception is returned.
- The method does not modify the exception or its chain. It only reads `InnerException`.
- Because the walk follows `InnerException` links, a cyclic chain would loop forever. .NET exception chains are not cyclic in normal use, so this is rarely a concern.
- `AggregateException` instances created by `Task.WhenAll` or `Parallel` keep their individual failures in `InnerExceptions`, which this method does not inspect. For those, read `InnerExceptions` directly.
- The method is stateless and thread-safe.
