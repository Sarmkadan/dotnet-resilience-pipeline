#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using DotNetResiliencePipeline.Domain.Policies;

namespace DotNetResiliencePipeline.Services;

/// <summary>
/// Extension methods for <see cref="CircuitBreakerService"/> that inspect and reset a set of circuit breaker policies.
/// The service does not track the policies it has executed, so callers pass the policies to examine.
/// </summary>
public static class CircuitBreakerServiceExtensions
{
    /// <summary>
    /// Determines whether any of the given circuit breaker policies is currently open.
    /// </summary>
    /// <param name="service">The circuit breaker service that owns the policies.</param>
    /// <param name="policies">The circuit breaker policies to inspect.</param>
    /// <returns><c>true</c> if at least one policy is in the <see cref="CircuitBreakerPolicy.CircuitState.Open"/> state; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="policies"/> is null, or if <paramref name="policies"/> contains a null entry.</exception>
    public static bool IsAnyCircuitOpen(this CircuitBreakerService service, IEnumerable<CircuitBreakerPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(policies);

        return policies.Any(policy => GetState(service, policy) == CircuitBreakerPolicy.CircuitState.Open);
    }

    /// <summary>
    /// Gets the names of the given circuit breaker policies that are currently open.
    /// </summary>
    /// <param name="service">The circuit breaker service that owns the policies.</param>
    /// <param name="policies">The circuit breaker policies to inspect.</param>
    /// <returns>The names of the open policies, in the order they were supplied. The list is empty if none are open.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="policies"/> is null, or if <paramref name="policies"/> contains a null entry.</exception>
    public static IReadOnlyList<string> GetOpenCircuitNames(this CircuitBreakerService service, IEnumerable<CircuitBreakerPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(policies);

        return policies
            .Where(policy => GetState(service, policy) == CircuitBreakerPolicy.CircuitState.Open)
            .Select(policy => policy.Name)
            .ToList();
    }

    /// <summary>
    /// Attempts to get the current state of the circuit breaker policy with the given name.
    /// </summary>
    /// <param name="service">The circuit breaker service that owns the policies.</param>
    /// <param name="policies">The circuit breaker policies to search.</param>
    /// <param name="name">The name of the policy to look up. Comparison is ordinal and case-sensitive.</param>
    /// <param name="state">When this method returns <c>true</c>, the current state of the matching policy; otherwise the default value.</param>
    /// <returns><c>true</c> if a policy with the given name was found; otherwise <c>false</c>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/>, <paramref name="policies"/>, or <paramref name="name"/> is null, or if <paramref name="policies"/> contains a null entry.</exception>
    public static bool TryGetState(
        this CircuitBreakerService service,
        IEnumerable<CircuitBreakerPolicy> policies,
        string name,
        out CircuitBreakerPolicy.CircuitState state)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(policies);
        ArgumentNullException.ThrowIfNull(name);

        foreach (var policy in policies)
        {
            ArgumentNullException.ThrowIfNull(policy, nameof(policies));

            if (string.Equals(policy.Name, name, StringComparison.Ordinal))
            {
                state = GetState(service, policy);
                return true;
            }
        }

        state = default;
        return false;
    }

    /// <summary>
    /// Manually resets every given circuit breaker policy to the closed state.
    /// </summary>
    /// <param name="service">The circuit breaker service that performs the resets.</param>
    /// <param name="policies">The circuit breaker policies to reset.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="service"/> or <paramref name="policies"/> is null, or if <paramref name="policies"/> contains a null entry.</exception>
    public static void ResetAll(this CircuitBreakerService service, IEnumerable<CircuitBreakerPolicy> policies)
    {
        ArgumentNullException.ThrowIfNull(service);
        ArgumentNullException.ThrowIfNull(policies);

        foreach (var policy in policies)
        {
            ArgumentNullException.ThrowIfNull(policy, nameof(policies));
            service.ResetCircuit(policy);
        }
    }

    /// <summary>
    /// Reads the state of a policy through <see cref="CircuitBreakerService.GetCircuitState"/>.
    /// </summary>
    private static CircuitBreakerPolicy.CircuitState GetState(CircuitBreakerService service, CircuitBreakerPolicy? policy)
    {
        ArgumentNullException.ThrowIfNull(policy);

        return Enum.Parse<CircuitBreakerPolicy.CircuitState>(service.GetCircuitState(policy));
    }
}
