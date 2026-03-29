using Declar.Core.Host;

namespace Declar.Core.Test;

public class HostQueryTests
{
    [Test]
    public void ParseLinuxOsRelease_WithUbuntuId_ReturnsUbuntuDistro()
    {
        var info = HostQuery.ParseLinuxOsRelease(
            "NAME=Ubuntu\nID=ubuntu\nPRETTY_NAME=\"Ubuntu 24.04 LTS\"");

        Assert.Multiple(() =>
        {
            Assert.That(info.OperatingSystem, Is.EqualTo(HostOperatingSystem.Linux));
            Assert.That(info.Distro, Is.EqualTo(HostDistro.Ubuntu));
            Assert.That(info.DistroId, Is.EqualTo("ubuntu"));
            Assert.That(info.DistroName, Is.EqualTo("Ubuntu 24.04 LTS"));
        });
    }

    [Test]
    public void ParseLinuxOsRelease_WithArchId_ReturnsArchDistro()
    {
        var info = HostQuery.ParseLinuxOsRelease(
            "NAME=Arch Linux\nID=arch\nPRETTY_NAME=\"Arch Linux\"");

        Assert.Multiple(() =>
        {
            Assert.That(info.OperatingSystem, Is.EqualTo(HostOperatingSystem.Linux));
            Assert.That(info.Distro, Is.EqualTo(HostDistro.Arch));
            Assert.That(info.DistroId, Is.EqualTo("arch"));
        });
    }

    [Test]
    public void ParseLinuxOsRelease_WithUnknownId_ReturnsLinuxOther()
    {
        var info = HostQuery.ParseLinuxOsRelease("NAME=MyDistro\nID=mydistro");

        Assert.Multiple(() =>
        {
            Assert.That(info.OperatingSystem, Is.EqualTo(HostOperatingSystem.Linux));
            Assert.That(info.Distro, Is.EqualTo(HostDistro.LinuxOther));
            Assert.That(info.DistroId, Is.EqualTo("mydistro"));
        });
    }
}