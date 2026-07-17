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
    /// <returns>An empty list; <see cref="CliCommandHandler"/> is always valid if not <see langword="null"/>.</returns>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static IReadOnlyList<string> Validate(this CliCommandHandler? value)
    {
        ArgumentNullException.ThrowIfNull(value);
        return Array.Empty<string>();
    }

    /// <summary>
    /// Determines whether the specified <see cref="CliCommandHandler"/> instance is valid.
    /// </summary>
    /// <param name="value">The handler instance to check.</param>
    /// <returns><see langword="true"/> if the handler is valid (not <see langword="null"/>); otherwise, <see langword="false"/>.</returns>
    public static bool IsValid(this CliCommandHandler? value) => value is not null;

    /// <summary>
    /// Ensures that the specified <see cref="CliCommandHandler"/> instance is valid.
    /// </summary>
    /// <param name="value">The handler instance to validate.</param>
    /// <exception cref="ArgumentNullException">Thrown if <paramref name="value"/> is <see langword="null"/>.</exception>
    public static void EnsureValid(this CliCommandHandler? value)
    {
        ArgumentNullException.ThrowIfNull(value);
    }
}
