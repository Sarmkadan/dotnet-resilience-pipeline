#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
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

    /// <summary>
    /// Initializes the service with an optional logger.
    /// </summary>
    public CircuitBreakerService(ILogger<CircuitBreakerService>? logger = null)
    {
        _logger = logger ?? NullLogger<CircuitBreakerService>.Instance;
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
