namespace Declar.Core;

public interface IFlag
{
    bool CanHandle(FlagToken flag);

    FlagApplyResult Apply(FlagToken flag, ExecutionOptions options);
}
