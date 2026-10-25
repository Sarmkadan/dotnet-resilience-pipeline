#nullable enable

// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;

namespace DotNetResiliencePipeline.Cli;

/// <summary>
/// Extension methods for <see cref="CommandOptions"/> that read flags and named arguments.
/// </summary>
public static class CommandOptionsExtensions
{
    /// <summary>
    /// Determines whether the switch with the specified name was present on the command line.
    /// Matches either the long form <c>--name</c> or the short form <c>-n</c> (first character of <paramref name="name"/>).
    /// </summary>
    /// <param name="options">The command options to inspect.</param>
    /// <param name="name">The switch name without leading dashes (for example, <c>reset</c>).</param>
    /// <returns>true if the switch is present; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="name"/> is null.</exception>
    /// <exception cref="ArgumentException">Thrown when <paramref name="name"/> is empty.</exception>
    public static bool HasFlag(this CommandOptions options, string name)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(name);
        if (name.Length == 0)
            throw new ArgumentException("Flag name cannot be empty.", nameof(name));

        return options.Flags.Contains($"--{name}") || options.Flags.Contains($"-{name[0]}");
    }

    /// <summary>
    /// Gets the value of a named argument, or a default value when the argument is absent.
    /// </summary>
    /// <param name="options">The command options to inspect.</param>
    /// <param name="key">The argument name (matched case-insensitively).</param>
    /// <param name="defaultValue">The value returned when the argument is not present.</param>
    /// <returns>The argument value, or <paramref name="defaultValue"/> if the argument is not present.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="key"/> is null.</exception>
    public static string? GetArgumentOrDefault(this CommandOptions options, string key, string? defaultValue = null)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(key);

        return options.Arguments.TryGetValue(key, out var value) ? value : defaultValue;
    }

    /// <summary>
    /// Attempts to read a named argument as a 32-bit signed integer.
    /// </summary>
    /// <param name="options">The command options to inspect.</param>
    /// <param name="key">The argument name (matched case-insensitively).</param>
    /// <param name="value">When this method returns, contains the parsed value if successful; otherwise, 0.</param>
    /// <returns>true if the argument is present and parses as an integer; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="key"/> is null.</exception>
    public static bool TryGetInt(this CommandOptions options, string key, out int value)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(key);

        if (options.Arguments.TryGetValue(key, out var raw))
            return int.TryParse(raw, out value);

        value = 0;
        return false;
    }

    /// <summary>
    /// Attempts to read a named argument as a <see cref="TimeSpan"/>.
    /// </summary>
    /// <param name="options">The command options to inspect.</param>
    /// <param name="key">The argument name (matched case-insensitively).</param>
    /// <param name="value">When this method returns, contains the parsed value if successful; otherwise, <see cref="TimeSpan.Zero"/>.</param>
    /// <returns>true if the argument is present and parses as a time span; otherwise, false.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> or <paramref name="key"/> is null.</exception>
    public static bool TryGetTimeSpan(this CommandOptions options, string key, out TimeSpan value)
    {
        ArgumentNullException.ThrowIfNull(options);
        ArgumentNullException.ThrowIfNull(key);

        if (options.Arguments.TryGetValue(key, out var raw))
            return TimeSpan.TryParse(raw, out value);

        value = TimeSpan.Zero;
        return false;
    }

    /// <summary>
    /// Gets the names of all named arguments present on the command line.
    /// </summary>
    /// <param name="options">The command options to inspect.</param>
    /// <returns>A read-only list of argument names, in the dictionary's enumeration order; empty if there are none.</returns>
    /// <exception cref="ArgumentNullException">Thrown when <paramref name="options"/> is null.</exception>
    public static IReadOnlyList<string> GetArgumentNames(this CommandOptions options)
    {
        ArgumentNullException.ThrowIfNull(options);

        return options.Arguments.Keys.ToList();
    }
}
