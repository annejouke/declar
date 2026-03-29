namespace Declar.Core.Test;

public class TokenizerTests
{
    [Test]
    public void Tokenize_WithSingleInputAndNoFlags_ReturnsCommandDeclarationAndInput()
    {
        var result = Tokenizer.Tokenize(["pacman", "installed", "dotnet-sdk"]);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.ErrorMessage, Is.Null);
        Assert.That(result.Tokens, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Tokens!.Command, Is.EqualTo("pacman"));
            Assert.That(result.Tokens.Declaration, Is.EqualTo("installed"));
            Assert.That(result.Tokens.Inputs, Is.EqualTo(new[] { "dotnet-sdk" }));
            Assert.That(result.Tokens.Flags, Is.Empty);
        });
    }

    [Test]
    public void Tokenize_WithMultipleInputsAndLongFlags_PreservesBothSections()
    {
        var result = Tokenizer.Tokenize([
            "pacman",
            "installed",
            "git",
            "curl",
            "--report",
            "--profile",
            "system",
            "user"
        ]);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Tokens, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Tokens!.Inputs, Is.EqualTo(new[] { "git", "curl" }));
            Assert.That(result.Tokens.Flags.Count, Is.EqualTo(2));
            Assert.That(result.Tokens.Flags[0].Name, Is.EqualTo("--report"));
            Assert.That(result.Tokens.Flags[0].Values, Is.Empty);
            Assert.That(result.Tokens.Flags[1].Name, Is.EqualTo("--profile"));
            Assert.That(result.Tokens.Flags[1].Values, Is.EqualTo(new[] { "system", "user" }));
        });
    }

    [Test]
    public void Tokenize_WithGroupedShortFlags_ExpandsEachFlagAndAssignsValuesToLastOne()
    {
        var result = Tokenizer.Tokenize([
            "hosts",
            "uncommented",
            "127.0.0.1 localhost",
            "-crt",
            "preview"
        ]);

        Assert.That(result.IsValid, Is.True);
        Assert.That(result.Tokens, Is.Not.Null);
        Assert.Multiple(() =>
        {
            Assert.That(result.Tokens!.Flags.Count, Is.EqualTo(3));
            Assert.That(result.Tokens.Flags[0].Name, Is.EqualTo("-c"));
            Assert.That(result.Tokens.Flags[0].Values, Is.Empty);
            Assert.That(result.Tokens.Flags[1].Name, Is.EqualTo("-r"));
            Assert.That(result.Tokens.Flags[1].Values, Is.Empty);
            Assert.That(result.Tokens.Flags[2].Name, Is.EqualTo("-t"));
            Assert.That(result.Tokens.Flags[2].Values, Is.EqualTo(new[] { "preview" }));
        });
    }

    [Test]
    public void Tokenize_WithTooFewArguments_ReturnsInvalidResult()
    {
        var result = Tokenizer.Tokenize(["pacman", "installed"]);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Tokens, Is.Null);
        Assert.That(result.ErrorMessage, Is.EqualTo("Expected at least 3 non-flagged items: <command> <declaration> <input...>."));
    }

    [Test]
    public void Tokenize_WithFlagInCommandPosition_ReturnsInvalidResult()
    {
        var result = Tokenizer.Tokenize(["--report", "installed", "dotnet-sdk"]);

        Assert.That(result.IsValid, Is.False);
        Assert.That(result.Tokens, Is.Null);
        Assert.That(result.ErrorMessage, Is.EqualTo("The first two items must be non-flagged command language tokens."));
    }
}
