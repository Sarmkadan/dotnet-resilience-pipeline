#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Diagnostics;
using DotNetResiliencePipeline.Domain.Policies;
using DotNetResiliencePipeline.Exceptions;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Service handling circuit breaker policy execution and state management.
/// </summary>
public class CircuitBreakerService
{
    /// <summary>
    /// Executes an operation through the circuit breaker policy.
    /// </summary>
    public async Task<T> ExecuteAsync<T>(
        CircuitBreakerPolicy policy,
        Func<Task<T>> operation)
    {
        if (policy is null)
            throw new ArgumentNullException(nameof(policy));

        if (!policy.IsEnabled)
            return await operation();

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

        var stopwatch = Stopwatch.StartNew();

        try
        {
            var result = await operation();
            stopwatch.Stop();

            policy.RecordSuccess();

            return result;
        }
        catch (Exception ex)
        {
            stopwatch.Stop();
            policy.RecordFailure();

            throw;
        }
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
