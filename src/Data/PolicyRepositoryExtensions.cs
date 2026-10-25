#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Data;

/// <summary>
/// Provides query extension methods for policy repositories (<see cref="PolicyRepository"/> and any
/// other <see cref="IRepository{T}"/> over <see cref="ResiliencyPolicy"/>).
/// </summary>
/// <remarks>
/// These methods are built on <see cref="IRepository{T}.GetAll"/>, so each call takes a snapshot of the
/// stored policies. Results are not affected by later changes to the repository.
/// </remarks>
public static class PolicyRepositoryExtensions
{
    /// <summary>
    /// Gets all stored policies of the specified concrete policy type.
    /// </summary>
    /// <typeparam name="T">The concrete policy type to match, such as <c>RetryPolicy</c>.</typeparam>
    /// <param name="repository">The policy repository to query.</param>
    /// <returns>A new list containing the matching policies, or an empty list when none match.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    public static List<T> GetPoliciesOfType<T>(this IRepository<ResiliencyPolicy> repository)
        where T : ResiliencyPolicy
    {
        ArgumentNullException.ThrowIfNull(repository);

        return repository.GetAll().OfType<T>().ToList();
    }

    /// <summary>
    /// Counts the stored policies of the specified concrete policy type.
    /// </summary>
    /// <typeparam name="T">The concrete policy type to count, such as <c>CircuitBreakerPolicy</c>.</typeparam>
    /// <param name="repository">The policy repository to query.</param>
    /// <returns>The number of stored policies of type <typeparamref name="T"/>; zero when none match.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    public static int CountByType<T>(this IRepository<ResiliencyPolicy> repository)
        where T : ResiliencyPolicy
    {
        ArgumentNullException.ThrowIfNull(repository);

        return repository.GetAll().OfType<T>().Count();
    }

    /// <summary>
    /// Gets all stored policies whose <see cref="ResiliencyPolicy.IsEnabled"/> flag is set.
    /// </summary>
    /// <param name="repository">The policy repository to query.</param>
    /// <returns>A new list containing the enabled policies, or an empty list when none are enabled.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    public static List<ResiliencyPolicy> GetEnabledPolicies(this IRepository<ResiliencyPolicy> repository)
    {
        ArgumentNullException.ThrowIfNull(repository);

        return repository.GetAll().WhereEnabled().ToList();
    }

    /// <summary>
    /// Checks whether any stored policy has the specified friendly name.
    /// </summary>
    /// <param name="repository">The policy repository to query.</param>
    /// <param name="name">The policy name to look for. Comparison is ordinal and case-sensitive.</param>
    /// <returns><see langword="true"/> if a stored policy has the given name; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="name"/> is <see langword="null"/>, empty or whitespace.</exception>
    public static bool ExistsByName(this IRepository<ResiliencyPolicy> repository, string name)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(name);

        return repository.GetAll().Any(p => p.Name == name);
    }
}
