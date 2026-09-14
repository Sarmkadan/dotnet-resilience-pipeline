#nullable enable
// =============================================================================
// Author: Vladyslav Zaiets | https://sarmkadan.com
// CTO & Software Architect
// =============================================================================

using System;
using System.Collections.Generic;
using System.Text;

namespace DotNetResiliencePipeline.Cli
{
    /// <summary>
    /// Extension methods for <see cref="CommandParser"/>.
    /// </summary>
    public static class CommandParserExtensions
    {
        /// <summary>
        /// Attempts to parse the specified command-line arguments into <see cref="CommandOptions"/>.
        /// </summary>
        /// <param name="parser">The <see cref="CommandParser"/> instance (not used).</param>
        /// <param name="args">The command-line arguments to parse.</param>
        /// <param name="options">When this method returns, contains the parsed <see cref="CommandOptions"/> if parsing succeeded; otherwise, null.</param>
        /// <returns>true if <paramref name="args"/> was successfully parsed; otherwise, false.</returns>
        public static bool TryParse(this CommandParser parser, string[] args, out CommandOptions? options)
        {
            if (args == null)
            {
                options = null;
                return false;
            }

            try
            {
                options = new CommandParser(args).Parse();
                return true;
            }
            catch
            {
                options = null;
                return false;
            }
        }

        /// <summary>
        /// Parses a command line string into <see cref="CommandOptions"/>.
        /// </summary>
        /// <param name="parser">The <see cref="CommandParser"/> instance (not used).</param>
        /// <param name="commandLine">The command line string to parse.</param>
        /// <returns>A <see cref="CommandOptions"/> instance populated with the parsed arguments.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="commandLine"/> is null.</exception>
        public static CommandOptions ParseCommandLine(this CommandParser parser, string commandLine)
        {
            if (commandLine == null)
                throw new ArgumentNullException(nameof(commandLine));

            var args = SplitCommandLine(commandLine);
            return new CommandParser(args).Parse();
        }

        /// <summary>
        /// Splits a command line string into arguments, respecting double-quoted sections.
        /// </summary>
        /// <param name="commandLine">The command line string to split.</param>
        /// <returns>An array of argument strings.</returns>
        private static string[] SplitCommandLine(string commandLine)
        {
            var args = new List<string>();
            var current = new StringBuilder();
            bool inQuote = false;

            for (int i = 0; i < commandLine.Length; i++)
            {
                char c = commandLine[i];
                if (c == '"')
                {
                    inQuote = !inQuote;
                }
                else if (char.IsWhiteSpace(c) && !inQuote)
                {
                    if (current.Length > 0)
                    {
                        args.Add(current.ToString());
                        current.Clear();
                    }
                }
                else
                {
                    current.Append(c);
                }
            }

            if (current.Length > 0)
            {
                args.Add(current.ToString());
            }

            return args.ToArray();
        }
    }
}