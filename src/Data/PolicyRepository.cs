#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Data;

/// <summary>
/// Repository for managing policy persistence and retrieval.
/// </summary>
public class PolicyRepository : IRepository<ResiliencyPolicy>
{
    private readonly Dictionary<string, ResiliencyPolicy> _storage;
    private readonly object _lockObj = new object();

    public PolicyRepository()
    {
        _storage = new Dictionary<string, ResiliencyPolicy>();
    }

    /// <summary>
    /// Creates and stores a new policy.
    /// </summary>
    public void Create(ResiliencyPolicy entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        lock (_lockObj)
        {
            if (_storage.ContainsKey(entity.Id))
                throw new InvalidOperationException($"Policy with ID {entity.Id} already exists");

            _storage[entity.Id] = entity;
        }
    }

    /// <summary>
    /// Retrieves a policy by its ID.
    /// </summary>
    public ResiliencyPolicy? Read(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("ID cannot be empty", nameof(id));

        lock (_lockObj)
        {
            return _storage.TryGetValue(id, out var policy) ? policy : null;
        }
    }

    /// <summary>
    /// Updates an existing policy.
    /// </summary>
    public void Update(ResiliencyPolicy entity)
    {
        if (entity is null)
            throw new ArgumentNullException(nameof(entity));

        lock (_lockObj)
        {
            if (!_storage.ContainsKey(entity.Id))
                throw new KeyNotFoundException($"Policy with ID {entity.Id} not found");

            _storage[entity.Id] = entity;
        }
    }

    /// <summary>
    /// Deletes a policy by its ID.
    /// </summary>
    public void Delete(string id)
    {
        if (string.IsNullOrEmpty(id))
            throw new ArgumentException("ID cannot be empty", nameof(id));

        lock (_lockObj)
        {
            _storage.Remove(id);
        }
    }

    /// <summary>
    /// Gets all policies.
    /// </summary>
    public List<ResiliencyPolicy> GetAll()
    {
        lock (_lockObj)
        {
            return _storage.Values.ToList();
        }
    }

    /// <summary>
    /// Finds policies by type.
    /// </summary>
    public List<T> GetByType<T>() where T : ResiliencyPolicy
    {
        lock (_lockObj)
        {
            return _storage.Values.OfType<T>().ToList();
        }
    }

    /// <summary>
    /// Finds a policy by name.
    /// </summary>
    public ResiliencyPolicy? GetByName(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be empty", nameof(name));

        lock (_lockObj)
        {
            return _storage.Values.FirstOrDefault(p => p.Name == name);
        }
    }

    /// <summary>
    /// Finds policies by tag.
    /// </summary>
    public List<ResiliencyPolicy> GetByTag(string tag)
    {
        if (string.IsNullOrEmpty(tag))
            throw new ArgumentException("Tag cannot be empty", nameof(tag));

        lock (_lockObj)
        {
            return _storage.Values.Where(p => p.Tags.Contains(tag)).ToList();
        }
    }

    /// <summary>
    /// Gets count of all policies.
    /// </summary>
    public int Count()
    {
        lock (_lockObj)
        {
            return _storage.Count;
        }
    }

    /// <summary>
    /// Deletes all policies.
    /// </summary>
    public void Clear()
    {
        lock (_lockObj)
        {
            _storage.Clear();
        }
    }

    /// <summary>
    /// Checks if a policy exists.
    /// </summary>
    public bool Exists(string id)
    {
        lock (_lockObj)
        {
            return _storage.ContainsKey(id);
        }
    }
}
