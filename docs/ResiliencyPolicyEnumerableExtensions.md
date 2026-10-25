# ResiliencyPolicyEnumerableExtensions

The `ResiliencyPolicyEnumerableExtensions` class provides LINQ-style extension methods for collections of `ResiliencyPolicy` objects in the `DotNetResiliencePipeline.Domain.Policies` namespace. It is a `public static` class and is defined in `src/Domain/Policies/ResiliencyPolicyEnumerableExtensions.cs`.

## API

### WhereEnabled

```csharp
public static IEnumerable<ResiliencyPolicy> WhereEnabled(this IEnumerable<ResiliencyPolicy> policies)
```

Filters a collection of policies so that only policies whose `IsEnabled` property is `true` are returned. Policies with `IsEnabled == false` are skipped.

**Parameters:**
- `policies`: The collection of policies to filter.

**Returns:**
- An `IEnumerable<ResiliencyPolicy>` that yields only the enabled policies, in their original order.

**Exceptions:**
- `ArgumentNullException`: Thrown immediately, when the method is called, if `policies` is null.

**Behavior notes:**
- The null check runs eagerly, but the filtering is deferred. The `Where` query is not evaluated until the result is enumerated, so changes to `IsEnabled` made before enumeration are reflected in the output. Materialize the result with `ToList()` or `ToArray()` to take a snapshot.
- The method does not copy or modify the policies. It only reads `IsEnabled`.
- A `null` element inside `policies` causes a `NullReferenceException` during enumeration.

**Example:**

```csharp
using System.Collections.Generic;
using DotNetResiliencePipeline.Domain.Policies;

var retry = new RetryPolicy { MaxRetries = 3 };
var breaker = new CircuitBreakerPolicy("payments") { IsEnabled = false };

IEnumerable<ResiliencyPolicy> policies = new ResiliencyPolicy[] { retry, breaker };

// Only the retry policy is returned, because the circuit breaker is disabled.
List<ResiliencyPolicy> active = policies.WhereEnabled().ToList();
```

## Related

- [RetryPolicy](RetryPolicy.md)
- [CircuitBreakerPolicy](CircuitBreakerPolicy.md)
