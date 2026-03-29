using Declar.Core;

namespace Declar.Cli.Commands;

public sealed class SnapCommand : ICommand
{
    public string Name => "snap";

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
                changeArgsFactory: package => ["sudo", "snap", "install", package],
                missingRepositoryMessage: "No package with this name was found in configured snap repositories.",
                notInstalledInCurrentStateMessage: "Package exists but is not installed in current system state.",
                stillInstalledMessage: "Package is still installed in current system state.",
                preflight: preflightContext => PackageVendorCommandSupport.EnsureMetadataFreshAsync(
                    preflightContext,
                    cacheKey: "snap",
                    freshnessWindow: TimeSpan.FromMinutes(30),
                    fileName: "snap",
                    "refresh",
                    "--list"));
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
                changeArgsFactory: package => ["sudo", "snap", "remove", package],
                missingRepositoryMessage: "No package with this name was found in configured snap repositories.",
                notInstalledInCurrentStateMessage: "Package exists but is not installed in current system state.",
                stillInstalledMessage: "Package is still installed in current system state.");
        }
    }

    private static async Task<(bool Value, int ExitCode)> IsPackageInstalledAsync(CommandContext context, string package)
    {
        var queryResult = await context.Shell.RunProbeAsync("snap", "list", package);
        if (queryResult.ExitCode == 0)
        {
            return (true, 0);
        }

        var combinedText = $"{queryResult.StdErr}\n{queryResult.StdOut}".ToLowerInvariant();
        if (combinedText.Contains("no matching snaps installed", StringComparison.Ordinal)
            || combinedText.Contains("not found", StringComparison.Ordinal))
        {
            return (false, 0);
        }

        return (false, queryResult.ExitCode);
    }

    private static async Task<(bool ExistsInRepository, int ExitCode)> DoesPackageExistInRepositoryAsync(
        CommandContext context,
        string package)
    {
        var queryResult = await context.Shell.RunProbeAsync("snap", "info", package);
        if (queryResult.ExitCode == 0)
        {
            return (true, 0);
        }

        var combinedText = $"{queryResult.StdErr}\n{queryResult.StdOut}".ToLowerInvariant();
        if (combinedText.Contains("not found", StringComparison.Ordinal)
            || combinedText.Contains("no snap found", StringComparison.Ordinal)
            || combinedText.Contains("snap not found", StringComparison.Ordinal))
        {
            return (false, 0);
        }

        return (false, queryResult.ExitCode);
    }
}