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
    private readonly ConcurrentDictionary<CircuitBreakerPolicy, HalfOpenEntry> _halfOpenEntries = new();

    /// <summary>
    /// Holds the current number of concurrent probe executions for a given policy.
    /// </summary>
    private sealed class ProbeState
    {
        public int Current;
    }

    /// <summary>
    /// Tracks whether the half‑open transition has already been performed for a given policy.
    /// </summary>
    private sealed class HalfOpenEntry
    {
        public int Entered;
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
    /// <exception cref="ArgumentOutOfRangeException">When <paramref name="halfOpenMaxProbes"/> is less than 1.</exception>
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
    /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
    /// <param name="policy">The circuit breaker policy to apply.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <param name="cancellationToken">Optional cancellation token.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="policy"/> or <paramref name="operation"/> is <c>null</c>.</exception>
    /// <exception cref="CircuitBreakerOpenException">When the circuit is open or the half‑open probe limit is exceeded.</exception>
    public async Task<T> ExecuteAsync<T>(
        CircuitBreakerPolicy policy,
        Func<CancellationToken, Task<T>> operation,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(policy);
        ArgumentNullException.ThrowIfNull(operation);

        if (!policy.IsEnabled)
            return await operation(cancellationToken).ConfigureAwait(false);

        cancellationToken.ThrowIfCancellationRequested();

        // --------------------------------------------------------------------
        // Ensure a single thread performs the Open → HalfOpen transition.
        // --------------------------------------------------------------------
        if (policy.CurrentState == CircuitBreakerPolicy.CircuitState.Open)
        {
            var entry = _halfOpenEntries.GetOrAdd(policy, _ => new HalfOpenEntry());

            // Attempt to claim the transition. Only the thread that sees 0 → 1 proceeds.
            if (Interlocked.CompareExchange(ref entry.Entered, 1, 0) == 0)
            {
                // This thread performs the transition.
                policy.AttemptReset();
            }
            else
            {
                // Another thread already performed the transition; treat as still open.
                throw new CircuitBreakerOpenException(
                    policy.Name,
                    policy.TimeUntilHalfOpen ?? TimeSpan.Zero,
                    policy.ConsecutiveFailures);
            }
        }

        // At this point the policy may be HalfOpen or Closed.
        var currentState = policy.CurrentState;

        if (currentState == CircuitBreakerPolicy.CircuitState.Open)
        {
            // The policy could have reverted to Open (e.g., due to a race); reject the call.
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

            // Increment the concurrent probe counter atomically.
            int currentProbes = Interlocked.Increment(ref probeState.Current);

            // If we exceed the allowed limit, fail fast with the same exception.
            if (currentProbes > _halfOpenMaxProbes)
            {
                // Roll back the increment.
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
            var result = await operation(cancellationToken).ConfigureAwait(false);
            stopwatch.Stop();

            policy.RecordSuccess();

            return result;
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            stopwatch.Stop();
            // Do not count external cancellation as a circuit breaker failure.
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
                    policy.Name,
                    policy.CircuitBreakerTrips,
                    policy.ConsecutiveFailures,
                    ex.Message);
            }

            throw;
        }
        finally
        {
            // Decrement the probe counter if we entered half‑open mode.
            if (isHalfOpen && probeState != null)
            {
                Interlocked.Decrement(ref probeState.Current);
            }

            // Clean up half‑open entry when the circuit is no longer in HalfOpen state.
            if (policy.CurrentState != CircuitBreakerPolicy.CircuitState.HalfOpen)
            {
                _halfOpenEntries.TryRemove(policy, out _);
                _probeStates.TryRemove(policy, out _);
            }
        }
    }

    /// <summary>
    /// Executes an operation through the circuit breaker policy (without cancellation support).
    /// </summary>
    /// <typeparam name="T">The type of the result returned by the operation.</typeparam>
    /// <param name="policy">The circuit breaker policy to apply.</param>
    /// <param name="operation">The operation to execute.</param>
    /// <returns>The result of the operation.</returns>
    /// <exception cref="ArgumentNullException">When <paramref name="policy"/> or <paramref name="operation"/> is <c>null</c>.</exception>
    /// <exception cref="CircuitBreakerOpenException">When the circuit is open.</exception>
    public Task<T> ExecuteAsync<T>(CircuitBreakerPolicy policy, Func<Task<T>> operation) =>
        ExecuteAsync(policy, _ => operation(), CancellationToken.None);

    /// <summary>
    /// Manually opens a circuit breaker.
    /// </summary>
    /// <param name="policy">The circuit breaker policy to open.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="policy"/> is <c>null</c>.</exception>
    public void OpenCircuit(CircuitBreakerPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        // Simulate opening by setting high failure count.
        policy.RecordFailure();
    }

    /// <summary>
    /// Manually resets a circuit breaker.
    /// </summary>
    /// <param name="policy">The circuit breaker policy to reset.</param>
    /// <exception cref="ArgumentNullException">When <paramref name="policy"/> is <c>null</c>.</exception>
    public void ResetCircuit(CircuitBreakerPolicy policy)
    {
        ArgumentNullException.ThrowIfNull(policy);
        policy.ManualReset();
    }

    /// <summary>
    /// Gets the current state of a circuit breaker.
    /// </summary>
    /// <param name="policy">The circuit breaker policy.</param>
    /// <returns>The name of the current state, or <c>"Unknown"</c> if <paramref name="policy"/> is <c>null</c>.</returns>
    public string GetCircuitState(CircuitBreakerPolicy? policy) =>
        policy?.CurrentState.ToString() ?? "Unknown";
}
