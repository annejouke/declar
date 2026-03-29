namespace Declar.Cli.Flags;

public static class CommandHandlerFlagExtensions
{
    public static CommandHandler AddBuiltInFlags(this CommandHandler handler)
    {
        return handler
            .AddFlag(new ReportFlag())
            .AddFlag(new TestFlag());
    }
}
