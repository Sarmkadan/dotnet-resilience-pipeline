#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

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

    // ─── rule management ──────────────────────────────────────────────────────

    /// <summary>
    /// Registers an injection rule. Any existing rule with the same key is replaced.
    /// </summary>
    public void AddRule(InjectionRule rule)
    {
        if (rule is null) throw new ArgumentNullException(nameof(rule));
        if (string.IsNullOrWhiteSpace(rule.Key)) throw new ArgumentException("Rule key cannot be empty", nameof(rule));

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
        lock (_lock)
        {
            return _rules.Remove(key);
        }
    }

    /// <summary>
    /// Returns all currently registered injection rules.
    /// </summary>
    public IReadOnlyList<InjectionRule> GetRules()
    {
        lock (_lock)
        {
            return _rules.Values.ToList();
        }
    }

    /// <summary>
    /// Disables all injection rules without removing them.
    /// </summary>
    public void DisableAll()
    {
        lock (_lock)
        {
            foreach (var rule in _rules.Values)
                rule.IsEnabled = false;
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
        if (operation is null) throw new ArgumentNullException(nameof(operation));

        InjectionRule? rule;
        lock (_lock)
        {
            _rules.TryGetValue(ruleKey, out rule);
        }

        if (rule is null || !rule.IsEnabled || !ShouldInject(rule))
            return await operation(cancellationToken);

        lock (_lock) { TotalInjections++; }
        rule.InjectionsPerformed++;

        _logger.LogWarning("Injecting {Type} failure for rule '{Key}'", rule.Type, rule.Key);

        return rule.Type switch
        {
            InjectionType.Exception => InjectException<T>(rule),
            InjectionType.Latency   => await InjectLatencyAsync(rule, operation, cancellationToken),
            InjectionType.Timeout   => await InjectTimeoutAsync<T>(rule, cancellationToken),
            _ => await operation(cancellationToken)
        };
    }

    /// <summary>
    /// Executes a void operation with failure injection applied.
    /// </summary>
    public async Task ExecuteAsync(
        string ruleKey,
        Func<CancellationToken, Task> operation,
        CancellationToken cancellationToken = default)
    {
        await ExecuteAsync<object?>(ruleKey,
            async ct => { await operation(ct); return null; },
            cancellationToken);
    }

    // ─── injection strategies ─────────────────────────────────────────────────

    private static bool ShouldInject(InjectionRule rule)
    {
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
