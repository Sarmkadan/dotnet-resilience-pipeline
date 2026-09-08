using System;
using System.Text;

namespace DotNetResiliencePipeline.Utilities
{
    /// <summary>
    /// Extension methods for <see cref="TimeSpan"/> that provide jitter, min/max helpers,
    /// and a concise human‑readable string representation.
    /// </summary>
    public static class TimeSpanExtensions
    {
        private static readonly Random _random = new Random();
        private static readonly object _lock = new object();

        /// <summary>
        /// Applies a random jitter to the <paramref name="timeSpan"/>.
        /// The jitter is calculated as a factor of the original duration.
        /// <paramref name="factor"/> should be between 0.0 (no jitter) and 1.0 (full jitter).
        /// </summary>
        public static TimeSpan WithJitter(this TimeSpan timeSpan, double factor)
        {
            if (factor <= 0) return timeSpan;
            if (factor > 1) factor = 1;

            double jitterMs;
            lock (_lock)
            {
                // Random value in range [-factor, +factor]
                var rnd = _random.NextDouble() * 2 - 1;
                jitterMs = timeSpan.TotalMilliseconds * factor * rnd;
            }

            var resultMs = timeSpan.TotalMilliseconds + jitterMs;
            // Ensure we never return a negative duration.
            if (resultMs < 0) resultMs = 0;
            return TimeSpan.FromMilliseconds(resultMs);
        }

        /// <summary>
        /// Returns the smaller of two <see cref="TimeSpan"/> values.
        /// </summary>
        public static TimeSpan Min(this TimeSpan a, TimeSpan b) => a < b ? a : b;

        /// <summary>
        /// Returns the larger of two <see cref="TimeSpan"/> values.
        /// </summary>
        public static TimeSpan Max(this TimeSpan a, TimeSpan b) => a > b ? a : b;

        /// <summary>
        /// Restricts a <see cref="TimeSpan"/> value to the inclusive range defined by
        /// <paramref name="min"/> and <paramref name="max"/>.
        /// </summary>
        /// <param name="value">The value to restrict.</param>
        /// <param name="min">The inclusive minimum value.</param>
        /// <param name="max">The inclusive maximum value.</param>
        /// <returns>
        /// <paramref name="min"/> when <paramref name="value"/> is less than the minimum,
        /// <paramref name="max"/> when it is greater than the maximum; otherwise,
        /// <paramref name="value"/>.
        /// </returns>
        /// <exception cref="ArgumentException">
        /// Thrown when <paramref name="min"/> is greater than <paramref name="max"/>.
        /// </exception>
        public static TimeSpan Clamp(this TimeSpan value, TimeSpan min, TimeSpan max)
        {
            if (min > max)
            {
                throw new ArgumentException("The minimum value cannot be greater than the maximum value.");
            }

            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        /// <summary>
        /// Formats the <see cref="TimeSpan"/> as a concise human‑readable string.
        /// Example: "1h 2m 3s", "150ms", or "2d 4h".
        /// </summary>
        public static string ToHumanString(this TimeSpan ts)
        {
            var sb = new StringBuilder();

            void Append(int value, string suffix)
            {
                if (value > 0)
                {
                    if (sb.Length > 0) sb.Append(' ');
                    sb.Append(value).Append(suffix);
                }
            }

            Append(ts.Days, "d");
            Append(ts.Hours, "h");
            Append(ts.Minutes, "m");
            Append(ts.Seconds, "s");

            // Show milliseconds only if the total duration is less than a second
            if (ts.TotalSeconds < 1)
            {
                Append(ts.Milliseconds, "ms");
            }

            // If everything is zero, return "0s"
            if (sb.Length == 0) return "0s";

            return sb.ToString();
        }
    }
}
