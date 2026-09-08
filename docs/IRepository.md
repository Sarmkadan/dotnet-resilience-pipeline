# IRepository<T>

The `IRepository<T>` interface defines a generic contract for CRUD (Create, Read, Update, Delete) operations on entities of type `T`. It provides a standardized way to manage data storage and retrieval without exposing the underlying implementation details.

## API

### Management Methods

#### `public void Create(T entity)`
Creates and stores a new entity.
*   **Parameters**: `entity` - The entity to add.
*   **Return Value**: None.
*   **Exceptions**: Throws an exception if the entity is `null` (as implemented in `PolicyRepository`).

#### `public T? Read(string id)`
Retrieves an entity by its unique identifier.
*   **Parameters**: `id` - The unique identifier of the entity.
*   **Return Value**: The matching entity if found; otherwise, `null`.
*   **Exceptions**: Throws an exception if `id` is `null` or empty (as implemented in `PolicyRepository`).

#### `public void Update(T entity)`
Updates an existing entity with new values.
*   **Parameters**: `entity` - The entity containing updated data.
*   **Return Value**: None.
*   **Exceptions**: Throws an exception if the entity is `null` or if no entity with the matching identifier exists (as implemented in `PolicyRepository`).

#### `public void Delete(string id)`
Removes an entity from the repository based on its unique identifier.
*   **Parameters**: `id` - The unique identifier of the entity to remove.
*   **Return Value**: None.
*   **Exceptions**: Throws an exception if `id` is `null` or empty (as implemented in `PolicyRepository`).

#### `public void Clear()`
Removes all entities from the repository, resetting the collection to an empty state.
*   **Parameters**: None.
*   **Return Value**: None.
*   **Exceptions**: None.

### Query Methods

#### `public List<T> GetAll()`
Returns a list containing all entities currently stored in the repository.
*   **Parameters**: None.
*   **Return Value**: A `List<T>` containing all entries. Returns an empty list if no entities exist.
*   **Exceptions**: None.

#### `public int Count()`
Gets the total number of entities currently stored in the repository.
*   **Parameters**: None.
*   **Return Value**: An integer representing the current collection size.
*   **Exceptions**: None.

#### `public bool Exists(string id)`
Checks whether an entity with the given identifier is present in the repository.
*   **Parameters**: `id` - The unique identifier to check.
*   **Return Value**: `true` if the entity exists; otherwise, `false`.
*   **Exceptions**: Throws an exception if `id` is `null` or empty (as implemented in `PolicyRepository`).

## Implementation

The `PolicyRepository` class (`src/Data/PolicyRepository.cs`) provides a concrete implementation of `IRepository<ResiliencyPolicy>`. It adds thread safety via locking and extends the interface with additional query methods (`GetByType<T>`, `GetByName`, `GetByTag`) and an asynchronous persistence method (`SaveAsync`).

## Usage

### Example: Basic CRUD Operations
The following example demonstrates creating an entity, verifying its existence, updating it, and retrieving it by ID using the `IRepository<T>` interface with `PolicyRepository` as the implementation.

```csharp
// Using the IRepository interface with PolicyRepository as the implementation
IRepository<ResiliencyPolicy> repository = new PolicyRepository();

// Create a new retry policy
var retryPolicy = new ResiliencyPolicy 
{ 
    Id = "policy-001", 
    Name = "StandardRetry", 
    Type = typeof(RetryPolicy) 
};

repository.Create(retryPolicy);

// Verify existence
if (repository.Exists("policy-001"))
{
    // Retrieve by ID
    var fetched = repository.Read("policy-001");
    
    // Update the policy configuration
    if (fetched != null)
    {
        fetched.MaxRetries = 5;
        repository.Update(fetched);
    }
}

// Get all policies
var allPolicies = repository.GetAll();
Console.WriteLine($"Total policies: {repository.Count}");

// Clear all policies (e.g., during test teardown)
repository.Clear();
Console.WriteLine($"Repository count after clear: {repository.Count}");
```