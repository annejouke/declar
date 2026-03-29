using System.Runtime.InteropServices;

namespace Declar.Core.Host;

public static class HostQuery
{
    public static HostInfo GetCurrent()
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            var version = Environment.OSVersion.Version;
            var distro = version.Build >= 22000 ? HostDistro.Windows11 : HostDistro.Windows;
            return new HostInfo(HostOperatingSystem.Windows, distro, "windows", version.ToString());
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            return new HostInfo(HostOperatingSystem.MacOs, HostDistro.MacOs, "macos", "macOS");
        }

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
        {
            return GetLinuxHostInfo();
        }

        return new HostInfo(HostOperatingSystem.Unknown, HostDistro.Unknown);
    }

    public static HostInfo ParseLinuxOsRelease(string content)
    {
        var values = ParseKeyValues(content);

        values.TryGetValue("ID", out var idRaw);
        values.TryGetValue("NAME", out var nameRaw);
        values.TryGetValue("PRETTY_NAME", out var prettyNameRaw);

        var id = (idRaw ?? string.Empty).Trim().ToLowerInvariant();
        var distro = id switch
        {
            "ubuntu" => HostDistro.Ubuntu,
            "arch" => HostDistro.Arch,
            "fedora" => HostDistro.Fedora,
            "debian" => HostDistro.Debian,
            "cachyos" => HostDistro.CachyOs,
            _ => HostDistro.LinuxOther,
        };

        var name = !string.IsNullOrWhiteSpace(prettyNameRaw) ? prettyNameRaw : nameRaw;
        var normalizedId = string.IsNullOrWhiteSpace(id) ? null : id;
        var normalizedName = string.IsNullOrWhiteSpace(name) ? null : name;

        return new HostInfo(HostOperatingSystem.Linux, distro, normalizedId, normalizedName);
    }

    private static HostInfo GetLinuxHostInfo()
    {
        const string osReleasePath = "/etc/os-release";
        if (!File.Exists(osReleasePath))
        {
            return new HostInfo(HostOperatingSystem.Linux, HostDistro.LinuxOther, null, "Linux");
        }

        var content = File.ReadAllText(osReleasePath);
        return ParseLinuxOsRelease(content);
    }

    private static Dictionary<string, string> ParseKeyValues(string content)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        var lines = content.Split('\n', StringSplitOptions.TrimEntries);

        foreach (var line in lines)
        {
            if (string.IsNullOrWhiteSpace(line) || line.StartsWith("#", StringComparison.Ordinal))
            {
                continue;
            }

            var separatorIndex = line.IndexOf('=');
            if (separatorIndex <= 0)
            {
                continue;
            }

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().Trim('"');
            if (key.Length == 0)
            {
                continue;
            }

            result[key] = value;
        }

        return result;
    }
}