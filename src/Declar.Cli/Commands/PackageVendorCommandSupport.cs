using Declar.Core;

namespace Declar.Cli.Commands;

internal static class PackageVendorCommandSupport
{
    private static readonly TimeSpan RefreshLockTimeout = TimeSpan.FromSeconds(30);

    public static async Task<int> EnsurePackagesInStateAsync(
        CommandContext context,
        string declarationName,
        bool desiredInstalled,
        Func<CommandContext, string, Task<(bool Value, int ExitCode)>> isInstalledProbe,
        Func<CommandContext, string, Task<(bool ExistsInRepository, int ExitCode)>> existsProbe,
        Func<string, string[]> changeArgsFactory,
        string missingRepositoryMessage,
        string notInstalledInCurrentStateMessage,
        string stillInstalledMessage,
        Func<CommandContext, Task<PreflightResult>>? preflight = null)
    {
        if (preflight is not null)
        {
            context.Shell.DescribeAction($"running metadata refresh preflight for '{context.Command}'");
            var preflightResult = await preflight(context);
            if (!preflightResult.IsSuccess)
            {
                context.Reporter.Report(
                    StatementStatus.Error,
                    context.Command,
                    declarationName,
                    context.Inputs[0],
                    preflightResult.ErrorMessage ?? "Failed to refresh package metadata.");
                return preflightResult.ExitCode;
            }
        }

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

    public static async Task<PreflightResult> EnsureMetadataFreshAsync(
        CommandContext context,
        string cacheKey,
        TimeSpan freshnessWindow,
        string fileName,
        params string[] args)
    {
        if (context.Options.Test)
        {
            context.Shell.DescribeAction(
                $"preflight: would refresh '{cacheKey}' metadata via '{FormatCommand(fileName, args)}'; skipped because --test is enabled");
            return PreflightResult.Success();
        }

        var rootDirectory = Path.Combine(Path.GetTempPath(), "declar-refresh");
        var normalizedKey = NormalizeFileNameSegment(cacheKey);
        var stampFile = Path.Combine(rootDirectory, $"{normalizedKey}.stamp");
        var lockFile = Path.Combine(rootDirectory, $"{normalizedKey}.lock");

        if (TryGetFreshStampAge(stampFile, freshnessWindow, out var preLockAge))
        {
            context.Shell.DescribeAction(
                $"preflight: '{cacheKey}' metadata age {DescribeAge(preLockAge)} is within {DescribeAge(freshnessWindow)}; skipping refresh");
            return PreflightResult.Success();
        }

        Directory.CreateDirectory(rootDirectory);
        var lockHandle = await TryAcquireFileLockAsync(lockFile, RefreshLockTimeout);
        if (lockHandle is null)
        {
            return PreflightResult.Error(1, $"Timed out waiting for '{cacheKey}' metadata preflight lock.");
        }

        await using (lockHandle.ConfigureAwait(false))
        {
            if (TryGetFreshStampAge(stampFile, freshnessWindow, out var postLockAge))
            {
                context.Shell.DescribeAction(
                    $"preflight: '{cacheKey}' metadata became fresh while waiting (age {DescribeAge(postLockAge)}); skipping refresh");
                return PreflightResult.Success();
            }

            context.Shell.DescribeAction(
                $"preflight: refreshing '{cacheKey}' metadata via '{FormatCommand(fileName, args)}'");
            var refreshResult = await context.Shell.RunAsync(fileName, args);
            if (refreshResult.ExitCode != 0)
            {
                return PreflightResult.Error(
                    refreshResult.ExitCode,
                    ExtractShellError(refreshResult, "Metadata refresh command failed."));
            }

            File.WriteAllText(stampFile, DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString());
            context.Shell.DescribeAction($"preflight: refreshed '{cacheKey}' metadata");
            return PreflightResult.Success();
        }
    }

    private static bool TryGetFreshStampAge(string stampFile, TimeSpan freshnessWindow, out TimeSpan age)
    {
        age = TimeSpan.MaxValue;
        if (!File.Exists(stampFile))
        {
            return false;
        }

        var stampText = File.ReadAllText(stampFile).Trim();
        if (!long.TryParse(stampText, out var unixSeconds))
        {
            return false;
        }

        var stampedAt = DateTimeOffset.FromUnixTimeSeconds(unixSeconds);
        age = DateTimeOffset.UtcNow - stampedAt;
        return age >= TimeSpan.Zero && age <= freshnessWindow;
    }

    private static async Task<FileStream?> TryAcquireFileLockAsync(string lockFile, TimeSpan timeout)
    {
        var startedAt = DateTimeOffset.UtcNow;
        while (DateTimeOffset.UtcNow - startedAt < timeout)
        {
            try
            {
                return new FileStream(lockFile, FileMode.OpenOrCreate, FileAccess.ReadWrite, FileShare.None);
            }
            catch (IOException)
            {
                await Task.Delay(100);
            }
            catch (UnauthorizedAccessException)
            {
                await Task.Delay(100);
            }
        }

        return null;
    }

    private static string NormalizeFileNameSegment(string value)
    {
        var normalized = new char[value.Length];
        for (var index = 0; index < value.Length; index++)
        {
            var character = value[index];
            normalized[index] = char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-';
        }

        return new string(normalized);
    }

    private static string DescribeAge(TimeSpan age)
    {
        return age.TotalMinutes < 1
            ? $"{Math.Max(0, age.Seconds)}s"
            : $"{Math.Round(age.TotalMinutes, 1)}m";
    }

    private static string FormatCommand(string fileName, IEnumerable<string> args)
    {
        return string.Join(" ", [fileName, .. args]);
    }

    public sealed record PreflightResult(bool IsSuccess, int ExitCode, string? ErrorMessage)
    {
        public static PreflightResult Success()
        {
            return new PreflightResult(true, 0, null);
        }

        public static PreflightResult Error(int exitCode, string errorMessage)
        {
            return new PreflightResult(false, exitCode, errorMessage);
        }
    }
}