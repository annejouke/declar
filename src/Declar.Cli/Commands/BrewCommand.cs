using Declar.Core;

namespace Declar.Cli.Commands;

public sealed class BrewCommand : ICommand
{
    public string Name => "brew";

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
                changeArgsFactory: package => ["brew", "install", package],
                missingRepositoryMessage: "No package with this name was found in configured brew repositories.",
                notInstalledInCurrentStateMessage: "Package exists but is not installed in current system state.",
                stillInstalledMessage: "Package is still installed in current system state.");
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
                changeArgsFactory: package => ["brew", "uninstall", package],
                missingRepositoryMessage: "No package with this name was found in configured brew repositories.",
                notInstalledInCurrentStateMessage: "Package exists but is not installed in current system state.",
                stillInstalledMessage: "Package is still installed in current system state.");
        }
    }

    private static async Task<(bool Value, int ExitCode)> IsPackageInstalledAsync(CommandContext context, string package)
    {
        var formulaResult = await context.Shell.RunProbeAsync("brew", "list", "--formula", package);
        if (formulaResult.ExitCode == 0)
        {
            return (true, 0);
        }

        var caskResult = await context.Shell.RunProbeAsync("brew", "list", "--cask", package);
        if (caskResult.ExitCode == 0)
        {
            return (true, 0);
        }

        if (formulaResult.ExitCode == 1 && caskResult.ExitCode == 1)
        {
            return (false, 0);
        }

        return (false, formulaResult.ExitCode != 0 ? formulaResult.ExitCode : caskResult.ExitCode);
    }

    private static async Task<(bool ExistsInRepository, int ExitCode)> DoesPackageExistInRepositoryAsync(
        CommandContext context,
        string package)
    {
        var queryResult = await context.Shell.RunProbeAsync("brew", "info", package);
        if (queryResult.ExitCode == 0)
        {
            return (true, 0);
        }

        var combinedText = $"{queryResult.StdErr}\n{queryResult.StdOut}".ToLowerInvariant();
        if (combinedText.Contains("no available formula", StringComparison.Ordinal)
            || combinedText.Contains("no formulae or casks found", StringComparison.Ordinal)
            || combinedText.Contains("did you mean", StringComparison.Ordinal))
        {
            return (false, 0);
        }

        return (false, queryResult.ExitCode);
    }
}