using Declar.Core;

namespace Declar.Cli.Commands;

public sealed class ParuCommand : ICommand
{
    public string Name => "paru";

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
                changeArgsFactory: package => ["paru", "-S", "--noconfirm", package],
                missingRepositoryMessage: "No package with this name was found in configured paru repositories.",
                notInstalledInCurrentStateMessage: "Package exists but is not installed in current system state.",
                stillInstalledMessage: "Package is still installed in current system state.",
                preflight: preflightContext => PackageVendorCommandSupport.EnsureMetadataFreshAsync(
                    preflightContext,
                    cacheKey: "paru",
                    freshnessWindow: TimeSpan.FromMinutes(30),
                    fileName: "paru",
                    "-Sy",
                    "--noconfirm"));
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
                changeArgsFactory: package => ["paru", "-R", "--noconfirm", package],
                missingRepositoryMessage: "No package with this name was found in configured paru repositories.",
                notInstalledInCurrentStateMessage: "Package exists but is not installed in current system state.",
                stillInstalledMessage: "Package is still installed in current system state.");
        }
    }

    private static async Task<(bool Value, int ExitCode)> IsPackageInstalledAsync(CommandContext context, string package)
    {
        var queryResult = await context.Shell.RunProbeAsync("pacman", "-Q", package);
        return PackageVendorCommandSupport.ProbeBooleanFromExitCode(queryResult);
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
}
