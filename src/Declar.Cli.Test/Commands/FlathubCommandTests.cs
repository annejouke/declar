namespace Declar.Cli.Test;

public class FlathubCommandTests
{
    [Test]
    public async Task FlathubInstalled_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync(
            "flathub",
            "installed",
            "org.freedesktop.Platform",
            "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] flathub installed org.freedesktop.Platform"));
    }

    [Test]
    public async Task FlathubRemoved_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync(
            "flathub",
            "removed",
            "org.freedesktop.Platform",
            "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] flathub removed org.freedesktop.Platform"));
    }

    [Test]
    public async Task FlathubInstalled_WithUnknownFlag_ReturnsError()
    {
        var result = await CliProcessTestHelper.RunCliAsync(
            "flathub",
            "installed",
            "org.freedesktop.Platform",
            "--test",
            "--unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[ER] flathub installed org.freedesktop.Platform"));
        });
    }
}