# ValidationException
The `ValidationException` class is designed to handle validation errors in a structured and informative manner. It provides a way to store and manage validation errors, allowing for more effective error handling and debugging in applications. This exception type is particularly useful in scenarios where input data needs to be validated against certain rules or criteria.

## API
### Constructors
- `public ValidationException()`: Initializes a new instance of the `ValidationException` class.
- `public ValidationException()`: Overloaded constructor, exact parameters not specified.
- `public ValidationException()`: Another overloaded constructor, exact parameters not specified.
- `public ValidationException()`: Yet another overloaded constructor, exact parameters not specified.
### Properties
- `public Dictionary<string, string> ValidationErrors`: Gets a dictionary containing validation errors, where each key-value pair represents an error with its corresponding message.

## Usage
The following examples demonstrate how to use the `ValidationException` class in a C# application:
```csharp
// Example 1: Throwing a ValidationException with validation errors
try
{
    var validationErrors = new Dictionary<string, string>
    {
        {"Name", "Cannot be empty"},
        {"Email", "Invalid format"}
    };
    throw new ValidationException { ValidationErrors = validationErrors };
}
catch (ValidationException ex)
{
    foreach (var error in ex.ValidationErrors)
    {
        Console.WriteLine($"{error.Key}: {error.Value}");
    }
}

// Example 2: Using ValidationException in a validation method
public void ValidateUserInput(string name, string email)
{
    var validationErrors = new Dictionary<string, string>();
    if (string.IsNullOrEmpty(name))
    {
        validationErrors.Add("Name", "Cannot be empty");
    }
    if (!email.Contains("@"))
    {
        validationErrors.Add("Email", "Invalid format");
    }
    if (validationErrors.Count > 0)
    {
        throw new ValidationException { ValidationErrors = validationErrors };
    }
}
```

## Notes
When using the `ValidationException` class, consider the following:
- The `ValidationErrors` dictionary can be null if not initialized properly, leading to `NullReferenceException` when trying to access its elements.
- Since `ValidationException` is an exception, it should be used judiciously and only when validation fails, to avoid unnecessary exception handling overhead.
- The class itself does not seem to have any inherent thread-safety issues, given its immutable nature (once constructed, its state does not change). However, the usage of its instances in a multi-threaded environment should be carefully managed to avoid shared state issues, especially if the `ValidationErrors` dictionary is modified after the exception is thrown.
