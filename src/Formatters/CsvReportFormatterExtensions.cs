#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using System.Text;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Middleware;
using DotNetResiliencePipeline.Services;
using DotNetResiliencePipeline.Utilities;

namespace DotNetResiliencePipeline.Formatters
{
    /// <summary>
    /// Extension methods for <see cref="CsvReportFormatter"/>.
    /// </summary>
    public static class CsvReportFormatterExtensions
    {
        /// <summary>
        /// Formats the specified pipeline statistics as CSV and writes the result to a file asynchronously.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="stats">The pipeline statistics to format.</param>
        /// <param name="filePath">The path of the file to write the CSV to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="stats"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public static async Task WriteToFileAsync(this CsvReportFormatter formatter, PipelineStatistics stats, string filePath)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = stats ?? throw new ArgumentNullException(nameof(stats));
            _ = filePath ?? throw new ArgumentNullException(nameof(filePath));

            var csv = formatter.FormatPipelineMetrics(stats);
            await formatter.ExportToFileAsync(csv, filePath);
        }

        /// <summary>
        /// Formats the specified pipeline statistics as CSV and returns the result as an enumerable of lines.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="stats">The pipeline statistics to format.</param>
        /// <returns>An enumerable of CSV lines.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="stats"/> is null.</exception>
        public static IEnumerable<string> ToCsvLines(this CsvReportFormatter formatter, PipelineStatistics stats)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = stats ?? throw new ArgumentNullException(nameof(stats));

            var csv = formatter.FormatPipelineMetrics(stats);
            return csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Formats the specified pipeline statistics as CSV and returns only the header line.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="stats">The pipeline statistics to format.</param>
        /// <returns>The header line of the CSV, or an empty string if no header is present.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="stats"/> is null.</exception>
        public static string FormatWithHeaderOnly(this CsvReportFormatter formatter, PipelineStatistics stats)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = stats ?? throw new ArgumentNullException(nameof(stats));

