using Declar.Core.Files;

namespace Declar.Core.Test;

public class IniTests
{
    [Test]
    public async Task KeyExistsAsync_WithExistingKeys_ReturnsTrue()
    {
        var filePath = CreateTemporaryIniFile(
            "name = declar",
            "[network]",
            "host = localhost");

        var ini = new Ini(filePath);

        var hasName = await ini.KeyExistsAsync("name");
        var hasHostInNetwork = await ini.KeyExistsAsync("host", "network");
        var hasHostInMissing = await ini.KeyExistsAsync("host", "missing");

        Assert.Multiple(() =>
        {
            Assert.That(hasName, Is.True);
            Assert.That(hasHostInNetwork, Is.True);
            Assert.That(hasHostInMissing, Is.False);
        });
    }

    [Test]
    public async Task GetValueAsync_IgnoresCommentedLinesAndReturnsValue()
    {
        var filePath = CreateTemporaryIniFile(
            "; token = ignored",
            "token = abc123",
            "[server]",
            "port=8080",
            "# port = 9090");

        var ini = new Ini(filePath);

        var token = await ini.GetValueAsync("token");
        var serverPort = await ini.GetValueAsync("port", "server");
        var missing = await ini.GetValueAsync("missing");

        Assert.Multiple(() =>
        {
            Assert.That(token, Is.EqualTo("abc123"));
            Assert.That(serverPort, Is.EqualTo("8080"));
            Assert.That(missing, Is.Null);
        });
    }

    [Test]
    public async Task CommentLineAsync_WithMatchingLine_CommentsLine()
    {
        var filePath = CreateTemporaryIniFile("enabled = true");
        var ini = new Ini(filePath);

        var firstResult = await ini.CommentLineAsync("enabled = true");
        var secondResult = await ini.CommentLineAsync("enabled = true");

        var content = await File.ReadAllTextAsync(filePath);
        Assert.Multiple(() =>
        {
            Assert.That(firstResult, Is.True);
            Assert.That(secondResult, Is.False);
            Assert.That(content, Does.Contain("; enabled = true"));
        });
    }

    [Test]
    public async Task UncommentLineAsync_WithCommentedLine_UncommentsLine()
    {
        var filePath = CreateTemporaryIniFile("  ;   enabled = true");
        var ini = new Ini(filePath);

        var firstResult = await ini.UncommentLineAsync("enabled = true");
        var secondResult = await ini.UncommentLineAsync("enabled = true");

        var content = await File.ReadAllTextAsync(filePath);
        Assert.Multiple(() =>
        {
            Assert.That(firstResult, Is.True);
            Assert.That(secondResult, Is.False);
            Assert.That(content, Does.Contain("  enabled = true"));
        });
    }

    private static string CreateTemporaryIniFile(params string[] lines)
    {
        var filePath = Path.Combine(Path.GetTempPath(), $"declar-ini-test-{Guid.NewGuid():N}.ini");
        File.WriteAllLines(filePath, lines);
        return filePath;
    }
}