#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using CircuitState = DotNetResiliencePipeline.Domain.Policies.CircuitBreakerPolicy.CircuitState;

namespace DotNetResiliencePipeline.Domain.Policies;

/// <summary>
/// Provides display-name extensions for <see cref="CircuitState"/> values.
/// </summary>
public static class CircuitStateExtensions
{
    /// <summary>
    /// Converts a circuit state to its user-friendly display name.
    /// </summary>
    /// <param name="state">The circuit state to convert.</param>
    /// <returns>
    /// <c>Closed</c>, <c>Open</c>, or <c>Half-Open</c> for a defined circuit state;
    /// otherwise, the result of <see cref="object.ToString"/> for <paramref name="state"/>.
    /// </returns>
    public static string ToDisplayName(this CircuitState state)
    {
        return state switch
        {
            CircuitState.Closed => "Closed",
            CircuitState.Open => "Open",
            CircuitState.HalfOpen => "Half-Open",
            _ => state.ToString()
        };
    }
}
