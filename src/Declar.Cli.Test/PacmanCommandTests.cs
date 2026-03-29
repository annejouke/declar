namespace Declar.Cli.Test;

public class PacmanCommandTests
{
    [Test]
    public async Task PacmanInstalled_WithTestFlag_PrintsPlannedCommands()
    {
        var result = await CliProcessTestHelper.RunCliAsync("pacman", "installed", "dotnet-sdk", "--test");

        Assert.Multiple(() =>
        {
            Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] pacman installed dotnet-sdk"));
        });
    }

    [Test]
    public async Task PacmanRemoved_WithTestFlag_PrintsPlannedCommands()
    {
        var result = await CliProcessTestHelper.RunCliAsync("pacman", "removed", "dotnet-sdk", "--test");

        Assert.Multiple(() =>
        {
            Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] pacman removed dotnet-sdk"));
        });
    }

    [Test]
    public async Task PacmanInstalled_WithMissingPackageInTestMode_ReturnsErrorStatus()
    {
        var result = await CliProcessTestHelper.RunCliAsync(
            "pacman",
            "installed",
            "declar-this-package-should-not-exist",
            "--test");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[ER] pacman installed declar-this-package-should-not-exist"));
            Assert.That(result.StdOut, Does.Contain("| No package with this name was found in configured pacman repositories."));
        });
    }

    [Test]
    public async Task PacmanInstalled_WithUnknownFlag_ReturnsError()
    {
        var result = await CliProcessTestHelper.RunCliAsync("pacman", "installed", "dotnet-sdk", "--test", "--unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[ER] pacman installed dotnet-sdk"));
        });
    }
}
