namespace Declar.Cli.Commands;

public static class CommandHandlerCommandExtensions
{
    public static CommandHandler AddBuiltInCommands(this CommandHandler handler)
    {
        return handler
            .AddCommand(new PacmanCommand())
            .AddCommand(new HostsCommand());
    }
}
