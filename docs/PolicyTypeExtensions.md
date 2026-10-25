# PolicyTypeExtensions

Static class of extension methods for the `PolicyType` enum. It lives in the `DotNetResiliencePipeline.Constants` namespace, in `src/Constants/PolicyTypeExtensions.cs`. The enum itself is declared in `src/Constants/ResiliencyConstants.cs`.

Use it when you need to show a policy type to a person (logs, dashboards, CLI output) as a readable label instead of the raw enum name.

## API

### ToDisplayName

```csharp
public static string ToDisplayName(this PolicyType policyType)
```

Converts a policy type to its human-readable display name.

**Parameters:**
- `policyType`: The policy type to convert.

**Returns:** The display name for the policy type. Known values map to the labels in the table below. Any other value falls back to `policyType.ToString()`.

**Exceptions:** None. The method does not throw.

## Mapping Table

| `PolicyType` value | `ToDisplayName()` result |
|--------------------|--------------------------|
| `PolicyType.CircuitBreaker` | `"Circuit Breaker"` |
| `PolicyType.Bulkhead` | `"Bulkhead"` |
| `PolicyType.Retry` | `"Retry"` |
| `PolicyType.Timeout` | `"Timeout"` |
| `PolicyType.Fallback` | `"Fallback"` |
| any other value (for example `(PolicyType)99`) | `policyType.ToString()`, which is the number as text (`"99"`) for a value with no name |

The enum declares exactly these five members, so the fallback branch only runs for values cast from integers that do not match a member.

## Usage

### Basic conversion

```csharp
using DotNetResiliencePipeline.Constants;

string label = PolicyType.CircuitBreaker.ToDisplayName();
// label == "Circuit Breaker"
```

### Building a log message

```csharp
using DotNetResiliencePipeline.Constants;

PolicyType failed = PolicyType.Timeout;
Console.WriteLine($"{failed.ToDisplayName()} policy exceeded its limit.");
// Output: Timeout policy exceeded its limit.
```

### Iterating over all known types

```csharp
using DotNetResiliencePipeline.Constants;

foreach (PolicyType type in Enum.GetValues<PolicyType>())
{
    Console.WriteLine($"{(int)type,2}  {type,-15} -> {type.ToDisplayName()}");
}
// 0  CircuitBreaker  -> Circuit Breaker
// 1  Bulkhead        -> Bulkhead
// 2  Retry           -> Retry
// 3  Timeout         -> Timeout
// 4  Fallback        -> Fallback
```

### Fallback for an undefined value

```csharp
using DotNetResiliencePipeline.Constants;

string unknown = ((PolicyType)99).ToDisplayName();
// unknown == "99"
```

## Remarks

- The method has no state. Known values return string literals from the `switch` expression.
- The labels are defined only in `ToDisplayName`. To change one, edit `src/Constants/PolicyTypeExtensions.cs`, and update the table above to match.
- Display names are for presentation only. Do not parse them back into `PolicyType` values; use the enum directly.
