# TimeoutPolicyValidation

Provides validation helpers for `TimeoutPolicy` instances in the DotNetResiliencePipeline library.

## Overview

The `TimeoutPolicyValidation` static class contains extension methods for validating `TimeoutPolicy` objects. These methods check various properties of the policy against defined business rules and return human-readable error messages when validation fails.

## Methods

### Validate

```csharp
public static IReadOnlyList<string> Validate(this TimeoutPolicy value)
```

Validates a `TimeoutPolicy` instance and returns a list of human-readable validation errors.

- **Parameters**
  - `value`: The policy instance to validate.
- **Returns**
  - An empty list if the policy is valid; otherwise, a list of validation error messages.
- **Exceptions**
  - `ArgumentNullException`: Thrown when `value` is null.

### IsValid

```csharp
public static bool IsValid(this TimeoutPolicy value)
```

Determines whether the specified `TimeoutPolicy` instance is valid.

- **Parameters**
  - `value`: The policy instance to check.
- **Returns**
  - `true` if the policy is valid; otherwise, `false`.
- **Exceptions**
  - `ArgumentNullException`: Thrown when `value` is null.

### EnsureValid

```csharp
public static void EnsureValid(this TimeoutPolicy value)
```

Ensures that the specified `TimeoutPolicy` instance is valid. Throws an exception if validation fails.

- **Parameters**
  - `value`: The policy instance to validate.
- **Exceptions**
  - `ArgumentNullException`: Thrown when `value` is null.
  - `ArgumentException`: Thrown when the policy is invalid, containing all validation errors in the message.

## Validation Rules

The validation checks the following rules:

### Timeout

- Must be a positive time span (greater than `TimeSpan.Zero`).
- Error message: `"Timeout must be a positive time span."`

### TimeoutCount

- Cannot be negative.
- Error message: `"TimeoutCount cannot be negative."`

### AverageExecutionTimeMs

- Must be a valid number (not `NaN` or infinity).
- Cannot be negative.
- Error messages:
  - `"AverageExecutionTimeMs must be a valid number."` (for NaN or infinity)
  - `"AverageExecutionTimeMs cannot be negative."` (for negative values)

### LongestExecutionTimeMs

- Cannot be negative.
- Error message: `"LongestExecutionTimeMs cannot be negative."`

### ShortestExecutionTimeMs

- Cannot be negative.
- If the value is `long.MaxValue`, it indicates no execution times have been recorded yet.
- Error messages:
  - `"ShortestExecutionTimeMs cannot be negative."` (for negative values)
  - `"ShortestExecutionTimeMs has not been initialized with actual execution times."` (when equal to `long.MaxValue`)

### Consistency: Shortest vs Longest

- When both `ShortestExecutionTimeMs` and `LongestExecutionTimeMs` are set (greater than zero), the shortest must not be greater than the longest.
- Error message: `"ShortestExecutionTimeMs cannot be greater than LongestExecutionTimeMs."`

### Consistency: Average vs Longest

- When `AverageExecutionTimeMs` and `LongestExecutionTimeMs` are both positive, the average should not exceed 1.5 times the longest execution time.
- Error message: `"AverageExecutionTimeMs appears inconsistent with recorded execution times (too high compared to LongestExecutionTimeMs)."`

### TotalExecutions

- Cannot be negative.
- Error message: `"TotalExecutions cannot be negative."`

## Error Message Format

When validation fails, each rule violation produces a distinct error string. The `EnsureValid` method combines all errors into a single `ArgumentException` message formatted as:

```
TimeoutPolicy validation failed:
- [error message 1]
- [error message 2]
- [error message 3]
```

## Examples

### Valid Policy

```csharp
var policy = new TimeoutPolicy
{
    Timeout = TimeSpan.FromSeconds(30),
    TimeoutCount = 3,
    AverageExecutionTimeMs = 150,
    LongestExecutionTimeMs = 200,
    ShortestExecutionTimeMs = 100,
    TotalExecutions = 42
};

var errors = policy.Validate();
// errors.Count == 0

bool isValid = policy.IsValid();
// isValid == true

policy.EnsureValid();
// No exception thrown
```

### Invalid Policy (Multiple Errors)

```csharp
var policy = new TimeoutPolicy
{
    Timeout = TimeSpan.Zero, // Invalid: must be positive
    TimeoutCount = -1,       // Invalid: cannot be negative
    AverageExecutionTimeMs = double.NegativeInfinity, // Invalid: not a valid number
    LongestExecutionTimeMs = 100,
    ShortestExecutionTimeMs = 150, // Invalid: shortest > longest
    TotalExecutions = -5   // Invalid: cannot be negative
};

var errors = policy.Validate();
// errors contains:
//   "Timeout must be a positive time span."
//   "TimeoutCount cannot be negative."
//   "AverageExecutionTimeMs must be a valid number."
//   "ShortestExecutionTimeMs cannot be greater than LongestExecutionTimeMs."
//   "TotalExecutions cannot be negative."

bool isValid = policy.IsValid();
// isValid == false

try
{
    policy.EnsureValid();
}
catch (ArgumentException ex)
{
    // ex.Message contains:
    // "TimeoutPolicy validation failed:
    // - Timeout must be a positive time span.
    // - TimeoutCount cannot be negative.
    // - AverageExecutionTimeMs must be a valid number.
    // - ShortestExecutionTimeMs cannot be greater than LongestExecutionTimeMs.
    // - TotalExecutions cannot be negative."
}
```

### Uninitialized Shortest Execution Time

```csharp
var policy = new TimeoutPolicy
{
    Timeout = TimeSpan.FromSeconds(10),
    TimeoutCount = 0,
    AverageExecutionTimeMs = 0,
    LongestExecutionTimeMs = 0,
    ShortestExecutionTimeMs = long.MaxValue, // Indicates no executions recorded
    TotalExecutions = 0
};

var errors = policy.Validate();
// errors contains:
//   "ShortestExecutionTimeMs has not been initialized with actual execution times."
```