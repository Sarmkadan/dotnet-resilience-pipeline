#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System.Collections.Generic;
using System.Threading.Tasks;

namespace DotNetResiliencePipeline.Cli;

/// <summary>
/// Extension methods for <see cref="CliCommandHandler"/> and <see cref="CommandExecutionResult"/>.
/// </summary>
public static class CliCommandHandlerExtensions
{
    /// <summary>
    /// Determines whether the command execution was successful.
    /// </summary>
    /// <param name="result">The command execution result.</param>
    /// <returns><see langword="true"/> if the command was successful; otherwise, <see langword="false"/>.</returns>
    public static bool IsSuccess(this CommandExecutionResult result)
    {
        if (result is null)
            throw new System.ArgumentNullException(nameof(result));

        return result.Success;
    }

    /// <summary>
    /// Determines whether the command execution produced any output.
    /// </summary>
    /// <param name="result">The command execution result.</param>
    /// <returns><see langword="true"/> if the result contains a non-empty, non-whitespace message; otherwise, <see langword="false"/>.</returns>
    public static bool HasOutput(this CommandExecutionResult result)
    {
        if (result is null)
            throw new System.ArgumentNullException(nameof(result));

        return !string.IsNullOrWhiteSpace(result.Message);
    }

    /// <summary>
    /// Gets the exit code from the command execution result.
    /// </summary>
    /// <param name="result">The command execution result.</param>
    /// <returns>The exit code.</returns>
    public static int ToExitCode(this CommandExecutionResult result)
    {
        if (result is null)
            throw new System.ArgumentNullException(nameof(result));

        return result.ExitCode;
    }

    /// <summary>
    /// Executes multiple commands sequentially.
    /// </summary>
    /// <param name="handler">The CLI command handler.</param>
    /// <param name="optionsList">The collection of command options to execute.</param>
    /// <returns>A task that represents the asynchronous operation. The task result contains a list of command execution results.</returns>
    public static async Task<IList<CommandExecutionResult>> ExecuteManyAsync(this CliCommandHandler handler, IEnumerable<CommandOptions> optionsList)
    {
        if (handler is null)
            throw new System.ArgumentNullException(nameof(handler));
        if (optionsList is null)
            throw new System.ArgumentNullException(nameof(optionsList));

        var results = new List<CommandExecutionResult>();

        foreach (var options in optionsList)
        {
            if (options is null)
                throw new System.ArgumentException("Command options cannot be null.", nameof(optionsList));

            var result = await handler.ExecuteAsync(options);
            results.Add(result);
        }

        return results;
    }
}