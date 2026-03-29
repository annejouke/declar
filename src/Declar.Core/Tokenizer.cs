namespace Declar.Core;

public sealed record FlagToken(string Name, IReadOnlyList<string> Values);

public sealed record CommandTokens(
    string Command,
    string Declaration,
    IReadOnlyList<string> Inputs,
    IReadOnlyList<FlagToken> Flags);

public sealed record TokenizationResult(bool IsValid, string? ErrorMessage, CommandTokens? Tokens);

public static class Tokenizer
{
    public static TokenizationResult Tokenize(string[] args)
    {
        if (args.Length < 3)
        {
            return Invalid("Expected at least 3 non-flagged items: <command> <declaration> <input...>.");
        }

        if (IsFlag(args[0]) || IsFlag(args[1]))
        {
            return Invalid("The first two items must be non-flagged command language tokens.");
        }

        var command = args[0];
        var declaration = args[1];
        var inputs = new List<string>();
        var flags = new List<FlagToken>();
        var index = 2;

        while (index < args.Length && !IsFlag(args[index]))
        {
            inputs.Add(args[index]);
            index++;
        }

        if (inputs.Count == 0)
        {
            return Invalid("At least one input is required after <command> and <declaration>.");
        }

        while (index < args.Length)
        {
            var current = args[index];
            if (!IsFlag(current))
            {
                return Invalid("Flags must come after all command inputs.");
            }

            if (IsMixedShortFlag(current))
            {
                var expandedFlags = ExpandMixedShortFlags(current);
                for (var expandedIndex = 0; expandedIndex < expandedFlags.Count - 1; expandedIndex++)
                {
                    flags.Add(new FlagToken(expandedFlags[expandedIndex], Array.Empty<string>()));
                }

                var trailingValues = new List<string>();
                index++;

                while (index < args.Length && !IsFlag(args[index]))
                {
                    trailingValues.Add(args[index]);
                    index++;
                }

                flags.Add(new FlagToken(expandedFlags[^1], trailingValues));
                continue;
            }

            var values = new List<string>();
            index++;

            while (index < args.Length && !IsFlag(args[index]))
            {
                values.Add(args[index]);
                index++;
            }

            flags.Add(new FlagToken(current, values));
        }

        return new TokenizationResult(true, null, new CommandTokens(command, declaration, inputs, flags));
    }

    private static TokenizationResult Invalid(string message)
    {
        return new TokenizationResult(false, message, null);
    }

    private static bool IsFlag(string value)
    {
        return value.StartsWith("-", StringComparison.Ordinal) && value.Length > 1;
    }

    private static bool IsMixedShortFlag(string value)
    {
        return value.StartsWith("-", StringComparison.Ordinal)
            && !value.StartsWith("--", StringComparison.Ordinal)
            && value.Length > 2;
    }

    private static List<string> ExpandMixedShortFlags(string value)
    {
        return [.. value[1..].Select(character => $"-{character}")];
    }
}