            var csv = formatter.FormatPipelineMetrics(stats);
            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.FirstOrDefault() ?? string.Empty;
        }

        /// <summary>
        /// Formats the specified policies as CSV and writes the result to a file asynchronously.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="policies">The policies to format.</param>
        /// <param name="filePath">The path of the file to write the CSV to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="policies"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public static async Task WriteToFileAsync(this CsvReportFormatter formatter, List<ResiliencyPolicy> policies, string filePath)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = policies ?? throw new ArgumentNullException(nameof(policies));
            _ = filePath ?? throw new ArgumentNullException(nameof(filePath));

            var csv = formatter.FormatPolicies(policies);
            await formatter.ExportToFileAsync(csv, filePath);
        }

        /// <summary>
        /// Formats the specified policies as CSV and returns the result as an enumerable of lines.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="policies">The policies to format.</param>
        /// <returns>An enumerable of CSV lines.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="policies"/> is null.</exception>
        public static IEnumerable<string> ToCsvLines(this CsvReportFormatter formatter, List<ResiliencyPolicy> policies)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = policies ?? throw new ArgumentNullException(nameof(policies));

            var csv = formatter.FormatPolicies(policies);
            return csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Formats the specified policies as CSV and returns only the header line.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="policies">The policies to format.</param>
        /// <returns>The header line of the CSV, or an empty string if no header is present.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="policies"/> is null.</exception>
        public static string FormatWithHeaderOnly(this CsvReportFormatter formatter, List<ResiliencyPolicy> policies)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = policies ?? throw new ArgumentNullException(nameof(policies));

            var csv = formatter.FormatPolicies(policies);
            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.FirstOrDefault() ?? string.Empty;
        }

        /// <summary>
        /// Formats the specified execution history as CSV and writes the result to a file asynchronously.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="records">The execution records to format.</param>
        /// <param name="filePath">The path of the file to write the CSV to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="records"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public static async Task WriteToFileAsync(this CsvReportFormatter formatter, List<ExecutionRecord> records, string filePath)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = records ?? throw new ArgumentNullException(nameof(records));
            _ = filePath ?? throw new ArgumentNullException(nameof(filePath));

            var csv = formatter.FormatExecutionHistory(records);
            await formatter.ExportToFileAsync(csv, filePath);
        }

        /// <summary>
        /// Formats the specified execution history as CSV and returns the result as an enumerable of lines.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="records">The execution records to format.</param>
        /// <returns>An enumerable of CSV lines.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="records"/> is null.</exception>
        public static IEnumerable<string> ToCsvLines(this CsvReportFormatter formatter, List<ExecutionRecord> records)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = records ?? throw new ArgumentNullException(nameof(records));

            var csv = formatter.FormatExecutionHistory(records);
            return csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Formats the specified execution history as CSV and returns only the header line.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="records">The execution records to format.</param>
        /// <returns>The header line of the CSV, or an empty string if no header is present.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="records"/> is null.</exception>
        public static string FormatWithHeaderOnly(this CsvReportFormatter formatter, List<ExecutionRecord> records)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = records ?? throw new ArgumentNullException(nameof(records));

            var csv = formatter.FormatExecutionHistory(records);
            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.FirstOrDefault() ?? string.Empty;
        }

        /// <summary>
        /// Formats the specified performance metrics as CSV and writes the result to a file asynchronously.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="metrics">The performance metrics to format.</param>
        /// <param name="filePath">The path of the file to write the CSV to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public static async Task WriteToFileAsync(this CsvReportFormatter formatter, List<PerformanceMetrics> metrics, string filePath)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = metrics ?? throw new ArgumentNullException(nameof(metrics));
            _ = filePath ?? throw new ArgumentNullException(nameof(filePath));

            var csv = formatter.FormatPerformanceMetrics(metrics);
            await formatter.ExportToFileAsync(csv, filePath);
        }

        /// <summary>
        /// Formats the specified performance metrics as CSV and returns the result as an enumerable of lines.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="metrics">The performance metrics to format.</param>
        /// <returns>An enumerable of CSV lines.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
        public static IEnumerable<string> ToCsvLines(this CsvReportFormatter formatter, List<PerformanceMetrics> metrics)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = metrics ?? throw new ArgumentNullException(nameof(metrics));

            var csv = formatter.FormatPerformanceMetrics(metrics);
            return csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Formats the specified performance metrics as CSV and returns only the header line.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="metrics">The performance metrics to format.</param>
        /// <returns>The header line of the CSV, or an empty string if no header is present.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="metrics"/> is null.</exception>
        public static string FormatWithHeaderOnly(this CsvReportFormatter formatter, List<PerformanceMetrics> metrics)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = metrics ?? throw new ArgumentNullException(nameof(metrics));

            var csv = formatter.FormatPerformanceMetrics(metrics);
            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.FirstOrDefault() ?? string.Empty;
        }

        /// <summary>
        /// Formats the specified logs as CSV and writes the result to a file asynchronously.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="logs">The logs to format.</param>
        /// <param name="filePath">The path of the file to write the CSV to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logs"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public static async Task WriteToFileAsync(this CsvReportFormatter formatter, List<LogEntry> logs, string filePath)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = logs ?? throw new ArgumentNullException(nameof(logs));
            _ = filePath ?? throw new ArgumentNullException(nameof(filePath));

            var csv = formatter.FormatLogs(logs);
            await formatter.ExportToFileAsync(csv, filePath);
        }

        /// <summary>
        /// Formats the specified logs as CSV and returns the result as an enumerable of lines.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="logs">The logs to format.</param>
        /// <returns>An enumerable of CSV lines.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logs"/> is null.</exception>
        public static IEnumerable<string> ToCsvLines(this CsvReportFormatter formatter, List<LogEntry> logs)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = logs ?? throw new ArgumentNullException(nameof(logs));

            var csv = formatter.FormatLogs(logs);
            return csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Formats the specified logs as CSV and returns only the header line.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="logs">The logs to format.</param>
        /// <returns>The header line of the CSV, or an empty string if no header is present.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="logs"/> is null.</exception>
        public static string FormatWithHeaderOnly(this CsvReportFormatter formatter, List<LogEntry> logs)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = logs ?? throw new ArgumentNullException(nameof(logs));

            var csv = formatter.FormatLogs(logs);
            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.FirstOrDefault() ?? string.Empty;
        }

        /// <summary>
        /// Formats the specified error contexts as CSV and writes the result to a file asynchronously.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="errors">The error contexts to format.</param>
        /// <param name="filePath">The path of the file to write the CSV to.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="filePath"/> is null.</exception>
        /// <returns>A task representing the asynchronous write operation.</returns>
        public static async Task WriteToFileAsync(this CsvReportFormatter formatter, List<ErrorContext> errors, string filePath)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = errors ?? throw new ArgumentNullException(nameof(errors));
            _ = filePath ?? throw new ArgumentNullException(nameof(filePath));

            var csv = formatter.FormatErrors(errors);
            await formatter.ExportToFileAsync(csv, filePath);
        }

        /// <summary>
        /// Formats the specified error contexts as CSV and returns the result as an enumerable of lines.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="errors">The error contexts to format.</param>
        /// <returns>An enumerable of CSV lines.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
        public static IEnumerable<string> ToCsvLines(this CsvReportFormatter formatter, List<ErrorContext> errors)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = errors ?? throw new ArgumentNullException(nameof(errors));

            var csv = formatter.FormatErrors(errors);
            return csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>
        /// Formats the specified error contexts as CSV and returns only the header line.
        /// </summary>
        /// <param name="formatter">The <see cref="CsvReportFormatter"/> instance.</param>
        /// <param name="errors">The error contexts to format.</param>
        /// <returns>The header line of the CSV, or an empty string if no header is present.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="formatter"/> is null.</exception>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="errors"/> is null.</exception>
        public static string FormatWithHeaderOnly(this CsvReportFormatter formatter, List<ErrorContext> errors)
        {
            _ = formatter ?? throw new ArgumentNullException(nameof(formatter));
            _ = errors ?? throw new ArgumentNullException(nameof(errors));

            var csv = formatter.FormatErrors(errors);
            var lines = csv.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
            return lines.FirstOrDefault() ?? string.Empty;
        }
    }
}