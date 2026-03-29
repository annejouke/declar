using Declar.Core;

namespace Declar.Cli;

public sealed class TerminalReporter : ITerminalReporter
{
    private readonly object sync = new();
    private string? pendingStatementKey;

    public void Report(StatementStatus status, string command, string declaration, string state, string? reason = null)
    {
        var (label, color) = status switch
        {
            StatementStatus.Working => ("..", ConsoleColor.DarkGray),
            StatementStatus.Ok => ("OK", ConsoleColor.Green),
            StatementStatus.Changed => ("CH", ConsoleColor.Blue),
            StatementStatus.Error => ("ER", ConsoleColor.Red),
            _ => ("ER", ConsoleColor.Red),
        };

        var suffix = status == StatementStatus.Error && !string.IsNullOrWhiteSpace(reason)
            ? $" | {reason}"
            : string.Empty;
        var line = $"[{label}] {command} {declaration} {state}{suffix}";
        var statementKey = $"{command}|{declaration}|{state}";

        lock (sync)
        {
            if (status == StatementStatus.Working)
            {
                pendingStatementKey = statementKey;

                if (!Console.IsOutputRedirected)
                {
                    WriteColored(line, color, appendNewLine: false);
                }

                return;
            }

            var shouldReplacePendingLine =
                !Console.IsOutputRedirected
                && string.Equals(pendingStatementKey, statementKey, StringComparison.Ordinal);

            if (shouldReplacePendingLine)
            {
                Console.Write("\r");
                Console.Write(new string(' ', Math.Max(1, Console.BufferWidth - 1)));
                Console.Write("\r");
                WriteColored(line, color, appendNewLine: true);
                pendingStatementKey = null;
                return;
            }

            WriteColored(line, color, appendNewLine: true);
            pendingStatementKey = null;
        }
    }

    private static void WriteColored(string line, ConsoleColor color, bool appendNewLine)
    {
        var previousColor = Console.ForegroundColor;
        Console.ForegroundColor = color;
        if (appendNewLine)
        {
            Console.WriteLine(line);
        }
        else
        {
            Console.Write(line);
        }

        Console.ForegroundColor = previousColor;
    }
}
