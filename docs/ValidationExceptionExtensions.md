# ValidationExceptionExtensions

Provides utility methods for inspecting and manipulating `ValidationException` instances from the FluentValidation library. These extensions simplify common tasks such as checking for field-specific errors, retrieving formatted error messages, extracting affected field names, and augmenting an existing exception with additional validation failures.

## API

### HasErrorFor

```csharp
public static bool HasErrorFor(this ValidationException exception, string propertyName)
```

Determines whether the specified `ValidationException` contains at least one error associated with the given property name.

**Parameters:**
- `exception` — The `ValidationException` to inspect.
- `propertyName` — The property name to search for (case-sensitive matching depends on the underlying validation framework's behavior).

**Return Value:** `true` if one or more errors exist for `propertyName`; otherwise `false`.

**Throws:** `ArgumentNullException` if `exception` or `propertyName` is `null`.

---

### GetErrorMessage

```csharp
public static string GetErrorMessage(this ValidationException exception, string separator = "\n")
```

Concatenates all error messages from the `ValidationException` into a single string, using the specified separator between each message.

**Parameters:**
- `exception` — The `ValidationException` whose errors to aggregate.
- `separator` — The string inserted between individual error messages. Defaults to a newline character (`"\n"`).

**Return Value:** A string containing all error messages joined by the separator. Returns an empty string if the exception contains no errors.

**Throws:** `ArgumentNullException` if `exception` is `null`.

---

### GetErrorFields

```csharp
public static IEnumerable<string> GetErrorFields(this ValidationException exception)
```

Extracts the distinct property names that have associated validation errors.

**Parameters:**
- `exception` — The `ValidationException` to inspect.

**Return Value:** An `IEnumerable<string>` of unique property names for which errors exist. Returns an empty sequence if there are no errors.

**Throws:** `ArgumentNullException` if `exception` is `null`.

---

### WithAdditionalErrors

```csharp
public static ValidationException WithAdditionalErrors(
    this ValidationException exception,
    IEnumerable<ValidationFailure> additionalErrors)
```

Creates a new `ValidationException` that combines the original errors with the supplied additional validation failures.

**Parameters:**
- `exception` — The original `ValidationException`.
- `additionalErrors` — A collection of `ValidationFailure` instances to append.

**Return Value:** A new `ValidationException` instance containing both the original errors and the additional errors. The original exception is not modified.

**Throws:** `ArgumentNullException` if `exception` or `additionalErrors` is `null`.

## Usage

### Example 1: Inspecting and Logging Validation Results

```csharp
try
{
    await validator.ValidateAndThrowAsync(request, cancellationToken);
}
catch (ValidationException ex)
{
    // Check if a specific field failed validation
    if (ex.HasErrorFor("Email"))
    {
        logger.LogWarning("Email validation failed for request {RequestId}", request.Id);
    }

    // Log all affected fields
    var fields = ex.GetErrorFields();
    logger.LogError(
        "Validation failed for fields: {Fields}. Errors: {Errors}",
        string.Join(", ", fields),
        ex.GetErrorMessage(separator: "; "));
}
```

### Example 2: Augmenting Validation Errors Before Rethrowing

```csharp
catch (ValidationException ex) when (context.Request.IsRetryAttempt)
{
    var additionalFailures = new List<ValidationFailure>
    {
        new ValidationFailure("RetryCount", "Maximum retry attempts exceeded.")
    };

    // Create a combined exception with the extra context
    var enhancedException = ex.WithAdditionalErrors(additionalFailures);
    throw enhancedException;
}
```

## Notes

- All methods treat the original `ValidationException` as immutable. `WithAdditionalErrors` returns a new instance; it does not modify the existing exception.
- `GetErrorFields` returns distinct property names. If multiple errors exist for the same property, it appears only once in the result sequence.
- The `separator` parameter in `GetErrorMessage` is appended between messages but not at the end of the final string. Passing `null` as the separator uses the default newline.
- These methods are not thread-safe if the underlying `ValidationException` or its internal error collection is mutated concurrently. In practice, `ValidationException` instances are typically short-lived and not shared across threads, so this is rarely a concern.
- `HasErrorFor` relies on the property name as stored in each `ValidationFailure`. The matching behavior (case sensitivity, trimming) is determined by the validation library that produced the errors, not by these extension methods.
