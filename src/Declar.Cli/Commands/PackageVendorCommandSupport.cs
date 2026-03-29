using Declar.Core;

namespace Declar.Cli.Commands;

internal static class PackageVendorCommandSupport
{
    public static async Task<int> EnsurePackagesInStateAsync(
        CommandContext context,
        string declarationName,
        bool desiredInstalled,
        Func<CommandContext, string, Task<(bool Value, int ExitCode)>> isInstalledProbe,
        Func<CommandContext, string, Task<(bool ExistsInRepository, int ExitCode)>> existsProbe,
        Func<string, string[]> changeArgsFactory,
        string missingRepositoryMessage,
        string notInstalledInCurrentStateMessage,
        string stillInstalledMessage)
    {
        foreach (var package in context.Inputs)
        {
            context.Shell.DescribeAction(
                $"evaluating desired state '{declarationName}' for package '{package}'");
            context.Reporter.Report(StatementStatus.Working, context.Command, declarationName, package);

            context.Shell.DescribeAction($"checking installed state for '{package}'");
            var (isInstalled, isInstalledExitCode) = await isInstalledProbe(context, package);
            if (isInstalledExitCode != 0)
            {
                context.Reporter.Report(
                    StatementStatus.Error,
                    context.Command,
                    declarationName,
                    package,
                    "Failed to query current package state.");
                return isInstalledExitCode;
            }

            if (context.Options.Test)
            {
                context.Shell.DescribeAction(
                    $"--test mode active; validating expected state without mutating '{package}'");
                if (isInstalled == desiredInstalled)
                {
                    context.Reporter.Report(StatementStatus.Ok, context.Command, declarationName, package);
                    continue;
                }

                if (desiredInstalled)
                {
                    context.Shell.DescribeAction($"checking repository availability for '{package}'");
                    var (existsInRepository, existsProbeExitCode) = await existsProbe(context, package);
                    if (existsProbeExitCode != 0)
                    {
                        context.Reporter.Report(
                            StatementStatus.Error,
                            context.Command,
                            declarationName,
                            package,
                            "Failed to verify package availability in repositories.");
                        return existsProbeExitCode;
                    }

                    if (!existsInRepository)
                    {
                        context.Reporter.Report(
                            StatementStatus.Error,
                            context.Command,
                            declarationName,
                            package,
                            missingRepositoryMessage);
                        return 1;
                    }

                    context.Reporter.Report(
                        StatementStatus.Error,
                        context.Command,
                        declarationName,
                        package,
                        notInstalledInCurrentStateMessage);
                    return 1;
                }

                context.Reporter.Report(
                    StatementStatus.Error,
                    context.Command,
                    declarationName,
                    package,
                    stillInstalledMessage);
                return 1;
            }

            if (isInstalled == desiredInstalled)
            {
                context.Reporter.Report(StatementStatus.Ok, context.Command, declarationName, package);
                continue;
            }

            var changeArgs = changeArgsFactory(package);
            context.Shell.DescribeAction($"applying state change for '{package}'");
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

    public static (bool Value, int ExitCode) ProbeBooleanFromExitCode(CommandResult probeResult)
    {
        return probeResult.ExitCode switch
        {
            0 => (true, 0),
            1 => (false, 0),
            _ => (false, probeResult.ExitCode),
        };
    }

    public static string ExtractShellError(CommandResult result, string fallback)
    {
        var text = string.IsNullOrWhiteSpace(result.StdErr)
            ? result.StdOut
            : result.StdErr;
        var normalized = text.Trim();
        return string.IsNullOrWhiteSpace(normalized) ? fallback : normalized;
    }
}