#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// ====================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.ComponentModel.DataAnnotations;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Injects artificial failures, latency, and exceptions into operations to verify
/// that resilience policies behave correctly under adverse conditions.
/// </summary>
public sealed class FailureInjectionService
{
    private readonly ILogger<FailureInjectionService> _logger;
    private readonly Dictionary<string, InjectionRule> _rules = new(StringComparer.OrdinalIgnoreCase);
    private readonly object _lock = new();

    /// <summary>
    /// Total number of injections performed across all active rules.
    /// </summary>
    public long TotalInjections { get; private set; }

    /// <summary>
    /// Initializes the service with an optional logger.
    /// </summary>
    public FailureInjectionService(ILogger<FailureInjectionService>? logger = null)
    {
        _logger = logger ?? NullLogger<FailureInjectionService>.Instance;
    }

    public override string ToString() => $"FailureInjectionService {{ TotalInjections = {TotalInjections}, RulesCount = {_rules.Count} }}";

    // ─── rule management ──────────────────────────────────────────────────────

    /// <summary>
    /// Registers an injection rule. Any existing rule with the same key is replaced.
    /// </summary>
    public void AddRule(InjectionRule rule)
    {
        if (rule is null) throw new ArgumentNullException(nameof(rule));
        if (string.IsNullOrWhiteSpace(rule.Key)) throw new ArgumentException("Rule key cannot be empty", nameof(rule));

        _logger.LogInformation("Adding failure injection rule {Key} (type={Type})", rule.Key, rule.Type);

        lock (_lock)
        {
            _rules[rule.Key] = rule;
        }

        _logger.LogDebug("Failure injection rule '{Key}' registered (type={Type})", rule.Key, rule.Type);
    }

    /// <summary>
    /// Removes the injection rule with the given key.
    /// </summary>
    public bool RemoveRule(string key)
    {
        _logger.LogInformation("Attempting to remove failure injection rule {Key}", key);

        lock (_lock)
        {
            var removed = _rules.Remove(key);
            if (removed)
                _logger.LogInformation("Successfully removed failure injection rule {Key}", key);
            else
                _logger.LogInformation("Failed to remove failure injection rule {Key} (not found)", key);
            return removed;
        }
    }

    /// <summary>
    /// Returns all currently registered injection rules.
    /// </summary>
    public IReadOnlyList<InjectionRule> GetRules()
    {
        _logger.LogInformation("Retrieving all failure injection rules");

        lock (_lock)
        {
            var rules = _rules.Values.ToList();
            _logger.LogInformation("Retrieved {Count} failure injection rules", rules.Count);
            return rules;
        }
    }

    /// <summary>
    /// Disables all injection rules without removing them.
    /// </summary>
    public void DisableAll()
    {
        _logger.LogInformation("Disabling all failure injection rules");

        lock (_lock)
        {
            var count = 0;
            foreach (var rule in _rules.Values)
            {
                rule.IsEnabled = false;
                count++;
            }
            _logger.LogInformation("Disabled {Count} failure injection rules", count);
        }
    }

    // ─── execution ────────────────────────────────────────────────────────────

    /// <summary>
    /// Executes <paramref name="operation"/> with failure injection applied for the given
    /// <paramref name="ruleKey"/>. If no active rule exists the operation runs normally.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        string ruleKey,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting execution with rule key {RuleKey}", ruleKey);
        try
        {
            if (operation is null) throw new ArgumentNullException(nameof(operation));

            InjectionRule? rule;
            lock (_lock)
            {
                _rules.TryGetValue(ruleKey, out rule);
            }

            if (rule is null || !rule.IsEnabled || !ShouldInject(rule))
            {
                _logger.LogInformation("No active injection rule for key {RuleKey}, executing normally", ruleKey);
                return await operation(cancellationToken);
            }

            lock (_lock) { TotalInjections++; }
            rule.InjectionsPerformed++;

            _logger.LogWarning("Injecting {Type} failure for rule '{Key}'", rule.Type, rule.Key);

            return rule.Type switch
            {
                InjectionType.Exception => InjectException<T>(rule),
                InjectionType.Latency => await InjectLatencyAsync(rule, operation, cancellationToken),
                InjectionType.Timeout => await InjectTimeoutAsync<T>(rule, cancellationToken),
                _ => await operation(cancellationToken)
            };
        }
        finally
        {
            _logger.LogInformation("Finished execution with rule key {RuleKey} (total injections: {TotalInjections})", ruleKey, TotalInjections);
        }
    }

    /// <summary>
    /// Executes a void operation with failure injection applied.
    /// </summary>
    public async Task ExecuteAsync(
        string ruleKey,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Starting void execution with rule key {RuleKey}", ruleKey);
        try
        {
            await ExecuteAsync<object?>(ruleKey,
                async ct => { await operation(ct); return null; },
                cancellationToken);
        }
        finally
        {
            _logger.LogInformation("Finished void execution with rule key {RuleKey}", ruleKey);
        }
    }

    // ─── time window methods ────────────────────────────────────────────────────

    /// <summary>
    /// Pure method to check if a rule is active at a specific time.
    /// </summary>
    /// <param name="rule">The injection rule to check.</param>
    /// <param name="at">The DateTimeOffset to check against.</param>
    /// <returns>True if the rule is active at the specified time, false otherwise.</returns>
    public static bool IsActiveAt(InjectionRule rule, DateTimeOffset at)
    {
        // If no time window is configured, always active
        if (string.IsNullOrEmpty(rule.StartTime) && string.IsNullOrEmpty(rule.EndTime))
            return true;

        var time = TimeOnly.FromDateTime(at.DateTime);

        // Parse start time if configured
        TimeOnly? start = null;
        if (!string.IsNullOrEmpty(rule.StartTime))
        {
            var startParts = rule.StartTime.Split(':');
            var startHour = int.Parse(startParts[0]);
            var startMinute = int.Parse(startParts[1]);
            start = new TimeOnly(startHour, startMinute);
        }

        // Parse end time if configured
        TimeOnly? end = null;
        if (!string.IsNullOrEmpty(rule.EndTime))
        {
            var endParts = rule.EndTime.Split(':');
            var endHour = int.Parse(endParts[0]);
            var endMinute = int.Parse(endParts[1]);
            end = new TimeOnly(endHour, endMinute);
        }

        // If only start time configured, active from start time onwards
        if (start.HasValue && !end.HasValue)
            return time >= start.Value;

        // If only end time configured, active until end time
        if (!start.HasValue && end.HasValue)
            return time <= end.Value;

        // Both start and end configured - active between them (inclusive)
        return time >= start!.Value && time <= end!.Value;
    }

    // ─── injection strategies ─────────────────────────────────────────────────

    private static bool ShouldInject(InjectionRule rule)
    {
        // Check time window first
        if (!IsActiveAt(rule, DateTimeOffset.Now))
            return false;

        if (rule.InjectionRate >= 1.0) return true;
        if (rule.InjectionRate <= 0.0) return false;
        return Random.Shared.NextDouble() < rule.InjectionRate;
    }

    private static T InjectException<T>(InjectionRule rule)
    {
        var exceptionType = rule.ExceptionFactory?.Invoke()
        ?? new InjectedFaultException(rule.Key, rule.ExceptionMessage ?? "Injected fault");
        throw exceptionType;
    }

    private static async Task<T> InjectLatencyAsync<T>(
        InjectionRule rule,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken)
    {
        var delay = rule.LatencyDelay ?? TimeSpan.FromMilliseconds(500);
        await Task.Delay(delay, cancellationToken);
        return await operation(cancellationToken);
    }

    private static async Task<T> InjectTimeoutAsync<T>(InjectionRule rule, CancellationToken cancellationToken)
    {
        var delay = rule.TimeoutDuration ?? TimeSpan.FromSeconds(30);
        await Task.Delay(delay, cancellationToken);
        throw new OperationCanceledException($"Injected timeout after {delay.TotalSeconds}s for rule '{rule.Key}'");
    }
}

