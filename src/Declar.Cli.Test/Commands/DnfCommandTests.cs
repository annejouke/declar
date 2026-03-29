namespace Declar.Cli.Test;

public class DnfCommandTests
{
    [Test]
    public async Task DnfInstalled_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync("dnf", "installed", "curl", "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] dnf installed curl"));
    }

    [Test]
    public async Task DnfRemoved_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync("dnf", "removed", "curl", "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] dnf removed curl"));
    }

    [Test]
    public async Task DnfInstalled_WithUnknownFlag_ReturnsError()
    {
        var result = await CliProcessTestHelper.RunCliAsync("dnf", "installed", "curl", "--test", "--unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[ER] dnf installed curl"));
        });
    }
}