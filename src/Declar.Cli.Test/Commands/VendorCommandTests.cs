namespace Declar.Cli.Test;

public class VendorCommandTests
{
    [Test]
    public async Task VendorInstalled_Flathub_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync("vendor", "installed", "flathub", "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|CH|ER)\\] vendor installed flathub"));
    }

    [Test]
    public async Task VendorRemoved_Flathub_WithTestFlag_PrintsStatusLine()
    {
        var result = await CliProcessTestHelper.RunCliAsync("vendor", "removed", "flathub", "--test");

        Assert.That(result.StdOut, Does.Match("\\[(OK|CH|ER)\\] vendor removed flathub"));
    }

    [Test]
    public async Task VendorInstalled_WithUnsupportedVendor_ReturnsError()
    {
        var result = await CliProcessTestHelper.RunCliAsync("vendor", "installed", "npm", "--test");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain("[ER] vendor installed npm"));
            Assert.That(result.StdOut, Does.Contain("Unsupported vendor. Currently supported: flathub."));
        });
    }
}