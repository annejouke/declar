using Declar.Core;

namespace Declar.Cli.Commands;

public sealed class WingetCommand : ICommand
{
    public string Name => "winget";

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
                changeArgsFactory: package =>
                [
                    "winget",
                    "install",
                    "--exact",
                    "--id",
                    package,
                    "--accept-package-agreements",
                    "--accept-source-agreements",
                ],
                missingRepositoryMessage: "No package with this id was found in configured winget repositories.",
                notInstalledInCurrentStateMessage: "Package exists but is not installed in current system state.",
                stillInstalledMessage: "Package is still installed in current system state.",
                preflight: preflightContext => PackageVendorCommandSupport.EnsureMetadataFreshAsync(
                    preflightContext,
                    cacheKey: "winget",
                    freshnessWindow: TimeSpan.FromMinutes(30),
                    fileName: "winget",
                    "source",
                    "update"));
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
                changeArgsFactory: package =>
                [
                    "winget",
                    "uninstall",
                    "--exact",
                    "--id",
                    package,
                    "--accept-source-agreements",
                ],
                missingRepositoryMessage: "No package with this id was found in configured winget repositories.",
                notInstalledInCurrentStateMessage: "Package exists but is not installed in current system state.",
                stillInstalledMessage: "Package is still installed in current system state.");
        }
    }

    private static async Task<(bool Value, int ExitCode)> IsPackageInstalledAsync(CommandContext context, string package)
    {
        var queryResult = await context.Shell.RunProbeAsync(
            "winget",
            "list",
            "--exact",
            "--id",
            package,
            "--accept-source-agreements");

        var combinedText = $"{queryResult.StdErr}\n{queryResult.StdOut}".ToLowerInvariant();
        if (combinedText.Contains("no installed package found", StringComparison.Ordinal)
            || combinedText.Contains("no package found matching", StringComparison.Ordinal))
        {
            return (false, 0);
        }

        return queryResult.ExitCode == 0
            ? (true, 0)
            : (false, queryResult.ExitCode);
    }

    private static async Task<(bool ExistsInRepository, int ExitCode)> DoesPackageExistInRepositoryAsync(
        CommandContext context,
        string package)
    {
        var queryResult = await context.Shell.RunProbeAsync(
            "winget",
            "search",
            "--exact",
            "--id",
            package,
            "--accept-source-agreements");

        var combinedText = $"{queryResult.StdErr}\n{queryResult.StdOut}".ToLowerInvariant();
        if (combinedText.Contains("no package found", StringComparison.Ordinal)
            || combinedText.Contains("no package found matching", StringComparison.Ordinal))
        {
            return (false, 0);
        }

        return queryResult.ExitCode == 0
            ? (true, 0)
            : (false, queryResult.ExitCode);
    }
}