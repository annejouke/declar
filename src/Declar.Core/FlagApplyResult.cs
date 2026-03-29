namespace Declar.Core;

public sealed record FlagApplyResult(bool IsValid, string? ErrorMessage, ExecutionOptions Options)
{
    public static FlagApplyResult Valid(ExecutionOptions options)
    {
        return new FlagApplyResult(true, null, options);
    }

    public static FlagApplyResult Invalid(string errorMessage, ExecutionOptions options)
    {
        return new FlagApplyResult(false, errorMessage, options);
    }
}
