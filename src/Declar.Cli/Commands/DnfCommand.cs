using Declar.Core;

namespace Declar.Cli.Commands;

public sealed class DnfCommand : ICommand
{
    public string Name => "dnf";

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
            return await PackageVendorCommandSupport.EnsurePackagesInStateAsync(
                context,
                declarationName: Name,
                desiredInstalled: true,
                isInstalledProbe: IsPackageInstalledAsync,
                existsProbe: DoesPackageExistInRepositoryAsync,
                changeArgsFactory: package => ["sudo", "dnf", "install", "-y", package],
                missingRepositoryMessage: "No package with this name was found in configured dnf repositories.",
                notInstalledInCurrentStateMessage: "Package exists but is not installed in current system state.",
                stillInstalledMessage: "Package is still installed in current system state.",
                preflight: preflightContext => PackageVendorCommandSupport.EnsureMetadataFreshAsync(
                    preflightContext,
                    cacheKey: "dnf",
                    freshnessWindow: TimeSpan.FromMinutes(30),
                    fileName: "sudo",
                    "dnf",
                    "makecache",
                    "-y"));
        }
    }

    private sealed class RemovedDeclaration : IDeclaration
    {
        public string Name => "removed";

        public async Task<int> ExecuteAsync(CommandContext context)
        {
            return await PackageVendorCommandSupport.EnsurePackagesInStateAsync(
                context,
                declarationName: Name,
                desiredInstalled: false,
                isInstalledProbe: IsPackageInstalledAsync,
                existsProbe: DoesPackageExistInRepositoryAsync,
                changeArgsFactory: package => ["sudo", "dnf", "remove", "-y", package],
                missingRepositoryMessage: "No package with this name was found in configured dnf repositories.",
                notInstalledInCurrentStateMessage: "Package exists but is not installed in current system state.",
                stillInstalledMessage: "Package is still installed in current system state.");
        }
    }

    private static async Task<(bool Value, int ExitCode)> IsPackageInstalledAsync(CommandContext context, string package)
    {
        var queryResult = await context.Shell.RunProbeAsync("dnf", "list", "installed", package);
        return PackageVendorCommandSupport.ProbeBooleanFromExitCode(queryResult);
    }

    private static async Task<(bool ExistsInRepository, int ExitCode)> DoesPackageExistInRepositoryAsync(
        CommandContext context,
        string package)
    {
        var queryResult = await context.Shell.RunProbeAsync("dnf", "info", package);
        if (queryResult.ExitCode == 0)
        {
            return (true, 0);
        }

        var combinedText = $"{queryResult.StdErr}\n{queryResult.StdOut}".ToLowerInvariant();
        if (combinedText.Contains("no matching packages to list", StringComparison.Ordinal)
            || combinedText.Contains("no matching packages to show", StringComparison.Ordinal)
            || combinedText.Contains("no match for argument", StringComparison.Ordinal))
        {
            return (false, 0);
        }

        return (false, queryResult.ExitCode);
    }
}