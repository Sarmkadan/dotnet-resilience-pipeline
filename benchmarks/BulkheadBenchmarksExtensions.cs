using System;

namespace DotNetResiliencePipeline.Benchmarks
{
    /// <summary>
    /// Extension methods that add convenient, higher‑level operations for <see cref="BulkheadBenchmarks"/>.
    /// These methods are built only on the public members that already exist on the type.
    /// </summary>
    public static class BulkheadBenchmarksExtensions
    {
        /// <summary>
        /// Attempts to acquire a bulkhead slot and, if successful, immediately releases it.
        /// Returns <c>true</c> when a slot was acquired.
        /// </summary>
        /// <param name="benchmarks">The <see cref="BulkheadBenchmarks"/> instance.</param>
        /// <returns><c>true</c> if a slot was acquired; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <c>null</c>.</exception>
        public static bool TryAcquireAndRelease(this BulkheadBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            bool acquired = benchmarks.BulkheadPolicy_TryAcquireSlot_Available;
            if (acquired)
            {
                // Release the slot so the benchmark state remains unchanged.
                benchmarks.BulkheadPolicy_ReleaseSlot();
            }

            return acquired;
        }

        /// <summary>
        /// Records a queue‑wait event (using the existing benchmark method) and then returns the current utilization percentage.
        /// </summary>
        /// <param name="benchmarks">The <see cref="BulkheadBenchmarks"/> instance.</param>
        /// <returns>The current utilization percentage.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <c>null</c>.</exception>
        public static double RecordQueueWaitAndGetUtilization(this BulkheadBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            // The underlying benchmark records the wait time internally.
            benchmarks.BulkheadPolicy_RecordQueueWaitTime();

            // Return the latest utilization metric for callers that need it.
            return benchmarks.BulkheadPolicy_GetUtilizationPercentage();
        }

        /// <summary>
        /// Returns a short, human‑readable performance summary that includes utilization,
        /// queued and rejection percentages.
        /// </summary>
        /// <param name="benchmarks">The <see cref="BulkheadBenchmarks"/> instance.</param>
        /// <returns>A formatted performance summary string.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <c>null</c>.</exception>
        public static string GetPerformanceSummary(this BulkheadBenchmarks benchmarks)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);

            double utilization = benchmarks.BulkheadPolicy_GetUtilizationPercentage();
            double queued = benchmarks.BulkheadPolicy_GetQueuedPercentage();
            double rejection = benchmarks.BulkheadPolicy_GetRejectionPercentage();

            return $"Utilization: {utilization:P2}, Queued: {queued:P2}, Rejection: {rejection:P2}";
        }

        /// <summary>
        /// Determines whether the bulkhead is considered overloaded based on a utilization threshold.
        /// The default threshold is 80 % (0.8).
        /// </summary>
        /// <param name="benchmarks">The <see cref="BulkheadBenchmarks"/> instance.</param>
        /// <param name="utilizationThreshold">The utilization threshold between 0.0 and 1.0. Defaults to 0.8.</param>
        /// <returns><c>true</c> if the bulkhead utilization exceeds the threshold; otherwise, <c>false</c>.</returns>
        /// <exception cref="ArgumentNullException"><paramref name="benchmarks"/> is <c>null</c>.</exception>
        /// <exception cref="ArgumentOutOfRangeException"><paramref name="utilizationThreshold"/> is outside the valid range of 0.0 to 1.0.</exception>
        public static bool IsOverloaded(this BulkheadBenchmarks benchmarks, double utilizationThreshold = 0.8)
        {
            ArgumentNullException.ThrowIfNull(benchmarks);
            ArgumentOutOfRangeException.ThrowIfLessThan(utilizationThreshold, 0.0);
            ArgumentOutOfRangeException.ThrowIfGreaterThan(utilizationThreshold, 1.0);

            return benchmarks.BulkheadPolicy_GetUtilizationPercentage() > utilizationThreshold;
        }
    }
}