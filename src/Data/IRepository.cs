// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Data;

/// <summary>
/// Generic repository interface for CRUD operations.
/// </summary>
public interface IRepository<T> where T : class
{
    /// <summary>
    /// Creates and stores a new entity.
    /// </summary>
    void Create(T entity);

    /// <summary>
    /// Retrieves an entity by its ID.
    /// </summary>
    T? Read(string id);

    /// <summary>
    /// Updates an existing entity.
    /// </summary>
    void Update(T entity);

    /// <summary>
    /// Deletes an entity by its ID.
    /// </summary>
    void Delete(string id);

    /// <summary>
    /// Gets all entities.
    /// </summary>
    List<T> GetAll();

    /// <summary>
    /// Gets the total count of entities.
    /// </summary>
    int Count();

    /// <summary>
    /// Clears all entities.
    /// </summary>
    void Clear();

    /// <summary>
    /// Checks if an entity exists.
    /// </summary>
    bool Exists(string id);
}
