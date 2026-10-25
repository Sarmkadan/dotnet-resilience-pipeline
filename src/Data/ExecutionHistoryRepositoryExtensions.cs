#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Data;

/// <summary>
/// Provides query extension methods for <see cref="ExecutionHistoryRepository"/>.
/// </summary>
/// <remarks>
/// These methods are built on <see cref="ExecutionHistoryRepository.GetAll"/>, so each call works on a
/// snapshot of the stored records. Results are not affected by later changes to the repository.
/// </remarks>
public static class ExecutionHistoryRepositoryExtensions
{
    /// <summary>
    /// Gets the failed execution records that occurred at or after the specified time.
    /// </summary>
    /// <param name="repository">The execution history repository to query.</param>
    /// <param name="since">The UTC lower bound, inclusive, compared against <see cref="ExecutionRecord.ExecutedAt"/>.</param>
    /// <returns>A new list containing the matching failed records, or an empty list when none match.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    public static List<ExecutionRecord> GetFailuresSince(this ExecutionHistoryRepository repository, DateTime since)
    {
        ArgumentNullException.ThrowIfNull(repository);

        return repository.GetFailedExecutions()
            .Where(r => r.ExecutedAt >= since)
            .ToList();
    }

    /// <summary>
    /// Calculates the success rate of the execution records for the specified policy name.
    /// </summary>
    /// <param name="repository">The execution history repository to query.</param>
    /// <param name="policyName">The policy name to match. Comparison is ordinal and case-sensitive.</param>
    /// <returns>
    /// The percentage of successful records for the policy, from 0 to 100, or 0 when the policy has no records.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/>, empty or whitespace.</exception>
    public static double GetSuccessRate(this ExecutionHistoryRepository repository, string policyName)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        return repository.GetAll()
            .Where(r => r.PolicyName == policyName)
            .SuccessRate();
    }

    /// <summary>
    /// Gets the execution records with the longest execution times, slowest first.
    /// </summary>
    /// <param name="repository">The execution history repository to query.</param>
    /// <param name="count">The maximum number of records to return. Must be greater than zero.</param>
    /// <returns>
    /// A new list containing up to <paramref name="count"/> records ordered by
    /// <see cref="ExecutionRecord.ExecutionTimeMs"/> descending; fewer when the repository holds fewer records.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentOutOfRangeException"><paramref name="count"/> is zero or negative.</exception>
    public static List<ExecutionRecord> GetSlowestRecords(this ExecutionHistoryRepository repository, int count)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentOutOfRangeException.ThrowIfNegativeOrZero(count);

        return repository.GetAll()
            .OrderByDescending(r => r.ExecutionTimeMs)
            .Take(count)
            .ToList();
    }

    /// <summary>
    /// Checks whether the repository holds any execution records for the specified policy name.
    /// </summary>
    /// <param name="repository">The execution history repository to query.</param>
    /// <param name="policyName">The policy name to look for. Comparison is ordinal and case-sensitive.</param>
    /// <returns><see langword="true"/> if at least one record has the given policy name; otherwise <see langword="false"/>.</returns>
    /// <exception cref="ArgumentNullException"><paramref name="repository"/> is <see langword="null"/>.</exception>
    /// <exception cref="ArgumentException"><paramref name="policyName"/> is <see langword="null"/>, empty or whitespace.</exception>
    public static bool HasRecordsFor(this ExecutionHistoryRepository repository, string policyName)
    {
        ArgumentNullException.ThrowIfNull(repository);
        ArgumentException.ThrowIfNullOrWhiteSpace(policyName);

        return repository.GetAll().Any(r => r.PolicyName == policyName);
    }
}
