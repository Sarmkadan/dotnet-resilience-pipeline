#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using System.Text;

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Provides comprehensive validation utilities for resilience policies.
/// Validates configuration, identifies anti-patterns, and provides improvement suggestions.
/// </summary>
public static class PolicyValidationHelper
{
    /// <summary>
    /// Validates a policy configuration thoroughly.
    /// </summary>
    public static ValidationReport ValidatePolicy(ResiliencyPolicy policy)
    {
        var report = new ValidationReport { PolicyId = policy.Id, PolicyName = policy.Name };

        if (policy is CircuitBreakerPolicy cb)
            ValidateCircuitBreaker(cb, report);
        else if (policy is RetryPolicy retry)
            ValidateRetry(retry, report);
        else if (policy is BulkheadPolicy bulkhead)
            ValidateBulkhead(bulkhead, report);
        else if (policy is TimeoutPolicy timeout)
            ValidateTimeout(timeout, report);

        return report;
    }

    private static void ValidateCircuitBreaker(CircuitBreakerPolicy policy, ValidationReport report)
    {
        if (policy.FailureThreshold < 1)
            report.Errors.Add("FailureThreshold must be at least 1");

        if (policy.FailureThreshold > 1000)
            report.Warnings.Add("FailureThreshold > 1000 may be too lenient");

        if (policy.OpenDuration < TimeSpan.FromSeconds(1))
            report.Warnings.Add("OpenDuration < 1 second may cause rapid cycling");

        if (policy.OpenDuration > TimeSpan.FromHours(1))
            report.Warnings.Add("OpenDuration > 1 hour may prevent recovery");

        if (policy.SuccessThresholdInHalfOpen < 1)
            report.Errors.Add("SuccessThresholdInHalfOpen must be at least 1");
    }

    private static void ValidateRetry(RetryPolicy policy, ValidationReport report)
    {
        if (policy.MaxRetries < 0)
            report.Errors.Add("MaxRetries cannot be negative");

        if (policy.MaxRetries > 50)
            report.Warnings.Add("MaxRetries > 50 may cause excessive overhead");

        if (policy.InitialDelay < TimeSpan.FromMilliseconds(1))
            report.Warnings.Add("InitialDelay < 1ms may not allow service recovery");

        if (policy.InitialDelay > TimeSpan.FromMinutes(5))
            report.Warnings.Add("InitialDelay > 5 minutes may be too long");
    }

    private static void ValidateBulkhead(BulkheadPolicy policy, ValidationReport report)
    {
        if (policy.MaxParallelization < 1)
            report.Errors.Add("MaxParallelization must be at least 1");

        if (policy.MaxQueueLength < 0)
            report.Errors.Add("MaxQueueLength cannot be negative");

        if (policy.MaxParallelization > 10000)
            report.Warnings.Add("MaxParallelization > 10000 may cause memory issues");

        if (policy.MaxParallelization < policy.MaxQueueLength / 10)
            report.Warnings.Add("Queue length is much larger than parallelization limit");
    }

    private static void ValidateTimeout(TimeoutPolicy policy, ValidationReport report)
    {
        if (policy.Timeout < TimeSpan.FromMilliseconds(1))
            report.Errors.Add("Timeout must be at least 1 millisecond");

        if (policy.Timeout > TimeSpan.FromHours(1))
            report.Warnings.Add("Timeout > 1 hour is unusual");
    }

    /// <summary>
    /// Identifies potential anti-patterns in policy configuration.
    /// </summary>
    public static List<string> IdentifyAntiPatterns(ResiliencyPolicy policy)
    {
        var antiPatterns = new List<string>();

        if (policy is CircuitBreakerPolicy cb)
        {
            if (cb.FailureThreshold > 100 && cb.OpenDuration < TimeSpan.FromSeconds(10))
                antiPatterns.Add("High failure threshold with short open duration may not allow recovery");
        }

        if (policy is RetryPolicy retry)
        {
            if (retry.MaxRetries > 10 && retry.Strategy == RetryPolicy.BackoffStrategy.Exponential)
                antiPatterns.Add("Many retries with exponential backoff may exceed timeout");
        }

        if (!policy.IsEnabled)
            antiPatterns.Add("Policy is disabled - it won't provide any protection");

        return antiPatterns;
    }

    /// <summary>
    /// Provides optimization suggestions for a policy.
    /// </summary>
    public static List<string> SuggestOptimizations(ResiliencyPolicy policy)
    {
        var suggestions = new List<string>();

        if (policy is CircuitBreakerPolicy cb)
        {
            if (cb.SuccessThresholdInHalfOpen == 1)
                suggestions.Add("Consider increasing SuccessThresholdInHalfOpen to reduce false positives");

            if (cb.OpenDuration < TimeSpan.FromSeconds(60))
                suggestions.Add("Longer OpenDuration may improve recovery chances");
        }

        if (policy is RetryPolicy retry)
        {
            if (retry.Strategy == RetryPolicy.BackoffStrategy.Fixed)
                suggestions.Add("Exponential backoff may be more efficient than fixed delays");

            if (retry.MaxRetries < 3)
                suggestions.Add("At least 3 retries is recommended for transient failures");
        }

        if (policy is BulkheadPolicy bulkhead)
        {
            if (bulkhead.MaxQueueLength == 0)
                suggestions.Add("Queue length 0 will reject all requests when bulkhead is full");
        }

        return suggestions;
    }
}

/// <summary>
/// Detailed validation report for a policy.
/// </summary>
public sealed class ValidationReport
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public List<string> Errors { get; set; } = new();
    public List<string> Warnings { get; set; } = new();
    public List<string> Suggestions { get; set; } = new();

    public bool IsValid => Errors.Count == 0;

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Validation Report for {PolicyName} ({PolicyId})");
        sb.AppendLine(new string('=', 50));

        if (IsValid)
            sb.AppendLine("✓ Policy configuration is valid");
        else
            sb.AppendLine("✗ Policy has configuration errors");

        if (Errors.Count > 0)
        {
            sb.AppendLine("\nErrors:");
            foreach (var error in Errors)
                sb.AppendLine($"  ✗ {error}");
        }

        if (Warnings.Count > 0)
        {
            sb.AppendLine("\nWarnings:");
            foreach (var warning in Warnings)
                sb.AppendLine($"  ⚠ {warning}");
        }

        if (Suggestions.Count > 0)
        {
            sb.AppendLine("\nSuggestions:");
            foreach (var suggestion in Suggestions)
                sb.AppendLine($"  → {suggestion}");
        }

        return sb.ToString();
    }
}
