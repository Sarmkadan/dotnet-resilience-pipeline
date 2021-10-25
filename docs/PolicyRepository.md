# PolicyRepository

The `PolicyRepository` class serves as the central in-memory store for managing `ResiliencyPolicy` instances within the `dotnet-resilience-pipeline` project. It provides a comprehensive CRUD (Create, Read, Update, Delete) interface along with specialized querying capabilities to retrieve policies by type, name, or tags. This repository acts as the authoritative source for policy definitions during the application lifecycle, supporting both synchronous operations and asynchronous persistence mechanisms.

## API

### Constructors

#### `public PolicyRepository()`
Initializes a new instance of the `PolicyRepository` class with an empty internal collection.

### Management Methods

#### `public void Create(ResiliencyPolicy policy)`
Registers a new resiliency policy in the repository.
*   **Parameters**: `policy` - The `ResiliencyPolicy` instance to add.
*   **Return Value**: None.
*   **Exceptions**: Throws an exception if a policy with the same unique identifier or name already exists, or if the provided policy is null.

#### `public ResiliencyPolicy? Read(string id)`
Retrieves a specific policy by its unique identifier.
*   **Parameters**: `id` - The unique identifier of the policy.
*   **Return Value**: The matching `ResiliencyPolicy` if found; otherwise, `null`.
*   **Exceptions**: None.

#### `public void Update(ResiliencyPolicy policy)`
Updates an existing policy definition with new configuration values.
*   **Parameters**: `policy` - The `ResiliencyPolicy` instance containing updated data.
*   **Return Value**: None.
*   **Exceptions**: Throws an exception if no policy with the matching identifier exists.

#### `public void Delete(string id)`
Removes a policy from the repository based on its unique identifier.
*   **Parameters**: `id` - The unique identifier of the policy to remove.
*   **Return Value**: None.
*   **Exceptions**: Throws an exception if the specified ID does not exist in the repository.

#### `public void Clear()`
Removes all policies from the repository, resetting the collection to an empty state.
*   **Parameters**: None.
*   **Return Value**: None.
*   **Exceptions**: None.

### Query Methods

#### `public List<ResiliencyPolicy> GetAll()`
Returns a list containing all policies currently stored in the repository.
*   **Parameters**: None.
*   **Return Value**: A `List<ResiliencyPolicy>` containing all entries. Returns an empty list if no policies exist.
*   **Exceptions**: None.

#### `public List<T> GetByType<T>()`
Filters and returns all policies that match the specified generic type `T`.
*   **Parameters**: None (type specified via generic argument).
*   **Return Value**: A `List<T>` containing policies castable to the requested type.
*   **Exceptions**: None.

#### `public ResiliencyPolicy? GetByName(string name)`
Retrieves the first policy matching the specified name.
*   **Parameters**: `name` - The name of the policy to search for.
*   **Return Value**: The matching `ResiliencyPolicy` if found; otherwise, `null`.
*   **Exceptions**: None.

#### `public List<ResiliencyPolicy> GetByTag(string tag)`
Returns all policies associated with a specific tag.
*   **Parameters**: `tag` - The tag string to filter by.
*   **Return Value**: A `List<ResiliencyPolicy>` containing all matching policies. Returns an empty list if no matches are found.
*   **Exceptions**: None.

#### `public bool Exists(string id)`
Checks whether a policy with the given identifier is present in the repository.
*   **Parameters**: `id` - The unique identifier to check.
*   **Return Value**: `true` if the policy exists; otherwise, `false`.
*   **Exceptions**: None.

### Properties

#### `public int Count`
Gets the total number of policies currently stored in the repository.
*   **Return Value**: An integer representing the current collection size.

### Persistence

#### `public Task SaveAsync()`
Persists the current state of the repository to the underlying storage medium asynchronously.
*   **Parameters**: None.
*   **Return Value**: A `Task` that completes when the save operation is finished.
*   **Exceptions**: May throw I/O or serialization exceptions depending on the underlying storage implementation.

## Usage

### Example 1: Basic Lifecycle Management
The following example demonstrates creating a policy, verifying its existence, updating it, and retrieving it by name.

```csharp
var repository = new PolicyRepository();

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
    // Retrieve by name
    var fetched = repository.GetByName("StandardRetry");
    
    // Update the policy configuration
    if (fetched != null)
    {
        fetched.MaxRetries = 5;
        repository.Update(fetched);
    }
}

// Persist changes
await repository.SaveAsync();
```

### Example 2: Filtering and Bulk Operations
This example illustrates retrieving policies by specific types and tags, followed by a bulk cleanup operation.

```csharp
var repository = new PolicyRepository();

// Populate repository (assumed previously created)
// ...

// Get all circuit breaker policies
var circuitBreakers = repository.GetByType<CircuitBreakerPolicy>();

// Get all policies tagged with "production"
var prodPolicies = repository.GetByTag("production");

// Process specific policies
foreach (var policy in prodPolicies)
{
    Console.WriteLine($"Production Policy: {policy.Name}");
}

// Clear all policies if needed (e.g., during test teardown)
repository.Clear();
Console.WriteLine($"Repository count after clear: {repository.Count}");
```

## Notes

*   **Thread Safety**: The provided signatures indicate standard synchronous collection manipulation methods (`Create`, `Update`, `Delete`, `Clear`) alongside a single asynchronous persistence method (`SaveAsync`). The class does not inherently expose concurrent collection types or explicit locking mechanisms in its public API. Consequently, concurrent read/write operations from multiple threads without external synchronization may result in race conditions or inconsistent state. External locking is recommended when accessing this repository from multiple threads.
*   **Null Handling**: Read operations (`Read`, `GetByName`) explicitly return nullable references (`ResiliencyPolicy?`), indicating that callers must handle cases where the requested entity is not found. Conversely, query methods returning lists (`GetAll`, `GetByType`, `GetByTag`) return empty collections rather than `null` when no matches are found.
*   **Consistency**: The `SaveAsync` method suggests that modifications made via `Create`, `Update`, or `Delete` are initially held in memory. Data durability is only guaranteed after `SaveAsync` completes successfully. If the application terminates before `SaveAsync` is called, in-memory changes will be lost.
*   **Type Safety**: The `GetByType<T>` method relies on runtime type checking. It will only return policies that strictly match or derive from the specified generic type `T`.
