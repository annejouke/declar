using Declar.Cli.Commands;
using Declar.Core;

namespace Declar.Cli.Test;

public class ParuCommandTests
{
    [Test]
    public async Task ParuInstalled_WithTestFlag_PrintsPlannedCommands()
    {
        var result = await CliProcessTestHelper.RunCliAsync("paru", "installed", "dotnet-sdk", "--test");

        Assert.Multiple(() =>
        {
            Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] paru installed dotnet-sdk"));
        });
    }

    [Test]
    public async Task ParuRemoved_WithTestFlag_PrintsPlannedCommands()
    {
        var result = await CliProcessTestHelper.RunCliAsync("paru", "removed", "dotnet-sdk", "--test");

        Assert.Multiple(() =>
        {
            Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] paru removed dotnet-sdk"));
        });
    }

    [Test]
    public async Task ParuInstalled_WithMissingPackageInTestMode_ReturnsErrorStatus()
    {
        var result = await CliProcessTestHelper.RunCliAsync(
            "paru",
            "installed",
            "declar-this-package-should-not-exist",
            "--test");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[ER] paru installed declar-this-package-should-not-exist"));
            Assert.That(result.StdOut, Does.Contain("| No package with this name was found in configured paru repositories."));
        });
    }

    [Test]
    public async Task ParuInstalled_WithUnknownFlag_ReturnsError()
    {
        var result = await CliProcessTestHelper.RunCliAsync("paru", "installed", "dotnet-sdk", "--test", "--unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[ER] paru installed dotnet-sdk"));
        });
    }
}
