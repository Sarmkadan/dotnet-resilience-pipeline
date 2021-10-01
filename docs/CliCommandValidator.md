# CliCommandValidator

The `CliCommandValidator` class provides a structured mechanism for validating command-line interface inputs within the `dotnet-resilience-pipeline` project. It encapsulates the results of validation logic, distinguishing between critical errors that prevent execution and non-blocking warnings, while exposing a boolean flag for quick validity checks and a string representation for logging or debugging purposes.

## API

### `Validate`
```csharp
public ValidationResult Validate
```
Represents the method or property responsible for executing the validation logic against a specific command context.
*   **Purpose**: Triggers or retrieves the result of the validation process.
*   **Return Value**: Returns a `ValidationResult` object containing the aggregate state of the validation.
*   **Exceptions**: May throw exceptions if the underlying command structure is null or if the validation infrastructure is misconfigured.

### `IsValid`
```csharp
public bool IsValid
```
*   **Purpose**: Provides a quick boolean indicator of whether the validation passed without critical errors.
*   **Return Value**: Returns `true` if the `Errors` collection is empty; otherwise, returns `false`.
*   **Exceptions**: Does not throw exceptions.

### `Errors`
```csharp
public List<string> Errors
```
*   **Purpose**: Contains a list of human-readable error messages describing critical validation failures that must be resolved before command execution.
*   **Return Value**: Returns a mutable list of strings. If no errors occurred, the list is empty but not null.
*   **Exceptions**: Does not throw exceptions upon access; however, modifying the list while it is being enumerated by another thread may cause runtime exceptions.

### `Warnings`
```csharp
public List<string> Warnings
```
*   **Purpose**: Contains a list of human-readable warning messages indicating non-critical issues or deprecated usage patterns that do not prevent command execution.
*   **Return Value**: Returns a mutable list of strings. If no warnings were generated, the list is empty but not null.
*   **Exceptions**: Does not throw exceptions upon access.

### `ToString`
```csharp
public override string ToString
```
*   **Purpose**: Generates a formatted string representation of the validator's current state, typically including the validity status and a summary of errors and warnings.
*   **Return Value**: Returns a `string` summarizing the validation outcome.
*   **Exceptions**: Does not throw exceptions.

## Usage

### Example 1: Basic Validation Check
This example demonstrates how to instantiate the validator, run the validation, and check the `IsValid` flag before proceeding with logic.

```csharp
var validator = new CliCommandValidator();
var result = validator.Validate;

if (!validator.IsValid)
{
    foreach (var error in validator.Errors)
    {
        Console.WriteLine($"[ERROR] {error}");
    }
    return;
}

if (validator.Warnings.Count > 0)
{
    foreach (var warning in validator.Warnings)
    {
        Console.WriteLine($"[WARN] {warning}");
    }
}

// Proceed with command execution
Console.WriteLine("Validation passed. Executing pipeline...");
```

### Example 2: Logging Validation State
This example utilizes the `ToString` override to log the complete validation state for debugging or audit purposes.

```csharp
var validator = new CliCommandValidator();
// Assume validation logic runs internally or via the Validate property
var result = validator.Validate;

if (!validator.IsValid)
{
    // Log the full state summary
    System.Diagnostics.Debug.WriteLine(validator.ToString());
    
    // Handle specific critical failure
    throw new InvalidOperationException("Command validation failed.");
}
```

## Notes

*   **Collection Mutability**: The `Errors` and `Warnings` properties return mutable `List<string>` instances. Callers should exercise caution when modifying these lists concurrently, as the class does not appear to enforce internal synchronization on these collections.
*   **Thread Safety**: Based on the exposed signatures, `CliCommandValidator` is not inherently thread-safe. If an instance is shared across multiple threads, external locking mechanisms are required when reading or writing to the `Errors` and `Warnings` lists or when invoking `Validate`.
*   **Empty vs. Null**: The `Errors` and `Warnings` lists are expected to be initialized to empty lists rather than `null` when no issues are present, preventing `NullReferenceException` during enumeration.
*   **Validation Trigger**: Accessing the `Validate` member may trigger the actual validation logic depending on its implementation (method vs. computed property). It should be called before inspecting `IsValid`, `Errors`, or `Warnings` to ensure the state is current.
