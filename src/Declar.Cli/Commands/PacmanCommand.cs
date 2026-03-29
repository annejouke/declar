using Declar.Core;

namespace Declar.Cli.Commands;

public sealed class PacmanCommand : ICommand
{
    public string Name => "pacman";

    public IReadOnlyList<IDeclaration> Declarations { get; } =
    [
        new InstalledDeclaration(),
        new RemovedDeclaration(),
    ];

    private sealed class InstalledDeclaration : IDeclaration
    {
        public string Name => "installed";

        public async Task<int> ExecuteAsync(CommandContext context)
        {
            return await EnsurePackagesInStateAsync(
                context,
                declarationName: Name,
                desiredInstalled: true,
                changeArgsFactory: package => ["sudo", "pacman", "-S", "--noconfirm", package]);
        }
    }

    private sealed class RemovedDeclaration : IDeclaration
    {
        public string Name => "removed";

        public async Task<int> ExecuteAsync(CommandContext context)
        {
            return await EnsurePackagesInStateAsync(
                context,
                declarationName: Name,
                desiredInstalled: false,
                changeArgsFactory: package => ["sudo", "pacman", "-R", "--noconfirm", package]);
        }
    }

    private static async Task<int> EnsurePackagesInStateAsync(
        CommandContext context,
        string declarationName,
        bool desiredInstalled,
        Func<string, string[]> changeArgsFactory)
    {
        foreach (var package in context.Inputs)
        {
            context.Reporter.Report(StatementStatus.Working, context.Command, declarationName, package);

            var (Value, ExitCode) = await IsPackageInstalledAsync(context, package);
            if (ExitCode != 0)
            {
                context.Reporter.Report(
                    StatementStatus.Error,
                    context.Command,
                    declarationName,
                    package,
                    "Failed to query current package state.");
                return ExitCode;
            }

            if (context.Options.Test)
            {
                if (Value == desiredInstalled)
                {
                    context.Reporter.Report(StatementStatus.Ok, context.Command, declarationName, package);
                    continue;
                }

                if (desiredInstalled)
                {
                    var (ExistsInRepository, ExistsProbeExitCode) = await DoesPackageExistInRepositoryAsync(context, package);
                    if (ExistsProbeExitCode != 0)
                    {
                        context.Reporter.Report(
                            StatementStatus.Error,
                            context.Command,
                            declarationName,
                            package,
                            "Failed to verify package availability in repositories.");
                        return ExistsProbeExitCode;
                    }

                    if (!ExistsInRepository)
                    {
                        context.Reporter.Report(
                            StatementStatus.Error,
                            context.Command,
                            declarationName,
                            package,
                            "No package with this name was found in configured pacman repositories.");
                        return 1;
                    }

                    context.Reporter.Report(
                        StatementStatus.Error,
                        context.Command,
                        declarationName,
                        package,
                        "Package exists but is not installed in current system state.");
                    return 1;
                }

                context.Reporter.Report(
                    StatementStatus.Error,
                    context.Command,
                    declarationName,
                    package,
                    "Package is still installed in current system state.");
                return 1;
            }

            if (Value == desiredInstalled)
            {
                context.Reporter.Report(StatementStatus.Ok, context.Command, declarationName, package);
                continue;
            }

            var changeArgs = changeArgsFactory(package);
            var changeResult = await context.Shell.RunAsync(changeArgs[0], [.. changeArgs.Skip(1)]);
            if (changeResult.ExitCode != 0)
            {
                context.Reporter.Report(
                    StatementStatus.Error,
                    context.Command,
                    declarationName,
                    package,
                    ExtractShellError(changeResult, "Command execution failed."));
                return changeResult.ExitCode;
            }

            context.Reporter.Report(StatementStatus.Changed, context.Command, declarationName, package);
        }

        return 0;
    }

    private static async Task<(bool Value, int ExitCode)> IsPackageInstalledAsync(CommandContext context, string package)
    {
        var queryResult = await context.Shell.RunProbeAsync("pacman", "-Q", package);
        return queryResult.ExitCode switch
        {
            0 => (true, 0),
            1 => (false, 0),
            _ => (false, queryResult.ExitCode),
        };
    }

    private static async Task<(bool ExistsInRepository, int ExitCode)> DoesPackageExistInRepositoryAsync(
        CommandContext context,
        string package)
    {
        var queryResult = await context.Shell.RunProbeAsync("pacman", "-Si", package);
        if (queryResult.ExitCode == 0)
        {
            return (true, 0);
        }

        var combinedText = $"{queryResult.StdErr}\n{queryResult.StdOut}".ToLowerInvariant();
        if (combinedText.Contains("not found", StringComparison.Ordinal)
            || combinedText.Contains("target not found", StringComparison.Ordinal)
            || combinedText.Contains("could not find", StringComparison.Ordinal))
        {
            return (false, 0);
        }

        return (false, queryResult.ExitCode);
    }

    private static string ExtractShellError(CommandResult result, string fallback)
    {
        var text = string.IsNullOrWhiteSpace(result.StdErr)
            ? result.StdOut
            : result.StdErr;
        var normalized = text.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}
