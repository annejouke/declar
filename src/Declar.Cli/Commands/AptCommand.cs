using Declar.Core;

namespace Declar.Cli.Commands;

public sealed class AptCommand : ICommand
{
    public string Name => "apt";

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
                changeArgsFactory: package => ["sudo", "apt-get", "install", "-y", package],
                missingRepositoryMessage: "No package with this name was found in configured apt repositories.",
                notInstalledInCurrentStateMessage: "Package exists but is not installed in current system state.",
                stillInstalledMessage: "Package is still installed in current system state.",
                preflight: preflightContext => PackageVendorCommandSupport.EnsureMetadataFreshAsync(
                    preflightContext,
                    cacheKey: "apt",
                    freshnessWindow: TimeSpan.FromMinutes(30),
                    fileName: "sudo",
                    "apt-get",
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
                changeArgsFactory: package => ["sudo", "apt-get", "remove", "-y", package],
                missingRepositoryMessage: "No package with this name was found in configured apt repositories.",
                notInstalledInCurrentStateMessage: "Package exists but is not installed in current system state.",
                stillInstalledMessage: "Package is still installed in current system state.");
        }
    }

    private static async Task<(bool Value, int ExitCode)> IsPackageInstalledAsync(CommandContext context, string package)
    {
        var queryResult = await context.Shell.RunProbeAsync("dpkg", "-s", package);
        return PackageVendorCommandSupport.ProbeBooleanFromExitCode(queryResult);
    }

    private static async Task<(bool ExistsInRepository, int ExitCode)> DoesPackageExistInRepositoryAsync(
        CommandContext context,
        string package)
    {
        var queryResult = await context.Shell.RunProbeAsync("apt-cache", "show", package);
        if (queryResult.ExitCode == 0)
        {
            return (true, 0);
        }

        var combinedText = $"{queryResult.StdErr}\n{queryResult.StdOut}".ToLowerInvariant();
        if (combinedText.Contains("no packages found", StringComparison.Ordinal)
            || combinedText.Contains("unable to locate package", StringComparison.Ordinal)
            || combinedText.Contains("can't select", StringComparison.Ordinal))
        {
            return (false, 0);
        }

        return (false, queryResult.ExitCode);
    }
}