#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Service handling circuit breaker policy execution and state management.
/// </summary>
public sealed class CircuitBreakerService
{
    private readonly ILogger<CircuitBreakerService> _logger;
    private readonly int _halfOpenMaxProbes;
    private readonly ConcurrentDictionary<CircuitBreakerPolicy, ProbeState> _probeStates = new();

    /// <summary>
    /// Holds the current number of concurrent probe executions for a given policy.
    /// </summary>
    private sealed class ProbeState
    {
        public int Current;
    }

    /// <summary>
    /// Initializes the service with an optional logger and an optional limit for concurrent
    /// half‑open probes (default is 1).
    /// </summary>
    /// <param name="logger">Optional logger instance.</param>
    /// <param name="halfOpenMaxProbes">
    /// Maximum number of concurrent probe calls allowed while the circuit is half‑open.
    /// Must be greater than zero.
    /// </param>
    public CircuitBreakerService(ILogger<CircuitBreakerService>? logger = null, int halfOpenMaxProbes = 1)
    {
        _logger = logger ?? NullLogger<CircuitBreakerService>.Instance;
        if (halfOpenMaxProbes < 1)
            throw new ArgumentOutOfRangeException(nameof(halfOpenMaxProbes), "HalfOpenMaxProbes must be at least 1.");
        _halfOpenMaxProbes = halfOpenMaxProbes;
    }

    /// <summary>
    /// Executes an operation through the circuit breaker policy.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        CircuitBreakerPolicy policy,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (!policy.IsEnabled)
            return await operation(cancellationToken);

        cancellationToken.ThrowIfCancellationRequested();

        // Check if circuit should transition to half-open
        policy.AttemptReset();

        var currentState = policy.CurrentState;

        if (currentState == CircuitBreakerPolicy.CircuitState.Open)
        {
            throw new CircuitBreakerOpenException(
                policy.Name,
                policy.TimeUntilHalfOpen ?? TimeSpan.Zero,
                policy.ConsecutiveFailures);
        }

        // --------------------------------------------------------------------
        // Half‑open probe concurrency limiting
        // --------------------------------------------------------------------
        bool isHalfOpen = currentState == CircuitBreakerPolicy.CircuitState.HalfOpen;
        ProbeState? probeState = null;

        if (isHalfOpen)
        {
            probeState = _probeStates.GetOrAdd(policy, _ => new ProbeState());

            // Increment the concurrent probe counter atomically
            int currentProbes = Interlocked.Increment(ref probeState.Current);

            // If we exceed the allowed limit, fail fast with the same exception
            if (currentProbes > _halfOpenMaxProbes)
            {
                // Roll back the increment
                Interlocked.Decrement(ref probeState.Current);

                throw new CircuitBreakerOpenException(
                    policy.Name,
                    policy.TimeUntilHalfOpen ?? TimeSpan.Zero,
                    policy.ConsecutiveFailures);
            }
        }

        var stateBefore = policy.CurrentState;
        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await operation(cancellationToken);
            stopwatch.Stop();

            policy.RecordSuccess();

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            // Do not count external cancellation as a circuit breaker failure
            throw;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            policy.RecordFailure();

            if (stateBefore != CircuitBreakerPolicy.CircuitState.Open
                && policy.CurrentState == CircuitBreakerPolicy.CircuitState.Open)
            {
                _logger.LogWarning(
                    "Circuit breaker '{PolicyName}' tripped to Open after {Trips} total trip(s). " +
                    "Consecutive failures: {ConsecutiveFailures}. Last error: {ErrorMessage}",
                    policy.Name, policy.CircuitBreakerTrips, policy.ConsecutiveFailures, ex.Message);
            }

            throw;
        }
        finally
        {
            // Decrement the probe counter if we entered half‑open mode
            if (isHalfOpen && probeState != null)
            {
                Interlocked.Decrement(ref probeState.Current);
            }
        }
    }

    /// <summary>
    /// Executes an operation through the circuit breaker policy (without cancellation support).
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        CircuitBreakerPolicy policy,
        Func<Task<T>> operation)
    {
        return await ExecuteAsync(policy, _ => operation(), CancellationToken.None);
    }

    /// <summary>
    /// Manually opens a circuit breaker.
    /// </summary>
    public void OpenCircuit(CircuitBreakerPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        // Simulate opening by setting high failure count
        policy.RecordFailure();
    }

    /// <summary>
    /// Manually resets a circuit breaker.
    /// </summary>
    public void ResetCircuit(CircuitBreakerPolicy policy)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        policy.ManualReset();
    }

    /// <summary>
    /// Gets the current state of a circuit breaker.
    /// </summary>
    public string GetCircuitState(CircuitBreakerPolicy policy)
    {
        return policy?.CurrentState.ToString() ?? "Unknown";
    }
}
