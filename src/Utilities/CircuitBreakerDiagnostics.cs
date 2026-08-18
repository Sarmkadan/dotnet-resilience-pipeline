#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using DotNetResiliencePipeline.Domain.Policies;
using System.Text;

namespace DotNetResiliencePipeline.Utilities;

/// <summary>
/// Diagnostic utilities for circuit breaker analysis and troubleshooting.
/// Provides detailed state information and recommendations for optimization.
/// </summary>
public static class CircuitBreakerDiagnostics
{
    /// <summary>
    /// Generates a detailed diagnostic report for a circuit breaker.
    /// </summary>
    public static CircuitBreakerDiagnosticReport GenerateDiagnosticReport(CircuitBreakerPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var report = new CircuitBreakerDiagnosticReport
        {
            PolicyId = policy.Id,
            PolicyName = policy.Name,
            CurrentState = policy.CurrentState,
            FailureThreshold = policy.FailureThreshold,
            OpenDuration = policy.OpenDuration,
            SuccessThreshold = policy.SuccessThresholdInHalfOpen,
            GeneratedAt = DateTime.UtcNow
        };

        // Check for potential issues
        report.Issues.AddRange(IdentifyIssues(policy));
        report.Recommendations.AddRange(GetRecommendations(policy));

        return report;
    }

    /// <summary>
    /// Identifies issues with circuit breaker configuration.
    /// </summary>
    private static List<string> IdentifyIssues(CircuitBreakerPolicy policy)
    {
        var issues = new List<string>();

        if (policy.FailureThreshold < 2)
            issues.Add("Low failure threshold may cause unnecessary circuit opens");

        if (policy.FailureThreshold > 100)
            issues.Add("High failure threshold may allow too many failures before opening");

        if (policy.OpenDuration < TimeSpan.FromSeconds(10))
            issues.Add("Very short open duration may cause rapid state cycling");

        if (policy.OpenDuration > TimeSpan.FromHours(1))
            issues.Add("Very long open duration may prevent service recovery");

        if (policy.SuccessThresholdInHalfOpen == 1)
            issues.Add("Allowing single success in half-open may be risky");

        if (!policy.IsEnabled)
            issues.Add("Circuit breaker is disabled and provides no protection");

        return issues;
    }

    /// <summary>
    /// Gets optimization recommendations for a circuit breaker.
    /// </summary>
    private static List<string> GetRecommendations(CircuitBreakerPolicy policy)
    {
        var recommendations = new List<string>();

        if (policy.FailureThreshold < 5)
            recommendations.Add("Consider increasing failure threshold to reduce false positives");

        if (policy.OpenDuration < TimeSpan.FromSeconds(30))
            recommendations.Add("Increase open duration to allow sufficient service recovery time");

        if (policy.SuccessThresholdInHalfOpen < 3)
            recommendations.Add("Increase success threshold for more reliable recovery validation");

        // Add state-specific recommendations
        if (policy.CurrentState == CircuitBreakerPolicy.CircuitState.Open)
        {
            recommendations.Add("Circuit is currently OPEN - monitor service health before attempting operations");
        }
        else if (policy.CurrentState == CircuitBreakerPolicy.CircuitState.HalfOpen)
        {
            recommendations.Add("Circuit is in HALF-OPEN state - test operations will determine if service recovered");
        }

        return recommendations;
    }

    /// <summary>
    /// Analyzes circuit breaker effectiveness.
    /// </summary>
    public static CircuitBreakerEffectiveness AnalyzeEffectiveness(
        CircuitBreakerPolicy policy,
        long totalExecutions,
        long failedExecutions)
    {
        ArgumentNullException.ThrowIfNull(policy);

        var effectiveness = new CircuitBreakerEffectiveness
        {
            PolicyName = policy.Name,
            TotalExecutions = totalExecutions,
            FailedExecutions = failedExecutions,
            FailureRate = totalExecutions > 0 ? (failedExecutions * 100.0) / totalExecutions : 0,
            CurrentState = policy.CurrentState
        };

        // Rate effectiveness
        if (effectiveness.FailureRate < 5)
        {
            effectiveness.EffectivenessRating = "Excellent";
            effectiveness.IsProblematic = false;
        }
        else if (effectiveness.FailureRate < 15)
        {
            effectiveness.EffectivenessRating = "Good";
            effectiveness.IsProblematic = false;
        }
        else if (effectiveness.FailureRate < 30)
        {
            effectiveness.EffectivenessRating = "Fair";
            effectiveness.IsProblematic = true;
        }
        else
        {
            effectiveness.EffectivenessRating = "Poor";
            effectiveness.IsProblematic = true;
        }

        return effectiveness;
    }

