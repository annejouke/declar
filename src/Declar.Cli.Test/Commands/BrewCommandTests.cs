namespace Declar.Cli.Test;

public class BrewCommandTests
{
    [Test]
    public async Task BrewInstalled_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync("brew", "installed", "curl", "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] brew installed curl"));
    }

    [Test]
    public async Task BrewRemoved_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync("brew", "removed", "curl", "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] brew removed curl"));
    }

    [Test]
    public async Task BrewInstalled_WithUnknownFlag_ReturnsError()
    {
        var result = await CliProcessTestHelper.RunCliAsync("brew", "installed", "curl", "--test", "--unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[ER] brew installed curl"));
        });
    }
}