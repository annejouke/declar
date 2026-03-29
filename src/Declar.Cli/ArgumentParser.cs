using Declar.Core;

namespace Declar.Cli;

public sealed record ParsedCliArguments(
    bool IsValid,
    string? ErrorMessage,
    string? Command,
    string? Declaration,
    IReadOnlyList<string>? Inputs,
    IReadOnlyList<FlagToken>? Flags);

public static class ArgumentParser
{
    public static ParsedCliArguments Parse(string[] args)
    {
        var normalizedArgs = NormalizeArgs(args);
        var tokenizationResult = Tokenizer.Tokenize(normalizedArgs);
        if (!tokenizationResult.IsValid)
        {
            return new ParsedCliArguments(false, tokenizationResult.ErrorMessage, null, null, null, null);
        }

        var tokens = tokenizationResult.Tokens!;
        return new ParsedCliArguments(true, null, tokens.Command, tokens.Declaration, tokens.Inputs, tokens.Flags);
    }

    private static string[] NormalizeArgs(string[] args)
    {
        if (args.Length > 0 && string.Equals(args[0], "--", StringComparison.Ordinal))
        {
            return [.. args.Skip(1)];
        }

        return args;
    }
}
