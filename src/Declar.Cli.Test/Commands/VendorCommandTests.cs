namespace Declar.Cli.Test;

public class VendorCommandTests
{
    private static readonly string[] Vendors =
    [
        "winget",
        "apt",
        "dnf",
        "flathub",
        "brew",
    ];

    [TestCaseSource(nameof(Vendors))]
    public async Task VendorInstalled_WithTestFlag_PrintsStatusLine(string vendor)
    {
        var package = GetLikelyPackageName(vendor);
        var result = await CliProcessTestHelper.RunCliAsync(vendor, "installed", package, "--test");

        Assert.That(result.StdOut, Does.Match($"\\[(OK|ER)\\] {vendor} installed {package}"));
    }

    [TestCaseSource(nameof(Vendors))]
    public async Task VendorRemoved_WithTestFlag_PrintsStatusLine(string vendor)
    {
        var package = GetLikelyPackageName(vendor);
        var result = await CliProcessTestHelper.RunCliAsync(vendor, "removed", package, "--test");

        Assert.That(result.StdOut, Does.Match($"\\[(OK|ER)\\] {vendor} removed {package}"));
    }

    [TestCaseSource(nameof(Vendors))]
    public async Task VendorInstalled_WithUnknownFlag_ReturnsError(string vendor)
    {
        var package = GetLikelyPackageName(vendor);
        var result = await CliProcessTestHelper.RunCliAsync(vendor, "installed", package, "--test", "--unknown");

        Assert.Multiple(() =>
        {
            Assert.That(result.ExitCode, Is.Not.EqualTo(0));
            Assert.That(result.StdOut, Does.Contain($"[ER] {vendor} installed {package}"));
        });
    }

    private static string GetLikelyPackageName(string vendor)
    {
        return vendor switch
        {
            "winget" => "Microsoft.PowerShell",
            "flathub" => "org.freedesktop.Platform",
            _ => "curl",
        };
    }
}