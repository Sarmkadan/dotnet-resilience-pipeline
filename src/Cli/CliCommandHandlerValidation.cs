#nullable enable

using System;
using System.Collections.Generic;

namespace DotNetResiliencePipeline.Cli;

/// <summary>
/// Provides validation helpers for <see cref="CliCommandHandler"/> instances.
/// </summary>
public static class CliCommandHandlerValidation
{
    /// <summary>
    /// Validates the specified <see cref="CliCommandHandler"/> instance.
    /// </summary>
    /// <param name="value">The handler instance to validate.</param>
    /// <returns>A list of validation problems; empty if the handler is valid.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    public static IReadOnlyList<string> Validate(this CliCommandHandler? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var problems = new List<string>();

        // CliCommandHandler is a service class with injected dependencies
        // All validation is done at construction time, so there's nothing to check here
        // The handler itself is always valid if it's not null

        return problems;
    }

    /// <summary>
    /// Determines whether the specified <see cref="CliCommandHandler"/> instance is valid.
    /// </summary>
    /// <param name="value">The handler instance to check.</param>
    /// <returns><see langword="true"/> if the handler is valid; otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this CliCommandHandler? value)
    {
        try
        {
            _ = value.Validate();
            return true;
        }
        catch (ArgumentNullException)
        {
            return false;
        }
    }

    /// <summary>
    /// Ensures that the specified <see cref="CliCommandHandler"/> instance is valid.
    /// </summary>
    /// <param name="value">The handler instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown if the handler is not valid, containing a list of validation problems.</exception>
    public static void EnsureValid(this CliCommandHandler? value)
    {
        ArgumentNullException.ThrowIfNull(value);

        // CliCommandHandler is a service class with injected dependencies
        // All validation is done at construction time via constructor parameter validation
        // There are no runtime state issues to check for this handler
    }
}
