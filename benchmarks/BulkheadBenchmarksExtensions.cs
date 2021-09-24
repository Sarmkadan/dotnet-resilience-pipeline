using System;

namespace Benchmarks
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
        public static bool TryAcquireAndRelease(this BulkheadBenchmarks benchmarks)
        {
            if (benchmarks == null) throw new ArgumentNullException(nameof(benchmarks));

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
        public static double RecordQueueWaitAndGetUtilization(this BulkheadBenchmarks benchmarks)
        {
            if (benchmarks == null) throw new ArgumentNullException(nameof(benchmarks));

            // The underlying benchmark records the wait time internally.
            benchmarks.BulkheadPolicy_RecordQueueWaitTime();

            // Return the latest utilization metric for callers that need it.
            return benchmarks.BulkheadPolicy_GetUtilizationPercentage();
        }

        /// <summary>
        /// Returns a short, human‑readable performance summary that includes utilization,
        /// queued and rejection percentages.
        /// </summary>
        public static string GetPerformanceSummary(this BulkheadBenchmarks benchmarks)
        {
            if (benchmarks == null) throw new ArgumentNullException(nameof(benchmarks));

            double utilization = benchmarks.BulkheadPolicy_GetUtilizationPercentage();
            double queued = benchmarks.BulkheadPolicy_GetQueuedPercentage();
            double rejection = benchmarks.BulkheadPolicy_GetRejectionPercentage();

            return $"Utilization: {utilization:P2}, Queued: {queued:P2}, Rejection: {rejection:P2}";
        }

        /// <summary>
        /// Determines whether the bulkhead is considered overloaded based on a utilization threshold.
        /// The default threshold is 80 % (0.8).
        /// </summary>
        public static bool IsOverloaded(this BulkheadBenchmarks benchmarks, double utilizationThreshold = 0.8)
        {
            if (benchmarks == null) throw new ArgumentNullException(nameof(benchmarks));
            if (utilizationThreshold < 0.0 || utilizationThreshold > 1.0)
                throw new ArgumentOutOfRangeException(nameof(utilizationThreshold), "Threshold must be between 0.0 and 1.0.");

            return benchmarks.BulkheadPolicy_GetUtilizationPercentage() > utilizationThreshold;
        }
    }
}
