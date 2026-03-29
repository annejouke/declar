namespace Declar.Cli.Test;

public class AptCommandTests
{
    [Test]
    public async Task AptInstalled_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync("apt", "installed", "curl", "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] apt installed curl"));
    }

    [Test]
    public async Task AptRemoved_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync("apt", "removed", "curl", "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] apt removed curl"));
    }

    [Test]
    public async Task AptInstalled_WithUnknownFlag_ReturnsError()
    {
        var result = await CliProcessTestHelper.RunCliAsync("apt", "installed", "curl", "--test", "--unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[ER] apt installed curl"));
        });
    }
}