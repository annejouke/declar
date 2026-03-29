namespace Declar.Cli.Test;

public class HostsCommandTests
{
    [Test]
    public async Task HostsUncommented_WithGroupedShortFlags_PrintsDryRunEditLines()
    {
        var result = await CliProcessTestHelper.RunCliAsync("hosts", "uncommented", "127.0.0.1 localhost", "-rt");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[OK] hosts uncommented 127.0.0.1 localhost"));
        });
    }

    [Test]
    public async Task HostsCommented_WithTestFlag_PrintsDryRunEditLines()
    {
        var result = await CliProcessTestHelper.RunCliAsync("hosts", "commented", "127.0.0.1 localhost", "--test");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[OK] hosts commented 127.0.0.1 localhost"));
        });
    }

    [Test]
    public async Task HostsRemoved_WithTestFlag_PrintsDryRunEditLines()
    {
        var result = await CliProcessTestHelper.RunCliAsync("hosts", "removed", "127.0.0.1 localhost", "--test");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[OK] hosts removed 127.0.0.1 localhost"));
        });
    }
}