    /// <summary>
    /// Suggests optimal circuit breaker configuration based on observed failure patterns.
    /// </summary>
    public static CircuitBreakerConfiguration SuggestOptimalConfiguration(
        string policyName,
        double observedFailureRate,
        long averageRecoveryTimeMs)
    {
        ArgumentException.ThrowIfNullOrEmpty(policyName);

        var config = new CircuitBreakerConfiguration { PolicyName = policyName };

        // Suggest failure threshold based on failure rate
        config.SuggestedFailureThreshold = observedFailureRate switch
        {
            < 5 => 10,  // Very stable - higher threshold
            < 15 => 7,  // Mostly stable
            < 30 => 5,  // Moderate instability
            _ => 3      // High instability - lower threshold
        };

        // Suggest open duration based on recovery time
        var recoveryDuration = TimeSpan.FromMilliseconds(averageRecoveryTimeMs);
        config.SuggestedOpenDuration = recoveryDuration.TotalMilliseconds switch
        {
            < 1000 => TimeSpan.FromSeconds(10),
            < 5000 => TimeSpan.FromSeconds(30),
            < 30000 => TimeSpan.FromMinutes(1),
            _ => TimeSpan.FromMinutes(2)
        };

        config.SuggestedSuccessThreshold = observedFailureRate switch
        {
            < 5 => 2,   // Low risk - can use lower threshold
            < 15 => 3,  // Moderate
            < 30 => 5,  // Higher risk - require more confirmations
            _ => 10     // Very high risk
        };

        return config;
    }
}

/// <summary>
/// Diagnostic report for a circuit breaker policy.
/// </summary>
public sealed class CircuitBreakerDiagnosticReport
{
    public string PolicyId { get; set; } = string.Empty;
    public string PolicyName { get; set; } = string.Empty;
    public CircuitBreakerPolicy.CircuitState CurrentState { get; set; }
    public int FailureThreshold { get; set; }
    public TimeSpan OpenDuration { get; set; }
    public int SuccessThreshold { get; set; }
    public DateTime GeneratedAt { get; set; }
    public List<string> Issues { get; set; } = new();
    public List<string> Recommendations { get; set; } = new();

    public bool HasIssues => Issues.Count > 0;

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Circuit Breaker Diagnostic Report: {PolicyName}");
        sb.AppendLine(new string('=', 50));
        sb.AppendLine($"Current State: {CurrentState}");
        sb.AppendLine($"Failure Threshold: {FailureThreshold}");
        sb.AppendLine($"Open Duration: {OpenDuration.TotalSeconds}s");
        sb.AppendLine($"Success Threshold (Half-Open): {SuccessThreshold}");

        if (Issues.Count > 0)
        {
            sb.AppendLine("\nIssues Detected:");
            foreach (var issue in Issues)
                sb.AppendLine($"  ⚠ {issue}");
        }

        if (Recommendations.Count > 0)
        {
            sb.AppendLine("\nRecommendations:");
            foreach (var rec in Recommendations)
                sb.AppendLine($"  → {rec}");
        }

        return sb.ToString();
    }
}

/// <summary>
/// Effectiveness analysis for a circuit breaker.
/// </summary>
public sealed class CircuitBreakerEffectiveness
{
    public string PolicyName { get; set; } = string.Empty;
    public long TotalExecutions { get; set; }
    public long FailedExecutions { get; set; }
    public double FailureRate { get; set; }
    public CircuitBreakerPolicy.CircuitState CurrentState { get; set; }
    public string EffectivenessRating { get; set; } = string.Empty;
    public bool IsProblematic { get; set; }
}

/// <summary>
/// Configuration suggestion for circuit breaker.
/// </summary>
public sealed class CircuitBreakerConfiguration
{
    public string PolicyName { get; set; } = string.Empty;
    public int SuggestedFailureThreshold { get; set; }
    public TimeSpan SuggestedOpenDuration { get; set; }
    public int SuggestedSuccessThreshold { get; set; }

    public override string ToString()
    {
        return $"Suggested Configuration for {PolicyName}:\n" +
               $"  Failure Threshold: {SuggestedFailureThreshold}\n" +
               $"  Open Duration: {SuggestedOpenDuration.TotalSeconds}s\n" +
               $"  Success Threshold: {SuggestedSuccessThreshold}";
    }
}
