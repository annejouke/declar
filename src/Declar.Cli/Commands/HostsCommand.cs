using System.Runtime.InteropServices;
using Declar.Core;

namespace Declar.Cli.Commands;

public sealed class HostsCommand : ICommand
{
    public string Name => "hosts";

    public IReadOnlyList<IDeclaration> Declarations { get; } =
    [
        new CommentedDeclaration(),
        new UncommentedDeclaration(),
        new RemovedDeclaration(),
    ];

    private static string HostsPath => GetHostsFilePath();

    private sealed class CommentedDeclaration : IDeclaration
    {
        public string Name => "commented";

        public async Task<int> ExecuteAsync(CommandContext context)
        {
            if (context.Options.Test)
            {
                foreach (var lineToEnsure in context.Inputs)
                {
                    context.Reporter.Report(StatementStatus.Working, context.Command, Name, lineToEnsure);
                    context.Reporter.Report(StatementStatus.Ok, context.Command, Name, lineToEnsure);
                }

                return 0;
            }

            return await UpdateHostsEntriesAsync(
                context,
                Name,
                context.Inputs,
                missingLineFactory: line => $"# {line}",
                transformMatchedLine: line => IsCommented(line) ? line : $"# {NormalizeHostsLine(line)}",
                alreadyInDesiredState: IsCommented);
        }
    }

    private sealed class UncommentedDeclaration : IDeclaration
    {
        public string Name => "uncommented";

        public async Task<int> ExecuteAsync(CommandContext context)
        {
            if (context.Options.Test)
            {
                foreach (var lineToEnsure in context.Inputs)
                {
                    context.Reporter.Report(StatementStatus.Working, context.Command, Name, lineToEnsure);
                    context.Reporter.Report(StatementStatus.Ok, context.Command, Name, lineToEnsure);
                }

                return 0;
            }

            return await UpdateHostsEntriesAsync(
                context,
                Name,
                context.Inputs,
                missingLineFactory: line => line,
                transformMatchedLine: line => line,
                alreadyInDesiredState: line => !IsCommented(line));
        }
    }

    private sealed class RemovedDeclaration : IDeclaration
    {
        public string Name => "removed";

        public async Task<int> ExecuteAsync(CommandContext context)
        {
            if (context.Options.Test)
            {
                foreach (var lineToEnsure in context.Inputs)
                {
                    context.Reporter.Report(StatementStatus.Working, context.Command, Name, lineToEnsure);
                    context.Reporter.Report(StatementStatus.Ok, context.Command, Name, lineToEnsure);
                }

                return 0;
            }

            if (!File.Exists(HostsPath))
            {
                foreach (var lineToEnsure in context.Inputs)
                {
                    context.Reporter.Report(
                        StatementStatus.Error,
                        context.Command,
                        Name,
                        lineToEnsure,
                        $"Hosts file not found: {HostsPath}");
                }

                return 1;
            }

            var lines = (await File.ReadAllLinesAsync(HostsPath)).ToList();

            foreach (var lineToRemove in context.Inputs)
            {
                context.Reporter.Report(StatementStatus.Working, context.Command, Name, lineToRemove);

                var normalizedTarget = NormalizeHostsLine(lineToRemove);
                var removedCount = lines.RemoveAll(line =>
                    string.Equals(NormalizeHostsLine(line), normalizedTarget, StringComparison.Ordinal));

                if (removedCount == 0)
                {
                    context.Reporter.Report(StatementStatus.Ok, context.Command, Name, lineToRemove);
                    continue;
                }

                context.Reporter.Report(StatementStatus.Changed, context.Command, Name, lineToRemove);
            }

            await File.WriteAllLinesAsync(HostsPath, lines);

            return 0;
        }
    }

    private static async Task<int> UpdateHostsEntriesAsync(
        CommandContext context,
        string declaration,
        IReadOnlyList<string> inputs,
        Func<string, string> missingLineFactory,
        Func<string, string> transformMatchedLine,
        Func<string, bool> alreadyInDesiredState)
    {
        if (!File.Exists(HostsPath))
        {
            foreach (var input in inputs)
            {
                context.Reporter.Report(
                    StatementStatus.Error,
                    context.Command,
                    declaration,
                    input,
                    $"Hosts file not found: {HostsPath}");
            }

            return 1;
        }

        var lines = await File.ReadAllLinesAsync(HostsPath);
        var anyChanged = false;

        foreach (var input in inputs)
        {
            context.Reporter.Report(StatementStatus.Working, context.Command, declaration, input);

            var normalizedTarget = NormalizeHostsLine(input);
            var matched = false;
            var changed = false;

            for (var i = 0; i < lines.Length; i++)
            {
                var currentNormalized = NormalizeHostsLine(lines[i]);
                if (!string.Equals(currentNormalized, normalizedTarget, StringComparison.Ordinal))
                {
                    continue;
                }

                matched = true;
                if (alreadyInDesiredState(lines[i]))
                {
                    break;
                }

                lines[i] = transformMatchedLine(input);
                anyChanged = true;
                changed = true;
                break;
            }

            if (!matched)
            {
                lines = [.. lines, missingLineFactory(input)];
                anyChanged = true;
                changed = true;
            }

            context.Reporter.Report(
                changed ? StatementStatus.Changed : StatementStatus.Ok,
                context.Command,
                declaration,
                input);
        }

        if (!anyChanged)
        {
            return 0;
        }

        await File.WriteAllLinesAsync(HostsPath, lines);
        return 0;
    }

    private static string GetHostsFilePath()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var systemRoot = Environment.GetEnvironmentVariable("SystemRoot") ?? "C:\\Windows";
            return Path.Combine(systemRoot, "System32", "drivers", "etc", "hosts");
        }

        return "/etc/hosts";
    }

    private static bool IsCommented(string line)
    {
        return line.TrimStart().StartsWith("#", StringComparison.Ordinal);
    }

    private static string NormalizeHostsLine(string line)
    {
        var normalized = line.Trim();
        if (normalized.StartsWith("#", StringComparison.Ordinal))
        {
            normalized = normalized[1..].Trim();
        }

        return normalized;
    }
}
