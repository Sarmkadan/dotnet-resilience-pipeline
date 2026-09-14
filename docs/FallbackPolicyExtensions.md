# Fallback Policy Extensions and Validation

This document describes the extension methods and validation helpers for `FallbackPolicy` in the `DotNetResiliencePipeline.Domain.Policies` namespace.

## FallbackPolicyExtensions.cs

Provides extension methods for configuring and monitoring fallback policies.

### AddFallbackTriggers

```csharp
public static void AddFallbackTriggers(this FallbackPolicy policy, IEnumerable<Type> exceptionTypes)
```

Adds multiple exception types to the list of triggers for the fallback policy.

**Parameters:**
- `policy`: The fallback policy to configure.
- `exceptionTypes`: The collection of exception types to add.

**Exceptions:**
- `ArgumentNullException`: Thrown if `policy` or `exceptionTypes` is null.
- `ArgumentException`: Thrown if `exceptionTypes` is empty.

**Validation Rules:**
- Throws `ArgumentNullException` if `policy` is null.
- Throws `ArgumentNullException` if `exceptionTypes` is null.
- Throws `ArgumentException` if `exceptionTypes` contains no elements.

**Example:**
```csharp
var policy = new FallbackPolicy(...);
var triggers = new[] { typeof(IOException), typeof(TimeoutException) };
policy.AddFallbackTriggers(triggers);
```

### IsFallbackHealthy

```csharp
public static bool IsFallbackHealthy(this FallbackPolicy policy, double minSuccessRate = 0.8)
```

Determines if the fallback policy is operating within a healthy success rate threshold.

**Parameters:**
- `policy`: The fallback policy to check.
- `minSuccessRate`: The minimum acceptable success rate (0.0 to 1.0). Default is 0.8.

**Returns:**
- True if the policy is healthy or has not been invoked yet; otherwise, false.

**Exceptions:**
- `ArgumentNullException`: Thrown if `policy` is null.

**Validation Rules:**
- Throws `ArgumentNullException` if `policy` is null.
- If the fallback hasn't been invoked (`FallbackInvocationCount == 0`), returns true by default.
- Otherwise, compares the policy's success rate (`GetFallbackSuccessRate()`) against `minSuccessRate`.

**Example:**
```csharp
var policy = new FallbackPolicy(...);
// After some executions...
bool isHealthy = policy.IsFallbackHealthy(0.9); // Requires 90% success rate
```

### GetExecutionSummary

```csharp
public static string GetExecutionSummary(this FallbackPolicy policy)
```

Generates a human-readable summary of the fallback policy's execution statistics.

**Parameters:**
- `policy`: The fallback policy to summarize.

**Returns:**
- A string containing the key statistics.

**Exceptions:**
- `ArgumentNullException`: Thrown if `policy` is null.

**Validation Rules:**
- Throws `ArgumentNullException` if `policy` is null.

**Example:**
```csharp
var policy = new FallbackPolicy(...);
// After some executions...
string summary = policy.GetExecutionSummary();
// Example output: "Fallback Statistics: Invocations: 10, Successes: 8, Failures: 2, Success Rate: 80.00%, Avg Execution Time: 125.50ms"
```

## FallbackPolicyValidation.cs

Provides validation helpers for `FallbackPolicy` instances.

### Validate

```csharp
public static IReadOnlyList<string> Validate(this FallbackPolicy? value)
```

Validates the specified fallback policy.

**Parameters:**
- `value`: The fallback policy to validate.

**Returns:**
- A list of validation problems; empty if the policy is valid.

**Exceptions:**
- `ArgumentNullException`: Thrown if `value` is null.

**Validation Rules:**
Performs the following checks and returns corresponding error messages:
1. **Name validation** (inherited from `ResiliencyPolicy`):
   - Checks if `Name` is null, empty, or whitespace.
   - Error: "Name cannot be null, empty, or whitespace."
2. **Fallback timeout validation**:
   - Checks if `FallbackTimeout` is less than or equal to `TimeSpan.Zero`.
   - Error: "FallbackTimeout must be a positive time span."
3. **Fallback trigger exceptions validation** (when `FallbackOnAnyException` is false):
   - Checks if `FallbackTriggerExceptions.Count` is zero.
   - Error: "Must have fallback trigger exceptions when FallbackOnAnyException is false."
4. **Fallback trigger exceptions collection validation**:
   - Checks if the collection is null.
   - Error: "FallbackTriggerExceptions collection is null."
   - Iterates through each exception type in the collection:
     - Checks for null elements.
       - Error: "FallbackTriggerExceptions collection contains a null element."
     - Checks if the type is assignable from `Exception`.
       - Error: "FallbackTriggerExceptions contains invalid type '{exceptionType.Name}' which is not an Exception."
5. **Statistics counters validation**:
   - Checks if `FallbackInvocationCount` is negative.
     - Error: "FallbackInvocationCount cannot be negative."
   - Checks if `SuccessfulFallbackCount` is negative.
     - Error: "SuccessfulFallbackCount cannot be negative."
   - Checks if `FailedFallbackCount` is negative.
     - Error: "FailedFallbackCount cannot be negative."
6. **Average execution time validation**:
   - Checks if `AverageFallbackExecutionTimeMs` is negative.
     - Error: "AverageFallbackExecutionTimeMs cannot be negative."

**Example:**
```csharp
var policy = new FallbackPolicy { Name = "", FallbackTimeout = TimeSpan.Zero };
var problems = policy.Validate();
// problems will contain:
//   "Name cannot be null, empty, or whitespace."
//   "FallbackTimeout must be a positive time span."
```

### IsValid

```csharp
public static bool IsValid(this FallbackPolicy? value) => value?.Validate().Count == 0;
```

Determines whether the specified fallback policy is valid.

**Parameters:**
- `value`: The fallback policy to check.

**Returns:**
- True if the policy is valid; otherwise, false.

**Exceptions:**
- `ArgumentNullException`: Thrown if `value` is null.

**Validation Rules:**
- Delegates to `Validate()` and checks if the returned list is empty.

**Example:**
```csharp
var policy = new FallbackPolicy { Name = "Test", FallbackTimeout = TimeSpan.FromSeconds(5) };
bool isValid = policy.IsValid(); // Returns true if all validations pass
```

### EnsureValid

```csharp
public static void EnsureValid(this FallbackPolicy? value)
```

Ensures that the specified fallback policy is valid.

**Parameters:**
- `value`: The fallback policy to validate.

**Exceptions:**
- `ArgumentNullException`: Thrown if `value` is null.
- `ArgumentException`: Thrown if the policy is not valid, containing a list of validation problems.

**Validation Rules:**
- Delegates to `Validate()` and throws an `ArgumentException` if any validation problems exist, formatting all problems into a single message.

**Example:**
```csharp
var policy = new FallbackPolicy { Name = "" };
try
{
    policy.EnsureValid();
}
catch (ArgumentException ex)
{
    // ex.Message will contain:
    // "FallbackPolicy validation failed:
    // Name cannot be null, empty, or whitespace."
}
```