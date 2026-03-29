namespace Declar.Cli.Test;

public class WingetCommandTests
{
    [Test]
    public async Task WingetInstalled_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync(
            "winget",
            "installed",
            "Microsoft.PowerShell",
            "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] winget installed Microsoft.PowerShell"));
    }

    [Test]
    public async Task WingetRemoved_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync(
            "winget",
            "removed",
            "Microsoft.PowerShell",
            "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|ER)\\] winget removed Microsoft.PowerShell"));
    }

    [Test]
    public async Task WingetInstalled_WithUnknownFlag_ReturnsError()
    {
        var result = await CliProcessTestHelper.RunCliAsync(
            "winget",
            "installed",
            "Microsoft.PowerShell",
            "--test",
            "--unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[ER] winget installed Microsoft.PowerShell"));
        });
    }
}