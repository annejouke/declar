using Declar.Core;

namespace Declar.Cli.Flags;

public sealed class TestFlag : IFlag
{
    public bool CanHandle(FlagToken flag)
    {
        return string.Equals(flag.Name, "--test", StringComparison.Ordinal)
            || string.Equals(flag.Name, "-t", StringComparison.Ordinal);
    }

    public FlagApplyResult Apply(FlagToken flag, ExecutionOptions options)
    {
        if (flag.Values.Count != 0)
        {
            return FlagApplyResult.Invalid($"Flag '{flag.Name}' does not accept a value.", options);
        }

        return FlagApplyResult.Valid(options with { Test = true });
    }
}