/// <summary>
/// Defines the type of fault to inject.
/// </summary>
public enum InjectionType
{
    /// <summary>Throw a configurable exception.</summary>
    Exception,

    /// <summary>Add artificial latency before delegating to the real operation.</summary>
    Latency,

    /// <summary>Simulate a hung operation that never completes within a reasonable time.</summary>
    Timeout
}

/// <summary>
/// Describes when and how to inject a fault.
/// </summary>
public sealed class InjectionRule
{
    /// <summary>Unique key identifying this rule (e.g. "payment-service").</summary>
    public string Key { get; set; } = string.Empty;

    /// <summary>Type of fault to inject.</summary>
    public InjectionType Type { get; set; } = InjectionType.Exception;

    /// <summary>Whether the rule is active. Defaults to <c>true</c>.</summary>
    public bool IsEnabled { get; set; } = true;

    /// <summary>
    /// Probability (0.0–1.0) that the fault is injected on each call.
    /// Use 1.0 to always inject; 0.0 to never inject.
    /// </summary>
    public double InjectionRate { get; set; } = 1.0;

    /// <summary>Exception message used when <see cref="ExceptionFactory"/> is null.</summary>
    public string? ExceptionMessage { get; set; }

    /// <summary>Factory that returns the exception to throw (optional; overrides <see cref="ExceptionMessage"/>).</summary>
    public Func<Exception>? ExceptionFactory { get; set; }

    /// <summary>Delay added before the operation when <see cref="Type"/> is <see cref="InjectionType.Latency"/>.</summary>
    public TimeSpan? LatencyDelay { get; set; }

    /// <summary>Duration of the simulated hang when <see cref="Type"/> is <see cref="InjectionType.Timeout"/>.</summary>
    public TimeSpan? TimeoutDuration { get; set; }

    /// <summary>Running count of how many times this rule has triggered an injection.</summary>
    public long InjectionsPerformed { get; internal set; }

    /// <summary>
    /// Start time of the active window (inclusive).
    /// Format: HH:mm (24-hour format). Default: null (always active).
    /// </summary>
    [RegularExpression("^([01]?[0-9]|2[0-3]):([0-5][0-9])$",
        ErrorMessage = "StartTime must be in HH:mm format (24-hour clock)")]
    public string? StartTime { get; set; }

    /// <summary>
    /// End time of the active window (inclusive).
    /// Format: HH:mm (24-hour format). Default: null (always active).
    /// </summary>
    [RegularExpression("^([01]?[0-9]|2[0-3]):([0-5][0-9])$",
        ErrorMessage = "EndTime must be in HH:mm format (24-hour clock)")]
    public string? EndTime { get; set; }

    public override string ToString() => $"InjectionRule {{ Key = {Key}, Type = {Type}, IsEnabled = {IsEnabled}, InjectionRate = {InjectionRate}, ExceptionMessage = {ExceptionMessage}, ExceptionFactory = {ExceptionFactory} }}";
}

/// <summary>
/// Exception thrown by the failure injection service to represent an injected fault.
/// </summary>
public sealed class InjectedFaultException : Exception
{
    /// <summary>Rule key that triggered this injection.</summary>
    public string RuleKey { get; }

    /// <summary>
    /// Initializes a new <see cref="InjectedFaultException"/>.
    /// </summary>
    public InjectedFaultException(string ruleKey, string message)
        : base(message)
    {
        RuleKey = ruleKey;
    }
}