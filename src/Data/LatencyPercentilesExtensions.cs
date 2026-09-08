#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace DotNetResiliencePipeline.Data
{
    /// <summary>
    /// Extension methods for <see cref="LatencyPercentiles"/>.
    /// </summary>
    public static class LatencyPercentilesExtensions
    {
        /// <summary>
        /// Converts the latency percentiles to a read-only dictionary.
        /// </summary>
        /// <param name="percentiles">The latency percentiles to convert.</param>
        /// <returns>A read-only dictionary containing the percentiles.</returns>
        public static IReadOnlyDictionary<string, double> ToDictionary(this DotNetResiliencePipeline.Data.LatencyPercentiles percentiles)
        {
            return new ReadOnlyDictionary<string, double>(new Dictionary<string, double>
            {
                ["p50"] = percentiles.P50,
                ["p90"] = percentiles.P90,
                ["p99"] = percentiles.P99
            });
        }
    }
}