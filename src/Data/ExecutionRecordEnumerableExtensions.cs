#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

namespace DotNetResiliencePipeline.Data;

/// <summary>
/// Provides extension methods for sequences of <see cref="ExecutionRecord"/> instances.
/// </summary>
public static class ExecutionRecordEnumerableExtensions
{
    /// <summary>
    /// Calculates the percentage of execution records that completed successfully.
    /// </summary>
    /// <param name="records">The execution records to evaluate.</param>
    /// <returns>
    /// The percentage of successful records, from 0 to 100, or 0 when the sequence is empty.
    /// </returns>
    /// <exception cref="ArgumentNullException"><paramref name="records"/> is <see langword="null"/>.</exception>
    public static double SuccessRate(this IEnumerable<ExecutionRecord> records)
    {
        ArgumentNullException.ThrowIfNull(records);

        long totalCount = 0;
        long successCount = 0;

        foreach (var record in records)
        {
            totalCount++;
            if (record.IsSuccess)
            {
                successCount++;
            }
        }

        return totalCount == 0 ? 0 : successCount * 100.0 / totalCount;
    }
}
