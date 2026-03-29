using Declar.Core;
using Declar.Core.Host;

namespace Declar.Cli.Commands;

public sealed class VendorCommand : ICommand
{
    private const string FlathubRemoteUrl = "https://dl.flathub.org/repo/flathub.flatpakrepo";

    public string Name => "vendor";

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
            return await EnsureVendorStateAsync(context, declarationName: Name, desiredInstalled: true);
        }
    }

    private sealed class RemovedDeclaration : IDeclaration
    {
        public string Name => "removed";

        public async Task<int> ExecuteAsync(CommandContext context)
        {
            return await EnsureVendorStateAsync(context, declarationName: Name, desiredInstalled: false);
        }
    }

    private static async Task<int> EnsureVendorStateAsync(CommandContext context, string declarationName, bool desiredInstalled)
    {
        foreach (var vendorName in context.Inputs)
        {
            context.Reporter.Report(StatementStatus.Working, context.Command, declarationName, vendorName);

            if (!string.Equals(vendorName, "flathub", StringComparison.OrdinalIgnoreCase))
            {
                context.Reporter.Report(
                    StatementStatus.Error,
                    context.Command,
                    declarationName,
                    vendorName,
                    "Unsupported vendor. Currently supported: flathub.");
                return 1;
            }

            var result = desiredInstalled
                ? await EnsureFlathubInstalledAsync(context)
                : await EnsureFlathubRemovedAsync(context);

            if (result.ExitCode != 0)
            {
                context.Reporter.Report(
                    StatementStatus.Error,
                    context.Command,
                    declarationName,
                    vendorName,
                    result.ErrorMessage);
                return result.ExitCode;
            }

            context.Reporter.Report(
                result.HasChanged ? StatementStatus.Changed : StatementStatus.Ok,
                context.Command,
                declarationName,
                vendorName);
        }

        return 0;
    }

    private static async Task<VendorOperationResult> EnsureFlathubInstalledAsync(CommandContext context)
    {
        context.Shell.DescribeAction("flatpak install tree: detect host -> ensure flatpak -> ensure flathub remote");

        var host = HostQuery.GetCurrent();
        context.Shell.DescribeAction($"detected host: os={host.OperatingSystem}, distro={host.Distro}, id={host.DistroId ?? "(none)"}");

        var changed = false;
        var (flatpakInstalled, flatpakProbeExitCode) = await IsFlatpakInstalledAsync(context);
        if (flatpakProbeExitCode != 0)
        {
            return VendorOperationResult.Error(flatpakProbeExitCode, "Failed to query whether flatpak is installed.");
        }

        if (!flatpakInstalled)
        {
            var installPlan = BuildFlatpakInstallPlan(host);
            if (installPlan is null)
            {
                return VendorOperationResult.Error(
                    1,
                    $"Installing flathub is not implemented for host distro '{host.Distro}'.");
            }

            foreach (var command in installPlan.Commands)
            {
                context.Shell.DescribeAction($"installing flatpak via: {string.Join(" ", command)}");
                var installResult = await context.Shell.RunAsync(command[0], [.. command.Skip(1)]);
                if (installResult.ExitCode != 0)
                {
                    return VendorOperationResult.Error(
                        installResult.ExitCode,
                        PackageVendorCommandSupport.ExtractShellError(installResult, "Failed to install flatpak."));
                }
            }

            changed = true;
        }

        var (hasFlathubRemote, remoteProbeExitCode) = await HasFlathubRemoteAsync(context);
        if (remoteProbeExitCode != 0)
        {
            return VendorOperationResult.Error(remoteProbeExitCode, "Failed to query configured flatpak remotes.");
        }

        if (!hasFlathubRemote)
        {
            context.Shell.DescribeAction("adding user-scoped flathub remote (non-interactive)");
            var addRemoteResult = await context.Shell.RunAsync(
                "flatpak",
                "remote-add",
                "--user",
                "--if-not-exists",
                "flathub",
                FlathubRemoteUrl);
            if (addRemoteResult.ExitCode != 0)
            {
                return VendorOperationResult.Error(
                    addRemoteResult.ExitCode,
                    PackageVendorCommandSupport.ExtractShellError(addRemoteResult, "Failed to add flathub remote."));
            }

            changed = true;
        }

        return VendorOperationResult.Success(changed);
    }

    private static async Task<VendorOperationResult> EnsureFlathubRemovedAsync(CommandContext context)
    {
        context.Shell.DescribeAction("flathub remove tree: check flatpak -> remove flathub remote if present");

        var (flatpakInstalled, flatpakProbeExitCode) = await IsFlatpakInstalledAsync(context);
        if (flatpakProbeExitCode != 0)
        {
            return VendorOperationResult.Error(flatpakProbeExitCode, "Failed to query whether flatpak is installed.");
        }

        if (!flatpakInstalled)
        {
            return VendorOperationResult.Success(false);
        }

        var (hasFlathubRemote, remoteProbeExitCode) = await HasFlathubRemoteAsync(context);
        if (remoteProbeExitCode != 0)
        {
            return VendorOperationResult.Error(remoteProbeExitCode, "Failed to query configured flatpak remotes.");
        }

        if (!hasFlathubRemote)
        {
            return VendorOperationResult.Success(false);
        }

        context.Shell.DescribeAction("removing user-scoped flathub remote");
        var removeRemoteResult = await context.Shell.RunAsync("flatpak", "remote-delete", "--user", "flathub");
        if (removeRemoteResult.ExitCode != 0)
        {
            return VendorOperationResult.Error(
                removeRemoteResult.ExitCode,
                PackageVendorCommandSupport.ExtractShellError(removeRemoteResult, "Failed to remove flathub remote."));
        }

        return VendorOperationResult.Success(true);
    }

    private static FlatpakInstallPlan? BuildFlatpakInstallPlan(HostInfo host)
    {
        // Decision tree for flatpak installation:
        // - Linux/Ubuntu -> apt-get install flatpak
        // - Linux/Debian -> apt-get install flatpak
        // - Linux/Arch or CachyOs -> pacman -S flatpak
        // - Linux/Fedora -> dnf install flatpak
        // - Other hosts -> unsupported for now
        return host.OperatingSystem switch
        {
            HostOperatingSystem.Linux when host.Distro is HostDistro.Ubuntu or HostDistro.Debian =>
                new FlatpakInstallPlan([["sudo", "apt-get", "install", "-y", "flatpak"]]),

            HostOperatingSystem.Linux when host.Distro is HostDistro.Arch or HostDistro.CachyOs =>
                new FlatpakInstallPlan([["sudo", "pacman", "-S", "--noconfirm", "flatpak"]]),

            HostOperatingSystem.Linux when host.Distro is HostDistro.Fedora =>
                new FlatpakInstallPlan([["sudo", "dnf", "install", "-y", "flatpak"]]),

            _ => null,
        };
    }

    private static async Task<(bool IsInstalled, int ExitCode)> IsFlatpakInstalledAsync(CommandContext context)
    {
        var probeResult = await context.Shell.RunProbeAsync("flatpak", "--version");
        return probeResult.ExitCode switch
        {
            0 => (true, 0),
            127 => (false, 0),
            _ => (false, probeResult.ExitCode),
        };
    }

    private static async Task<(bool Exists, int ExitCode)> HasFlathubRemoteAsync(CommandContext context)
    {
        var probeResult = await context.Shell.RunProbeAsync("flatpak", "remote-list", "--user", "--columns=name");
        if (probeResult.ExitCode != 0)
        {
            return (false, probeResult.ExitCode);
        }

        var lines = probeResult.StdOut
            .Split('\n', StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries);
        var exists = lines.Any(line => string.Equals(line, "flathub", StringComparison.OrdinalIgnoreCase));
        return (exists, 0);
    }

    private sealed record FlatpakInstallPlan(IReadOnlyList<string[]> Commands);

    private sealed record VendorOperationResult(bool HasChanged, int ExitCode, string? ErrorMessage)
    {
        public static VendorOperationResult Success(bool hasChanged)
        {
            return new VendorOperationResult(hasChanged, 0, null);
        }

        public static VendorOperationResult Error(int exitCode, string errorMessage)
        {
            return new VendorOperationResult(false, exitCode, errorMessage);
        }
    }
}