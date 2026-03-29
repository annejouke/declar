namespace Declar.Core.Host;

public sealed record HostInfo(
    HostOperatingSystem OperatingSystem,
    HostDistro Distro,
    string? DistroId = null,
    string? DistroName = null);