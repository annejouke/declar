namespace Declar.Cli.Test;

public class SnapCommandTests
{
    [Test]
    public async Task SnapInstalled_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync("snap", "installed", "curl", "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] snap installed curl"));
    }

    [Test]
    public async Task SnapRemoved_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync("snap", "removed", "curl", "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] snap removed curl"));
    }

    [Test]
    public async Task SnapInstalled_WithUnknownFlag_ReturnsError()
    {
        var result = await CliProcessTestHelper.RunCliAsync("snap", "installed", "curl", "--test", "--unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[ER] snap installed curl"));
        });
    }

    [Test]
    public async Task SnapInstalled_WithReportFlag_PrintsPreflightTraceLines()
    {
        var result = await CliProcessTestHelper.RunCliAsync("snap", "installed", "curl", "--test", "--report");

        Assert.Multiple(() =>
        {
            Assert.That(result.StdOut, Does.Contain("     running metadata refresh preflight for 'snap'"));
            Assert.That(result.StdOut, Does.Contain("     preflight: would refresh 'snap' metadata via 'snap refresh --list'; skipped because --test is enabled"));
            Assert.That(result.StdOut, Does.Contain("     evaluating desired state 'installed' for package 'curl'"));
        });
    }
}